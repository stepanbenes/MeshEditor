using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

using MeshEditor.AvaloniaUI.Controls;
using MeshEditor.AvaloniaUI.Dialogs;
using MeshEditor.AvaloniaUI.Rendering;
using MeshEditor.Common;
using MeshEditor.CoreInterface;
using MeshEditor.Data;
using SixLabors.ImageSharp; // Image.Save(path) extension
using Image = Avalonia.Controls.Image;
using MeshEditor.Graphics;
using MeshEditor.WinUI;

using DrawingRectangle = System.Drawing.Rectangle;

namespace MeshEditor.AvaloniaUI
{
	public enum LayoutMode
	{
		None = 0,
		Preprocessor,
		Postprocessor
	}

	/// <summary>
	/// Main window of the application (port of MeshEditor.WinUI.MainForm).
	/// </summary>
	public partial class MainWindow : Window
	{
		/// <summary>
		/// Same values as System.Windows.Forms.FormWindowState so that config.json stays compatible with the WinForms UI.
		/// </summary>
		public enum FormWindowState
		{
			Normal = 0,
			Minimized = 1,
			Maximized = 2,
		}

		class MainWindowSettings
		{
			public int Width { get; set; } = 800;
			public int Height { get; set; } = 600;
			public FormWindowState State { get; set; } = FormWindowState.Maximized;
			public int PositionLeft { get; set; } = 100;
			public int PositionTop { get; set; } = 100;
			public string LastLoadedMesh { get; set; }
			public string LastCheckedUpdateVersion { get; set; }
		}

		#region Fields, Constructor

		public const int PANEL_MINSIZE = 30;

		private OpenGLViewport activeControl;
		private readonly List<OpenGLViewport> openGLControls;
		private LongOpNotifier longOpNotifier;

		private readonly string[] arguments;

		private int takeScreenshotLastFilterIndex;
		private string takeScreenshotLastFilename;

		private LayoutMode layoutMode;

		private readonly MainWindowSettings mainWindowSettings;

		private readonly Dictionary<LongOpNotifier.Token, ProgressViewDialog> progressViewForms = new Dictionary<LongOpNotifier.Token, ProgressViewDialog>();

		private bool closeConfirmed;

		public MainWindow()
			: this(Array.Empty<string>())
		{ }

		public MainWindow(string[] args)
		{
			ConfigurationManager.LoadConfiguration();

			InitializeComponent();

			// the hidden WinForms menu item only provides this shortcut; hot keys of invisible menu items don't fire in Avalonia
			KeyBindings.Add(new KeyBinding { Gesture = new KeyGesture(Key.F6, KeyModifiers.Shift), Command = new DelegateCommand(() => invisibleSelectEntitiesByPropertyAddToolStripMenuItem_Click(this, null)) });

			// access keys in the drop-down of the tool strip (WinForms ToolStripDropDownButton supports them)
			FlyoutAccessKeys.Attach((MenuFlyout)propertyColorsTtoolStripDropDownButton.Flyout);

			SceneFacade.EditorModeChanged += editorModeChanged;
			SceneFacade.ShowError += SceneFacade_ShowError;

			this.openGLControls = new List<OpenGLViewport>();
			this.arguments = args;
			this.longOpNotifier = null;
			initLongOpNotifier();

			// make sure an OpenGL context exists before the settings are applied
			OffscreenGlContext.Root.MakeCurrent();

			// load applications settings accessed by Options dialog
			SceneSettings.LoadFromConfigurationFile();

			PropertyColorProvider.LoadPropertyColors();

			// load window state settings
			mainWindowSettings = loadMainWindowSettings();

			setPreprocessorLayoutMode();

			initializeMainOpenGlControl(contentContainer);

			updateCaption();
			editorModeChanged(null, null);

			if (UpdateChecker.IsUpdateServiceAvailableForThisPlatform)
			{
				_ = checkForUpdatesSilently(); // don't await
			}
		}

		#endregion

		#region Properties

		public LayoutMode LayoutMode => layoutMode;

		public LongOpNotifier LongOpNotifier => longOpNotifier;

		/// <summary>
		/// Viewport that receives the commands (exposed for UI tests).
		/// </summary>
		internal OpenGLViewport ActiveViewport => activeControl;

		/// <summary>
		/// All viewports (exposed for UI tests).
		/// </summary>
		internal IReadOnlyList<OpenGLViewport> Viewports => openGLControls;

		#endregion

		#region Overrides

		protected override async void OnOpened(EventArgs e)
		{
			base.OnOpened(e);

			activateControl(activeControl);

			if (arguments != null && arguments.Length > 0) // load file in command file arguments
			{
				await openFiles(arguments[0]);
			}
			else if (File.Exists(mainWindowSettings.LastLoadedMesh)) // load last loaded file (if exists)
			{
				await openFiles(mainWindowSettings.LastLoadedMesh);
			}
		}

		protected override async void OnClosing(WindowClosingEventArgs e)
		{
			base.OnClosing(e);
			if (closeConfirmed || e.Cancel)
				return;

			// asking the user is asynchronous in Avalonia, so cancel now and close again when confirmed
			e.Cancel = true;

			List<OpenGLViewport> controls = new List<OpenGLViewport>(openGLControls.Count);
			controls.Add(activeControl);
			foreach (OpenGLViewport c in openGLControls)
				if (c != activeControl)
					controls.Add(c);
			if (!await askToSaveChanges(controls, true))
			{
				closeConfirmed = true;
				Close();
			}
		}

		protected override void OnClosed(EventArgs e)
		{
			// save settings from Options dialog
			SceneSettings.SaveToConfigurationFile();

			// save window state settings
			saveMainWindowSettings();

			ConfigurationManager.Save();

			// dispose all meshes
			foreach (OpenGLViewport c in openGLControls)
				c.DisposeScene();

			SceneFacade.EditorModeChanged -= editorModeChanged;
			SceneFacade.ShowError -= SceneFacade_ShowError;

			base.OnClosed(e);
		}

		#endregion

		#region Split window feature

		private Border contentContainer;

		private void splitWindowToolStripMenuItem_Click(object sender, RoutedEventArgs e)
		{
			if (activeControl == null)
				return;

			OpenGLViewport controlToSplit = activeControl;
			Border parent = controlToSplit.MyContainer;

			if (Math.Max(parent.Bounds.Width, parent.Bounds.Height) < PANEL_MINSIZE * 2) // too small, nothing to split
				return;

			var orientation = parent.Bounds.Width < parent.Bounds.Height ? Orientation.Horizontal : Orientation.Vertical;
			ViewSplitContainer newContainer = new ViewSplitContainer(orientation);
			newContainer.SplitterMoved += splitContainer_SplitterMoved;

			parent.Child = null;
			newContainer.Panel1.Child = controlToSplit;
			controlToSplit.MyContainer = newContainer.Panel1;

			OpenGLViewport newOpenGLControl = new OpenGLViewport(controlToSplit.SceneFacade);
			newContainer.Panel2.Child = newOpenGLControl;
			newOpenGLControl.MyContainer = newContainer.Panel2;

			parent.Child = newContainer;

			registerNewControl(newOpenGLControl);
			activateControl(controlToSplit); // focus the original control
		}

		private async void splitContainer_SplitterMoved(object sender, EventArgs e)
		{
			ViewSplitContainer sc = (ViewSplitContainer)sender;

			if (sc.SplitterDistance < PANEL_MINSIZE)
			{
				if (await removePanel(sc.Panel1))
					extendPanel(sc.Panel2);
			}
			else if (sc.SplitterDistance > sc.Extent - ViewSplitContainer.SplitterSize - PANEL_MINSIZE)
			{
				if (await removePanel(sc.Panel2))
					extendPanel(sc.Panel1);
			}
		}

		/// <summary>
		/// Recursively walks the nested containers of the panel and removes all OpenGL viewports.
		/// </summary>
		private async Task<bool> removePanel(Border panel)
		{
			List<OpenGLViewport> controls = new List<OpenGLViewport>();
			getAllControlsInPanel(panel, controls);
			if (await askToSaveChanges(controls, true))
				return false;
			removeAllControlsInPanel(panel);
			return true;
		}

		private void getAllControlsInPanel(Border panel, List<OpenGLViewport> controls)
		{
			Control content = panel.Child;
			if (content is OpenGLViewport c)
			{
				controls.Add(c);
			}
			else
			{
				ViewSplitContainer nested = (ViewSplitContainer)content;
				getAllControlsInPanel(nested.Panel1, controls);
				getAllControlsInPanel(nested.Panel2, controls);
			}
		}

		private void removeAllControlsInPanel(Border panel)
		{
			Control content = panel.Child;
			if (content is OpenGLViewport toRemove)
			{
				removeControlFromPanel(toRemove);
			}
			else
			{
				ViewSplitContainer nested = (ViewSplitContainer)content;
				removeAllControlsInPanel(nested.Panel1);
				removeAllControlsInPanel(nested.Panel2);
			}
			panel.Child = null;
		}

		private void removeControlFromPanel(OpenGLViewport toRemove)
		{
			unregisterControl(toRemove);
			toRemove.MyContainer.Child = null;
		}

		private void extendPanel(Border panel)
		{
			Control content = panel.Child;
			var splitContainer = (ViewSplitContainer)panel.Parent;
			var parent = (Border)splitContainer.Parent;

			panel.Child = null;
			parent.Child = content;
			if (content is OpenGLViewport viewport)
				viewport.MyContainer = parent;

			setFocusToRemainedControl(content);
		}

		private void setFocusToRemainedControl(Control control)
		{
			if (control is OpenGLViewport viewport)
				activateControl(viewport);
			else
				setFocusToRemainedControl(((ViewSplitContainer)control).Panel1.Child);
		}

		#endregion

		#region UI command handlers (menu items)

		private void editorModeChanged(object sender, EventArgs e)
		{
			if (!Dispatcher.UIThread.CheckAccess())
			{
				Dispatcher.UIThread.Post(() => editorModeChanged(sender, e));
				return;
			}

			EditorMode mode = SceneFacade.EditorMode;
			setChecked(toolStripButtonOrbit, mode == EditorMode.Orbit);
			setChecked(toolStripButtonPan, mode == EditorMode.Pan);
			setChecked(toolStripButtonLookAround, mode == EditorMode.LookAround);
			setChecked(toolStripButtonZoomWindow, mode == EditorMode.ZoomWindow);
			setChecked(toolStripButtonSelectElements, mode == EditorMode.SelectElements);
			setChecked(toolStripButtonSelectNodes, mode == EditorMode.SelectNodes);
			setChecked(toolStripButtonSelectFaces, mode == EditorMode.SelectFaces);
			setChecked(toolStripButtonSelectEdges, mode == EditorMode.SelectEdges);
			setChecked(toolStripButtonSelectBeams, mode == EditorMode.SelectBeams);

			activeControl?.SceneFacade.SetRenderModeAccordingToEditorMode();
		}

		private static void setChecked(Button toolButton, bool isChecked) => toolButton.Classes.Set("checked", isChecked);

		private static bool isChecked(Button toolButton) => toolButton.Classes.Contains("checked");

		private void SceneFacade_ShowError(object sender, ShowErrorEventArgs ea)
		{
			Dispatcher.UIThread.Post(() => _ = MessageBox.Show(ea.Message, ea.Caption, MessageBoxButtons.OK, MessageBoxIcon.Error));
		}

		private void meshNeedRefreshHandler(object sender, MeshNeedRefreshEventArgs ea)
		{
			foreach (OpenGLViewport c in openGLControls)
			{
				if (ea.SkipSender && ReferenceEquals(sender, c))
					continue;
				if (c.SceneFacade.ContainsMeshWithIdentifier(ea.MeshIdentifier))
					c.Invalidate();
			}
		}

		private void toolsToolStripMenuItem_DropDownOpening(object sender, RoutedEventArgs e)
		{
			restoreCuttedItemsToolStripMenuItem.IsEnabled = (bool)activeControl.SceneFacade.GetValue(AvailableValue.MeshHasHiddenElements);
			closeActiveWindowToolStripMenuItem.IsEnabled = activeWindowCanBeClosed();
			bool containsMesh = activeControl.SceneFacade.ContainsMesh;
			listOfSelectedItemsToolStripMenuItem.IsEnabled = showHideElementsToolStripMenuItem.IsEnabled = cutsToolStripMenuItem.IsEnabled = meshInfoToolStripMenuItem.IsEnabled = containsMesh;
		}

		private async void openToolStripMenuItem_Click(object sender, RoutedEventArgs e)
		{
			// ask to save the opened mesh first
			if (activeControl.SceneFacade.ContainsMesh)
			{
				List<OpenGLViewport> controls = new List<OpenGLViewport> { activeControl };
				if (await askToSaveChanges(controls, true)) // cancelled, don't open anything
					return;
			}

			var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
			{
				AllowMultiple = true,
				FileTypeFilter = FileDialogFilter.Parse(SceneFacade.InputFileFormatFilter),
			});
			string[] fileNames = files.Select(f => f.TryGetLocalPath()).Where(p => p != null).ToArray();
			if (fileNames.Length > 0)
			{
				// TODO: close CutEditorForm and ShowHideElementsForm here once they are ported
				await openFiles(fileNames);
			}
		}

		/// <summary>
		/// Opens mesh file(s) in the active viewport (also used by UI tests).
		/// </summary>
		internal async Task openFiles(params string[] fileNames)
		{
			if (fileNames.Length == 1 && fileNames[0].EndsWith(SceneFacade.SolutionFileExtension)) // check if file to open is solution file
			{
				// TODO: port PostprocessViewControl (MeshEditor.DataVisualizer)
				await NotPorted.Show("Opening a solution (postprocessor)", "PostprocessViewControl");
			}
			else
			{
				setPreprocessorLayoutMode();
				activeControl.LoadFiles(fileNames);
				await activeControl.WaitForIOActionAsync();
			}
		}

		private async void saveToolStripMenuItem_Click(object sender, RoutedEventArgs e)
		{
			if (!activeControl.SceneFacade.ContainsMesh)
				return; // nothing to save

			await saveMeshChooseFile(activeControl);
		}

		private async Task<bool> saveMeshChooseFile(OpenGLViewport control)
		{
			MessageBoxResult withoutHiddenResult = await findoutIfSaveWithoutHiddenElements(control);
			if (withoutHiddenResult == MessageBoxResult.Cancel)
				return false;
			bool saveWithoutHiddenElements = withoutHiddenResult == MessageBoxResult.Yes;

			var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
			{
				SuggestedFileName = Utilities.Functions.MakeTextValidFilename(control.SceneFacade.MeshName),
				FileTypeChoices = FileDialogFilter.Parse(SceneFacade.OutputFileFormatFilter),
				ShowOverwritePrompt = true,
			});
			string fileName = file?.TryGetLocalPath();
			if (fileName == null)
				return false;

			control.SaveToFile(fileName, saveWithoutHiddenElements);
			await control.WaitForIOActionAsync();
			return true;
		}

		private async Task<MessageBoxResult> findoutIfSaveWithoutHiddenElements(OpenGLViewport control)
		{
			bool hasCuttedElements = (bool)control.SceneFacade.GetValue(AvailableValue.MeshHasHiddenElements);
			if (!hasCuttedElements)
				return MessageBoxResult.No;
			string meshName = control.SceneFacade.MeshName;
			return await MessageBox.Show("Save mesh '" + meshName + "' without hidden elements?" + Environment.NewLine + "(Click \"Yes\" to save mesh as it is or \"No\" to save entire mesh in original form.)", "Mesh has some hidden elements", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, defaultButtonIndex: 1);
		}

		private void exitToolStripMenuItem_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}

		private async void settingsToolStripMenuItem_Click(object sender, RoutedEventArgs e)
		{
			List<SceneFacade> scenes = new List<SceneFacade>();
			foreach (OpenGLViewport c in openGLControls)
				scenes.Add(c.SceneFacade);

			var sf = new SettingsDialog(scenes);
			await sf.ShowDialog(this);
		}

		private async void meshInfoToolStripMenuItem_Click(object sender, RoutedEventArgs e)
		{
			var sf = new MeshInfoDialog(activeControl.SceneFacade, this.longOpNotifier);
			await sf.ShowDialog(this);
		}

		private void contextMenuStrip_Closed(object sender, RoutedEventArgs e)
		{
			activeControl.SceneFacade.MakeToComputeVisibleNodes();
		}

		private async void cutMeshToolStripMenuItem_Click(object sender, RoutedEventArgs e)
		{
			// TODO: port CutEditorForm
			await NotPorted.Show("Cut / Select area", "CutEditorForm");
		}

		private async void showHideElementsToolStripMenuItem_Click(object sender, RoutedEventArgs e)
		{
			if (activeControl.SceneFacade.ContainsMesh)
			{
				// TODO: port ShowHideElementsForm
				await NotPorted.Show("Show / Hide elements", "ShowHideElementsForm");
			}
		}

		private void selectAllToolStripMenuItem_Click(object sender, RoutedEventArgs e)
		{
			activeControl.SceneFacade.PerformAction(AvailableAction.SelectAllItems);
		}

		private void unselectAllToolStripMenuItem_Click(object sender, RoutedEventArgs e)
		{
			activeControl.SceneFacade.PerformAction(AvailableAction.UnselectAllItems);
		}

		private void invertSelectionToolStripMenuItem_Click(object sender, RoutedEventArgs e)
		{
			activeControl.SceneFacade.PerformAction(AvailableAction.InvertSelection);
		}

		private void deleteSelectedElementsToolStripMenuItem_Click(object sender, RoutedEventArgs e)
		{
			using (longOpNotifier.Begin("Deleting selected elements"))
			{
				activeControl.SceneFacade.PerformAction(AvailableAction.DeleteSelectedElements);
			}
		}

		private void restoreMeshToolStripMenuItem_Click(object sender, RoutedEventArgs e)
		{
			using (longOpNotifier.Begin("Restoring mesh"))
			{
				activeControl.SceneFacade.PerformAction(AvailableAction.RestoreMesh);
			}
		}

		private void splitActiveWindowToolStripMenuItem_Click(object sender, RoutedEventArgs e)
		{
			splitWindowToolStripMenuItem_Click(sender, e);
		}

		private void selectToolChosen(object sender, RoutedEventArgs e)
		{
			uncheckAllSelectToolMenuItems();

			if (sender is MenuItem { Tag: EditorMode contextMenuMode }) // item of the viewport context menu
			{
				SceneFacade.EditorMode = contextMenuMode;
				return;
			}

			if (sender == elementsToolStripMenuItem1 || sender == contextMenuItems?.Elements)
				SceneFacade.EditorMode = EditorMode.SelectElements;
			else if (sender == nodesToolStripMenuItem1 || sender == contextMenuItems?.Nodes)
				SceneFacade.EditorMode = EditorMode.SelectNodes;
			else if (sender == selectFacesToolStripMenuItem || sender == contextMenuItems?.Faces)
				SceneFacade.EditorMode = EditorMode.SelectFaces;
			else if (sender == edgesToolStripMenuItem1 || sender == contextMenuItems?.Edges)
				SceneFacade.EditorMode = EditorMode.SelectEdges;
			else if (sender == beamsToolStripMenuItem1 || sender == contextMenuItems?.Beams)
				SceneFacade.EditorMode = EditorMode.SelectBeams;
		}

		private void uncheckAllSelectToolMenuItems()
		{
			elementsToolStripMenuItem1.IsChecked = nodesToolStripMenuItem1.IsChecked = selectFacesToolStripMenuItem.IsChecked = edgesToolStripMenuItem1.IsChecked = beamsToolStripMenuItem1.IsChecked = false;
			contextMenuItems?.UncheckSelectTools();
		}

		private void cameraToolChosen(object sender, RoutedEventArgs e)
		{
			uncheckAllCameraToolMenuItems();

			if (sender is MenuItem { Tag: EditorMode contextMenuMode }) // item of the viewport context menu
			{
				SceneFacade.EditorMode = contextMenuMode;
				return;
			}

			if (sender == orbitToolStripMenuItem1 || sender == contextMenuItems?.Orbit)
				SceneFacade.EditorMode = EditorMode.Orbit;
			else if (sender == panToolStripMenuItem1 || sender == contextMenuItems?.Pan)
				SceneFacade.EditorMode = EditorMode.Pan;
			else if (sender == lookAroundToolStripMenuItem1 || sender == contextMenuItems?.LookAround)
				SceneFacade.EditorMode = EditorMode.LookAround;
			else if (sender == zoomWindowToolStripMenuItem || sender == contextMenuItems?.ZoomWindow)
				SceneFacade.EditorMode = EditorMode.ZoomWindow;
		}

		private void uncheckAllCameraToolMenuItems()
		{
			orbitToolStripMenuItem1.IsChecked = panToolStripMenuItem1.IsChecked = lookAroundToolStripMenuItem1.IsChecked = zoomWindowToolStripMenuItem.IsChecked = false;
			contextMenuItems?.UncheckCameraTools();
		}

		private void selectToolStripMenuItem_DropDownOpening(object sender, RoutedEventArgs e)
		{
			uncheckAllSelectToolMenuItems();
			switch (SceneFacade.EditorMode)
			{
				case EditorMode.SelectElements:
					elementsToolStripMenuItem1.IsChecked = true;
					if (contextMenuItems != null) contextMenuItems.Elements.IsChecked = true;
					break;
				case EditorMode.SelectNodes:
					nodesToolStripMenuItem1.IsChecked = true;
					if (contextMenuItems != null) contextMenuItems.Nodes.IsChecked = true;
					break;
				case EditorMode.SelectFaces:
					selectFacesToolStripMenuItem.IsChecked = true;
					if (contextMenuItems != null) contextMenuItems.Faces.IsChecked = true;
					break;
				case EditorMode.SelectEdges:
					edgesToolStripMenuItem1.IsChecked = true;
					if (contextMenuItems != null) contextMenuItems.Edges.IsChecked = true;
					break;
				case EditorMode.SelectBeams:
					beamsToolStripMenuItem1.IsChecked = true;
					if (contextMenuItems != null) contextMenuItems.Beams.IsChecked = true;
					break;
			}
		}

		private void cameraToolStripMenuItem_DropDownOpened(object sender, RoutedEventArgs e)
		{
			uncheckAllCameraToolMenuItems();
			switch (SceneFacade.EditorMode)
			{
				case EditorMode.Orbit:
					orbitToolStripMenuItem1.IsChecked = true;
					if (contextMenuItems != null) contextMenuItems.Orbit.IsChecked = true;
					break;
				case EditorMode.Pan:
					panToolStripMenuItem1.IsChecked = true;
					if (contextMenuItems != null) contextMenuItems.Pan.IsChecked = true;
					break;
				case EditorMode.LookAround:
					lookAroundToolStripMenuItem1.IsChecked = true;
					if (contextMenuItems != null) contextMenuItems.LookAround.IsChecked = true;
					break;
				case EditorMode.ZoomWindow:
					zoomWindowToolStripMenuItem.IsChecked = true;
					if (contextMenuItems != null) contextMenuItems.ZoomWindow.IsChecked = true;
					break;
			}

			if (contextMenuItems != null)
				contextMenuItems.CloseWindow.IsVisible = activeWindowCanBeClosed();
		}

		private void viewToolStripMenuItem_DropDownOpened(object sender, RoutedEventArgs e)
		{
			axesToolStripMenuItem.IsChecked = (bool)activeControl.SceneFacade.GetValue(AvailableValue.DrawAxes);
			axisArrowsToolStripMenuItem.IsChecked = (bool)activeControl.SceneFacade.GetValue(AvailableValue.DrawAxisArrows);
			beamsToolStripMenuItem2.IsChecked = (bool)activeControl.SceneFacade.GetValue(AvailableValue.DrawBeams);
			nodeNumbersToolStripMenuItem.IsChecked = (bool)activeControl.SceneFacade.GetValue(AvailableValue.DrawNodeNumbers);
			elementNumbersToolStripMenuItem.IsChecked = (bool)activeControl.SceneFacade.GetValue(AvailableValue.DrawElementNumbers);
			beamNumbersToolStripMenuItem.IsChecked = (bool)activeControl.SceneFacade.GetValue(AvailableValue.DrawBeamNumbers);
			intermediateNodesToolStripMenuItem.IsChecked = (bool)activeControl.SceneFacade.GetValue(AvailableValue.DrawQuadraticNodes);

			signalNodeToolStripMenuItem.IsChecked = (bool)activeControl.SceneFacade.GetValue(AvailableValue.NodeSignalIsSet);
			signalElementToolStripMenuItem.IsChecked = (bool)activeControl.SceneFacade.GetValue(AvailableValue.ElementSignalIsSet);

			signalNodeToolStripMenuItem.IsEnabled = signalElementToolStripMenuItem.IsEnabled = activeControl.SceneFacade.ContainsMesh;
		}

		private async void setPropertyOfSelectedItemsToolStripMenuItem_Click(object sender, RoutedEventArgs e)
		{
			await activeControl.SetPropertyOfSelectedItems();
		}

		private void selectIncidingItemsToolStripMenuItem_Click(object sender, RoutedEventArgs e)
		{
			activeControl.SceneFacade.PerformAction(AvailableAction.SelectIncidingItems);
		}

		private async void selectItemsByPropertyToolStripMenuItem_Click(object sender, RoutedEventArgs e)
		{
			await activeControl.SelectItemsWithProperty(getNameOfItemsToSelectAcordingToEditorMode() ?? "elements");
		}

		private async void invisibleSelectEntitiesByPropertyAddToolStripMenuItem_Click(object sender, RoutedEventArgs e)
		{
			await activeControl.SelectItemsWithProperty(getNameOfItemsToSelectAcordingToEditorMode() ?? "elements", addToSelection: true);
		}

		private sealed class DelegateCommand : System.Windows.Input.ICommand
		{
			private readonly Action action;
			public DelegateCommand(Action action) => this.action = action;
			public event EventHandler CanExecuteChanged { add { } remove { } }
			public bool CanExecute(object parameter) => true;
			public void Execute(object parameter) => action();
		}

		private void cameraStandardViewToolStripMenuItem_Click(object sender, RoutedEventArgs e)
		{
			CameraView view;
			if (sender == frontToolStripMenuItem)
				view = CameraView.Front;
			else if (sender == backToolStripMenuItem)
				view = CameraView.Back;
			else if (sender == leftToolStripMenuItem)
				view = CameraView.Left;
			else if (sender == rightToolStripMenuItem)
				view = CameraView.Right;
			else if (sender == topToolStripMenuItem)
				view = CameraView.Top;
			else if (sender == bottomToolStripMenuItem)
				view = CameraView.Bottom;
			else if (sender == isoToolStripMenuItem)
				view = CameraView.Iso;
			else
				return;
			activeControl.SceneFacade.PerformAction(AvailableAction.CameraStandardView, view);
		}

		private void editToolStripMenuItem_DropDownOpening(object sender, RoutedEventArgs e)
		{
			const string prefix = "Select ";
			const string suffixInciding = " inciding with selected faces";
			const string suffixByProperty = " by property";
			string selectIncidingName, selectAllName;

			string nameOfItemsToSelect = getNameOfItemsToSelectAcordingToEditorMode() ?? "elements";

			switch (SceneFacade.EditorMode)
			{
				case EditorMode.SelectElements:
				case EditorMode.SelectEdges:
				case EditorMode.SelectNodes:
					selectIncidingItemsToolStripMenuItem.IsEnabled = true;
					selectIncidingName = selectAllName = nameOfItemsToSelect;
					break;
				default:
					selectIncidingItemsToolStripMenuItem.IsEnabled = false;
					selectIncidingName = "items";
					selectAllName = nameOfItemsToSelect;
					break;
			}

			selectIncidingItemsToolStripMenuItem.Header = prefix + selectIncidingName + suffixInciding;
			selectAllToolStripMenuItem.Header = "Select all " + selectAllName;
			selectItemsByPropertyToolStripMenuItem.Header = prefix + selectAllName + suffixByProperty;
			takeScreenshotToolStripMenuItem.IsEnabled = activeControl.SceneFacade.ContainsMesh;
		}

		private void renderMode_item_click(object sender, RoutedEventArgs e)
		{
			RenderMode mode = RenderMode.None;
			setChecked(toolStripButtonPoints, false);
			setChecked(toolStripButtonBorderLines, false);
			setChecked(toolStripButtonAllLines, false);
			setChecked(toolStripButtonFaces, false);
			if (sender == pointsToolStripMenuItem)
			{
				mode = RenderMode.Points;
				setChecked(toolStripButtonPoints, true);
			}
			else if (sender == borderLinesToolStripMenuItem)
			{
				mode = RenderMode.BorderLines;
				setChecked(toolStripButtonBorderLines, true);
			}
			else if (sender == allLinesToolStripMenuItem)
			{
				mode = RenderMode.AllLines;
				setChecked(toolStripButtonAllLines, true);
			}
			else if (sender == allLinesPointsToolStripMenuItem)
			{
				mode = RenderMode.LinesPoints;
				setChecked(toolStripButtonAllLines, true);
				setChecked(toolStripButtonPoints, true);
			}
			else if (sender == facesRenderToolStripMenuItem)
			{
				mode = RenderMode.Faces;
				setChecked(toolStripButtonFaces, true);
			}
			else if (sender == facesBorderLinesToolStripMenuItem)
			{
				mode = RenderMode.FacesBorder;
				setChecked(toolStripButtonFaces, true);
				setChecked(toolStripButtonBorderLines, true);
			}
			else if (sender == facesAllLinesToolStripMenuItem)
			{
				mode = RenderMode.FacesLines;
				setChecked(toolStripButtonFaces, true);
				setChecked(toolStripButtonAllLines, true);
			}
			else if (sender == facesAllLinesPointsToolStripMenuItem)
			{
				mode = RenderMode.FacesLinesPoints;
				setChecked(toolStripButtonFaces, true);
				setChecked(toolStripButtonAllLines, true);
				setChecked(toolStripButtonPoints, true);
			}
			activeControl.SceneFacade.SetValue(AvailableValue.RenderMode, mode);
			updateRenderModeButtons();
		}

		private void propertyColors_DropDownOpening(object sender, RoutedEventArgs e)
		{
			updateColorModeButtons();
		}

		private void propertyColorsFlyout_Opening(object sender, EventArgs e)
		{
			updateColorModeButtons();
		}

		private void updateColorModeButtons()
		{
			object parameter = activeControl.SceneFacade.GetValue(AvailableValue.ColorMode);
			if (parameter == null)
				return;
			PropertyColorsMode mode = (PropertyColorsMode)parameter;
			elementPropertyColorsToolStripMenuItem.IsChecked = elementPropertyColorsToolStripMenuItem2.IsChecked = (mode & PropertyColorsMode.Elements) != 0;
			nodePropertyColorsToolStripMenuItem.IsChecked = nodePropertyColorsToolStripMenuItem2.IsChecked = (mode & PropertyColorsMode.Nodes) != 0;
			facePropertyColorsToolStripMenuItem.IsChecked = facesPropertyColorsToolStripMenuItem2.IsChecked = (mode & PropertyColorsMode.Faces) != 0;
			edgePropertyColorsToolStripMenuItem.IsChecked = edgesPropertyColorsToolStripMenuItem2.IsChecked = (mode & PropertyColorsMode.Edges) != 0;
			beamPropertyColorsToolStripMenuItem.IsChecked = beamsPropertyColorsToolStripMenuItem2.IsChecked = (mode & PropertyColorsMode.Beams) != 0;
		}

		private void propertyColorsToolStripMenuItem_Click(object sender, RoutedEventArgs e)
		{
			object parameter = activeControl.SceneFacade.GetValue(AvailableValue.ColorMode);
			PropertyColorsMode mode = PropertyColorsMode.None;
			if (parameter != null)
				mode = (PropertyColorsMode)parameter;

			if (sender == elementPropertyColorsToolStripMenuItem || sender == elementPropertyColorsToolStripMenuItem2)
			{
				mode ^= PropertyColorsMode.Elements;
				mode &= ~PropertyColorsMode.Faces; // turn off face colors
			}
			else if (sender == facePropertyColorsToolStripMenuItem || sender == facesPropertyColorsToolStripMenuItem2)
			{
				mode ^= PropertyColorsMode.Faces;
				mode &= ~PropertyColorsMode.Elements; // turn off element colors
			}
			else if (sender == nodePropertyColorsToolStripMenuItem || sender == nodePropertyColorsToolStripMenuItem2)
				mode ^= PropertyColorsMode.Nodes;
			else if (sender == edgePropertyColorsToolStripMenuItem || sender == edgesPropertyColorsToolStripMenuItem2)
				mode ^= PropertyColorsMode.Edges;
			else if (sender == beamPropertyColorsToolStripMenuItem || sender == beamsPropertyColorsToolStripMenuItem2)
				mode ^= PropertyColorsMode.Beams;

			activeControl.SceneFacade.SetValue(AvailableValue.ColorMode, mode);
			activeControl.SceneFacade.PerformAction(AvailableAction.UpdateColorBuffers);
			updateColorModeButtons();
		}

		private void midsideNodesToolStripMenuItem_Click(object sender, RoutedEventArgs e)
		{
			bool value = !(bool)activeControl.SceneFacade.GetValue(AvailableValue.DrawQuadraticNodes);
			activeControl.SceneFacade.SetValue(AvailableValue.DrawQuadraticNodes, value);
			intermediateNodesToolStripMenuItem.IsChecked = value;
		}

		private void zoomToFitToolStripMenuItem_Click(object sender, RoutedEventArgs e)
		{
			activeControl.SceneFacade.PerformAction(AvailableAction.ZoomToFit);
		}

		private async void aboutToolStripMenuItem1_Click(object sender, RoutedEventArgs e)
		{
			var dialog = new AboutDialog();
			await dialog.ShowDialog(this);
		}

		private async void closeActiveMeshToolStripMenuItem_Click(object sender, RoutedEventArgs e)
		{
			if (activeControl.SceneFacade.ContainsMesh)
			{
				List<OpenGLViewport> controls = new List<OpenGLViewport> { activeControl };
				if (await askToSaveChanges(controls, true))
					return;
				// -----------------------------------
				activeControl.ClearScene();
				activeControl.SceneFacade.PerformAction(AvailableAction.Refresh);
				// -----------------------------------
				// mainly to save memory, collect garbage afterwards
				GC.Collect();
			}

			await removeActiveWindow();
			updateCaption();
			updateStatus();
		}

		private async Task<bool> removeActiveWindow()
		{
			if (activeControl.MyContainer.Parent is not ViewSplitContainer container)
				return false;
			Border toRemove = activeControl.MyContainer;
			Border toExtend = (container.Panel1 == activeControl.MyContainer) ? container.Panel2 : container.Panel1;
			if (await removePanel(toRemove))
			{
				extendPanel(toExtend);
				return true;
			}
			return false;
		}

		private void refreshToolStripMenuItem_Click(object sender, RoutedEventArgs e)
		{
			activeControl.SceneFacade.PerformAction(AvailableAction.Refresh);
		}

		private async void listOfSelectedItemsToolStripMenuItem_Click(object sender, RoutedEventArgs e)
		{
			if (!activeControl.SceneFacade.ContainsMesh)
				return;
			ListOfSelectedItemsDialog form;
			using (longOpNotifier.Begin("Creating list of selected items"))
			{
				form = new ListOfSelectedItemsDialog(activeControl.SceneFacade);
			}
			await form.ShowDialog(this);
		}

		private void axesToolStripMenuItem_Click(object sender, RoutedEventArgs e)
		{
			bool value = !(bool)activeControl.SceneFacade.GetValue(AvailableValue.DrawAxes);
			activeControl.SceneFacade.SetValue(AvailableValue.DrawAxes, value);
			axesToolStripMenuItem.IsChecked = value;
		}

		private void axisArrowsToolStripMenuItem_Click(object sender, RoutedEventArgs e)
		{
			bool value = !(bool)activeControl.SceneFacade.GetValue(AvailableValue.DrawAxisArrows);
			activeControl.SceneFacade.SetValue(AvailableValue.DrawAxisArrows, value);
			axisArrowsToolStripMenuItem.IsChecked = value;
		}

		private void beamsToolStripMenuItem2_Click(object sender, RoutedEventArgs e)
		{
			bool value = !(bool)activeControl.SceneFacade.GetValue(AvailableValue.DrawBeams);
			activeControl.SceneFacade.SetValue(AvailableValue.DrawBeams, value);
			beamsToolStripMenuItem2.IsChecked = value;
		}

		private async void signalNodeToolStripMenuItem_Click(object sender, RoutedEventArgs e)
		{
			if (activeControl.SceneFacade.ContainsMesh)
			{
				bool signalIsSet = (bool)activeControl.SceneFacade.GetValue(AvailableValue.NodeSignalIsSet);
				if (signalIsSet)
					activeControl.SignalNodeByID(null); // clear signal
				else
					await activeControl.SignalNodeByID(); // ask user for node ids
			}
		}

		private async void signalElementToolStripMenuItem_Click(object sender, RoutedEventArgs e)
		{
			if (activeControl.SceneFacade.ContainsMesh)
			{
				bool value = (bool)activeControl.SceneFacade.GetValue(AvailableValue.ElementSignalIsSet);
				await activeControl.SignalElementByID(value, longOpNotifier);
			}
		}

		private void signalDataMaximumToolStripMenuItem_Click(object sender, RoutedEventArgs e)
		{
			if (activeControl.SceneFacade.ContainsMesh)
			{
				if (activeControl.SceneFacade.GetValue(AvailableValue.DataVisualizer) is IDataVisualizer dataVisualizer)
				{
					activeControl.SignalNodeByID(dataVisualizer.GetIDsOfNodesWithMaximumDataValue());
				}
			}
		}

		private void signalDataMinimumToolStripMenuItem_Click(object sender, RoutedEventArgs e)
		{
			if (activeControl.SceneFacade.ContainsMesh)
			{
				if (activeControl.SceneFacade.GetValue(AvailableValue.DataVisualizer) is IDataVisualizer dataVisualizer)
				{
					activeControl.SignalNodeByID(dataVisualizer.GetIDsOfNodesWithMinimumDataValue());
				}
			}
		}

		private async void checkForUpdatesToolStripMenuItem_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				bool updateExists = await checkForUpdatesAsync(maxVersionToIgnoreString: null);
				if (!updateExists)
				{
					await MessageBox.Show("You already have the latest version installed.", "No update available", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
			}
			catch (Exception ex)
			{
				await OpenGLViewport.ShowErrorMessage("Can't check for updates", ex.Message);
			}
		}

		private async void userGuideToolStripMenuItem_Click(object sender, RoutedEventArgs e)
		{
			string userGuideFilePath = UpdateChecker.MeshEditorUpdateUri + "/user-guide.pdf";
			try
			{
				ProcessStarter.OpenBrowser(userGuideFilePath);
			}
			catch (Exception)
			{
				await MessageBox.Show($"The path '{userGuideFilePath} could not be open.'", "Can't show the User guide");
			}
		}

		#endregion

		#region Helper methods

		private async Task checkForUpdatesSilently()
		{
			try
			{
				_ = await checkForUpdatesAsync(maxVersionToIgnoreString: mainWindowSettings.LastCheckedUpdateVersion);
			}
			catch
			{
				// swallow exceptions
			}
		}

		private async Task<bool> checkForUpdatesAsync(string maxVersionToIgnoreString)
		{
			var updateChecker = new UpdateChecker();

			bool updateExists = await updateChecker.CheckForUpdates();

			mainWindowSettings.LastCheckedUpdateVersion = updateChecker.ServerVersion.ToString();

			if (updateExists)
			{
				if (Version.TryParse(maxVersionToIgnoreString, out Version maxVersionToIgnore) && maxVersionToIgnore >= updateChecker.ServerVersion)
					return false;

				StringBuilder questionTextBuilder = new StringBuilder();

				questionTextBuilder.AppendLine("There is a new version of this application.");
				questionTextBuilder.AppendLine("New version: " + updateChecker.ServerVersion);
				questionTextBuilder.AppendLine("Current version: " + updateChecker.CurrentVersion);
				questionTextBuilder.AppendLine();
				questionTextBuilder.AppendLine($"Link to download: {updateChecker.PackageFileUri}");
				questionTextBuilder.AppendLine();
				questionTextBuilder.Append("Do you want to download the new version?");

				var dialogResult = await MessageBox.Show(questionTextBuilder.ToString(), "New version available", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
				if (dialogResult == MessageBoxResult.Yes)
				{
					ProcessStarter.OpenBrowser(updateChecker.PackageFileUri); // start web browser with package file uri
					this.Close(); // close application
				}
			}
			return updateExists;
		}

		private void initializeMainOpenGlControl(Border container)
		{
			OpenGLViewport mainOpenGLControl = new OpenGLViewport(null);
			container.Child = mainOpenGLControl;
			mainOpenGLControl.MyContainer = container;

			registerNewControl(mainOpenGLControl);
			// the window is not shown yet; the control is activated in OnOpened
			activeControl = mainOpenGLControl;
			activeControl.IsActive = true;
		}

		/// <summary>
		/// Asks the user to save unsaved meshes of the given viewports.
		/// </summary>
		/// <returns>True when the operation was cancelled (CancelEventArgs.Cancel in the WinForms version).</returns>
		private async Task<bool> askToSaveChanges(List<OpenGLViewport> controls, bool canBeCancelled)
		{
			string caption = "Save changes?";

			HashSet<int> processedMeshes = new HashSet<int>();
			// ----------------------------------------------------
			// don't ask for meshes which remain open in other viewports
			foreach (OpenGLViewport c in openGLControls)
				if (c.SceneFacade.ContainsMesh && !controls.Contains(c))
					processedMeshes.Add(c.SceneFacade.ActiveMeshUniqueIdentifier.Value);
			// ----------------------------------------------------
			// NOTE: iterating only over active meshes, there may be more meshes in each control
			foreach (OpenGLViewport control in controls)
			{
				if (control.SceneFacade.ContainsMesh && processedMeshes.Add(control.SceneFacade.ActiveMeshUniqueIdentifier.Value))
				{
					bool unsaved = (bool)control.SceneFacade.GetValue(AvailableValue.UnsavedChangesInMesh);
					if (unsaved)
					{
						string text = "Do you want to save changes to '" + control.SceneFacade.MeshName + "'?";
						MessageBoxResult result = await MessageBox.Show(text, caption, canBeCancelled ? MessageBoxButtons.YesNoCancel : MessageBoxButtons.YesNo, MessageBoxIcon.Question);
						if (result == MessageBoxResult.Cancel)
						{
							return true;
						}
						else if (result == MessageBoxResult.Yes)
						{
							if (!await saveMeshChooseFile(control))
							{
								if (canBeCancelled)
									return true;
							}
						}
					}
				}
			}

			// -------------------------------------------------
			// check that all operations are finished
			foreach (OpenGLViewport control in controls)
			{
				if (!control.IOActionInProgress)
					continue;

				await control.WaitForIOActionAsync();
				if (!control.IOProcessedFinishedCorrectly)
					return true;
			}
			return false;
		}

		private void initLongOpNotifier()
		{
			longOpNotifier = new LongOpNotifier();

			longOpNotifier.HasBegun += (token, isCancellable) => onUIThread(() =>
			{
				Cursor = new Cursor(StandardCursorType.Wait);
				if (activeControl != null)
					activeControl.Cursor = new Cursor(StandardCursorType.Wait);
				string stateText = longOpNotifier.GetState(token).ToString();
				statusLabel.Text = string.IsNullOrEmpty(stateText) ? "Operation in progress..." : stateText;
				statusLabel.Foreground = Brushes.Blue;

				setupProgressViewTimer(token, isCancellable);
			});
			longOpNotifier.HasEnded += token => onUIThread(() =>
			{
				Cursor = Cursor.Default;
				activeControl?.SetCursorAccordingToEditorMode();
				statusLabel.Foreground = Brushes.Black;
				updateStatus();
				if (progressViewForms.TryGetValue(token, out ProgressViewDialog progressViewForm))
				{
					progressViewForms.Remove(token);
					progressViewForm.Quit();
				}
			});
			longOpNotifier.ProgressChanged += token => onUIThread(() =>
			{
				LongOpNotifier.State operationState = longOpNotifier.GetState(token);
				statusLabel.Text = operationState.ToString();

				if (progressViewForms.TryGetValue(token, out ProgressViewDialog progressViewForm))
				{
					progressViewForm.Caption = operationState.TaskName;
					progressViewForm.OperationName = operationState.OperationName;
					progressViewForm.SetProgressState(operationState.PercentDone);
				}
			});
		}

		/// <summary>
		/// Shows the progress window when the long operation takes longer than 500 ms.
		/// </summary>
		private void setupProgressViewTimer(LongOpNotifier.Token operationToken, bool isCancellable)
		{
			DispatcherTimer.RunOnce(() =>
			{
				if (longOpNotifier.IsRunning(operationToken) && !progressViewForms.ContainsKey(operationToken))
				{
					LongOpNotifier.State state = longOpNotifier.GetState(operationToken);
					var progressViewForm = new ProgressViewDialog(state.TaskName ?? "Operation in progress...", isCancellable, logger: longOpNotifier.GetLogger(operationToken));
					progressViewForms[operationToken] = progressViewForm;
					if (isCancellable)
						progressViewForm.Cancel += (s, e) => longOpNotifier.Cancel(operationToken);
					progressViewForm.OperationName = state.OperationName;
					progressViewForm.SetProgressState(state.PercentDone);

					Cursor = Cursor.Default;
					activeControl?.SetCursorAccordingToEditorMode();

					progressViewForm.Show(this);
				}
			}, TimeSpan.FromMilliseconds(500));
		}

		private static void onUIThread(Action action)
		{
			if (Dispatcher.UIThread.CheckAccess())
				action();
			else
				Dispatcher.UIThread.Post(action);
		}

		private void setRenderModeAcordingToToolStripButtons()
		{
			RenderMode mode = RenderMode.None;
			if (isChecked(toolStripButtonPoints))
				mode |= RenderMode.Points;
			if (isChecked(toolStripButtonBorderLines))
				mode |= RenderMode.BorderLines;
			if (isChecked(toolStripButtonAllLines))
				mode |= RenderMode.AllLines;
			if (isChecked(toolStripButtonFaces))
				mode |= RenderMode.Faces;
			activeControl.SceneFacade.SetValue(AvailableValue.RenderMode, mode);
		}

		private string getNameOfItemsToSelectAcordingToEditorMode()
		{
			switch (SceneFacade.EditorMode)
			{
				case EditorMode.SelectElements:
					return "elements";
				case EditorMode.SelectEdges:
					return "edges";
				case EditorMode.SelectNodes:
					return "nodes";
				case EditorMode.SelectBeams:
					return "beams";
				case EditorMode.SelectFaces:
					return "faces";
				default:
					return null;
			}
		}

		private void registerNewControl(OpenGLViewport openGLControl)
		{
			openGLControl.ContextMenu = createContextMenu();
			openGLControl.ActivationRequested += openGLControl_MouseDown;

			openGLControl.IOActionDone += delegate
			{
				updateCaption();
				updateStatus();
				updateRenderModeButtons();
			};

			openGLControl.MeshNeedRefresh += meshNeedRefreshHandler;
			openGLControl.ActionPerformed += delegate
			{
				updateStatus();
			};
			openGLControl.ColorModeChanged += delegate
			{
				updateColorModeButtons();
			};
			openGLControl.RenderModeChanged += delegate
			{
				updateRenderModeButtons();
			};
			openGLControl.ScreenshotNeeded += async (sender, args) =>
			{
				await saveScreenshot(sender as OpenGLViewport, args.ScreenshotWindow);
			};

			this.openGLControls.Add(openGLControl);
		}

		private void updateRenderModeButtons()
		{
			object parameter = activeControl.SceneFacade.GetValue(AvailableValue.RenderMode);
			if (parameter == null)
				return;
			RenderMode mode = (RenderMode)parameter;
			setChecked(toolStripButtonFaces, (mode & RenderMode.Faces) != 0);
			setChecked(toolStripButtonPoints, (mode & RenderMode.Points) != 0);
			setChecked(toolStripButtonBorderLines, (mode & RenderMode.BorderLines) != 0);
			setChecked(toolStripButtonAllLines, (mode & RenderMode.AllLines) != 0);
			// ---------------------------------------------------------
			// menu
			pointsToolStripMenuItem.IsChecked = mode == RenderMode.Points;
			borderLinesToolStripMenuItem.IsChecked = mode == RenderMode.BorderLines;
			allLinesToolStripMenuItem.IsChecked = mode == RenderMode.AllLines;
			allLinesPointsToolStripMenuItem.IsChecked = mode == (RenderMode.AllLines | RenderMode.Points);
			facesRenderToolStripMenuItem.IsChecked = mode == RenderMode.Faces;
			facesBorderLinesToolStripMenuItem.IsChecked = mode == RenderMode.FacesBorder;
			facesAllLinesToolStripMenuItem.IsChecked = mode == RenderMode.FacesLines;
			facesAllLinesPointsToolStripMenuItem.IsChecked = mode == RenderMode.FacesLinesPoints;
		}

		private void openGLControl_MouseDown(object sender, EventArgs e)
		{
			if (activeControl != sender)
				activateControl((OpenGLViewport)sender);
		}

		private void unregisterControl(OpenGLViewport toRemove)
		{
			if (toRemove == activeControl) // deactivate when active
				activeControl = null;
			toRemove.MeshNeedRefresh -= meshNeedRefreshHandler;
			toRemove.ActivationRequested -= openGLControl_MouseDown;
			openGLControls.Remove(toRemove);
			toRemove.DisposeScene(); // release the scene (mainly the mesh)
		}

		private void activateControl(OpenGLViewport openGLControl)
		{
			this.activeControl = openGLControl;
			this.activeControl.IsActive = true;
			// update the list of visible nodes
			activeControl.SceneFacade.PerformAction(AvailableAction.UpdateVisibleNodes);
			activeControl.Refresh();

			foreach (OpenGLViewport c in openGLControls)
			{
				if (c != activeControl && c.IsActive)
				{
					c.IsActive = false;
					c.Refresh();
				}
			}
			activeControl.Focus();

			updateCaption();
			updateStatus();
			updateColorModeButtons();
			updateRenderModeButtons();
		}

		private void updateCaption()
		{
			string caption = "MeshEditor";
			if (activeControl != null)
			{
				string sceneTitle = activeControl.SceneFacade.Title;
				if (!string.IsNullOrEmpty(sceneTitle))
				{
					caption += " - " + sceneTitle;
				}
			}
			this.Title = caption;
		}

		private void updateStatus()
		{
			if (activeControl == null)
				return;
			string desc = (string)activeControl.SceneFacade.GetValue(AvailableValue.Status);
			statusLabel.Text = string.IsNullOrEmpty(desc) ? "Ready" : desc;
		}

		private bool activeWindowCanBeClosed()
		{
			return activeControl.MyContainer.Parent is ViewSplitContainer;
		}

		private MainWindowSettings loadMainWindowSettings()
		{
			var settings = ConfigurationManager.GetConfigurationObject<MainWindowSettings>("MainWindowSettings") ?? new MainWindowSettings();
			if (settings.State == FormWindowState.Normal)
			{
				// sizes are stored in device pixels (like WinForms); Avalonia uses device independent units
				double scaling = Screens?.Primary?.Scaling ?? 1.0;
				Width = settings.Width / scaling;
				Height = settings.Height / scaling;
				Position = new PixelPoint(settings.PositionLeft, settings.PositionTop);
			}
			WindowState = settings.State switch
			{
				FormWindowState.Maximized => WindowState.Maximized,
				FormWindowState.Minimized => WindowState.Minimized,
				_ => WindowState.Normal,
			};
			return settings;
		}

		private void saveMainWindowSettings()
		{
			mainWindowSettings.State = WindowState switch
			{
				WindowState.Maximized or WindowState.FullScreen => FormWindowState.Maximized,
				WindowState.Minimized => FormWindowState.Minimized,
				_ => FormWindowState.Normal,
			};
			if (WindowState == WindowState.Normal)
			{
				mainWindowSettings.Width = (int)Math.Round(Bounds.Width * RenderScaling);
				mainWindowSettings.Height = (int)Math.Round(Bounds.Height * RenderScaling);
				mainWindowSettings.PositionLeft = Position.X;
				mainWindowSettings.PositionTop = Position.Y;
			}
			mainWindowSettings.LastLoadedMesh = activeControl?.SceneFacade.MeshSourceFileName;
			ConfigurationManager.SetConfigurationObject("MainWindowSettings", mainWindowSettings);
		}

		private void fileToolStripMenuItem_DropDownOpening(object sender, RoutedEventArgs e)
		{
			closeActiveMeshToolStripMenuItem.IsEnabled = activeControl.SceneFacade.ContainsMesh;
		}

		#endregion

		#region Tool strip handlers

		private void nodeNumbersToolStripMenuItem_Click(object sender, RoutedEventArgs e)
		{
			bool value = !(bool)activeControl.SceneFacade.GetValue(AvailableValue.DrawNodeNumbers);
			activeControl.SceneFacade.SetValue(AvailableValue.DrawNodeNumbers, value);
			nodeNumbersToolStripMenuItem.IsChecked = value;
		}

		private void elementNumbersToolStripMenuItem_Click(object sender, RoutedEventArgs e)
		{
			bool value = !(bool)activeControl.SceneFacade.GetValue(AvailableValue.DrawElementNumbers);
			activeControl.SceneFacade.SetValue(AvailableValue.DrawElementNumbers, value);
			elementNumbersToolStripMenuItem.IsChecked = value;
		}

		private void beamNumbersToolStripMenuItem_Click(object sender, RoutedEventArgs e)
		{
			bool value = !(bool)activeControl.SceneFacade.GetValue(AvailableValue.DrawBeamNumbers);
			activeControl.SceneFacade.SetValue(AvailableValue.DrawBeamNumbers, value);
			beamNumbersToolStripMenuItem.IsChecked = value;
		}

		private void toolStripButtonPoints_Click(object sender, RoutedEventArgs e)
		{
			setChecked(toolStripButtonPoints, !isChecked(toolStripButtonPoints));
			setRenderModeAcordingToToolStripButtons();
		}

		private void toolStripButtonBorderLines_Click(object sender, RoutedEventArgs e)
		{
			setChecked(toolStripButtonBorderLines, !isChecked(toolStripButtonBorderLines));
			if (isChecked(toolStripButtonBorderLines))
				setChecked(toolStripButtonAllLines, false);
			setRenderModeAcordingToToolStripButtons();
		}

		private void toolStripButtonAllLines_Click(object sender, RoutedEventArgs e)
		{
			setChecked(toolStripButtonAllLines, !isChecked(toolStripButtonAllLines));
			if (isChecked(toolStripButtonAllLines))
				setChecked(toolStripButtonBorderLines, false);
			setRenderModeAcordingToToolStripButtons();
		}

		private void toolStripButtonFaces_Click(object sender, RoutedEventArgs e)
		{
			setChecked(toolStripButtonFaces, !isChecked(toolStripButtonFaces));
			setRenderModeAcordingToToolStripButtons();
		}

		private void toolStripButtonEditorModeTool_Click(object sender, RoutedEventArgs e)
		{
			EditorMode editorMode = EditorMode.None;
			if (sender == toolStripButtonOrbit)
				editorMode = EditorMode.Orbit;
			else if (sender == toolStripButtonPan)
				editorMode = EditorMode.Pan;
			else if (sender == toolStripButtonLookAround)
				editorMode = EditorMode.LookAround;
			else if (sender == toolStripButtonZoomWindow)
				editorMode = EditorMode.ZoomWindow;
			else if (sender == toolStripButtonSelectElements)
				editorMode = EditorMode.SelectElements;
			else if (sender == toolStripButtonSelectNodes)
				editorMode = EditorMode.SelectNodes;
			else if (sender == toolStripButtonSelectFaces)
				editorMode = EditorMode.SelectFaces;
			else if (sender == toolStripButtonSelectEdges)
				editorMode = EditorMode.SelectEdges;
			else if (sender == toolStripButtonSelectBeams)
				editorMode = EditorMode.SelectBeams;
			SceneFacade.EditorMode = editorMode;
			activeControl.Focus(); // WinForms tool strip buttons don't take the keyboard focus from the viewport
		}

		private void toolStripButtonSelectItemsByProperty_MouseEnter(object sender, PointerEventArgs e)
		{
			string itemsName = getNameOfItemsToSelectAcordingToEditorMode() ?? "elements";
			ToolTip.SetTip(toolStripButtonSelectItemsByProperty, "Select " + itemsName + " by property (F6)");
		}

		private async void takeScreenshotToolStripMenuItem_Click(object sender, RoutedEventArgs e)
		{
			// open screenshot options dialog which has two options: take whole screen or take selection window
			var screenshotOptionsForm = new ScreenshotOptionsDialog();
			if (await screenshotOptionsForm.ShowDialog(this))
			{
				if (screenshotOptionsForm.UseSelectionArea)
				{
					SceneFacade.EditorMode = EditorMode.ScreenshotWindow;
				}
				else // take whole screen
				{
					await saveScreenshot(activeControl, DrawingRectangle.Empty);
				}
			}
		}

		private async void configurePropertyColorsToolStripMenuItem_Click(object sender, RoutedEventArgs e)
		{
			var configurePropertyColorsForm = new ConfigurePropertyColorsDialog(openGLControls.Select(c => c.SceneFacade).ToList(), (IEnumerable<Property>)activeControl.SceneFacade.GetValue(AvailableValue.AllMeshPropertiesSorted));
			await configurePropertyColorsForm.ShowDialog(this);
		}

		private async Task saveScreenshot(OpenGLViewport control, DrawingRectangle screenshotWindow)
		{
			Debug.Assert(control != null);
			var fileTypes = FileDialogFilter.Parse("PNG image format (*.png)|*.png|JPEG image format (*.jpg; *.jpeg)|*.jpg;*.jpeg|BMP image format (*.bmp)|*.bmp");
			string suggestedFileName = takeScreenshotLastFilename;
			if (suggestedFileName == null && control.SceneFacade.ContainsMesh)
				suggestedFileName = Utilities.Functions.MakeTextValidFilename(control.SceneFacade.MeshName);

			var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
			{
				SuggestedFileName = suggestedFileName,
				FileTypeChoices = fileTypes,
				SuggestedFileType = fileTypes[Math.Clamp(takeScreenshotLastFilterIndex - 1, 0, fileTypes.Count - 1)],
				ShowOverwritePrompt = true,
			});
			string fileName = file?.TryGetLocalPath();
			if (fileName == null)
				return;

			string extension = Path.GetExtension(fileName).ToLowerInvariant();
			if (extension != ".png" && extension != ".jpg" && extension != ".jpeg" && extension != ".bmp")
			{
				await MessageBox.Show("This file extension (image format) is not supported.", "Can't take screenshot", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			byte[] pixels = control.TakeScreenshot(screenshotWindow, out int width, out int height);
			using (var image = SixLabors.ImageSharp.Image.LoadPixelData<SixLabors.ImageSharp.PixelFormats.Bgra32>(pixels, width, height))
			{
				image.Save(fileName); // image format is chosen according to the file extension
			}

			this.takeScreenshotLastFilterIndex = extension == ".png" ? 1 : extension == ".bmp" ? 3 : 2;
			this.takeScreenshotLastFilename = Path.GetFileName(fileName);
		}

		private void postprocessToolStripMenuItem_DropDownOpening(object sender, RoutedEventArgs e)
		{
			closeSolutionToolStripMenuItem.IsEnabled = LayoutMode == LayoutMode.Postprocessor;
			signalDataMinimumToolStripMenuItem.IsEnabled = signalDataMaximumToolStripMenuItem.IsEnabled = (activeControl.SceneFacade.GetValue(AvailableValue.DataVisualizer) as IDataVisualizer)?.DisplayData ?? false;
		}

		private async void importFEMResultsToolStripMenuItem_Click(object sender, RoutedEventArgs e)
		{
			// TODO: port ImportFEMResultsForm and PostprocessViewControl (MeshEditor.DataVisualizer)
			await NotPorted.Show("Import FEM results", "ImportFEMResultsForm");
		}

		private async void openSolutionToolStripMenuItem_Click(object sender, RoutedEventArgs e)
		{
			// TODO: port SolutionBrowserForm and PostprocessViewControl (MeshEditor.DataVisualizer)
			await NotPorted.Show("Open solution", "SolutionBrowserForm");
		}

		private void closeSolutionToolStripMenuItem_Click(object sender, RoutedEventArgs e)
		{
			setPreprocessorLayoutMode();
		}

		private void setPreprocessorLayoutMode()
		{
			// the WinForms UI hosts the viewports in PreprocessViewControl, which is a plain container
			if (contentContainer == null)
			{
				contentContainer = new Border();
				centralPanel.Child = contentContainer;
			}
			layoutMode = LayoutMode.Preprocessor;
			activeControl?.Focus();
		}

		#endregion

		#region Context menu of the viewports

		/// <summary>
		/// Items of the currently open viewport context menu (contextMenuStrip in the WinForms UI).
		/// </summary>
		private ContextMenuItems contextMenuItems;

		private sealed class ContextMenuItems
		{
			public MenuItem Elements, Nodes, Faces, Edges, Beams;
			public MenuItem Orbit, Pan, LookAround, ZoomWindow;
			public MenuItem CloseWindow;

			public void UncheckSelectTools() => Elements.IsChecked = Nodes.IsChecked = Faces.IsChecked = Edges.IsChecked = Beams.IsChecked = false;

			public void UncheckCameraTools() => Orbit.IsChecked = Pan.IsChecked = LookAround.IsChecked = ZoomWindow.IsChecked = false;
		}

		private ContextMenu createContextMenu()
		{
			MenuItem item(string header, string icon, EventHandler<RoutedEventArgs> click, bool checkable = true, EditorMode? mode = null)
			{
				var menuItem = new MenuItem { Header = header, Tag = mode };
				if (checkable)
					menuItem.ToggleType = MenuItemToggleType.CheckBox;
				if (icon != null)
					menuItem.Icon = new Image { Source = new Bitmap(AssetLoader.Open(new Uri($"avares://MeshEditor.Avalonia/Assets/Icons/{icon}.png"))) };
				menuItem.Click += click;
				return menuItem;
			}

			var items = new ContextMenuItems
			{
				Elements = item("_Elements", "selectElements", selectToolChosen, mode: EditorMode.SelectElements),
				Nodes = item("_Nodes", "selectNodes", selectToolChosen, mode: EditorMode.SelectNodes),
				Faces = item("_Faces", "selectFaces", selectToolChosen, mode: EditorMode.SelectFaces),
				Edges = item("Ed_ges", "selectEdges", selectToolChosen, mode: EditorMode.SelectEdges),
				Beams = item("_Beams", "selectBeams", selectToolChosen, mode: EditorMode.SelectBeams),
				Orbit = item("_Orbit", "orbit", cameraToolChosen, mode: EditorMode.Orbit),
				Pan = item("_Pan", "pan", cameraToolChosen, mode: EditorMode.Pan),
				LookAround = item("_Look Around", "lookAround", cameraToolChosen, mode: EditorMode.LookAround),
				ZoomWindow = item("_Zoom window", "zoomWindow", cameraToolChosen, mode: EditorMode.ZoomWindow),
				CloseWindow = item("_Close window", null, closeActiveMeshToolStripMenuItem_Click, checkable: false),
			};

			var selectMenu = new MenuItem { Header = "_Select" };
			selectMenu.Items.Add(items.Elements);
			selectMenu.Items.Add(items.Nodes);
			selectMenu.Items.Add(items.Faces);
			selectMenu.Items.Add(items.Edges);
			selectMenu.Items.Add(items.Beams);
			selectMenu.SubmenuOpened += (s, e) => { contextMenuItems = items; selectToolStripMenuItem_DropDownOpening(s, e); };

			var contextMenu = new ContextMenu();
			contextMenu.Items.Add(selectMenu);
			contextMenu.Items.Add(new Separator());
			contextMenu.Items.Add(items.Orbit);
			contextMenu.Items.Add(items.Pan);
			contextMenu.Items.Add(items.LookAround);
			contextMenu.Items.Add(items.ZoomWindow);
			contextMenu.Items.Add(new Separator());
			contextMenu.Items.Add(item("Split _window", null, splitWindowToolStripMenuItem_Click, checkable: false));
			contextMenu.Items.Add(items.CloseWindow);

			contextMenu.Opened += (s, e) => { contextMenuItems = items; cameraToolStripMenuItem_DropDownOpened(s, e); };
			contextMenu.Closed += (s, e) => { contextMenuStrip_Closed(s, e); contextMenuItems = null; };
			return contextMenu;
		}

		#endregion
	}
}
