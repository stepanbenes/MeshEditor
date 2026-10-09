using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using MeshEditor.CoreInterface;
using MeshEditor.Data;

namespace MeshEditor.AvaloniaUI.Dialogs
{
	/// <summary>
	/// Row of the property descriptions table.
	/// </summary>
	public sealed class PropertyDescriptionRow : INotifyPropertyChanged
	{
		private string commands, comment;

		public PropertyDescriptionRow(Property property, EntityType entityType, string commands, string comment)
		{
			Property = property;
			EntityType = entityType;
			this.commands = commands;
			this.comment = comment;
		}

		public Property Property { get; }
		public EntityType EntityType { get; }
		public int PropertyValue => int.Parse(Property.ToString());
		public string PropertyText => Property.ToString();
		public string EntityText => EntityType.ToString();

		public string Commands
		{
			get => commands;
			set { commands = value; onPropertyChanged(); }
		}

		public string Comment
		{
			get => comment;
			set { comment = value; onPropertyChanged(); }
		}

		public event PropertyChangedEventHandler PropertyChanged;

		private void onPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}

	/// <summary>
	/// Basic information about the mesh, property descriptions (comments, preprocessor commands) and edge angle limits
	/// (port of MeshEditor.WinUI.MeshInfoForm).
	/// </summary>
	public partial class MeshInfoDialog : Window
	{
		private readonly SceneFacade sceneFacade;
		private readonly LongOpNotifier longOpNotifier;
		private readonly ObservableCollection<PropertyDescriptionRow> rows = new ObservableCollection<PropertyDescriptionRow>();
		private bool initialized;

		private static int lastSelectedTabIndex = 1; // default tab is "Property descriptions"

		public MeshInfoDialog()
			: this(SceneFacade.GetEmptyScene(), new LongOpNotifier())
		{ }

		public MeshInfoDialog(SceneFacade sceneFacade, LongOpNotifier longOpNotifier)
		{
			InitializeComponent();
			this.sceneFacade = sceneFacade;
			this.longOpNotifier = longOpNotifier;
			dataGridViewPropertyDescriptions.ItemsSource = rows;
			init();
			initialized = true;
			_ = computeNodeCountAsync();
		}

		internal ObservableCollection<PropertyDescriptionRow> Rows => rows;

		/// <summary>
		/// Completes when the node count (computed in background) is shown.
		/// </summary>
		internal Task NodeCountComputed { get; private set; } = Task.CompletedTask;

		public new async Task ShowDialog(Window owner)
		{
			await this.ShowModalAsync(owner);
		}

		private Task computeNodeCountAsync()
		{
			if (!sceneFacade.ContainsMesh)
				return NodeCountComputed = Task.CompletedTask;
			return NodeCountComputed = computeNodeCount();

			async Task computeNodeCount()
			{
				try
				{
					object result = await Task.Run(() => sceneFacade.GetValue(AvailableValue.NodeCount));
					if (result is int)
						labelNodeCount.Text = "Node count: " + result;
				}
				catch (Exception ex)
				{
					Console.Error.WriteLine($"NodeCountComputer: {ex.GetType()}: {ex.Message}");
				}
			}
		}

		private void init()
		{
			tabControl.SelectedIndex = lastSelectedTabIndex;

			if (sceneFacade.ContainsMesh)
			{
				MeshStatistics statistics = sceneFacade.GetValue(AvailableValue.MeshStatistics) as MeshStatistics;
				if (statistics != null)
				{
					fillPropertyDescriptionsListView(statistics);
					firstBorderLimitTrackBar.Value = (int)statistics.SoftBorderLimit;
					secondBorderLimitTrackBar.Value = (int)statistics.HardBorderLimit;
					if (statistics.EdgeAnglesHistogram != null)
						histogramViewer.SetHistogram(statistics.EdgeAnglesHistogram);
				}
				firstBorderLimitLabel.Text = "First border limit: " + (int)firstBorderLimitTrackBar.Value + "°";
				secondBorderLimitLabel.Text = "Second border limit: " + (int)secondBorderLimitTrackBar.Value + "°";
				histogramViewer.FirstLimit = (int)firstBorderLimitTrackBar.Value;
				histogramViewer.SecondLimit = (int)secondBorderLimitTrackBar.Value;
			}

			initBasicMeshInfo();
		}

		private static string commandsText(List<PropertyCommand> commands)
		{
			StringBuilder text = new StringBuilder();
			if (commands != null)
			{
				foreach (PropertyCommand command in commands)
					text.AppendLine(command.ToString());
			}
			return text.ToString().Trim();
		}

		private void fillPropertyDescriptionsListView(MeshStatistics statistics)
		{
			var newRows = new List<PropertyDescriptionRow>();
			foreach (PropertyEntityPair propertyTargetPair in statistics.GetAllPropertyEntityPairs())
			{
				statistics.PropertyCommands.TryGetValue(propertyTargetPair, out List<PropertyCommand> commands);
				statistics.PropertyComments.TryGetValue(propertyTargetPair.Property, out string comment);
				newRows.Add(new PropertyDescriptionRow(propertyTargetPair.Property, propertyTargetPair.EntityType, commandsText(commands), comment));
			}
			// sort list by property value
			foreach (var row in newRows.OrderBy(r => r.PropertyValue))
				rows.Add(row);
			// the first row is current like in the WinForms DataGridView
			if (rows.Count > 0)
				dataGridViewPropertyDescriptions.SelectedIndex = 0;
		}

		private void initBasicMeshInfo()
		{
			if (!sceneFacade.ContainsMesh)
			{
				basicInfoPanel.IsEnabled = false;
				return;
			}

			bool meshIsCutted = (bool)sceneFacade.GetValue(AvailableValue.MeshHasHiddenElements);
			if (meshIsCutted)
				groupBoxBasicInfo.Header += " (after cut)";

			labelNodeCount.Text = "Node count: ...";
			labelElementCount.Text = "Element count: " + sceneFacade.GetValue(AvailableValue.ElementCount);
			labelBeamCount.Text = "(Beam count: " + sceneFacade.GetValue(AvailableValue.BeamCount) + ")";
			labelFaceCount.Text = "Face count: " + sceneFacade.GetValue(AvailableValue.FaceCount);
			labelEdgeCount.Text = "Edge count: " + sceneFacade.GetValue(AvailableValue.EdgeCount);
		}

		private void firstBorderLimitTrackBar_ValueChanged(object sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
		{
			if (histogramViewer == null)
				return; // XAML is being loaded
			if (sceneFacade != null && sceneFacade.ContainsMesh)
			{
				firstBorderLimitLabel.Text = "First border limit: " + (int)firstBorderLimitTrackBar.Value + "°";
				if (firstBorderLimitTrackBar.Value > secondBorderLimitTrackBar.Value)
					secondBorderLimitTrackBar.Value = firstBorderLimitTrackBar.Value;
			}
			histogramViewer.FirstLimit = (int)firstBorderLimitTrackBar.Value;
		}

		private void secondBorderLimitTrackBar_ValueChanged(object sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
		{
			if (histogramViewer == null)
				return; // XAML is being loaded
			if (sceneFacade != null && sceneFacade.ContainsMesh)
			{
				secondBorderLimitLabel.Text = "Second border limit: " + (int)secondBorderLimitTrackBar.Value + "°";
				if (secondBorderLimitTrackBar.Value < firstBorderLimitTrackBar.Value)
					firstBorderLimitTrackBar.Value = secondBorderLimitTrackBar.Value;
			}
			histogramViewer.SecondLimit = (int)secondBorderLimitTrackBar.Value;
		}

		private void buttonApply_Click(object sender, RoutedEventArgs e)
		{
			if (sceneFacade.ContainsMesh)
			{
				MeshStatistics statistics = sceneFacade.GetValue(AvailableValue.MeshStatistics) as MeshStatistics;
				if (statistics != null)
				{
					statistics.SetBorderLimits((float)firstBorderLimitTrackBar.Value, (float)secondBorderLimitTrackBar.Value);
					sceneFacade.PerformAction(AvailableAction.RecreateBuffers);
				}
				using (longOpNotifier.Begin("Updating border limits"))
				{
					var previousCursor = Cursor;
					Cursor = new Cursor(StandardCursorType.Wait);
					sceneFacade.PerformAction(AvailableAction.Refresh);
					Cursor = previousCursor;
				}
			}
		}

		private void buttonClose_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private async void buttonEditPropertyCommands_Click(object sender, RoutedEventArgs e)
		{
			await editPropertyCommands();
		}

		private async void buttonEditComment_Click(object sender, RoutedEventArgs e)
		{
			await editPropertyComment();
		}

		private async void dataGridViewPropertyDescriptions_DoubleClick(object sender, TappedEventArgs e)
		{
			await editPropertyCommands();
		}

		private async void dataGridViewPropertyDescriptions_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				e.Handled = true;
				await editPropertyCommands();
			}
		}

		private void tabControl_SelectedIndexChanged(object sender, SelectionChangedEventArgs e)
		{
			if (initialized && ReferenceEquals(e.Source, tabControl))
				lastSelectedTabIndex = tabControl.SelectedIndex;
		}

		internal async Task editPropertyCommands()
		{
			if (dataGridViewPropertyDescriptions.SelectedItem is not PropertyDescriptionRow selectedRow)
				return;
			MeshStatistics statistics = sceneFacade.GetValue(AvailableValue.MeshStatistics) as MeshStatistics;
			if (statistics == null)
				return;

			PropertyEntityPair propertyTargetPair = new PropertyEntityPair(selectedRow.Property, selectedRow.EntityType);
			statistics.PropertyCommands.TryGetValue(propertyTargetPair, out List<PropertyCommand> commands);

			var propertyCommandForm = new PropertyCommandDialog(propertyTargetPair, commands);
			if (await propertyCommandForm.ShowDialog(this))
			{
				// save command
				if (propertyCommandForm.Commands == null || propertyCommandForm.Commands.Count == 0)
					statistics.PropertyCommands.Remove(propertyTargetPair);
				else
					statistics.PropertyCommands[propertyTargetPair] = propertyCommandForm.Commands;

				string commandString = commandsText(propertyCommandForm.Commands);
				if ((selectedRow.Commands ?? string.Empty) != commandString)
				{
					selectedRow.Commands = commandString;
					// the mesh now has unsaved changes
					sceneFacade.SetValue(AvailableValue.UnsavedChangesInMesh, true);
				}
			}
		}

		internal async Task editPropertyComment()
		{
			if (dataGridViewPropertyDescriptions.SelectedItem is not PropertyDescriptionRow selectedRow)
				return;
			MeshStatistics statistics = sceneFacade.GetValue(AvailableValue.MeshStatistics) as MeshStatistics;
			if (statistics == null)
				return;

			Property property = selectedRow.Property;
			var inputValueForm = new InputValueDialog("Set description of property", "Property " + property + ":", selectedRow.Comment)
			{
				SizeToContent = SizeToContent.Height,
				Width = 500,
			};
			if (await inputValueForm.ShowDialog(this))
			{
				// save comment
				if (string.IsNullOrEmpty(inputValueForm.InputValue))
					statistics.PropertyComments.Remove(property);
				else
					statistics.PropertyComments[property] = inputValueForm.InputValue;

				if ((selectedRow.Comment ?? string.Empty) != inputValueForm.InputValue)
				{
					setAllCommentsOfProperty(property, inputValueForm.InputValue); // update all lines with this property
					// the mesh now has unsaved changes
					sceneFacade.SetValue(AvailableValue.UnsavedChangesInMesh, true);
				}
			}
		}

		private void setAllCommentsOfProperty(Property property, string comment)
		{
			string propertyString = property.ToString();
			foreach (var row in rows)
			{
				if (row.PropertyText == propertyString)
					row.Comment = comment;
			}
		}
	}
}
