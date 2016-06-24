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

		public LayersTreeViewControl()
		{
			InitializeComponent();
			treeViewLayers.AfterSelect += treeViewLayers_AfterSelect;
		}

		public Guid? SelectedLayerId => (treeViewLayers.SelectedNode?.Tag as ILayerInfo)?.Id;

		public void SetLayerTree(IEnumerable<ILayerInfo> layers)
		{
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
