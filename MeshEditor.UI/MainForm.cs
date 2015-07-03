using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using OpenTK.Graphics.OpenGL;
using MeshEditor.Graphics;
using MeshEditor.IO;
using MeshEditor.Data;
using MeshEditor.Construction;
using System.IO;
using System.Diagnostics;
using MeshEditor.WinUI;
using MeshEditor.Utilities;
using MeshEditor.CoreInterface;
using System.ComponentModel;
using OpenTK;
using Wintellect.PowerCollections;
using System.Threading;

namespace MeshEditor.WinUI
{
	/// <summary>
	/// Trida reprezentujici hlavni okno programu
	/// </summary>
	public partial class MainForm : Form
	{

		#region Fields, Constructor

		private OpenGLControl activeControl;
		private List<OpenGLControl> openGLControls;
		private LongOpNotifier longOpNotifier;

		private CutEditorForm cutEditorForm;
		private ShowHideElementsForm showHideElementsForm;

		private string settingsFilePath, userGuidFilePath;

		public const int PANEL_MINSIZE = 30;

		private string[] arguments;

		public LongOpNotifier LongOpNotifier
		{
			get { return longOpNotifier; }
		}
		

		public MainForm(string[] args)
		{
			Toolkit.Init();

			InitializeComponent();

			this.settingsFilePath = Path.Combine(Application.StartupPath, SceneFacade.AppSettingsFilename);
			this.userGuidFilePath = Path.Combine(Application.StartupPath, SceneFacade.UserGuideFileName);

			SceneFacade.EditorModeChanged += new EventHandler(editorModeChanged);
			SceneFacade.ShowError += new ShowErrorEventHandler(SceneFacade_ShowError);

			this.cutEditorForm = null;
			this.showHideElementsForm = null;
			this.openGLControls = new List<OpenGLControl>();
			this.arguments = args;
			this.longOpNotifier = null;
			initLongOpNotifier();


			// !!!
			// using(){} otestovat depth-buffer bits; pokud je 16, tak nastavit priznak a pak volat OpenGLControl s parametrem


			OpenGLControl mainOpenGLControl = OpenGLControl.Create(null, openGLControl_MouseDown);
			
			

			// musi byt inicializovat opengl kontext
			AppSettings.LoadFromFile(this.settingsFilePath);

            this.centralPanel.Controls.Add(mainOpenGLControl);
            mainOpenGLControl.MyContainer = this.centralPanel;
            mainOpenGLControl.Dock = DockStyle.Fill;

			registerNewControl(mainOpenGLControl);
			activateControl(mainOpenGLControl); // zakladni opengl okno je nyni ulozeno v activeControl

			updateCaption();
			editorModeChanged(null, null);
		}

		#endregion

		#region Overrides

		protected override void OnShown(EventArgs e)
		{
			base.OnShown(e);

			if (arguments != null && arguments.Length > 0) // nactu soubor v argumentu
			{
				activeControl.LoadFile(arguments[0]);
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
			base.OnClosed(e);
			foreach (OpenGLControl c in openGLControls)
				c.DisposeScene();

			// ulozit nastaveni do souboru
			AppSettings.SaveToFile(this.settingsFilePath);
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
			toolStripButtonRotateZ.Checked = (mode == EditorMode.RotateZ);
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
				if (c != sender && (string.IsNullOrEmpty(ea.MeshToRefresh) || c.SceneFacade.MeshFilename == ea.MeshToRefresh))
					c.Invalidate();
			}
		}

		private void toolsToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
		{
			restoreCuttedItemsToolStripMenuItem.Enabled = (bool)activeControl.SceneFacade.GetValue(AvailableValue.MeshHasHiddenElements);
			closeActiveWindowToolStripMenuItem.Enabled = activeWindowCanBeClosed();
			bool containsMesh = activeControl.SceneFacade.ContainsMesh;
			listOfSelectedItemsToolStripMenuItem.Enabled = showHideElementsToolStripMenuItem.Enabled = cutsToolStripMenuItem.Enabled = meshInfoToolStripMenuItem.Enabled = invertAllNormalsToolStripMenuItem.Enabled = containsMesh;
		}

		private void openToolStripMenuItem_Click(object sender, EventArgs e)
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

			openFileDialog.Filter = SceneFacade.InputFileFormatFilter;
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
				activeControl.LoadFile(openFileDialog.FileName);
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
			saveFileDialog.FileName = Path.GetFileNameWithoutExtension(control.SceneFacade.MeshFilename);
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
			string filename = Path.GetFileName(control.SceneFacade.MeshFilename);
			return MessageBox.Show("Save mesh " + filename + " without hidden elements?" + Environment.NewLine + "(Click \"Yes\" to save mesh as it is or \"No\" to save entire mesh in original form.)", filename + " has some hidden elements", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
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
			this.showHideElementsForm = new ShowHideElementsForm(activeControl.SceneFacade, this.longOpNotifier);
			this.showHideElementsForm.FormClosed += delegate { this.showHideElementsForm = null; };
			this.showHideElementsForm.Show();
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
			longOpNotifier.Begin();
			activeControl.SceneFacade.PerformAction(AvailableAction.DeleteSelectedElements);
			longOpNotifier.End();
		}

		private void restoreMeshToolStripMenuItem_Click(object sender, EventArgs e)
		{
			longOpNotifier.Begin();
			activeControl.SceneFacade.PerformAction(AvailableAction.RestoreMesh);
			longOpNotifier.End();
		}

		private void invertAllNormalsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			activeControl.SceneFacade.PerformAction(AvailableAction.InvertAllNormals);
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
			else if (sender == toolStripMenuItemRotateZ || sender == toolStripMenuItemRotateZ1)
				SceneFacade.EditorMode = EditorMode.RotateZ;
		}

		private void uncheckAllCameraToolMenuItems()
		{
			orbitToolStripMenuItem.Checked = panToolStripMenuItem.Checked = lookAroundToolStripMenuItem.Checked = zoomWindowToolStripMenuItem.Checked = false;
			orbitToolStripMenuItem1.Checked = panToolStripMenuItem1.Checked = lookAroundToolStripMenuItem1.Checked = zoomWindowToolStripMenuItem1.Checked = false;
			toolStripMenuItemRotateZ.Checked = toolStripMenuItemRotateZ1.Checked = false;
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
				case EditorMode.RotateZ:
					toolStripMenuItemRotateZ.Checked = toolStripMenuItemRotateZ1.Checked = true;
					break;
			}

			closeWindowToolStripMenuItem.Visible = activeWindowCanBeClosed();
		}
		
		private void viewToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			//axesToolStripMenuItem.Checked = nodeNumbersToolStripMenuItem.Checked = false;
			bool value = (bool)activeControl.SceneFacade.GetValue(AvailableValue.DrawAxes);
			axesToolStripMenuItem.Checked = value;
			value = (bool)activeControl.SceneFacade.GetValue(AvailableValue.DrawBeams);
			beamsToolStripMenuItem2.Checked = value;
			value = (bool)activeControl.SceneFacade.GetValue(AvailableValue.DrawNodeNumbers);
			nodeNumbersToolStripMenuItem.Checked = value;
			value = (bool)activeControl.SceneFacade.GetValue(AvailableValue.DrawElementNumbers);
			elementNumbersToolStripMenuItem.Checked = value;
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
			else if(sender == allLinesPointsToolStripMenuItem)
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

		private void readDocumentationToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (!File.Exists(this.userGuidFilePath))
			{
				OpenGLControl.ShowErrorMessage("Can't open file " + Path.GetFileName(this.userGuidFilePath), "File " + Path.GetFileName(this.userGuidFilePath) + " in application directory does not exists.");
				return;
			}

			try
			{
				Process process = new Process();
				process.StartInfo = new ProcessStartInfo(this.userGuidFilePath);
				process.Start();
			}
			catch (Exception ex)
			{
				OpenGLControl.ShowErrorMessage("Can't open file " + Path.GetFileName(this.userGuidFilePath), ex.Message);
			}
		}

		private void undoToolStripMenuItem_Click(object sender, EventArgs e)
		{
			undoToolStripMenuItem.Enabled = toolStripButtonUndo.Enabled = false;
			longOpNotifier.Begin();
			activeControl.SceneFacade.PerformAction(AvailableAction.Undo);
			longOpNotifier.End();
			undoRedoSetEnableIndication();
		}
		
		private void redoToolStripMenuItem_Click(object sender, EventArgs e)
		{
			redoToolStripMenuItem.Enabled = toolStripButtonRedo.Enabled = false;
			longOpNotifier.Begin();
			activeControl.SceneFacade.PerformAction(AvailableAction.Redo);
			longOpNotifier.End();
			undoRedoSetEnableIndication();
		}
		
		private void undoRedoSetEnableIndication()
		{
			undoToolStripMenuItem.Enabled = toolStripButtonUndo.Enabled = (bool)activeControl.SceneFacade.GetValue(AvailableValue.IsUndoPossible);
			redoToolStripMenuItem.Enabled = toolStripButtonRedo.Enabled = (bool)activeControl.SceneFacade.GetValue(AvailableValue.IsRedoPossible);
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

		private void removeActiveWindow()
		{
			if(!(activeControl.MyContainer is SplitterPanel))
				return;
			SplitContainer container = activeControl.MyContainer.Parent as SplitContainer;
			if (container == null)
				return;
			SplitterPanel toRemove = (SplitterPanel)activeControl.MyContainer;
			SplitterPanel toExtend = (container.Panel1 == activeControl.MyContainer) ? container.Panel2 : container.Panel1;
			if (removePanel(toRemove))
				extendPanel(toExtend);
		}

		private void refreshToolStripMenuItem_Click(object sender, EventArgs e)
		{
			activeControl.SceneFacade.PerformAction(AvailableAction.Refresh);
		}

		private void listOfSelectedItemsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (!activeControl.SceneFacade.ContainsMesh)
				return;
			longOpNotifier.Begin();
			ListOfSelectedItemsForm form = new ListOfSelectedItemsForm(activeControl.SceneFacade);
			longOpNotifier.End();
			form.ShowDialog();
		}

		private void axesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			bool value = (bool)activeControl.SceneFacade.GetValue(AvailableValue.DrawAxes);
			value = !value;
			activeControl.SceneFacade.SetValue(AvailableValue.DrawAxes, value);
			axesToolStripMenuItem.Checked = value;
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
				bool value = (bool)activeControl.SceneFacade.GetValue(AvailableValue.NodeSignalIsSet);
				activeControl.SignalNodeByID(value, longOpNotifier);
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

		#endregion

		#region Help methods

		private void askToSaveChanges(List<OpenGLControl> controls, bool canBeCancelled, CancelEventArgs e)
		{
			string caption = "Save changes?";
			
			Set<string> processedMeshes = new Set<string>();
			// ----------------------------------------------------
			// zajistit, ze se me to nebude ptat na site, ktere jsou jeste otevrene
			foreach (OpenGLControl c in openGLControls)
				if (c.SceneFacade.ContainsMesh && !controls.Contains(c))
					processedMeshes.Add(c.SceneFacade.MeshFilename);
			// ----------------------------------------------------
			foreach (OpenGLControl control in controls)
			{
				if (control.SceneFacade.ContainsMesh && !processedMeshes.Add(control.SceneFacade.MeshFilename))
				{
					bool unsaved = (bool)control.SceneFacade.GetValue(AvailableValue.UnsavedChangesInMesh);
					if (unsaved)
					{
						string text = "Do you want to save changes to " + Path.GetFileName(control.SceneFacade.MeshFilename) + "?";
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
			longOpNotifier.HasBegun += delegate
			{
				this.Cursor = Cursors.WaitCursor;
				activeControl.Cursor = Cursors.WaitCursor;
				statusLabel.Text = "Wait for operation to finish ...";
				statusLabel.ForeColor = Color.Blue;
				statusStrip.Refresh();
			};
			longOpNotifier.HasEnd += delegate
			{
				this.Cursor = Cursors.Default;
				activeControl.SetCursorAccordingToEditorMode();
				statusLabel.ForeColor = Color.Black;
				updateStatus();
			};
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
				undoRedoSetEnableIndication();
				updateRenderModeButtons();
			};

			openGLControl.MeshNeedRefresh += meshNeedRefreshHandler;
			openGLControl.ActionPerformed += delegate
			{
				updateStatus();
				undoRedoSetEnableIndication();
			};
			openGLControl.ColorModeChanged += delegate
			{
				updateColorModeButtons();
			};
			openGLControl.RenderModeChanged += delegate
			{
				updateRenderModeButtons();
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
		}
		
		private void updateCaption()
		{
			this.Text = "Mesh Editor"; /**/
			if (activeControl != null && activeControl.SceneFacade.MeshFilename != null)
				this.Text += " - " + Path.GetFileName(activeControl.SceneFacade.MeshFilename);
		}
		
		private void updateStatus()
		{
			string desc = (string)this.activeControl.SceneFacade.GetValue(AvailableValue.Status);
			this.statusLabel.Text = string.IsNullOrEmpty(desc) ? "Ready" : desc;
		}

		private bool activeWindowCanBeClosed()
		{
			return activeControl.MyContainer is SplitterPanel;
		}

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

		//private void alwaysShowNumbersToolStripMenuItem_Click(object sender, EventArgs e)
		//{
		//    bool value = (bool)activeControl.SceneFacade.GetValue(AvailableValue.AlwaysShowNumbers);
		//    value = !value;
		//    activeControl.SceneFacade.SetValue(AvailableValue.AlwaysShowNumbers, value);
		//    alwaysShowNumbersToolStripMenuItem.Checked = value;
		//}

		#endregion

		#region Tool strip handlers

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
			else if (sender == toolStripButtonRotateZ)
				editorMode = EditorMode.RotateZ;
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

		#endregion

	}
}
