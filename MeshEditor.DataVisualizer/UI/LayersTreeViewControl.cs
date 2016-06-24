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

namespace MeshEditor.WinUI
{
	public partial class LayersTreeViewControl : UserControl
	{
		public event EventHandler<LayerEventArgs> SelectedLayerChanged;

		Dictionary<Guid, TreeNode> layerIdTreeNodeMap;

		public LayersTreeViewControl()
		{
			InitializeComponent();
			treeViewLayers.AfterSelect += treeViewLayers_AfterSelect;
			layerIdTreeNodeMap = new Dictionary<Guid, TreeNode>();
		}

		public Guid? SelectedLayerId
		{
			get
			{
				if (treeViewLayers.SelectedNode == null)
					return null;
				var layerInfo = (ILayerInfo)treeViewLayers.SelectedNode.Tag;
				return layerInfo.Id;
			}
			set
			{
				TreeNode treeNodeToSelect = null;
				if (value.HasValue && layerIdTreeNodeMap.TryGetValue(value.Value, out treeNodeToSelect))
					treeViewLayers.SelectedNode = treeNodeToSelect;
				else
					treeViewLayers.SelectedNode = null;
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
			SelectedLayerChanged?.Invoke(this, new LayerEventArgs(SelectedLayerId));
		}

		private TreeNode createTreeNode(ILayerInfo layer)
		{
			var treeNode = new TreeNode(layer.Name) { Tag = layer };
			layerIdTreeNodeMap[layer.Id] = treeNode;
			if (layer.Children != null)
			{
				foreach (var child in layer.Children)
				{
					treeNode.Nodes.Add(createTreeNode(child));
				}
			}
			return treeNode;
		}
	}
}
