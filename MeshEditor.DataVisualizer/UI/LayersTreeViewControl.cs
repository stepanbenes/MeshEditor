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
			treeViewLayers.BeforeSelect += treeViewLayers_BeforeSelect;
			treeViewLayers.AfterSelect += treeViewLayers_AfterSelect;
			treeViewLayers.BeforeCheck += treeViewLayers_BeforeCheck;
			treeViewLayers.AfterCheck += treeViewLayers_AfterCheck;
			layerIdTreeNodeMap = new Dictionary<Guid, TreeNode>();
		}

		public event EventHandler<LayerSelectionEventArgs> LayerUnselected;
		public event EventHandler<LayerSelectionEventArgs> LayerSelected;
		public event EventHandler<LayerSelectionEventArgs> LayerChecked;
		public event EventHandler<LayerSelectionEventArgs> LayerUnchecked;
		public event EventHandler<LayerSelectionEventArgs> ReloadLayerRequested;

		public ILayerInfo GetSelectedLayer() => treeViewLayers.SelectedNode?.Tag as ILayerInfo;
		public bool IsLayerSelected(Guid layerId) => layerIdTreeNodeMap.ContainsKey(layerId) && layerIdTreeNodeMap[layerId].IsSelected;
		public bool IsLayerChecked(Guid layerId) => layerIdTreeNodeMap.ContainsKey(layerId) && layerIdTreeNodeMap[layerId].Checked;

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

		public void SetCheckedLayers(IEnumerable<Guid> layerIds)
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

		private void treeViewLayers_BeforeSelect(object sender, TreeViewCancelEventArgs e)
		{
			if (treeViewLayers.SelectedNode != null && treeViewLayers.SelectedNode != e.Node)
			{
				treeViewLayers.SelectedNode.BackColor = treeViewLayers.BackColor; // manually set color of previous selected node to unselected
				treeViewLayers.SelectedNode.ForeColor = treeViewLayers.ForeColor;

				LayerUnselected?.Invoke(this, new LayerSelectionEventArgs((ILayerInfo)treeViewLayers.SelectedNode.Tag));
			}
		}

		private void treeViewLayers_AfterSelect(object sender, TreeViewEventArgs e)
		{
			e.Node.BackColor = SystemColors.Highlight; // manually highlight selected node
			e.Node.ForeColor = Color.White;

			var layerInfo = (ILayerInfo)e.Node.Tag;
			LayerSelected?.Invoke(this, new LayerSelectionEventArgs(layerInfo));
		}

		private void treeViewLayers_BeforeCheck(object sender, TreeViewCancelEventArgs e)
		{
			if (checkingTreeNodes)
				return;

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

		private void reloadLayerToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Debug.Assert(treeViewLayers.SelectedNode != null);
			var layerInfo = (ILayerInfo)treeViewLayers.SelectedNode.Tag;
			ReloadLayerRequested?.Invoke(this, new LayerSelectionEventArgs(layerInfo));
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
	}
}
