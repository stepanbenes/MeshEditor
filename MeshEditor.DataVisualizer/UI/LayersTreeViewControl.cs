using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MeshEditor.SolutionManager.IO;
using MeshEditor.DataVisualizer.Data;

namespace MeshEditor.WinUI
{
	public partial class LayersTreeViewControl : UserControl
	{
		Dictionary<Guid, TreeNode> layerIdTreeNodeMap;
		bool checkingTreeNodes;

		public LayersTreeViewControl()
		{
			InitializeComponent();
			treeViewLayers.AfterSelect += treeViewLayers_AfterSelect;
			treeViewLayers.AfterCheck += treeViewLayers_AfterCheck;
			layerIdTreeNodeMap = new Dictionary<Guid, TreeNode>();
		}

		public event EventHandler<LayerSelectionEventArgs> LayerSelectionChanged;

		public void SetSelectedLayer(Guid? layerId)
		{
			try
			{
				checkingTreeNodes = true;
				checkAllNodes(treeViewLayers.Nodes, false);
				TreeNode treeNode;
				if (layerId.HasValue && layerIdTreeNodeMap.TryGetValue(layerId.Value, out treeNode))
				{
					treeNode.Checked = true;
				}
			}
			finally
			{
				checkingTreeNodes = false;
			}
		}

		public void SetLayerTree(IEnumerable<ILayerInfo> layers)
		{
			treeViewLayers.Nodes.Clear();
			layerIdTreeNodeMap.Clear();
			foreach (var layer in layers)
			{
				treeViewLayers.Nodes.Add(createTreeNode(layer));
			}
			treeViewLayers.ExpandAll();
		}

		private void treeViewLayers_AfterSelect(object sender, TreeViewEventArgs e)
		{
			var layerInfo = (ILayerInfo)e.Node.Tag;
			// TODO: update context commands
		}

		private void treeViewLayers_AfterCheck(object sender, TreeViewEventArgs e)
		{
			if (checkingTreeNodes)
				return;

			try
			{
				checkingTreeNodes = true;
				bool isChecked = e.Node.Checked;
				checkAllNodes(treeViewLayers.Nodes, false);
				e.Node.Checked = isChecked;
				if (e.Node.Checked)
				{
					var layerInfo = (ILayerInfo)e.Node.Tag;
					LayerSelectionChanged?.Invoke(this, new LayerSelectionEventArgs(layerInfo.Id));
				}
				else
				{
					LayerSelectionChanged?.Invoke(this, new LayerSelectionEventArgs(null));
				}
			}
			finally
			{
				checkingTreeNodes = false;
			}
		}

		private TreeNode createTreeNode(ILayerInfo layer)
		{
			var treeNode = new TreeNode(layer.Name) { Tag = layer };
			layerIdTreeNodeMap[layer.Id] = treeNode; // put in cache
			if (layer.Children != null)
			{
				foreach (var child in layer.Children)
				{
					treeNode.Nodes.Add(createTreeNode(child));
				}
			}
			return treeNode;
		}

		private void checkAllNodes(TreeNodeCollection nodes, bool isChecked)
		{
			foreach (TreeNode node in nodes)
			{
				node.Checked = isChecked;
				checkChildren(node, isChecked);
			}
		}

		private void checkChildren(TreeNode rootNode, bool isChecked)
		{
			foreach (TreeNode node in rootNode.Nodes)
			{
				checkChildren(node, isChecked);
				node.Checked = isChecked;
			}
		}
	}
}
