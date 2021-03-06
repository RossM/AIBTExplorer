﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AIBTViewer
{
    struct BTPath
    {
        public List<Behavior> Path;

        public override string ToString()
        {
            return string.Join("|", Path.Select(b => b.Key));
        }
    }

    public partial class BTViewer : Form
    {
        public BTViewer()
        {
            InitializeComponent();
        }

        class LayerInfo
        {
            public string Path;
            public bool Enabled;
        }

        private BehaviorTree BT;
        private List<LayerInfo> layers = new List<LayerInfo>();

        private HashSet<string> expandedPaths = new HashSet<string>();

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            foreach (string layerProp in Properties.Settings.Default.Layers)
            {
                var parts = layerProp.Split(new []{'|'}, 2);
                if (parts.Length == 2)
                    layers.Add(new LayerInfo { Enabled = (parts[0] == "1"), Path = parts[1] });
            }

            behaviorTreeView.BeforeExpand += behaviorTreeView_BeforeExpand;

            UpdateLayersTreeView();

            Task.Run(new System.Action(ParseConfig)).Wait();

            UpdateBehaviorTreeView();
        }

        private void UpdateBehaviorTreeView()
        {
            behaviorTreeView.BeginUpdate();
            behaviorTreeView.Nodes.Clear();

            var rootKeys = new HashSet<string>(BT.Roots().Select(r => r.Key));
            foreach (var key in PublicRoots)
                rootKeys.Add(key);

            foreach (var key in rootKeys.OrderByDescending(k => PublicRoots.Contains(k)).ThenBy(k => k))
            {
                Behavior root;
                if (BT.Tree.TryGetValue(key, out root))
                {
                    var btPath = new BTPath { Path = new List<Behavior>() };
                    var newNode = AddNode(btPath, root, behaviorTreeView.Nodes);
                    btPath.Path.Add(root);
                    Expand(newNode, btPath);

                    if (expandedPaths.Contains(newNode.Tag.ToString()))
                        newNode.Expand();
                }
            }
            behaviorTreeView.EndUpdate();

            if (configParser.Errors.Count > 0 || analyzer.Errors.Count > 0)
            {
                errorListBox.BeginUpdate();
                errorListBox.Items.Clear();

                foreach (var error in configParser.Errors)
                    errorListBox.Items.Add(error);
                foreach (var error in analyzer.Errors)
                    errorListBox.Items.Add(error);

                errorListBox.EndUpdate();

                errorListBox.Show();
                errorLabel.Show();
            }
            else
            {
                errorListBox.Hide();
                errorLabel.Hide();
            }

            overviewTooltipStatuslabel.Text = string.Format("{0} files, {1} nodes", layers.Count(l => l.Enabled),
                BT.Tree.Count);
            
            expandedPaths.Clear();
        }

        private string[] PublicRoots = new string[]
        {
            "genericairoot",
            "panickedroot",
            "genericscamperroot",
        };

        private ConfigParser configParser;
        private Analyzer analyzer;

        private void UpdateLayersTreeView()
        {
            layersTreeView.BeginUpdate();
            layersTreeView.Nodes.Clear();
            foreach (var layer in layers)
            {
                TreeNode node = new TreeNode(FileShortName(layer.Path));
                node.ToolTipText = layer.Path;
                node.Checked = layer.Enabled;
                layersTreeView.Nodes.Add(node);
            }
            layersTreeView.EndUpdate();
        }

        void behaviorTreeView_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            behaviorTreeView.BeginUpdate();
            foreach (var child in e.Node.Nodes.Cast<TreeNode>())
            {
                if (child.Nodes.Count == 0)
                {
                    var btPath = (BTPath) child.Tag;
                    // if (Analyzer.ShouldShow(btPath))
                    Expand(child, btPath);
                }
            }
            behaviorTreeView.EndUpdate();
        }

        private void Expand(TreeNode node, BTPath btPath)
        {
            var behavior = btPath.Path[btPath.Path.Count - 1];
            foreach (var child in behavior.TypeLink.OrderBy(n => n.BehaviorName))
            {
                AddNode(btPath, child, node.Nodes);
            }

            for (int i = 0; i < behavior.ChildLink.Count; i++)
            {
                var child = behavior.ChildLink[i];
                AddNode(btPath, child, node.Nodes, i < behavior.Param.Count ? behavior.Param[i] : null);
            }
        }

        private TreeNode AddNode(BTPath btPath, Behavior behavior, TreeNodeCollection treeNodeCollection, string param = null)
        {
            var font = behaviorTreeView.Font;

            var newBTPath = ExtendPath(btPath, behavior);
            behavior = newBTPath.Path[newBTPath.Path.Count - 1];

            var nodeLabel = NodeLabel(behavior, param);
            var newNode = treeNodeCollection.Add(behavior.Key, nodeLabel);
            newNode.Tag = newBTPath;

            if (behavior.Annotations.Contains("HasSelectAbility"))
                newNode.ForeColor = Color.Black;
            //else if (behavior.Annotations.Contains("HasUpdateBestTarget"))
            //    newNode.ForeColor = Color.DarkRed;
            else if (behavior.Annotations.Contains("HasAction"))
            {
                if (behavior.Annotations.Contains("ConditionValued"))
                    newNode.ForeColor = Color.DarkCyan;
                else
                    newNode.ForeColor = Color.DarkGreen;
            }
            else
                newNode.ForeColor = Color.Blue;

            if (!Analyzer.ShouldShow(newBTPath))
                font = new Font(font, FontStyle.Strikeout);

            newNode.NodeFont = font;

            return newNode;
        }

        private static BTPath ExtendPath(BTPath btPath, Behavior behavior)
        {
            var newBTPath = new BTPath { Path = new List<Behavior>() };
            if (btPath.Path != null)
                newBTPath.Path.AddRange(btPath.Path);
            newBTPath.Path.Add(behavior);

            while (true)
            {
                var newBehavior = Analyzer.HideNodes(btPath, behavior);
                if (newBehavior == null)
                    return newBTPath;

                behavior = newBehavior;
                newBTPath.Path.Add(behavior);
            }
        }

        private static string NodeLabel(Behavior child, string param = null)
        {
            var result = new StringBuilder();
            result.AppendFormat("{0} [{1}]", child.BehaviorName, child.NodeType);
            if (child.Parent.Count > 1)
                result.AppendFormat(" [{0}]", child.Parent.Count);
            if (param != null)
                result.AppendFormat(" {0}%", param);
            return result.ToString();
        }

        private void ParseConfig()
        {
            configParser = new ConfigParser();
            BT = configParser.ReadData(layers.Where(l => l.Enabled).Select(l => l.Path));

            analyzer = new Analyzer();
            analyzer.Analyze(BT);
        }

        private void behaviorTreeView_Layout(object sender, LayoutEventArgs e)
        {
            behaviorTreeView.Scrollable = true;
        }

        private void addLayerButton_Click(object sender, EventArgs e)
        {
            var dialog = new OpenFileDialog { FileName = "*.ini" };
            var result = dialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                layers.Add(new LayerInfo { Enabled = true, Path = dialog.FileName });

                LayerPathsChanged();
            }
        }

        private void behaviorTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            var path = (BTPath) e.Node.Tag;
            var behavior = path.Path[path.Path.Count - 1];

            if (behavior.FileName == null || behavior.RawText == null)
            {
                behaviorTextBox.Text = "";
                fileNameLabel.Text = "<Unknown>";
                return;
            }

            behaviorTextBox.Lines = behavior.RawText.Split('\n');
            fileNameLabel.Text = string.Format("{0} : {1}", FileShortName(behavior.FileName), behavior.OriginalLineNumber + 1);
        }

        private static string FileShortName(string fileName)
        {
            var filePathParts = fileName.Split('\\');
            var filePathIndex = Math.Max(filePathParts.Length - 2, 0);
            if (filePathIndex > 0 && filePathParts[filePathIndex].ToLowerInvariant() == "config")
                filePathIndex--;

            var shortPath = string.Join("\\", filePathParts.Take(filePathIndex + 1));
            var files = Directory.GetFiles(shortPath, "*.XComMod");
            if (files.Length > 0)
                return Path.GetFileNameWithoutExtension(files[0]);

            return filePathParts[filePathIndex];
        }

        private void refreshButton_Click(object sender, EventArgs e)
        {
            SaveExpandedNodes();
            
            Task.Run(new System.Action(ParseConfig)).Wait();

            UpdateBehaviorTreeView();
        }

        private void removeLayerButton_Click(object sender, EventArgs e)
        {
            var index = layersTreeView.SelectedNode.Index;

            if (index >= 0 && index < layers.Count)
            {
                layers.RemoveAt(index);

                LayerPathsChanged();
            }
        }

        private void LayerPathsChanged()
        {
            Properties.Settings.Default.Layers.Clear();
            foreach (var layer in layers)
            {
                Properties.Settings.Default.Layers.Add(string.Format("{0}|{1}", layer.Enabled ? 1 : 0, layer.Path));
            }
            Properties.Settings.Default.Save();

            UpdateLayersTreeView();

            SaveExpandedNodes();

            Task.Run(new System.Action(ParseConfig)).Wait();

            UpdateBehaviorTreeView();
        }

        private void SaveExpandedNodes()
        {
            Queue<TreeNodeCollection> worklist = new Queue<TreeNodeCollection>();
            expandedPaths.Clear();
            worklist.Enqueue(behaviorTreeView.Nodes);
            while (worklist.Count > 0)
            {
                var nodes = worklist.Dequeue();
                foreach (TreeNode node in nodes)
                {
                    if (node.IsExpanded)
                    {
                        expandedPaths.Add(((BTPath) node.Tag).ToString());
                        worklist.Enqueue(node.Nodes);
                    }
                }
            }
        }

        private void layersTreeView_AfterCheck(object sender, TreeViewEventArgs e)
        {
            if (layers[e.Node.Index].Enabled != e.Node.Checked)
            {
                layers[e.Node.Index].Enabled = e.Node.Checked;
                LayerPathsChanged();
            }
        }

        private void behaviorTreeView_AfterExpand(object sender, TreeViewEventArgs e)
        {
            foreach (var child in e.Node.Nodes.Cast<TreeNode>())
            {
                if (expandedPaths.Contains(child.Tag.ToString()))
                    child.Expand();
            }
        }
    }
}
