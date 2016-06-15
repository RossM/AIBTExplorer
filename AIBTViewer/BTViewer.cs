using System;
using System.Collections.Generic;
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
    public partial class BTViewer : Form
    {
        public BTViewer()
        {
            InitializeComponent();
        }

        private BehaviorTree BT;
        private List<string> layerPaths = new List<string>(); 

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            layerPaths.Add(@"C:\Program Files (x86)\Steam\SteamApps\common\XCOM 2\XComGame\Config\DefaultAI.ini");

            behaviorTreeView.BeforeExpand += behaviorTreeView_BeforeExpand;

            UpdateLayersListBox();

            Task.Run(new System.Action(ParseConfig)).Wait();

            UpdateBehaviorTreeView();
        }

        private void UpdateBehaviorTreeView()
        {
            behaviorTreeView.BeginUpdate();
            behaviorTreeView.Nodes.Clear();
            foreach (var root in BT.Roots())
            {
                var newNode = AddNode(root, behaviorTreeView.Nodes);
                Expand(newNode, root);
            }
            behaviorTreeView.EndUpdate();
        }

        private void UpdateLayersListBox()
        {
            layersListBox.BeginUpdate();
            layersListBox.Items.Clear();
            foreach (var path in layerPaths)
                layersListBox.Items.Add(path);
            layersListBox.EndUpdate();
        }

        void behaviorTreeView_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            behaviorTreeView.BeginUpdate();
            foreach (var child in e.Node.Nodes.Cast<TreeNode>())
            {
                if (child.Nodes.Count == 0)
                    Expand(child, (Behavior)child.Tag);
            }
            behaviorTreeView.EndUpdate();
        }

        private static void Expand(TreeNode node, Behavior behavior)
        {
                        foreach (var child in behavior.TypeLink.OrderBy(n => n.BehaviorName))
            {
                AddNode(child, node.Nodes);
            }

            for (int i = 0; i < behavior.ChildLink.Count; i++)
            {
                var child = behavior.ChildLink[i];
                AddNode(child, node.Nodes, i < behavior.Param.Count ? behavior.Param[i] : null);
            }
        }

        private static TreeNode AddNode(Behavior behavior, TreeNodeCollection treeNodeCollection, string param = null)
        {
            var newNode = treeNodeCollection.Add(behavior.Key, NodeLabel(behavior, param));
            newNode.Tag = behavior;

            if (behavior.Annotations.Contains("HasSelectAbility"))
                newNode.ForeColor = Color.Black;
            else if (behavior.Annotations.Contains("HasUpdateBestTarget"))
                newNode.ForeColor = Color.DarkRed;
            else if (behavior.Annotations.Contains("HasAction"))
            {
                if (behavior.Annotations.Contains("ConditionValued"))
                    newNode.ForeColor = Color.DarkCyan;
                else
                    newNode.ForeColor = Color.DarkGreen;
            }
            else
                newNode.ForeColor = Color.Blue;

            return newNode;
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

            var config = new ConfigParser();
            BT = config.ReadData(layerPaths);

            var analyzer = new Analyzer();
            analyzer.Analyze(BT);
        }

        private void BTViewer_Layout(object sender, LayoutEventArgs e)
        {
            mainSplitContainer.Bounds = DisplayRectangle;
            mainSplitContainer.Height -= statusStrip1.Height;
        }

        private void behaviorTreeView_Layout(object sender, LayoutEventArgs e)
        {
            behaviorTreeView.Scrollable = true;
        }

        private void splitContainer1_Panel1_Layout(object sender, LayoutEventArgs e)
        {
            behaviorTreeView.Bounds = mainSplitContainer.Panel1.DisplayRectangle;

        }

        private void addLayerButton_Click(object sender, EventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.FileName = "*.ini";
            var result = dialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                layerPaths.Add(dialog.FileName);

                UpdateLayersListBox();

                Task.Run(new System.Action(ParseConfig)).Wait();

                UpdateBehaviorTreeView();
            }
        }

    }
}
