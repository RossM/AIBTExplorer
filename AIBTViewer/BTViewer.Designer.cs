using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace AIBTViewer
{
    partial class BTViewer
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.behaviorTreeView = new System.Windows.Forms.TreeView();
            this.mainSplitContainer = new System.Windows.Forms.SplitContainer();
            this.errorLabel = new System.Windows.Forms.Label();
            this.errorListBox = new System.Windows.Forms.ListBox();
            this.layersTreeView = new System.Windows.Forms.TreeView();
            this.refreshButton = new System.Windows.Forms.Button();
            this.fileNameLabel = new System.Windows.Forms.Label();
            this.behaviorTextBox = new System.Windows.Forms.TextBox();
            this.removeLayerButton = new System.Windows.Forms.Button();
            this.addLayerButton = new System.Windows.Forms.Button();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            ((System.ComponentModel.ISupportInitialize)(this.mainSplitContainer)).BeginInit();
            this.mainSplitContainer.Panel1.SuspendLayout();
            this.mainSplitContainer.Panel2.SuspendLayout();
            this.mainSplitContainer.SuspendLayout();
            this.SuspendLayout();
            // 
            // behaviorTreeView
            // 
            this.behaviorTreeView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.behaviorTreeView.Location = new System.Drawing.Point(3, 3);
            this.behaviorTreeView.Name = "behaviorTreeView";
            this.behaviorTreeView.Size = new System.Drawing.Size(450, 643);
            this.behaviorTreeView.TabIndex = 0;
            this.behaviorTreeView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.behaviorTreeView_AfterSelect);
            this.behaviorTreeView.Layout += new System.Windows.Forms.LayoutEventHandler(this.behaviorTreeView_Layout);
            // 
            // mainSplitContainer
            // 
            this.mainSplitContainer.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.mainSplitContainer.BackColor = System.Drawing.SystemColors.Control;
            this.mainSplitContainer.Location = new System.Drawing.Point(-2, -1);
            this.mainSplitContainer.Name = "mainSplitContainer";
            // 
            // mainSplitContainer.Panel1
            // 
            this.mainSplitContainer.Panel1.Controls.Add(this.behaviorTreeView);
            // 
            // mainSplitContainer.Panel2
            // 
            this.mainSplitContainer.Panel2.Controls.Add(this.errorLabel);
            this.mainSplitContainer.Panel2.Controls.Add(this.errorListBox);
            this.mainSplitContainer.Panel2.Controls.Add(this.layersTreeView);
            this.mainSplitContainer.Panel2.Controls.Add(this.refreshButton);
            this.mainSplitContainer.Panel2.Controls.Add(this.fileNameLabel);
            this.mainSplitContainer.Panel2.Controls.Add(this.behaviorTextBox);
            this.mainSplitContainer.Panel2.Controls.Add(this.removeLayerButton);
            this.mainSplitContainer.Panel2.Controls.Add(this.addLayerButton);
            this.mainSplitContainer.Size = new System.Drawing.Size(964, 646);
            this.mainSplitContainer.SplitterDistance = 456;
            this.mainSplitContainer.TabIndex = 1;
            // 
            // errorLabel
            // 
            this.errorLabel.AutoSize = true;
            this.errorLabel.Location = new System.Drawing.Point(9, 295);
            this.errorLabel.Name = "errorLabel";
            this.errorLabel.Size = new System.Drawing.Size(34, 13);
            this.errorLabel.TabIndex = 8;
            this.errorLabel.Text = "Errors";
            this.errorLabel.Visible = false;
            // 
            // errorListBox
            // 
            this.errorListBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.errorListBox.FormattingEnabled = true;
            this.errorListBox.HorizontalScrollbar = true;
            this.errorListBox.IntegralHeight = false;
            this.errorListBox.Location = new System.Drawing.Point(12, 311);
            this.errorListBox.Name = "errorListBox";
            this.errorListBox.Size = new System.Drawing.Size(488, 322);
            this.errorListBox.TabIndex = 7;
            this.errorListBox.Visible = false;
            // 
            // layersTreeView
            // 
            this.layersTreeView.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.layersTreeView.CheckBoxes = true;
            this.layersTreeView.Location = new System.Drawing.Point(121, 7);
            this.layersTreeView.Name = "layersTreeView";
            this.layersTreeView.ShowRootLines = false;
            this.layersTreeView.Size = new System.Drawing.Size(379, 81);
            this.layersTreeView.TabIndex = 6;
            this.layersTreeView.AfterCheck += new System.Windows.Forms.TreeViewEventHandler(this.layersTreeView_AfterCheck);
            // 
            // refreshButton
            // 
            this.refreshButton.Location = new System.Drawing.Point(12, 65);
            this.refreshButton.Name = "refreshButton";
            this.refreshButton.Size = new System.Drawing.Size(103, 23);
            this.refreshButton.TabIndex = 5;
            this.refreshButton.Text = "Refresh";
            this.refreshButton.UseVisualStyleBackColor = true;
            this.refreshButton.Click += new System.EventHandler(this.refreshButton_Click);
            // 
            // fileNameLabel
            // 
            this.fileNameLabel.AutoSize = true;
            this.fileNameLabel.Location = new System.Drawing.Point(9, 100);
            this.fileNameLabel.Name = "fileNameLabel";
            this.fileNameLabel.Size = new System.Drawing.Size(48, 13);
            this.fileNameLabel.TabIndex = 4;
            this.fileNameLabel.Text = "fileName";
            // 
            // behaviorTextBox
            // 
            this.behaviorTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.behaviorTextBox.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.behaviorTextBox.Location = new System.Drawing.Point(12, 116);
            this.behaviorTextBox.Multiline = true;
            this.behaviorTextBox.Name = "behaviorTextBox";
            this.behaviorTextBox.ReadOnly = true;
            this.behaviorTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.behaviorTextBox.Size = new System.Drawing.Size(488, 172);
            this.behaviorTextBox.TabIndex = 3;
            // 
            // removeLayerButton
            // 
            this.removeLayerButton.Location = new System.Drawing.Point(12, 36);
            this.removeLayerButton.Name = "removeLayerButton";
            this.removeLayerButton.Size = new System.Drawing.Size(103, 23);
            this.removeLayerButton.TabIndex = 2;
            this.removeLayerButton.Text = "Remove Layer";
            this.removeLayerButton.UseVisualStyleBackColor = true;
            this.removeLayerButton.Click += new System.EventHandler(this.removeLayerButton_Click);
            // 
            // addLayerButton
            // 
            this.addLayerButton.Location = new System.Drawing.Point(12, 7);
            this.addLayerButton.Name = "addLayerButton";
            this.addLayerButton.Size = new System.Drawing.Size(103, 23);
            this.addLayerButton.TabIndex = 1;
            this.addLayerButton.Text = "Add Layer...";
            this.addLayerButton.UseVisualStyleBackColor = true;
            this.addLayerButton.Click += new System.EventHandler(this.addLayerButton_Click);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Location = new System.Drawing.Point(0, 635);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(962, 22);
            this.statusStrip1.TabIndex = 2;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // BTViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(962, 657);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.mainSplitContainer);
            this.Name = "BTViewer";
            this.Text = "AIBT Explorer";
            this.mainSplitContainer.Panel1.ResumeLayout(false);
            this.mainSplitContainer.Panel2.ResumeLayout(false);
            this.mainSplitContainer.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.mainSplitContainer)).EndInit();
            this.mainSplitContainer.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TreeView behaviorTreeView;
        private System.Windows.Forms.SplitContainer mainSplitContainer;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.Button removeLayerButton;
        private System.Windows.Forms.Button addLayerButton;
        private System.Windows.Forms.TextBox behaviorTextBox;
        private System.Windows.Forms.Label fileNameLabel;
        private System.Windows.Forms.Button refreshButton;
        private System.Windows.Forms.TreeView layersTreeView;
        private System.Windows.Forms.ListBox errorListBox;
        private System.Windows.Forms.Label errorLabel;
    }
}

