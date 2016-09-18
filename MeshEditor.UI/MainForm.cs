using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;

using MeshEditor.Graphics;
using MeshEditor.Data;
using System.IO;
using System.Diagnostics;
using MeshEditor.CoreInterface;
using System.ComponentModel;
using OpenTK;
using System.Threading;
using System.Threading.Tasks;
using MeshEditor.DataVisualizer.UI;
using System.Text;
using MeshEditor.Common;

namespace MeshEditor.WinUI
{
	/// <summary>
	/// Trida reprezentujici hlavni okno programu
	/// </summary>
	public partial class MainForm : Form
	{

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

		private OpenGLControl activeControl;
		private List<OpenGLControl> openGLControls;
		private LongOpNotifier longOpNotifier;
		private Dictionary<LongOpNotifier.Token, ProgressViewForm> progressViewForms = new Dictionary<LongOpNotifier.Token, ProgressViewForm>();

		private CutEditorForm cutEditorForm;
		private ShowHideElementsForm showHideElementsForm;

		private string[] arguments;

		private int takeScreenshotLastFilterIndex;
		private string takeScreenshotLastFilename;

		private LayoutMode layoutMode;

		private readonly MainWindowSettings mainWindowSettings;

		public MainForm(string[] args)
		{
			Toolkit.Init();

			InitializeComponent();

			SceneFacade.EditorModeChanged += new EventHandler(editorModeChanged);
			SceneFacade.ShowError += new ShowErrorEventHandler(SceneFacade_ShowError);

			this.cutEditorForm = null;
			this.showHideElementsForm = null;
			this.openGLControls = new List<OpenGLControl>();
			this.arguments = args;
			this.longOpNotifier = null;
			initLongOpNotifier();

			// load applications settings accessed by Options dialog (OpenGL context must be initialized first)
			SceneSettings.LoadFromConfigurationFile();

			PropertyColorProvider.LoadPropertyColors();

			// load window state settings
			mainWindowSettings = loadMainWindowSettings();

			setPreprocessorLayoutMode();

			initializeMainOpenGlControl(centralPanel.Controls.Cast<ContentViewControl>().Single().Content);

			updateCaption();
			editorModeChanged(null, null);

			if (UpdateChecker.IsUpdateServiceAvailableForThisPlatform)
			{
				checkForUpdatesSilently();
			}
		}

		#endregion

		#region Properties

		public LayoutMode LayoutMode => layoutMode;

		public LongOpNotifier LongOpNotifier => longOpNotifier;

		#endregion

		#region Overrides

		protected override async void OnShown(EventArgs e)
		{
			base.OnShown(e);

			if (arguments != null && arguments.Length > 0) // load file in command file arguments
			{
				await openFiles(arguments[0]);
			}
			else if (File.Exists(mainWindowSettings.LastLoadedMesh)) // load last loaded file (if exists)
			{
				await openFiles(mainWindowSettings.LastLoadedMesh);
			}
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			base.OnClosing(e);

			List<OpenGLControl> controls = new List<OpenGLControl>(openGLControls.Count);
			controls.Add(activeControl);
			foreach (OpenGLControl c in openGLControls)
				if (c != activeControl)
					controls.Add(c);
			askToSaveChanges(controls, true, e);
		}

		protected override void OnClosed(EventArgs e)
		{
			// save settings from Options dialog
			SceneSettings.SaveToConfigurationFile();

			// save window state settings
			saveMainWindowSettings();

			ConfigurationManager.Save();

			// dispose all meshes
			foreach (OpenGLControl c in openGLControls)
				c.DisposeScene();

			base.OnClosed(e);
		}

		#endregion

		#region Split window feature

		private void splitWindowToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (activeControl == null)
				return;

			OpenGLControl controlToSplit = activeControl;
			Control parent = controlToSplit.MyContainer;

			if (Math.Max(parent.Width, parent.Height) < PANEL_MINSIZE * 2) // je moc malej, takze nic rozdelovat nebudu
				return;

			SplitContainer newContainer = new SplitContainer();

			if (parent.Width < parent.Height)
			{
				newContainer.Orientation = Orientation.Horizontal;
				newContainer.Height = parent.Height;
				newContainer.SplitterDistance = parent.Height / 2;
			}
			else
			{
				newContainer.Orientation = Orientation.Vertical;
				newContainer.Width = parent.Width;
				newContainer.SplitterDistance = parent.Width / 2;
			}

			newContainer.SplitterMoving += delegate { newContainer.Panel1.Invalidate(); newContainer.Panel2.Invalidate(); };
			newContainer.Dock = DockStyle.Fill;
			newContainer.Panel1MinSize = newContainer.Panel2MinSize = 0;
			newContainer.SplitterIncrement = 20;

			newContainer.MouseDown += splitContainer_MouseDown;
			newContainer.MouseUp += splitContainer_MouseUp;
			newContainer.MouseMove += splitContainer_MouseMove;

			newContainer.Panel1.Controls.Add(controlToSplit);
			controlToSplit.MyContainer = newContainer.Panel1;

			OpenGLControl newOpenGLControl = OpenGLControl.Create(controlToSplit.SceneFacade, openGLControl_MouseDown);

			newOpenGLControl.Dock = DockStyle.Fill;

			newContainer.Panel2.Controls.Add(newOpenGLControl);
			newOpenGLControl.MyContainer = newContainer.Panel2;

			parent.Controls.Clear();
			parent.Controls.Add(newContainer);

			registerNewControl(newOpenGLControl);
			activateControl(controlToSplit); // nastavim fokus na puvodni ovladaci prvek
		}

		private void splitContainer_MouseDown(object sender, MouseEventArgs e)
		{
			/* This disables the normal move behavior */
			((SplitContainer)sender).IsSplitterFixed = true;
		}

		private void splitContainer_MouseUp(object sender, MouseEventArgs e)
		{
			SplitContainer sc = (SplitContainer)sender;
			/* This allows the splitter to be moved normally again */
			sc.IsSplitterFixed = false;

			if (sc.SplitterDistance < PANEL_MINSIZE)
			{
				if (removePanel(sc.Panel1))
					extendPanel(sc.Panel2);
			}
			else
			{
				int limit = (sc.Orientation == Orientation.Horizontal) ? sc.Height - PANEL_MINSIZE : sc.Width - PANEL_MINSIZE;
				if (sc.SplitterDistance > limit)
				{
					if (removePanel(sc.Panel2))
						extendPanel(sc.Panel1);
				}
			}
		}

		private void splitContainer_MouseMove(object sender, MouseEventArgs e)
		{
			SplitContainer sc = (SplitContainer)sender;

			/* Check to make sure the splitter won't be updated by the 
			   normal move behavior also */
			if (sc.IsSplitterFixed)
			{
				/* Make sure that the button used to move the splitter 
				   is the left mouse button */
				if (e.Button.Equals(MouseButtons.Left))
				{
					/* Checks to see if the splitter is aligned Vertically */
					if (sc.Orientation.Equals(Orientation.Vertical))
					{
						/* Only move the splitter if the mouse is within 
						   the appropriate bounds */
						if (e.X > 0 && e.X < sc.Width)
						{
							/* Move the splitter */
							sc.SplitterDistance = e.X;
						}
					}
					/* If it isn't aligned vertically then it must be 
					   horizontal */
					else
					{
						/* Only move the splitter if the mouse is within 
						   the appropriate bounds */
						if (e.Y > 0 && e.Y < sc.Height)
						{
							/* Move the splitter */
							sc.SplitterDistance = e.Y;
						}
					}
				}
				/* If a button other than left is pressed or no button 
				   at all */
				else
				{
					/* This allows the splitter to be moved normally again */
					sc.IsSplitterFixed = false;
				}
			}
		}

		/// <summary>
		/// rekurzivne projde vnorenymi kontejnery v panelu a odebere vsechny OpenGL prvky
		/// </summary>
		/// <param name="panel">panel, ktery ma byt smazan</param>
		private bool removePanel(SplitterPanel panel)
		{
			List<OpenGLControl> controls = new List<OpenGLControl>();
			getAllControlsInPanel(panel, controls);
			CancelEventArgs args = new CancelEventArgs();
			askToSaveChanges(controls, true, args);
			if (args.Cancel)
				return false;
			removeAllControlsInPanel(panel);
			return true;
		}

		private void getAllControlsInPanel(SplitterPanel panel, List<OpenGLControl> controls)
		{
			Control content = panel.Controls[0];
			if (content is OpenGLControl)
			{
				OpenGLControl c = (OpenGLControl)content;
				controls.Add(c);
			}
			else
			{
				SplitContainer nested = (SplitContainer)content;
				getAllControlsInPanel(nested.Panel1, controls);
				getAllControlsInPanel(nested.Panel2, controls);
			}
		}

		private void removeAllControlsInPanel(SplitterPanel panel)
		{
			Control content = panel.Controls[0];
			if (content is OpenGLControl)
			{
				OpenGLControl toRemove = (OpenGLControl)content;
				removeControlFromPanel(toRemove);
			}
			else
			{
				SplitContainer nested = (SplitContainer)content;
				removeAllControlsInPanel(nested.Panel1);
				removeAllControlsInPanel(nested.Panel2);
			}
			panel.Controls.Clear();
		}

		private void removeControlFromPanel(OpenGLControl toRemove)
		{
			unregisterControl(toRemove);
			toRemove.MyContainer.Controls.Remove(toRemove);
		}

		private void extendPanel(SplitterPanel panel)
		{
			Control content = panel.Controls[0];
			Control parent = panel.Parent.Parent;

			panel.Controls.Clear();
			parent.Controls.Clear();
			parent.Controls.Add(content);
			content.Dock = DockStyle.Fill;
			if (content is OpenGLControl)
				((OpenGLControl)content).MyContainer = parent;

			setFocusToRemainedControl(content);

			//MessageBox.Show("Memory used before collection: " + GC.GetTotalMemory(false));
			//GC.Collect(); // smazu nepouzivanou pamet (uvolnene mesh objekty)
			//MessageBox.Show("Memory used after full collection: " + GC.GetTotalMemory(true));
		}

		private void setFocusToRemainedControl(Control control)
		{
			if (control is OpenGLControl)
				activateControl((OpenGLControl)control); // aktivuju ho
			else
				setFocusToRemainedControl(((SplitContainer)control).Panel1.Controls[0]);
		}

		#endregion

		#region UI command handlers (menu items)

		private void editorModeChanged(object sender, EventArgs e)
		{
			EditorMode mode = SceneFacade.EditorMode;
			toolStripButtonOrbit.Checked = (mode == EditorMode.Orbit);
			toolStripButtonPan.Checked = (mode == EditorMode.Pan);
			toolStripButtonLookAround.Checked = (mode == EditorMode.LookAround);
			toolStripButtonZoomWindow.Checked = (mode == EditorMode.ZoomWindow);
			toolStripButtonSelectElements.Checked = (mode == EditorMode.SelectElements);
			toolStripButtonSelectNodes.Checked = (mode == EditorMode.SelectNodes);
			toolStripButtonSelectFaces.Checked = (mode == EditorMode.SelectFaces);
			toolStripButtonSelectEdges.Checked = (mode == EditorMode.SelectEdges);
			toolStripButtonSelectBeams.Checked = (mode == EditorMode.SelectBeams);

			activeControl.SceneFacade.SetRenderModeAccordingToEditorMode();
		}

		void SceneFacade_ShowError(object sender, ShowErrorEventArgs ea)
		{
			MessageBox.Show(ea.Message, ea.Caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
		}

		private void meshNeedRefreshHandler(object sender, MeshNeedRefreshEventArgs ea)
		{
			foreach (OpenGLControl c in openGLControls)
			{
				if (ea.SkipSender && ReferenceEquals(sender, c))
					continue;
				if (c.SceneFacade.ContainsMeshWithIdentifier(ea.MeshIdentifier))
					c.Invalidate();
			}
		}

		private void toolsToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
		{
			restoreCuttedItemsToolStripMenuItem.Enabled = (bool)activeControl.SceneFacade.GetValue(AvailableValue.MeshHasHiddenElements);
			closeActiveWindowToolStripMenuItem.Enabled = activeWindowCanBeClosed();
			bool containsMesh = activeControl.SceneFacade.ContainsMesh;
			listOfSelectedItemsToolStripMenuItem.Enabled = showHideElementsToolStripMenuItem.Enabled = cutsToolStripMenuItem.Enabled = meshInfoToolStripMenuItem.Enabled = containsMesh;
		}

		private async void openToolStripMenuItem_Click(object sender, EventArgs e)
		{
			// zeptat se nejdriv na ulozeni otevrene site
			if (activeControl.SceneFacade.ContainsMesh)
			{
				List<OpenGLControl> controls = new List<OpenGLControl>();
				controls.Add(activeControl);
				CancelEventArgs cancelArgs = new CancelEventArgs();
				askToSaveChanges(controls, true, cancelArgs);
				if (cancelArgs.Cancel) // pokud bylo stornovano, tak ani nic neotvirat
					return;
			}
			// ---------------------------------------------

			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = SceneFacade.InputFileFormatFilter;
			openFileDialog.Multiselect = true;
			if (openFileDialog.ShowDialog() == DialogResult.OK)
			{
				// nejdriv zavrit okna, co by mohli vadit
				if (this.showHideElementsForm != null)
				{
					this.showHideElementsForm.Close();
					this.showHideElementsForm = null;
				}
				if (this.cutEditorForm != null)
				{
					this.cutEditorForm.Close();
					this.cutEditorForm = null;
				}
				// ---------------------------------------------
				await openFiles(openFileDialog.FileNames);
			}
		}

		private async Task openFiles(params string[] fileNames)
		{
			if (fileNames.Length == 1 && fileNames[0].EndsWith(SceneFacade.SolutionFileExtension)) // check if file to open is solution file
			{
				setPostprocessorLayoutMode();
				var postprocessView = getCurrentPostprocessView();
				await postprocessView.LoadLocalSolutionAsync(fileNames[0]);
			}
			else
			{
				setPreprocessorLayoutMode();
				activeControl.LoadFiles(fileNames);
			}
		}

		private void saveToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (!activeControl.SceneFacade.ContainsMesh)
				return; // nemam co ukladat

			saveMeshChooseFile(activeControl);
		}

		private bool saveMeshChooseFile(OpenGLControl control)
		{
			DialogResult withoutHiddenResult = findoutIfSaveWithoutHiddenElements(control);
			if (withoutHiddenResult == DialogResult.Cancel)
				return false;
			bool saveWithoutHiddenElements = withoutHiddenResult == DialogResult.Yes;
			SaveFileDialog saveFileDialog = new SaveFileDialog();
			saveFileDialog.FileName = Utilities.Functions.MakeTextValidFilename(control.SceneFacade.MeshName);
			saveFileDialog.Filter = SceneFacade.OutputFileFormatFilter;
			DialogResult saveDialogResult = saveFileDialog.ShowDialog();
			if (saveDialogResult == DialogResult.Cancel)
				return false;
			else if (saveDialogResult == DialogResult.OK)
			{
				control.SaveToFile(saveFileDialog.FileName, saveWithoutHiddenElements);
			}
			return true;
		}

		private DialogResult findoutIfSaveWithoutHiddenElements(OpenGLControl control)
		{
			bool hasCuttedElements = (bool)control.SceneFacade.GetValue(AvailableValue.MeshHasHiddenElements);
			if (!hasCuttedElements)
				return DialogResult.No;
			string meshName = control.SceneFacade.MeshName;
			return MessageBox.Show("Save mesh '" + meshName + "' without hidden elements?" + Environment.NewLine + "(Click \"Yes\" to save mesh as it is or \"No\" to save entire mesh in original form.)", "Mesh has some hidden elements", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			List<SceneFacade> scenes = new List<SceneFacade>();
			foreach (OpenGLControl c in openGLControls)
				scenes.Add(c.SceneFacade);

			SettingsForm sf = new SettingsForm(scenes);
			sf.ShowDialog();
		}

		private void meshInfoToolStripMenuItem_Click(object sender, EventArgs e)
		{
			MeshInfoForm sf = new MeshInfoForm(activeControl.SceneFacade, this.longOpNotifier);
			sf.ShowDialog();
		}

		private void contextMenuStrip_Closed(object sender, ToolStripDropDownClosedEventArgs e)
		{
			activeControl.SceneFacade.MakeToComputeVisibleNodes(); /**/
		}

		private void cutMeshToolStripMenuItem_Click(object sender, EventArgs e)
		{
			this.cutEditorForm = new CutEditorForm(activeControl.SceneFacade, this.longOpNotifier);
			this.cutEditorForm.ParentFormFocusNeeded += delegate { this.Focus(); };
			this.cutEditorForm.FormClosed += delegate { this.cutEditorForm = null; };
			this.cutEditorForm.Show();
		}

		private void showHideElementsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (activeControl.SceneFacade.ContainsMesh)
			{
				this.showHideElementsForm = new ShowHideElementsForm(activeControl.SceneFacade, this.longOpNotifier);
				this.showHideElementsForm.FormClosed += delegate { this.showHideElementsForm = null; };
				this.showHideElementsForm.Show();
			}
		}

		private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
		{
			activeControl.SceneFacade.PerformAction(AvailableAction.SelectAllItems);
		}

		private void unselectAllToolStripMenuItem_Click(object sender, EventArgs e)
		{
			activeControl.SceneFacade.PerformAction(AvailableAction.UnselectAllItems);
		}

		private void invertSelectionToolStripMenuItem_Click(object sender, EventArgs e)
		{
			activeControl.SceneFacade.PerformAction(AvailableAction.InvertSelection);
		}

		private void deleteSelectedElementsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			using (longOpNotifier.Begin("Deleting selected elements"))
			{
				activeControl.SceneFacade.PerformAction(AvailableAction.DeleteSelectedElements);
			}
		}

		private void restoreMeshToolStripMenuItem_Click(object sender, EventArgs e)
		{
			using (longOpNotifier.Begin("Restoring mesh"))
			{
				activeControl.SceneFacade.PerformAction(AvailableAction.RestoreMesh);
			}
		}

		private void splitActiveWindowToolStripMenuItem_Click(object sender, EventArgs e)
		{
			splitWindowToolStripMenuItem_Click(sender, e);
		}

		private void selectToolChosen(object sender, EventArgs e)
		{
			uncheckAllSelectToolMenuItems();

			if (sender == elementsToolStripMenuItem || sender == elementsToolStripMenuItem1)
				SceneFacade.EditorMode = EditorMode.SelectElements;
			else if (sender == nodesToolStripMenuItem || sender == nodesToolStripMenuItem1)
				SceneFacade.EditorMode = EditorMode.SelectNodes;
			else if (sender == facesToolStripMenuItem || sender == selectFacesToolStripMenuItem)
				SceneFacade.EditorMode = EditorMode.SelectFaces;
			else if (sender == edgesToolStripMenuItem || sender == edgesToolStripMenuItem1)
				SceneFacade.EditorMode = EditorMode.SelectEdges;
			else if (sender == beamsToolStripMenuItem || sender == beamsToolStripMenuItem1)
				SceneFacade.EditorMode = EditorMode.SelectBeams;
		}

		private void uncheckAllSelectToolMenuItems()
		{
			elementsToolStripMenuItem.Checked = nodesToolStripMenuItem.Checked = facesToolStripMenuItem.Checked = edgesToolStripMenuItem.Checked = beamsToolStripMenuItem.Checked = false;
			elementsToolStripMenuItem1.Checked = nodesToolStripMenuItem1.Checked = selectFacesToolStripMenuItem.Checked = edgesToolStripMenuItem1.Checked = beamsToolStripMenuItem1.Checked = false;
		}

		private void cameraToolChosen(object sender, EventArgs e)
		{
			uncheckAllCameraToolMenuItems();

			if (sender == orbitToolStripMenuItem || sender == orbitToolStripMenuItem1)
				SceneFacade.EditorMode = EditorMode.Orbit;
			else if (sender == panToolStripMenuItem || sender == panToolStripMenuItem1)
				SceneFacade.EditorMode = EditorMode.Pan;
			else if (sender == lookAroundToolStripMenuItem || sender == lookAroundToolStripMenuItem1)
				SceneFacade.EditorMode = EditorMode.LookAround;
			else if (sender == zoomWindowToolStripMenuItem || sender == zoomWindowToolStripMenuItem1)
				SceneFacade.EditorMode = EditorMode.ZoomWindow;
		}

		private void uncheckAllCameraToolMenuItems()
		{
			orbitToolStripMenuItem.Checked = panToolStripMenuItem.Checked = lookAroundToolStripMenuItem.Checked = zoomWindowToolStripMenuItem.Checked = false;
			orbitToolStripMenuItem1.Checked = panToolStripMenuItem1.Checked = lookAroundToolStripMenuItem1.Checked = zoomWindowToolStripMenuItem1.Checked = false;
		}

		private void selectToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
		{
			uncheckAllSelectToolMenuItems();
			switch (SceneFacade.EditorMode)
			{
				case EditorMode.SelectElements:
					elementsToolStripMenuItem.Checked = elementsToolStripMenuItem1.Checked = true;
					break;
				case EditorMode.SelectNodes:
					nodesToolStripMenuItem.Checked = nodesToolStripMenuItem1.Checked = true;
					break;
				case EditorMode.SelectFaces:
					facesToolStripMenuItem.Checked = selectFacesToolStripMenuItem.Checked = true;
					break;
				case EditorMode.SelectEdges:
					edgesToolStripMenuItem.Checked = edgesToolStripMenuItem1.Checked = true;
					break;
				case EditorMode.SelectBeams:
					beamsToolStripMenuItem.Checked = beamsToolStripMenuItem1.Checked = true;
					break;
			}
		}

		private void cameraToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			uncheckAllCameraToolMenuItems();
			switch (SceneFacade.EditorMode)
			{
				case EditorMode.Orbit:
					orbitToolStripMenuItem.Checked = orbitToolStripMenuItem1.Checked = true;
					break;
				case EditorMode.Pan:
					panToolStripMenuItem.Checked = panToolStripMenuItem1.Checked = true;
					break;
				case EditorMode.LookAround:
					lookAroundToolStripMenuItem.Checked = lookAroundToolStripMenuItem1.Checked = true;
					break;
				case EditorMode.ZoomWindow:
					zoomWindowToolStripMenuItem.Checked = zoomWindowToolStripMenuItem1.Checked = true;
					break;
			}

			closeWindowToolStripMenuItem.Visible = activeWindowCanBeClosed();
		}

		private void viewToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			//axesToolStripMenuItem.Checked = nodeNumbersToolStripMenuItem.Checked = false;
			bool value = (bool)activeControl.SceneFacade.GetValue(AvailableValue.DrawAxes);
			axesToolStripMenuItem.Checked = value;
			value = (bool)activeControl.SceneFacade.GetValue(AvailableValue.DrawAxisArrows);
			axisArrowsToolStripMenuItem.Checked = value;
			value = (bool)activeControl.SceneFacade.GetValue(AvailableValue.DrawBeams);
			beamsToolStripMenuItem2.Checked = value;
			value = (bool)activeControl.SceneFacade.GetValue(AvailableValue.DrawNodeNumbers);
			nodeNumbersToolStripMenuItem.Checked = value;
			value = (bool)activeControl.SceneFacade.GetValue(AvailableValue.DrawElementNumbers);
			elementNumbersToolStripMenuItem.Checked = value;
			value = (bool)activeControl.SceneFacade.GetValue(AvailableValue.DrawBeamNumbers);
			beamNumbersToolStripMenuItem.Checked = value;
			value = (bool)activeControl.SceneFacade.GetValue(AvailableValue.DrawQuadraticNodes);
			intermediateNodesToolStripMenuItem.Checked = value;

			value = (bool)activeControl.SceneFacade.GetValue(AvailableValue.NodeSignalIsSet);
			signalNodeToolStripMenuItem.Checked = value;
			value = (bool)activeControl.SceneFacade.GetValue(AvailableValue.ElementSignalIsSet);
			signalElementToolStripMenuItem.Checked = value;

			signalNodeToolStripMenuItem.Enabled = signalElementToolStripMenuItem.Enabled = activeControl.SceneFacade.ContainsMesh;
		}

		private void setPropertyOfSelectedItemsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			activeControl.SetPropertyOfSelectedItems();
		}

		private void selectIncidingItemsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			activeControl.SceneFacade.PerformAction(AvailableAction.SelectIncidingItems);
		}

		private void selectItemsByPropertyToolStripMenuItem_Click(object sender, EventArgs e)
		{
			activeControl.SelectItemsWithProperty(getNameOfItemsToSelectAcordingToEditorMode() ?? "elements");
		}

		private void invisibleSelectEntitiesByPropertyAddToolStripMenuItem_Click(object sender, EventArgs e)
		{
			activeControl.SelectItemsWithProperty(getNameOfItemsToSelectAcordingToEditorMode() ?? "elements", addToSelection: true);
		}

		private void cameraStandardViewToolStripMenuItem_Click(object sender, EventArgs e)
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

		private void editToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
		{
			const string prefix = "Select ";
			const string suffixInciding = " inciding with selected faces";
			const string suffixByProperty = " by property";
			//string selectToolName;
			string selectIncidingName, selectAllName;

			string nameOfItemsToSelect = getNameOfItemsToSelectAcordingToEditorMode() ?? "elements";

			switch (SceneFacade.EditorMode)
			{
				case EditorMode.SelectElements:
				case EditorMode.SelectEdges:
				case EditorMode.SelectNodes:
					selectIncidingItemsToolStripMenuItem.Enabled = true;
					selectIncidingName = selectAllName = nameOfItemsToSelect;
					break;
				default:
					selectIncidingItemsToolStripMenuItem.Enabled = false;
					selectIncidingName = "items";
					selectAllName = nameOfItemsToSelect;
					break;
			}

			selectIncidingItemsToolStripMenuItem.Text = prefix + selectIncidingName + suffixInciding;
			selectAllToolStripMenuItem.Text = "Select all " + selectAllName;
			selectItemsByPropertyToolStripMenuItem.Text = prefix + selectAllName + suffixByProperty;
			takeScreenshotToolStripMenuItem.Enabled = activeControl.SceneFacade.ContainsMesh;
		}

		private void renderMode_item_click(object sender, EventArgs e)
		{

			RenderMode mode = RenderMode.None;
			toolStripButtonPoints.Checked = toolStripButtonBorderLines.Checked = toolStripButtonAllLines.Checked = toolStripButtonFaces.Checked = false;
			// -------------------------------------------------------------------
			if (sender == pointsToolStripMenuItem)
			{
				mode = RenderMode.Points;
				toolStripButtonPoints.Checked = true;
			}
			else if (sender == borderLinesToolStripMenuItem)
			{
				mode = RenderMode.BorderLines;
				toolStripButtonBorderLines.Checked = true;
			}
			else if (sender == allLinesToolStripMenuItem)
			{
				mode = RenderMode.AllLines;
				toolStripButtonAllLines.Checked = true;
			}
			else if (sender == allLinesPointsToolStripMenuItem)
			{
				mode = RenderMode.LinesPoints;
				toolStripButtonAllLines.Checked = toolStripButtonPoints.Checked = true;
			}
			else if (sender == facesRenderToolStripMenuItem)
			{
				mode = RenderMode.Faces;
				toolStripButtonFaces.Checked = true;
			}
			else if (sender == facesBorderLinesToolStripMenuItem)
			{
				mode = RenderMode.FacesBorder;
				toolStripButtonFaces.Checked = toolStripButtonBorderLines.Checked = true;
			}
			else if (sender == facesAllLinesToolStripMenuItem)
			{
				mode = RenderMode.FacesLines;
				toolStripButtonFaces.Checked = toolStripButtonAllLines.Checked = true;
			}
			else if (sender == facesAllLinesPointsToolStripMenuItem)
			{
				mode = RenderMode.FacesLinesPoints;
				toolStripButtonFaces.Checked = toolStripButtonAllLines.Checked = toolStripButtonPoints.Checked = true;
			}
			// -------------------------------------------------------------------
			activeControl.SceneFacade.SetValue(AvailableValue.RenderMode, mode);
		}

		private void propertyColors_DropDownOpening(object sender, EventArgs e)
		{
			updateColorModeButtons();
		}

		private void updateColorModeButtons()
		{
			object parameter = activeControl.SceneFacade.GetValue(AvailableValue.ColorMode);
			if (parameter == null)
				return;
			PropertyColorsMode mode = (PropertyColorsMode)parameter;
			elementPropertyColorsToolStripMenuItem.Checked = elementPropertyColorsToolStripMenuItem2.Checked = (mode & PropertyColorsMode.Elements) != 0;
			nodePropertyColorsToolStripMenuItem.Checked = nodePropertyColorsToolStripMenuItem2.Checked = (mode & PropertyColorsMode.Nodes) != 0;
			facePropertyColorsToolStripMenuItem.Checked = facesPropertyColorsToolStripMenuItem2.Checked = (mode & PropertyColorsMode.Faces) != 0;
			edgePropertyColorsToolStripMenuItem.Checked = edgesPropertyColorsToolStripMenuItem2.Checked = (mode & PropertyColorsMode.Edges) != 0;
			beamPropertyColorsToolStripMenuItem.Checked = beamsPropertyColorsToolStripMenuItem2.Checked = (mode & PropertyColorsMode.Beams) != 0;
		}

		private void propertyColorsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			object parameter = activeControl.SceneFacade.GetValue(AvailableValue.ColorMode);
			PropertyColorsMode mode = PropertyColorsMode.None;
			if (parameter != null)
				mode = (PropertyColorsMode)parameter;

			if (sender == elementPropertyColorsToolStripMenuItem || sender == elementPropertyColorsToolStripMenuItem2)
			{
				mode ^= PropertyColorsMode.Elements;
				mode &= ~PropertyColorsMode.Faces; // vypnout barvy ploch
			}
			else if (sender == facePropertyColorsToolStripMenuItem || sender == facesPropertyColorsToolStripMenuItem2)
			{
				mode ^= PropertyColorsMode.Faces;
				mode &= ~PropertyColorsMode.Elements; // vypnout barvy prvku
			}
			else if (sender == nodePropertyColorsToolStripMenuItem || sender == nodePropertyColorsToolStripMenuItem2)
				mode ^= PropertyColorsMode.Nodes;
			else if (sender == edgePropertyColorsToolStripMenuItem || sender == edgesPropertyColorsToolStripMenuItem2)
				mode ^= PropertyColorsMode.Edges;
			else if (sender == beamPropertyColorsToolStripMenuItem || sender == beamsPropertyColorsToolStripMenuItem2)
				mode ^= PropertyColorsMode.Beams;

			activeControl.SceneFacade.SetValue(AvailableValue.ColorMode, mode);
			activeControl.SceneFacade.PerformAction(AvailableAction.UpdateColorBuffers);
		}

		private void midsideNodesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			intermediateNodesToolStripMenuItem.Checked = !intermediateNodesToolStripMenuItem.Checked;
			activeControl.SceneFacade.SetValue(AvailableValue.DrawQuadraticNodes, intermediateNodesToolStripMenuItem.Checked);
		}

		private void zoomToFitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			activeControl.SceneFacade.PerformAction(AvailableAction.ZoomToFit);
		}

		private void aboutToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			AboutDialog dialog = new AboutDialog();
			dialog.ShowDialog();
		}

		private void closeActiveMeshToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (activeControl.SceneFacade.ContainsMesh)
			{
				List<OpenGLControl> controls = new List<OpenGLControl>();
				controls.Add(activeControl);
				CancelEventArgs args = new CancelEventArgs();
				askToSaveChanges(controls, true, args);
				if (args.Cancel)
					return;
				// -----------------------------------
				activeControl.ClearScene();
				activeControl.SceneFacade.PerformAction(AvailableAction.Refresh);
				// -----------------------------------
				// tenhle prikaz je hlavne kvuli setreni pameti, takze po nem proved sber odpadku
				GC.Collect();
			}

			removeActiveWindow();
		}

		private bool removeActiveWindow()
		{
			if (!(activeControl.MyContainer is SplitterPanel))
				return false;
			SplitContainer container = activeControl.MyContainer.Parent as SplitContainer;
			if (container == null)
				return false;
			SplitterPanel toRemove = (SplitterPanel)activeControl.MyContainer;
			SplitterPanel toExtend = (container.Panel1 == activeControl.MyContainer) ? container.Panel2 : container.Panel1;
			if (removePanel(toRemove))
			{
				extendPanel(toExtend);
				return true;
			}
			return false;
		}

		private void refreshToolStripMenuItem_Click(object sender, EventArgs e)
		{
			activeControl.SceneFacade.PerformAction(AvailableAction.Refresh);
		}

		private void listOfSelectedItemsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (!activeControl.SceneFacade.ContainsMesh)
				return;
			ListOfSelectedItemsForm form;
			using (longOpNotifier.Begin("Creating list of selected items"))
			{
				form = new ListOfSelectedItemsForm(activeControl.SceneFacade);
			}
			form.ShowDialog();
		}

		private void axesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			bool value = (bool)activeControl.SceneFacade.GetValue(AvailableValue.DrawAxes);
			value = !value;
			activeControl.SceneFacade.SetValue(AvailableValue.DrawAxes, value);
			axesToolStripMenuItem.Checked = value;
		}

		private void axisArrowsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			bool value = (bool)activeControl.SceneFacade.GetValue(AvailableValue.DrawAxisArrows);
			value = !value;
			activeControl.SceneFacade.SetValue(AvailableValue.DrawAxisArrows, value);
			axisArrowsToolStripMenuItem.Checked = value;
		}

		private void beamsToolStripMenuItem2_Click(object sender, EventArgs e)
		{
			bool value = (bool)activeControl.SceneFacade.GetValue(AvailableValue.DrawBeams);
			value = !value;
			activeControl.SceneFacade.SetValue(AvailableValue.DrawBeams, value);
			beamsToolStripMenuItem2.Checked = value;
		}

		private void signalNodeToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (activeControl.SceneFacade.ContainsMesh)
			{
				bool signalIsSet = (bool)activeControl.SceneFacade.GetValue(AvailableValue.NodeSignalIsSet);
				if (signalIsSet)
					activeControl.SignalNodeByID(null); // clear signal
				else
					activeControl.SignalNodeByID(/*, longOpNotifier*/); // ask user for node ids
			}
		}

		private void signalElementToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (activeControl.SceneFacade.ContainsMesh)
			{
				bool value = (bool)activeControl.SceneFacade.GetValue(AvailableValue.ElementSignalIsSet);
				activeControl.SignalElementByID(value, longOpNotifier);
			}
		}

		private void signalDataMaximumToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (activeControl.SceneFacade.ContainsMesh)
			{
				IDataVisualizer dataVisualizer = activeControl.SceneFacade.GetValue(AvailableValue.DataVisualizer) as IDataVisualizer;
				if (dataVisualizer != null)
				{
					activeControl.SignalNodeByID(dataVisualizer.GetIDsOfNodesWithMaximumDataValue());
				}
			}
		}

		private void signalDataMinimumToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (activeControl.SceneFacade.ContainsMesh)
			{
				IDataVisualizer dataVisualizer = activeControl.SceneFacade.GetValue(AvailableValue.DataVisualizer) as IDataVisualizer;
				if (dataVisualizer != null)
				{
					activeControl.SignalNodeByID(dataVisualizer.GetIDsOfNodesWithMinimumDataValue());
				}
			}
		}

		private async void checkForUpdatesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			try
			{
				bool updateExists = await checkForUpdatesAsync(maxVersionToIgnoreString: null);
				if (!updateExists)
				{
					MessageBox.Show("You already have the latest version installed.", "No update available", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
			}
			catch (Exception ex)
			{
				OpenGLControl.ShowErrorMessage("Can't check for updates", ex.Message);
			}
		}

		#endregion

		#region Helper methods

		private async void checkForUpdatesSilently()
		{
			try
			{
				var success = await checkForUpdatesAsync(maxVersionToIgnoreString: mainWindowSettings.LastCheckedUpdateVersion); // swallow exceptions
			}
			catch { }
		}

		private async Task<bool> checkForUpdatesAsync(string maxVersionToIgnoreString)
		{
			var updateChecker = new UpdateChecker();

			bool updateExists = await updateChecker.CheckForUpdates();

			mainWindowSettings.LastCheckedUpdateVersion = updateChecker.ServerVersion.ToString();

			if (updateExists)
			{
				Version maxVersionToIgnore;
				if (!Version.TryParse(maxVersionToIgnoreString, out maxVersionToIgnore) && maxVersionToIgnore >= updateChecker.ServerVersion)
					return false;

				StringBuilder questionTextBuilder = new StringBuilder();

				questionTextBuilder.AppendLine("There is a new version of this application.");
				questionTextBuilder.AppendLine("New version: " + updateChecker.ServerVersion);
				questionTextBuilder.AppendLine("Current version: " + updateChecker.CurrentVersion);
				questionTextBuilder.AppendLine();
				questionTextBuilder.Append("Do you want to download the new version?");

				var dialogResult = MessageBox.Show(questionTextBuilder.ToString(), "New version available", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
				if (dialogResult == DialogResult.Yes)
				{
					Process.Start(updateChecker.PackageFileUri); // start web browser with package file uri
					this.Close(); // close application
				}
			}
			return updateExists;
		}

		private PreprocessViewControl getCurrentPreprocessView()
		{
			return centralPanel.Controls.OfType<PreprocessViewControl>().SingleOrDefault();
		}

		private PostprocessViewControl getCurrentPostprocessView()
		{
			return centralPanel.Controls.OfType<PostprocessViewControl>().SingleOrDefault();
		}

		private void clearContentView()
		{
			switch (LayoutMode)
			{
				case LayoutMode.None:
					centralPanel.Controls.Clear();
					break;
				case LayoutMode.Preprocessor:
				case LayoutMode.Postprocessor:
					var contentView = centralPanel.Controls.Cast<ContentViewControl>().Single();
					contentView.Content = null;
					centralPanel.Controls.Remove(contentView);
					contentView.Dispose();
					break;
				default:
					throw new NotSupportedException();
			}
		}

		private void initializeMainOpenGlControl(Control contentContainer)
		{
			OpenGLControl mainOpenGLControl = OpenGLControl.Create(null, openGLControl_MouseDown);
			contentContainer.Controls.Add(mainOpenGLControl);

			mainOpenGLControl.MyContainer = contentContainer;
			mainOpenGLControl.Dock = DockStyle.Fill;

			registerNewControl(mainOpenGLControl);
			activateControl(mainOpenGLControl); // zakladni opengl okno je nyni ulozeno v activeControl
		}

		private void askToSaveChanges(List<OpenGLControl> controls, bool canBeCancelled, CancelEventArgs e)
		{
			string caption = "Save changes?";

			HashSet<int> processedMeshes = new HashSet<int>();
			// ----------------------------------------------------
			// zajistit, ze se me to nebude ptat na site, ktere jsou jeste otevrene
			foreach (OpenGLControl c in openGLControls)
				if (c.SceneFacade.ContainsMesh && !controls.Contains(c))
					processedMeshes.Add(c.SceneFacade.ActiveMeshUniqueIdentifier.Value);
			// ----------------------------------------------------
			// TODO: iterating only over active meshes, there may be more meshes in each control
			foreach (OpenGLControl control in controls)
			{
				if (control.SceneFacade.ContainsMesh && processedMeshes.Add(control.SceneFacade.ActiveMeshUniqueIdentifier.Value))
				{
					bool unsaved = (bool)control.SceneFacade.GetValue(AvailableValue.UnsavedChangesInMesh);
					if (unsaved)
					{
						string text = "Do you want to save changes to '" + control.SceneFacade.MeshName + "'?";
						DialogResult result = MessageBox.Show(text, caption, canBeCancelled ? MessageBoxButtons.YesNoCancel : MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
						if (result == DialogResult.Cancel) // zrusit
						{
							e.Cancel = true;
							return;
						}
						else if (result == DialogResult.Yes) // ulozit
						{
							if (!saveMeshChooseFile(control))
							{
								if (canBeCancelled)
								{
									e.Cancel = true;
									return;
								}
							}
						}
					}
				}
			}

			// -------------------------------------------------
			// zkontrolovat, jestli jsou vsechny operace dokoncene
			foreach (OpenGLControl control in controls)
			{
				if (!control.IOActionInProgress)
					continue;

				while (control.IOActionInProgress)
				{
					Thread.Sleep(50);
					Application.DoEvents();
				}
				if (!control.IOProcessedFinishedCorrectly)
				{
					e.Cancel = true;
					return;
				}
			}

		}

		private void initLongOpNotifier()
		{
			longOpNotifier = new LongOpNotifier();

			longOpNotifier.HasBegun += (token, isCancellable) =>
			{
				this.Cursor = Cursors.WaitCursor;
				activeControl.Cursor = Cursors.WaitCursor;
				string stateText = longOpNotifier.GetState(token).ToString();
				statusLabel.Text = string.IsNullOrEmpty(stateText) ? "Operation in progress..." : stateText;
				statusLabel.ForeColor = Color.Blue;
				statusStrip.Refresh();

				setupProgressViewTimer(token, isCancellable);
			};
			longOpNotifier.HasEnded += token =>
			{
				this.Cursor = Cursors.Default;
				activeControl.SetCursorAccordingToEditorMode();
				statusLabel.ForeColor = Color.Black;
				updateStatus();
				ProgressViewForm progressViewForm;
				if (progressViewForms.TryGetValue(token, out progressViewForm))
				{
					progressViewForm.Quit();
					progressViewForm = null;
					progressViewForms.Remove(token);
				}
			};
			longOpNotifier.ProgressChanged += token =>
			{
				Action<LongOpNotifier.Token> reportAction = reportOperationProgress;
				this.Invoke(reportAction, token); // dispatch to UI thread
			};
		}

		private void reportOperationProgress(LongOpNotifier.Token operationToken)
		{
			LongOpNotifier.State operationState = longOpNotifier.GetState(operationToken);
			statusLabel.Text = operationState.ToString();
			statusStrip.Refresh();

			ProgressViewForm progressViewForm;
			if (progressViewForms.TryGetValue(operationToken, out progressViewForm))
			{
				progressViewForm.Caption = operationState.TaskName;
				progressViewForm.OperationName = operationState.OperationName;
				progressViewForm.SetProgressState(operationState.PercentDone);
			}
		}

		private void setupProgressViewTimer(LongOpNotifier.Token operationToken, bool isCancellable)
		{
			System.Windows.Forms.Timer delayTimer = new System.Windows.Forms.Timer();
			delayTimer.Interval = 500;
			delayTimer.Tick += delegate
			{
				delayTimer.Stop();
				if (longOpNotifier.IsRunning(operationToken))
				{
					Debug.Assert(!progressViewForms.ContainsKey(operationToken));
					LongOpNotifier.State state = longOpNotifier.GetState(operationToken);
					ProgressViewForm progressViewForm = new ProgressViewForm(state.TaskName ?? "Operation in progress...", isCancellable);
					progressViewForms[operationToken] = progressViewForm;
					if (isCancellable)
						progressViewForm.Cancel += (s, e) => longOpNotifier.Cancel(operationToken);
					progressViewForm.OperationName = state.OperationName;
					progressViewForm.SetProgressState(state.PercentDone);

					this.Cursor = Cursors.Default;
					activeControl.SetCursorAccordingToEditorMode();

					progressViewForm.Show();
				}
			};
			delayTimer.Start();
		}

		private void setRenderModeAcordingToToolStripButtons()
		{
			RenderMode mode = RenderMode.None;
			if (toolStripButtonPoints.Checked)
				mode |= RenderMode.Points;
			if (toolStripButtonBorderLines.Checked)
				mode |= RenderMode.BorderLines;
			if (toolStripButtonAllLines.Checked)
				mode |= RenderMode.AllLines;
			if (toolStripButtonFaces.Checked)
				mode |= RenderMode.Faces;
			activeControl.SceneFacade.SetValue(AvailableValue.RenderMode, mode);
		}

		//private bool getNameOfToolSelectIncidingItems(out string name)
		//{
		//    switch (SceneFacade.EditorMode)
		//    {
		//        case EditorMode.SelectElements:
		//            name = "elements";
		//            return true;
		//        case EditorMode.SelectEdges:
		//            name = "edges";
		//            return true;
		//        case EditorMode.SelectNodes:
		//            name = "nodes";
		//            return true;
		//        case EditorMode.SelectBeams:
		//            name = "beams";
		//            return false;
		//        case EditorMode.SelectFaces:
		//            name = "faces";
		//            return false;
		//    }
		//    name = null;
		//    return false;
		//}

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

		private void registerNewControl(OpenGLControl openGLControl)
		{
			openGLControl.ContextMenuStrip = this.contextMenuStrip;

			//openGLControl.MouseDown += openGLControl_MouseDown; // uz se predava v konstruktoru OpenGLControl
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
			openGLControl.ScreenshotNeeded += (sender, args) =>
			{
				saveScreenshot(sender as OpenGLControl, args.ScreenshotWindow);
			};

			this.openGLControls.Add(openGLControl);
		}

		private void updateRenderModeButtons()
		{
			object parameter = activeControl.SceneFacade.GetValue(AvailableValue.RenderMode);
			if (parameter == null)
				return;
			RenderMode mode = (RenderMode)parameter;
			toolStripButtonFaces.Checked = (mode & RenderMode.Faces) != 0;
			toolStripButtonPoints.Checked = (mode & RenderMode.Points) != 0;
			toolStripButtonBorderLines.Checked = (mode & RenderMode.BorderLines) != 0;
			toolStripButtonAllLines.Checked = (mode & RenderMode.AllLines) != 0;
			// ---------------------------------------------------------
			// menu
			pointsToolStripMenuItem.Checked = mode == RenderMode.Points;
			borderLinesToolStripMenuItem.Checked = mode == RenderMode.BorderLines;
			allLinesToolStripMenuItem.Checked = mode == RenderMode.AllLines;
			allLinesPointsToolStripMenuItem.Checked = mode == (RenderMode.AllLines | RenderMode.Points);
			facesRenderToolStripMenuItem.Checked = mode == RenderMode.Faces;
			facesBorderLinesToolStripMenuItem.Checked = mode == RenderMode.FacesBorder;
			facesAllLinesToolStripMenuItem.Checked = mode == RenderMode.FacesLines;
			facesAllLinesPointsToolStripMenuItem.Checked = mode == RenderMode.FacesLinesPoints;
		}

		void openGLControl_MouseDown(object sender, MouseEventArgs e)
		{
			if (activeControl != sender)
				activateControl((OpenGLControl)sender);
		}

		private void unregisterControl(OpenGLControl toRemove)
		{
			if (toRemove == activeControl) // pokud je tento prvek aktivni, zneaktivnim ho
				activeControl = null;
			toRemove.MeshNeedRefresh -= meshNeedRefreshHandler;
			toRemove.MouseDown -= openGLControl_MouseDown;
			openGLControls.Remove(toRemove); // odstranim prvek ze seznamu opengl ovladacich prvku
			toRemove.DisposeScene(); // vycistim objekt scena daneho prvku - tento objekt obsahuje objekt typu mesh - hlavne ten potrebuju dostat z pameti
		}

		private void activateControl(OpenGLControl openGLControl)
		{
			this.activeControl = openGLControl;
			this.activeControl.IsActive = true;
			activeControl.MakeCurrent();
			// updatovat seznam viditelnych uzlu
			activeControl.SceneFacade.PerformAction(AvailableAction.UpdateVisibleNodes);
			activeControl.Refresh();

			foreach (OpenGLControl c in openGLControls)
			{
				if (c != activeControl && c.IsActive)
				{
					c.IsActive = false;
					c.Refresh();
				}
			}
			activeControl.Focus(); // jeste mu dam fokus

			updateCaption();
			updateStatus();
			updateColorModeButtons();
			updateRenderModeButtons();

			// update postprocess view
			var postprocessView = getCurrentPostprocessView();
			if (postprocessView != null)
				postprocessView.ActiveScene = activeControl.SceneFacade;
		}

		private void updateCaption()
		{
			string caption = "Mesh Editor"; /**/
			if (activeControl != null)
			{
				string sceneTitle = activeControl.SceneFacade.Title;
				if (!string.IsNullOrEmpty(sceneTitle))
				{
					caption += " - " + sceneTitle;
				}
				//string meshName = activeControl.SceneFacade.MeshName;
				//if (!string.IsNullOrEmpty(meshName))
				//{
				//	caption += " - " + meshName;
				//}
			}
			this.Text = caption;
		}

		private void updateStatus()
		{
			if (statusLabel.ForeColor == Color.Black)
			{
				string desc = (string)activeControl.SceneFacade.GetValue(AvailableValue.Status);
				statusLabel.Text = string.IsNullOrEmpty(desc) ? "Ready" : desc;
			}
		}

		private bool activeWindowCanBeClosed()
		{
			return activeControl.MyContainer is SplitterPanel;
		}

		private MainWindowSettings loadMainWindowSettings()
		{
			var mainFormSettings = ConfigurationManager.ReadConfigurationObject<MainWindowSettings>("MainWindowSettings") ?? new MainWindowSettings();
			if (mainFormSettings.State == FormWindowState.Normal)
			{
				this.Width = mainFormSettings.Width;
				this.Height = mainFormSettings.Height;

				this.Left = mainFormSettings.PositionLeft;
				this.Top = mainFormSettings.PositionTop;
			}
			this.WindowState = mainFormSettings.State; // must be AFTER setting window position (Left, Top)
			return mainFormSettings;
		}

		private void saveMainWindowSettings()
		{
			mainWindowSettings.State = this.WindowState;
			if (this.WindowState == FormWindowState.Normal)
			{
				mainWindowSettings.Width = this.Width;
				mainWindowSettings.Height = this.Height;
				mainWindowSettings.PositionLeft = this.Left;
				mainWindowSettings.PositionTop = this.Top;
			}
			mainWindowSettings.LastLoadedMesh = activeControl.SceneFacade.MeshSourceFileName;
			ConfigurationManager.WriteConfigurationObject("MainWindowSettings", mainWindowSettings);
		}

		private void fileToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
		{
			closeActiveMeshToolStripMenuItem.Enabled = activeControl.SceneFacade.ContainsMesh;
		}

		#endregion

		#region Tool strip handlers

		private void nodeNumbersToolStripMenuItem_Click(object sender, EventArgs e)
		{
			bool value = (bool)activeControl.SceneFacade.GetValue(AvailableValue.DrawNodeNumbers);
			value = !value;
			activeControl.SceneFacade.SetValue(AvailableValue.DrawNodeNumbers, value);
			nodeNumbersToolStripMenuItem.Checked = value;
		}

		private void elementNumbersToolStripMenuItem_Click(object sender, EventArgs e)
		{
			bool value = (bool)activeControl.SceneFacade.GetValue(AvailableValue.DrawElementNumbers);
			value = !value;
			activeControl.SceneFacade.SetValue(AvailableValue.DrawElementNumbers, value);
			elementNumbersToolStripMenuItem.Checked = value;
		}

		private void beamNumbersToolStripMenuItem_Click(object sender, EventArgs e)
		{
			bool value = (bool)activeControl.SceneFacade.GetValue(AvailableValue.DrawBeamNumbers);
			value = !value;
			activeControl.SceneFacade.SetValue(AvailableValue.DrawBeamNumbers, value);
			beamNumbersToolStripMenuItem.Checked = value;
		}

		private void toolStripButtonPoints_Click(object sender, EventArgs e)
		{
			toolStripButtonPoints.Checked = !toolStripButtonPoints.Checked;
			setRenderModeAcordingToToolStripButtons();
		}

		private void toolStripButtonBorderLines_Click(object sender, EventArgs e)
		{
			toolStripButtonBorderLines.Checked = !toolStripButtonBorderLines.Checked;
			if (toolStripButtonBorderLines.Checked)
				toolStripButtonAllLines.Checked = false;
			setRenderModeAcordingToToolStripButtons();
		}

		private void toolStripButtonAllLines_Click(object sender, EventArgs e)
		{
			toolStripButtonAllLines.Checked = !toolStripButtonAllLines.Checked;
			if (toolStripButtonAllLines.Checked)
				toolStripButtonBorderLines.Checked = false;
			setRenderModeAcordingToToolStripButtons();
		}

		private void toolStripButtonFaces_Click(object sender, EventArgs e)
		{
			toolStripButtonFaces.Checked = !toolStripButtonFaces.Checked;
			setRenderModeAcordingToToolStripButtons();
		}

		private void toolStripButtonEditorModeTool_Click(object sender, EventArgs e)
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
		}

		private void toolStripButtonSelectItemsByProperty_MouseEnter(object sender, EventArgs e)
		{
			string itemsName = getNameOfItemsToSelectAcordingToEditorMode() ?? "elements";
			toolStripButtonSelectItemsByProperty.Text = "Select " + itemsName + " by property (F6)";
		}

		private void takeScreenshotToolStripMenuItem_Click(object sender, EventArgs e)
		{
			// open screenshot options dialog which has two options: take whole screen or take selection window

			var screenshotOptionsForm = new ScreenshotOptionsForm();
			if (screenshotOptionsForm.ShowDialog() == DialogResult.OK)
			{
				if (screenshotOptionsForm.UseSelectionArea)
				{
					SceneFacade.EditorMode = EditorMode.ScreenshotWindow;
				}
				else // take whole screen
				{
					saveScreenshot(activeControl, Rectangle.Empty);
				}
			}
		}

		private void configurePropertyColorsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			var configurePropertyColorsForm = new ConfigurePropertyColorsForm(openGLControls.Select(c => c.SceneFacade), (IEnumerable<Property>)activeControl.SceneFacade.GetValue(AvailableValue.AllMeshPropertiesSorted));
			configurePropertyColorsForm.ShowDialog();
		}

		private void saveScreenshot(OpenGLControl control, Rectangle screenshotWindow)
		{
			Debug.Assert(control != null);
			SaveFileDialog dialog = new SaveFileDialog();
			dialog.Filter = "PNG image format (*.png)|*.png|JPEG image format (*.jpg; *.jpeg)|*.jpg;*.jpeg|BMP image format (*.bmp)|*.bmp";
			dialog.FilterIndex = this.takeScreenshotLastFilterIndex;
			if (takeScreenshotLastFilename != null)
			{
				dialog.FileName = takeScreenshotLastFilename;
			}
			else
			{
				if (control.SceneFacade.ContainsMesh)
				{
					//dialog.InitialDirectory = Path.GetDirectoryName(activeControl.SceneFacade.MeshFilename);
					dialog.FileName = Utilities.Functions.MakeTextValidFilename(control.SceneFacade.MeshName);
				}
			}
			if (dialog.ShowDialog() == DialogResult.OK)
			{
				System.Drawing.Imaging.ImageFormat imageFormat;
				switch (Path.GetExtension(dialog.FileName).ToLower())
				{
					case ".jpg":
					case ".jpeg":
						imageFormat = System.Drawing.Imaging.ImageFormat.Jpeg;
						break;
					case ".bmp":
						imageFormat = System.Drawing.Imaging.ImageFormat.Bmp;
						break;
					case ".png":
						imageFormat = System.Drawing.Imaging.ImageFormat.Png;
						break;
					default:
						MessageBox.Show("This file extension (image format) is not supported.", "Can't take screenshot", MessageBoxButtons.OK, MessageBoxIcon.Error);
						return;
				}

				using (Bitmap screenshot = control.TakeScreenshot(screenshotWindow))
				{
					screenshot.Save(dialog.FileName, imageFormat); // image format must correspond to file extension (.png)
				}

				this.takeScreenshotLastFilterIndex = dialog.FilterIndex;
				this.takeScreenshotLastFilename = Path.GetFileName(dialog.FileName);
			}
		}

		private void postprocessToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
		{
			closeSolutionToolStripMenuItem.Enabled = LayoutMode == LayoutMode.Postprocessor;
			signalDataMinimumToolStripMenuItem.Enabled = signalDataMaximumToolStripMenuItem.Enabled = (activeControl.SceneFacade.GetValue(AvailableValue.DataVisualizer) as IDataVisualizer)?.DisplayColors ?? false;
		}

		private async void importFEMResultsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			var importFEMResultsForm = new ImportFEMResultsForm { Owner = this };
			if (importFEMResultsForm.ShowDialog() == DialogResult.OK)
			{
				Debug.Assert(!string.IsNullOrEmpty(importFEMResultsForm.SolutionFileName));
				Debug.Assert(File.Exists(importFEMResultsForm.SolutionFileName));

				setPostprocessorLayoutMode();
				await getCurrentPostprocessView().LoadLocalSolutionAsync(importFEMResultsForm.SolutionFileName);
			}
		}

		private async void openSolutionToolStripMenuItem_Click(object sender, EventArgs e)
		{
			var solutionBrowserForm = new SolutionBrowserForm { Owner = this };

			if (solutionBrowserForm.ShowDialog() == DialogResult.OK)
			{
				Debug.Assert(solutionBrowserForm.SolutionLocation == SolutionBrowserForm.SolutionLocationType.Local || solutionBrowserForm.SolutionLocation == SolutionBrowserForm.SolutionLocationType.Remote);

				setPostprocessorLayoutMode();
				var postprocessView = getCurrentPostprocessView();

				switch (solutionBrowserForm.SolutionLocation)
				{
					case SolutionBrowserForm.SolutionLocationType.Local:
						Debug.Assert(solutionBrowserForm.LocalSolutionFileName != null);
						await postprocessView.LoadLocalSolutionAsync(solutionBrowserForm.LocalSolutionFileName);
						break;
					case SolutionBrowserForm.SolutionLocationType.Remote:
						Debug.Assert(solutionBrowserForm.RemoteSolutionId.HasValue);
						await postprocessView.LoadRemoteSolutionAsync(solutionBrowserForm.RemoteSolutionId.Value);
						break;
				}
			}
		}

		private void closeSolutionToolStripMenuItem_Click(object sender, EventArgs e)
		{
			setPreprocessorLayoutMode();
		}

		private void closeAllScenes()
		{
			if (activeControl == null)
				return;

			// close all open views, leave only one empty
			while (true)
			{
				activeControl.SceneFacade.SetValue(AvailableValue.DataVisualizer, null);
				activeControl.ClearScene();
				if (!removeActiveWindow())
					break;
			}

			updateCaption();
			updateStatus();
			updateColorModeButtons();
			updateRenderModeButtons();
		}

		private void setPreprocessorLayoutMode()
		{
			if (LayoutMode == LayoutMode.Postprocessor)
				closeAllScenes();
			Control content = centralPanel.Controls.Cast<ContentViewControl>().SingleOrDefault()?.Content ?? new Panel { Dock = DockStyle.Fill };
			clearContentView();
			layoutMode = LayoutMode.Preprocessor;
			var preprocessView = new PreprocessViewControl { Content = content, Dock = DockStyle.Fill };
			centralPanel.Controls.Add(preprocessView);
			activeControl?.Focus();
		}

		private void setPostprocessorLayoutMode()
		{
			closeAllScenes();
			Control content = centralPanel.Controls.Cast<ContentViewControl>().SingleOrDefault()?.Content ?? new Panel { Dock = DockStyle.Fill };
			clearContentView();
			layoutMode = LayoutMode.Postprocessor;
			var postprocessView = new PostprocessViewControl(longOpNotifier) { Content = content, Dock = DockStyle.Fill };
			centralPanel.Controls.Add(postprocessView);
			activeControl.SetNewScene(postprocessView.ActiveScene);
			activeControl.Focus();
		}

		#endregion

	}
}
