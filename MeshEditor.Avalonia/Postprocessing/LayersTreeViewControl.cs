using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using MeshEditor.DataVisualizer.Data;
using MeshEditor.LayerManager.Filters;
using MeshEditor.SolutionManager.IO;

namespace MeshEditor.AvaloniaUI.Postprocessing
{
	/// <summary>
	/// Node of the layer tree (TreeNode with Tag = ILayerInfo in the WinForms UI).
	/// </summary>
	public sealed class LayerTreeNode : INotifyPropertyChanged
	{
		private bool isChecked;

		public LayerTreeNode(ILayerInfo layer, LayerTreeNode parent)
		{
			Layer = layer;
			Parent = parent;
		}

		public ILayerInfo Layer { get; }
		public LayerTreeNode Parent { get; }
		public string Name => Layer.Name;
		public ObservableCollection<LayerTreeNode> Children { get; } = new ObservableCollection<LayerTreeNode>();

		public bool IsChecked
		{
			get => isChecked;
			set
			{
				if (isChecked != value)
				{
					isChecked = value;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChecked)));
				}
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
	}

	/// <summary>
	/// Tree of the solution layers with check boxes (visible layers) and context menu (filters, delete, reload)
	/// (port of MeshEditor.DataVisualizer.UI.LayersTreeViewControl).
	/// </summary>
	public class LayersTreeViewControl : DockPanel
	{
		private readonly Dictionary<Guid, LayerTreeNode> layerIdTreeNodeMap = new Dictionary<Guid, LayerTreeNode>();
		private readonly ObservableCollection<LayerTreeNode> rootNodes = new ObservableCollection<LayerTreeNode>();
		private readonly TreeView treeViewLayers;
		private readonly ContextMenu contextMenuStrip;
		private readonly MenuItem deleteLayerToolStripMenuItem;
		private bool checkingTreeNodesFlag;

		public event EventHandler<LayerSelectionEventArgs> LayerSelected;
		public event EventHandler<LayerSelectionEventArgs> LayerChecked;
		public event EventHandler<LayerSelectionEventArgs> LayerUnchecked;
		public event EventHandler<LayerSelectionEventArgs> LayerReloadRequested;
		public event EventHandler<LayerSelectionEventArgs> LayerDeleteRequested;
		public event EventHandler<LayerFilterEventArgs> LayerFilterRequested;

		public LayersTreeViewControl()
		{
			var labelCaption = new TextBlock { Text = "Layers", FontWeight = FontWeight.Bold, Margin = new Thickness(2, 0, 0, 4) };
			DockPanel.SetDock(labelCaption, Dock.Top);

			MenuItem item(string header, EventHandler<RoutedEventArgs> click)
			{
				var menuItem = new MenuItem { Header = header };
				menuItem.Click += click;
				return menuItem;
			}
			var applyFilter = new MenuItem { Header = "Apply filter..." };
			applyFilter.Items.Add(item("Deformation...", (s, e) => requestFilter(FilterType.Deformation)));
			applyFilter.Items.Add(item("Slice...", (s, e) => requestFilter(FilterType.Slice)));
			applyFilter.Items.Add(item("Iso-surface...", (s, e) => requestFilter(FilterType.IsoSurface)));
			deleteLayerToolStripMenuItem = item("Delete layer", (s, e) => { if (selectedNode is LayerTreeNode n) LayerDeleteRequested?.Invoke(this, new LayerSelectionEventArgs(n.Layer)); });
			contextMenuStrip = new ContextMenu();
			contextMenuStrip.Items.Add(applyFilter);
			contextMenuStrip.Items.Add(deleteLayerToolStripMenuItem);
			contextMenuStrip.Items.Add(new Separator());
			contextMenuStrip.Items.Add(item("Reload layer", (s, e) => { if (selectedNode is LayerTreeNode n) LayerReloadRequested?.Invoke(this, new LayerSelectionEventArgs(n.Layer)); }));
			contextMenuStrip.Opening += (s, e) =>
			{
				// only for the selected node (WinForms opens it on right click of the selected node)
				if (selectedNode == null)
				{
					e.Cancel = true;
					return;
				}
				deleteLayerToolStripMenuItem.IsEnabled = selectedNode.Parent != null; // master layers can't be deleted
			};

			treeViewLayers = new TreeView
			{
				ItemsSource = rootNodes,
				ItemTemplate = new FuncTreeDataTemplate<LayerTreeNode>((node, _) => createNodeView(node), node => node.Children),
				ContextMenu = contextMenuStrip,
				BorderBrush = Brush.Parse("#ABADB3"),
				BorderThickness = new Thickness(1),
			};
			treeViewLayers.SelectionChanged += (s, e) =>
			{
				if (selectedNode is LayerTreeNode node)
					LayerSelected?.Invoke(this, new LayerSelectionEventArgs(node.Layer));
			};

			Children.Add(labelCaption);
			Children.Add(treeViewLayers);
		}

		private LayerTreeNode selectedNode => treeViewLayers.SelectedItem as LayerTreeNode;

		internal IReadOnlyList<LayerTreeNode> RootNodes => rootNodes;

		internal TreeView TreeView => treeViewLayers;

		internal ContextMenu LayerContextMenu => contextMenuStrip;

		private Control createNodeView(LayerTreeNode node)
		{
			// the name is not the check box content: clicking the name selects the node (the check box would handle the click)
			var checkBox = new CheckBox { IsChecked = node.IsChecked, MinWidth = 0, Padding = new Thickness(0), VerticalAlignment = VerticalAlignment.Center };
			var label = new TextBlock { Text = node.Name, Margin = new Thickness(4, 0), VerticalAlignment = VerticalAlignment.Center };
			bool updating = false;
			checkBox.IsCheckedChanged += (s, e) =>
			{
				if (updating)
					return;
				setChecked(node, checkBox.IsChecked == true); // any layer can be checked, not only the selected one
			};
			node.PropertyChanged += (s, e) =>
			{
				updating = true;
				checkBox.IsChecked = node.IsChecked;
				updating = false;
			};
			return new StackPanel { Orientation = Orientation.Horizontal, Children = { checkBox, label } };
		}

		/// <summary>
		/// Checks/unchecks a layer and raises LayerChecked/LayerUnchecked (TreeView.AfterCheck).
		/// </summary>
		internal void setChecked(LayerTreeNode node, bool value)
		{
			if (node.IsChecked == value)
				return;
			node.IsChecked = value;
			if (checkingTreeNodesFlag)
				return;
			if (value)
				LayerChecked?.Invoke(this, new LayerSelectionEventArgs(node.Layer));
			else
				LayerUnchecked?.Invoke(this, new LayerSelectionEventArgs(node.Layer));
		}

		private void requestFilter(FilterType filterType)
		{
			if (selectedNode is LayerTreeNode node)
				LayerFilterRequested?.Invoke(this, new LayerFilterEventArgs(node.Layer, filterType));
		}

		#region Public methods (same as the WinForms control)

		public ILayerInfo GetSelectedLayer() => selectedNode?.Layer;

		public bool IsLayerSelected(Guid layerId) => layerIdTreeNodeMap.TryGetValue(layerId, out var node) && selectedNode == node;

		public bool IsLayerChecked(Guid layerId) => layerIdTreeNodeMap.TryGetValue(layerId, out var node) && node.IsChecked;

		public void SetSelectedLayer(Guid? layerId)
		{
			if (layerId.HasValue && layerIdTreeNodeMap.TryGetValue(layerId.Value, out LayerTreeNode node))
				treeViewLayers.SelectedItem = node;
			else
				treeViewLayers.SelectedItem = null;
		}

		public bool SetCheckedFlagOfLayer(Guid layerId, bool check)
		{
			var node = layerIdTreeNodeMap[layerId];
			if (node.IsChecked != check)
			{
				setChecked(node, check);
				return true;
			}
			return false;
		}

		public void SetCheckedLayers(IEnumerable<Guid> layerIds)
		{
			try
			{
				checkingTreeNodesFlag = true;
				foreach (var node in layerIdTreeNodeMap.Values) // uncheck all
					node.IsChecked = false;
				foreach (Guid layerId in layerIds ?? Enumerable.Empty<Guid>())
				{
					if (layerIdTreeNodeMap.TryGetValue(layerId, out var node))
						node.IsChecked = true;
				}
			}
			finally
			{
				checkingTreeNodesFlag = false;
			}
		}

		public void SetLayerTree(IEnumerable<ILayerInfo> layers)
		{
			rootNodes.Clear();
			layerIdTreeNodeMap.Clear();
			foreach (var layer in layers)
				rootNodes.Add(createTreeNode(layer, parent: null));
			Avalonia.Threading.Dispatcher.UIThread.Post(expandAll, Avalonia.Threading.DispatcherPriority.Loaded);
		}

		public void AddNewLayer(Guid parentLayerId, ILayerInfo layer, bool selectNewLayer)
		{
			LayerTreeNode parentTreeNode = layerIdTreeNodeMap[parentLayerId];
			LayerTreeNode newTreeNode = createTreeNode(layer, parentTreeNode);
			parentTreeNode.Children.Add(newTreeNode);
			Avalonia.Threading.Dispatcher.UIThread.Post(expandAll, Avalonia.Threading.DispatcherPriority.Loaded);
			if (selectNewLayer)
				treeViewLayers.SelectedItem = newTreeNode;
		}

		public void RemoveLayer(Guid layerId, bool selectParentLayer)
		{
			LayerTreeNode treeNodeToRemove = layerIdTreeNodeMap[layerId];
			if (IsLayerChecked(layerId)) // uncheck layer if checked
				LayerUnchecked?.Invoke(this, new LayerSelectionEventArgs(treeNodeToRemove.Layer));

			LayerTreeNode parentTreeNode = treeNodeToRemove.Parent;
			if (parentTreeNode != null)
				parentTreeNode.Children.Remove(treeNodeToRemove);
			else
				rootNodes.Remove(treeNodeToRemove);
			layerIdTreeNodeMap.Remove(layerId);

			if (selectParentLayer)
				treeViewLayers.SelectedItem = parentTreeNode;
		}

		#endregion

		private LayerTreeNode createTreeNode(ILayerInfo layer, LayerTreeNode parent)
		{
			var treeNode = new LayerTreeNode(layer, parent);
			layerIdTreeNodeMap[layer.Id] = treeNode;
			foreach (var child in layer.Children)
				treeNode.Children.Add(createTreeNode(child, treeNode));
			return treeNode;
		}

		private void expandAll()
		{
			foreach (var item in treeViewLayers.GetLogicalDescendants().OfType<TreeViewItem>())
				item.IsExpanded = true;
		}
	}
}
