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
using MeshEditor.LayerManager.Filters;

namespace MeshEditor.DataVisualizer.UI
{
	public partial class LayersTreeViewControl : UserControl
	{
		readonly Dictionary<Guid, TreeNode> layerIdTreeNodeMap;
		bool checkingTreeNodesFlag;

		public LayersTreeViewControl()
		{
			InitializeComponent();
			treeViewLayers.AfterSelect += treeViewLayers_AfterSelect;
			treeViewLayers.BeforeCheck += treeViewLayers_BeforeCheck;
			treeViewLayers.AfterCheck += treeViewLayers_AfterCheck;
			layerIdTreeNodeMap = new Dictionary<Guid, TreeNode>();
		}

		public event EventHandler<LayerSelectionEventArgs> LayerSelected;
		public event EventHandler<LayerSelectionEventArgs> LayerChecked;
		public event EventHandler<LayerSelectionEventArgs> LayerUnchecked;
		public event EventHandler<LayerSelectionEventArgs> LayerReloadRequested;
		public event EventHandler<LayerSelectionEventArgs> LayerDeleteRequested;
		public event EventHandler<LayerFilterEventArgs> LayerFilterRequested;

		public ILayerInfo GetSelectedLayer() => treeViewLayers.SelectedNode?.Tag as ILayerInfo;
		public bool IsLayerSelected(Guid layerId) => layerIdTreeNodeMap.ContainsKey(layerId) && layerIdTreeNodeMap[layerId].IsSelected;
		public bool IsLayerChecked(Guid layerId) => layerIdTreeNodeMap.ContainsKey(layerId) && layerIdTreeNodeMap[layerId].Checked;

		public void SetSelectedLayer(Guid? layerId)
		{
			if (layerId.HasValue && layerIdTreeNodeMap.TryGetValue(layerId.Value, out TreeNode treeNode))
			{
				treeViewLayers.SelectedNode = treeNode;
			}
			else
			{
				treeViewLayers.SelectedNode = null;
			}
		}

		public bool SetCheckedFlagOfLayer(Guid layerId, bool check)
		{
			bool wasChecked = layerIdTreeNodeMap[layerId].Checked;
			if (wasChecked != check)
			{
				layerIdTreeNodeMap[layerId].Checked = check;
				return true;
			}
			return false; ;
		}

		public void SetCheckedLayers(IEnumerable<Guid> layerIds)
		{
			try
			{
				checkingTreeNodesFlag = true;

				checkAllNodes(treeViewLayers.Nodes, isChecked: false); // uncheck all

				foreach (Guid layerId in layerIds)
				{
					Debug.Assert(layerIdTreeNodeMap.ContainsKey(layerId));
					layerIdTreeNodeMap[layerId].Checked = true;
				}
			}
			finally
			{
				checkingTreeNodesFlag = false;
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

		public void AddNewLayer(Guid parentLayerId, ILayerInfo layer, bool selectNewLayer)
		{
			TreeNode parentTreeNode = layerIdTreeNodeMap[parentLayerId];
			TreeNode newTreeNode = createTreeNode(layer);
			parentTreeNode.Nodes.Add(newTreeNode);
			if (selectNewLayer)
			{
				treeViewLayers.SelectedNode = newTreeNode;
			}
		}

		public void RemoveLayer(Guid layerId, bool selectParentLayer)
		{
			TreeNode treeNodeToRemove = layerIdTreeNodeMap[layerId];
			if (IsLayerChecked(layerId)) // uncheck layer if checked
			{
				LayerUnchecked?.Invoke(this, new LayerSelectionEventArgs((ILayerInfo)treeNodeToRemove.Tag));
			}
			TreeNode parentTreeNode = treeNodeToRemove.Parent;
			treeNodeToRemove.Remove();
			if (selectParentLayer)
			{
				treeViewLayers.SelectedNode = parentTreeNode;
			}
		}

		private void treeViewLayers_AfterSelect(object sender, TreeViewEventArgs e)
		{
			var layerInfo = (ILayerInfo)e.Node.Tag;
			LayerSelected?.Invoke(this, new LayerSelectionEventArgs(layerInfo));
		}

		private void treeViewLayers_BeforeCheck(object sender, TreeViewCancelEventArgs e)
		{
			if (checkingTreeNodesFlag)
				return;

			if (!e.Node.IsSelected) // it must be selected before checking/unchecking
				e.Cancel = true;
		}

		private void treeViewLayers_AfterCheck(object sender, TreeViewEventArgs e)
		{
			if (checkingTreeNodesFlag)
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
			foreach (var child in layer.Children)
			{
				treeNode.Nodes.Add(createTreeNode(child));
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

		private void reloadLayerToolStripMenuItem_Click(object sender, EventArgs e)
		{
			var layerInfo = (ILayerInfo)treeViewLayers.SelectedNode.Tag;
			LayerReloadRequested?.Invoke(this, new LayerSelectionEventArgs(layerInfo));
		}

		private void treeViewLayers_MouseUp(object sender, MouseEventArgs e)
		{
			// Show menu only if the right mouse button is clicked.
			if (e.Button == MouseButtons.Right)
			{
				// Point where the mouse is clicked.
				Point p = new Point(e.X, e.Y);

				// Get the node that the user has clicked.
				TreeNode node = treeViewLayers.GetNodeAt(p);
				if (node != null && node == treeViewLayers.SelectedNode)
				{
					contextMenuStrip.Show(treeViewLayers, p);
				}
			}
		}

		private void deformationToolStripMenuItem_Click(object sender, EventArgs e)
		{
			var layerInfo = (ILayerInfo)treeViewLayers.SelectedNode.Tag;
			LayerFilterRequested?.Invoke(this, new LayerFilterEventArgs(layerInfo, FilterType.Deformation));
		}

		private void sliceToolStripMenuItem_Click(object sender, EventArgs e)
		{
			var layerInfo = (ILayerInfo)treeViewLayers.SelectedNode.Tag;
			LayerFilterRequested?.Invoke(this, new LayerFilterEventArgs(layerInfo, FilterType.Slice));
		}

		private void deleteLayerToolStripMenuItem_Click(object sender, EventArgs e)
		{
			var layerInfo = (ILayerInfo)treeViewLayers.SelectedNode.Tag;
			LayerDeleteRequested?.Invoke(this, new LayerSelectionEventArgs(layerInfo));
		}

		private void contextMenuStrip_Opening(object sender, CancelEventArgs e)
		{
			var layerInfo = (ILayerInfo)treeViewLayers.SelectedNode.Tag;
			deleteLayerToolStripMenuItem.Enabled = !isMasterLayer(layerInfo);
		}

		private bool isMasterLayer(ILayerInfo layerInfo)
		{
			foreach (TreeNode node in treeViewLayers.Nodes)
			{
				if (layerInfo.Equals(node.Tag))
					return true;
			}
			return false;
		}
	}
}
