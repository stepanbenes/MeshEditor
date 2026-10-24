using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using MeshEditor.CoreInterface;
using MeshEditor.Cuts;

namespace MeshEditor.AvaloniaUI.Dialogs
{
	/// <summary>
	/// Cutting/selecting the mesh by cut planes picked on the mesh or by an expression (port of MeshEditor.WinUI.CutEditorForm).
	/// </summary>
	public partial class CutEditorDialog : Window
	{
		private const string INITIAL_EXPRESSION = @"x >= 0.0 and y >= 0.0";

		private static string savedExpression;
		private static CutTest savedTestMethod;

		/// <summary>
		/// The main window should get the focus (the user picks points on the mesh).
		/// </summary>
		public event EventHandler ParentFormFocusNeeded;

		private readonly LongOpNotifier longOpNotifier;
		private readonly SceneFacade sceneFacade;
		private bool initialized;

		public CutEditorDialog()
			: this(SceneFacade.GetEmptyScene(), new LongOpNotifier())
		{ }

		public CutEditorDialog(SceneFacade sceneFacade, LongOpNotifier longOpNotifier)
		{
			InitializeComponent();
			this.longOpNotifier = longOpNotifier;
			this.sceneFacade = sceneFacade;
			textBoxExpression.Text = INITIAL_EXPRESSION;
			comboBoxAction.SelectedIndex = 0;
			tabControl.SelectedItem = tabPageCuttingPlanes;
			textBoxExpression.LostFocus += textBoxExpression_Validating;

			sceneFacade.CutPlaneDefinitionPointsChanged += sceneFacade_CutPlaneDefinitionPointsChanged;

			updateCutPlaneList(); // load cut planes which the mesh already contains
			comboBoxCutType.ItemsSource = Enum.GetValues(typeof(CutInfo.CutMeshByPlanesType));
			comboBoxCutType.SelectedItem = CutInfo.CutMeshByPlanesType.Intersection;
			initialized = true;
		}

		protected override void OnClosed(EventArgs e)
		{
			sceneFacade.CutPlaneDefinitionPointsChanged -= sceneFacade_CutPlaneDefinitionPointsChanged;
			SceneFacade.EditorMode = EditorMode.Orbit;
			sceneFacade.CutPlanes.Clear(); // hide cut planes
			sceneFacade.PerformAction(AvailableAction.ClearPlaneDefinitionPoints); // includes Refresh
			base.OnClosed(e);
		}

		/// <summary>
		/// Expression is compiled when leaving the text box (Validating event of the WinForms text box).
		/// </summary>
		private async void textBoxExpression_Validating(object sender, RoutedEventArgs e)
		{
			if (textBoxExpression.Text != savedExpression && !await compileExpression(textBoxExpression.Text))
				textBoxExpression.Focus();
		}

		internal async Task<bool> compileExpression(string expression)
		{
			bool ook = false;
			string errorText = string.Empty;
			try
			{
				savedTestMethod = CutTestProvider.ProvideTestFunction(expression);
				savedExpression = expression;
				ook = true;
			}
			catch (RuntimeCompilationException)
			{
				errorText = "Please insert valid expression." + Environment.NewLine + "There were some errors while processing expression.";
			}
			catch (Exception)
			{
				errorText = "Please insert valid expression.";
			}

			if (!ook) // error
			{
				var previousBackground = textBoxExpression.Background;
				textBoxExpression.Background = Brushes.Red;
				await MessageBox.Show(errorText, "Inserted expression is invalid", MessageBoxButtons.OK, MessageBoxIcon.Error, owner: this);
				textBoxExpression.Background = previousBackground;
			}
			return ook;
		}

		private void precomputeMinimalElementRadius()
		{
			var previousCursor = Cursor;
			Cursor = new Cursor(StandardCursorType.Wait);
			_ = sceneFacade.GetValue(AvailableValue.MinimalElementRadius);
			Cursor = previousCursor;
		}

		internal async Task doIt()
		{
			CutInfo cutInfo = new CutInfo();
			switch (comboBoxAction.SelectedIndex)
			{
				case 0:
					cutInfo.Action = CutInfo.ActionType.Cut;
					break;
				case 1:
					cutInfo.Action = CutInfo.ActionType.SelectElements;
					break;
				case 2:
					cutInfo.Action = CutInfo.ActionType.SelectNodes;
					break;
				case 3:
					cutInfo.Action = CutInfo.ActionType.SelectFaces;
					break;
				case 4:
					cutInfo.Action = CutInfo.ActionType.SelectEdges;
					break;
				case 5:
					cutInfo.Action = CutInfo.ActionType.SelectBeams;
					break;
				default:
					throw new NotSupportedException();
			}

			cutInfo.HitDecision = checkBoxFullEntityMatch.IsChecked == true ? CutInfo.ItemHitDecision.AllNodes : CutInfo.ItemHitDecision.SomeNodes;

			if (tabControl.SelectedItem == tabPageExpression) // cut by expression
			{
				// (WinForms validates the expression when the text box loses focus and blocks the click when invalid)
				if (savedExpression == null || savedExpression != textBoxExpression.Text)
				{
					if (!await compileExpression(textBoxExpression.Text))
						return;
				}
				cutInfo.CutTestMethod = savedTestMethod;
			}
			else if (tabControl.SelectedItem == tabPageCuttingPlanes) // cut by cutting planes
			{
				cutInfo.CutTestMethod = null;
				cutInfo.Options = (CutInfo.CutMeshByPlanesType)comboBoxCutType.SelectedItem;
			}
			else
				throw new NotSupportedException();

			doCut(cutInfo);
		}

		private void doCut(CutInfo cutInfo)
		{
			var savedCursor = Cursor;
			Cursor = new Cursor(StandardCursorType.Wait);
			using (longOpNotifier.Begin("Creating cut through mesh"))
			{
				this.sceneFacade.PerformAction(AvailableAction.CutMesh, cutInfo);
			}
			Cursor = savedCursor;
		}

		private async void buttonDoIt_Click(object sender, RoutedEventArgs e)
		{
			await doIt();
		}

		private void buttonClose_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void buttonCreateCutPlane_Click(object sender, RoutedEventArgs e)
		{
			precomputeMinimalElementRadius();
			if (sceneFacade.CutPlaneDefinitionPointsCount < 2) // pick points
			{
				sceneFacade.PerformAction(AvailableAction.ClearPlaneDefinitionPoints);
				planeItems().Add("Select 2 or 3 plane definition points on mesh.");
				refreshPlaneItems();
				listBoxCutPlanes.SelectedItems.Clear();
				buttonInsertNextPoint_Click(null, null); // set the picking mode
			}
			else if (sceneFacade.CutPlaneDefinitionPointsCount == 2)
			{
				createNewCutPlane();
			}
		}

		private void buttonInsertNextPoint_Click(object sender, RoutedEventArgs e)
		{
			SceneFacade.EditorMode = EditorMode.PickCuttingPlanePoint;
			ParentFormFocusNeeded?.Invoke(this, EventArgs.Empty);
			listBoxCutPlanes.SelectedItems.Clear();
			listBoxCutPlanes_SelectedIndexChanged(null, null); // unselect planes
		}

		private void sceneFacade_CutPlaneDefinitionPointsChanged(object sender, EventArgs e)
		{
			if (tabControl.SelectedItem != tabPageCuttingPlanes)
				tabControl.SelectedItem = tabPageCuttingPlanes;

			switch (sceneFacade.CutPlaneDefinitionPointsCount)
			{
				case 0:
					buttonInsertNextPoint.IsVisible = false;
					break;
				case 1:
					buttonInsertNextPoint.IsVisible = true;
					listBoxCutPlanes.SelectedItems.Clear();
					listBoxCutPlanes_SelectedIndexChanged(null, null); // unselect planes
					break;
				case 2:
					buttonInsertNextPoint.IsVisible = true;
					break;
				case 3:
					createNewCutPlane();
					break;
			}
		}

		private void buttonDeleteSelectedPlanes_Click(object sender, RoutedEventArgs e)
		{
			sceneFacade.CutPlanes.RemoveAll(plane => plane.IsSelected);
			sceneFacade.PerformAction(AvailableAction.Refresh);
			updateCutPlaneList();
		}

		private void createNewCutPlane()
		{
			buttonInsertNextPoint.IsVisible = false;
			sceneFacade.PerformAction(AvailableAction.CreateCutPlane);
			updateCutPlaneList();
			listBoxCutPlanes.SelectedIndex = planeItems().Count - 1;
		}

		private System.Collections.Generic.List<string> planeItems()
		{
			return listBoxCutPlanes.ItemsSource as System.Collections.Generic.List<string> ?? new System.Collections.Generic.List<string>();
		}

		private void refreshPlaneItems()
		{
			var items = planeItems().ToList();
			updatingList = true;
			listBoxCutPlanes.ItemsSource = items;
			updatingList = false;
		}

		private bool updatingList;

		private void updateCutPlaneList()
		{
			updatingList = true;
			var items = new System.Collections.Generic.List<string>();
			for (int i = 0; i < sceneFacade.CutPlanes.Count; i++)
			{
				items.Add(sceneFacade.CutPlanes[i].ToString());
				sceneFacade.CutPlanes[i].IsSelected = false;
			}
			listBoxCutPlanes.ItemsSource = items;
			listBoxCutPlanes.SelectedItems.Clear();
			updatingList = false;
			buttonInvertSelectedPlanes.IsVisible = false;
			buttonDeleteSelectedPlanes.IsVisible = false;
			comboBoxCutType.IsVisible = labelCutType.IsVisible = sceneFacade.CutPlanes.Count > 1;
		}

		private void listBoxCutPlanes_SelectedIndexChanged(object sender, SelectionChangedEventArgs e)
		{
			if (!initialized || updatingList && sender != null)
				return;
			var selectedIndices = listBoxCutPlanes.Selection.SelectedIndexes.ToHashSet();
			for (int i = 0; i < sceneFacade.CutPlanes.Count; i++)
				sceneFacade.CutPlanes[i].IsSelected = selectedIndices.Contains(i);
			sceneFacade.PerformAction(AvailableAction.Refresh);
			buttonInvertSelectedPlanes.IsVisible = buttonDeleteSelectedPlanes.IsVisible = selectedIndices.Count > 0;
		}

		private void buttonInvertSelectedPlanes_Click(object sender, RoutedEventArgs e)
		{
			var items = planeItems().ToList();
			var selected = listBoxCutPlanes.Selection.SelectedIndexes.ToList();
			for (int i = 0; i < sceneFacade.CutPlanes.Count; i++)
			{
				if (sceneFacade.CutPlanes[i].IsSelected)
				{
					sceneFacade.CutPlanes[i].Invert();
					items[i] = sceneFacade.CutPlanes[i].ToString();
				}
			}
			updatingList = true;
			listBoxCutPlanes.ItemsSource = items;
			foreach (int index in selected)
				listBoxCutPlanes.Selection.Select(index);
			updatingList = false;
			sceneFacade.PerformAction(AvailableAction.Refresh);
		}

		private void comboBoxAction_SelectedIndexChanged(object sender, SelectionChangedEventArgs e)
		{
			if (buttonDoIt == null)
				return; // XAML is being loaded
			buttonDoIt.Content = comboBoxAction.SelectedIndex == 0 ? "Cut" : "Select";
		}

		private void buttonRestoreMesh_Click(object sender, RoutedEventArgs e)
		{
			buttonRestoreMesh.IsEnabled = false;
			var savedCursor = Cursor;
			Cursor = new Cursor(StandardCursorType.Wait);
			using (longOpNotifier.Begin("Restoring mesh"))
			{
				sceneFacade.PerformAction(AvailableAction.RestoreMesh);
			}
			Cursor = savedCursor;
			buttonRestoreMesh.IsEnabled = true;
		}

		private async void pictureBoxHelp_Click(object sender, PointerPressedEventArgs e)
		{
			string text = "Available variables:\n\tx y z\nAvailable relational operators:\n\t== != > < >= <=\nAvailable operations or functions:\n\t+ - * / and or\n\tpow(base,exp) log(base,arg) min(a,b) max(a,b) \n\tsin() cos() tan() abs() sqrt() \n\tasin() acos() atan() sinh() cosh() tanh()";
			await MessageBox.Show(text.Replace("\n", Environment.NewLine), "Expression input help", MessageBoxButtons.OK, MessageBoxIcon.Information, owner: this);
		}
	}
}
