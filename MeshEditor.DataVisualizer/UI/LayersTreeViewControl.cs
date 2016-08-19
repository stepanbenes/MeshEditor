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
using System.Diagnostics;

namespace MeshEditor.DataVisualizer.UI
{
	public partial class LayersTreeViewControl : UserControl
	{
		Dictionary<Guid, TreeNode> layerIdTreeNodeMap;
		bool checkingTreeNodes;

		public LayersTreeViewControl()
		{
			InitializeComponent();
			treeViewLayers.AfterSelect += treeViewLayers_AfterSelect;
			treeViewLayers.BeforeCheck += treeViewLayers_BeforeCheck;
			treeViewLayers.AfterCheck += treeViewLayers_AfterCheck;
			layerIdTreeNodeMap = new Dictionary<Guid, TreeNode>();
		}

		public event EventHandler<LayerSelectionEventArgs> LayerSelectionChanged;
		public event EventHandler<LayerSelectionEventArgs> LayerChecked;
		public event EventHandler<LayerSelectionEventArgs> LayerUnchecked;

		public ILayerInfo GetSelectedLayer() => treeViewLayers.SelectedNode?.Tag as ILayerInfo;

		public void SetSelectedLayer(Guid? layerId)
		{
			//checkAllNodes(treeViewLayers.Nodes, false);
			TreeNode treeNode;
			if (layerId.HasValue && layerIdTreeNodeMap.TryGetValue(layerId.Value, out treeNode))
			{
				treeViewLayers.SelectedNode = treeNode;
			}
			else
			{
				treeViewLayers.SelectedNode = null;
			}
		}

		public void SetCheckedLayers(IReadOnlyCollection<Guid> layerIds)
		{
			try
			{
				checkingTreeNodes = true;

				checkAllNodes(treeViewLayers.Nodes, isChecked: false); // uncheck all

				foreach (Guid layerId in layerIds)
				{
					Debug.Assert(layerIdTreeNodeMap.ContainsKey(layerId));
					layerIdTreeNodeMap[layerId].Checked = true;
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
			LayerSelectionChanged?.Invoke(this, new LayerSelectionEventArgs(layerInfo));

			// TODO: update context commands
		}

		private void treeViewLayers_BeforeCheck(object sender, TreeViewCancelEventArgs e)
		{
			if (!e.Node.IsSelected) // it must be selected before checking/unchecking
				e.Cancel = true;
		}

		private void treeViewLayers_AfterCheck(object sender, TreeViewEventArgs e)
		{
			if (checkingTreeNodes)
				return;

			var layerInfo = (ILayerInfo)e.Node.Tag;
			if (e.Node.Checked)
			{
				LayerChecked?.Invoke(this, new LayerSelectionEventArgs(layerInfo));
			}
			else
			{
				LayerUnchecked?.Invoke(this, new LayerSelectionEventArgs(layerInfo));
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
