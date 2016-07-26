using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using OpenTK;
using OpenTK.Graphics.OpenGL;

using MeshEditor.CoreInterface;
using System.IO;
using MeshEditor.IO;
using OpenTK.Graphics;
using MeshEditor.Cuts;
using MeshEditor.Graphics;
using MeshEditor.Utilities;
using System.Diagnostics;

namespace MeshEditor.WinUI
{
	/// <summary>
	/// Trida pro reprezentaci okna pro zobrazeni site.
	/// umoznuje zobrazeni OpenGL kontextu
	/// </summary>
	internal partial class OpenGLControl : GLControl
	{

		#region Fields

		private Control myContainer;
		private bool isActive;
		public event EventHandler<MeshNeedRefreshEventArgs> MeshNeedRefresh;
		public event EventHandler ActionPerformed;
		public event EventHandler ColorModeChanged;
		public event EventHandler RenderModeChanged;
		public event EventHandler<ScreenshotNeededEventArgs> ScreenshotNeeded;

		private SceneFacade sceneFacade;

		// ---------------------------------------
		private ProgressViewForm progressViewForm;
		private bool ioProcessCancelled;
		private bool ioProcessError;
		private bool saveWithoutHiddenElements;
		private BackgroundWorker backgroundFileLoader;
		private BackgroundWorker backgroundFileSaver;
		private System.Windows.Forms.Timer delayTimer;
		// ---------------------------------------

		public event EventHandler IOActionDone;

		#endregion

		#region Constructors

		/// <summary>
		/// Parameterless constructor, creates own object Scene
		/// </summary>
		private OpenGLControl(SceneFacade sceneToCopy, MouseEventHandler mouseDownHandler)
		{
			initializeControl(sceneToCopy, mouseDownHandler);
		}

		private OpenGLControl(SceneFacade sceneToCopy, MouseEventHandler mouseDownHandler, bool callBase)
			// ten radek s volanim konstruktoru predka zpusoboval nefunkcnost na linuxu
			: base(new GraphicsMode(new ColorFormat(SceneFacade.COLOR_BITS), SceneFacade.DEPTH_BITS))
		{
			initializeControl(sceneToCopy, mouseDownHandler);
		}

		private static bool callBaseConstructor;

		static OpenGLControl()
		{
			callBaseConstructor = false;
			try
			{
				using (GLControl glc = new GLControl())
				{
					glc.MakeCurrent();
					int depth;
					GL.GetInteger(GetPName.DepthBits, out depth);
					if (depth <= 16)
						callBaseConstructor = true;
				}
			}
			catch (Exception)
			{ }
		}

		public static OpenGLControl Create(SceneFacade sceneToCopy, MouseEventHandler mouseDownHandler)
		{
			if (callBaseConstructor)
			{
				try
				{
					return new OpenGLControl(sceneToCopy, mouseDownHandler, true);
				}
				catch (Exception)
				{ }
			}
			return new OpenGLControl(sceneToCopy, mouseDownHandler);
		}

		private void initializeControl(SceneFacade sceneToCopy, MouseEventHandler mouseDownHandler)
		{
			this.MouseDown += mouseDownHandler;

			this.isActive = false;
			if (sceneToCopy == null)
				sceneFacade = SceneFacade.GetEmptyScene();
			else
				sceneFacade = SceneFacade.GetCopyOf(sceneToCopy);

			SceneFacade.EditorModeChanged += editorModeChangedHandler;

			InitializeComponent();

			hookEvents();

			MakeCurrent();

			SceneFacade.InitializeGL();

			editorModeChangedHandler(null, null);

			// ------------------------------------
			backgroundFileLoader = new BackgroundWorker();
			backgroundFileLoader.DoWork += new DoWorkEventHandler(backgroundFileLoader_DoWork);
			backgroundFileLoader.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundFileLoader_RunWorkerCompleted);
			backgroundFileLoader.ProgressChanged += new ProgressChangedEventHandler(backgroundFileLoader_ProgressChanged);
			backgroundFileLoader.WorkerReportsProgress = true;
			backgroundFileLoader.WorkerSupportsCancellation = true;
			backgroundFileSaver = new BackgroundWorker();
			backgroundFileSaver.DoWork += new DoWorkEventHandler(backgroundFileSaver_DoWork);
			backgroundFileSaver.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundFileSaver_RunWorkerCompleted);
			backgroundFileSaver.ProgressChanged += new ProgressChangedEventHandler(backgroundFileLoader_ProgressChanged/**/);
			backgroundFileSaver.WorkerReportsProgress = true;
			backgroundFileSaver.WorkerSupportsCancellation = true;
			ioProcessCancelled = false;
			ioProcessError = false;
			delayTimer = null;


		}

		// =======================================================

		#endregion

		#region Properties

		public bool IOActionInProgress
		{
			get { return (backgroundFileLoader.IsBusy || backgroundFileSaver.IsBusy); }
		}

		public bool IOProcessedFinishedCorrectly
		{
			get { return !ioProcessCancelled && !ioProcessError; }
		}

		public SceneFacade SceneFacade
		{
			get { return sceneFacade; }
			set
			{
				if (sceneFacade != value)
				{
					unhookSceneEvents();
					sceneFacade.DisposeScene();
					sceneFacade = value;
					sceneFacade.Initialize();
					hookSceneEvents();
					OpenGLControl_Resize(null, null);
				}
			}
		}

		public Control MyContainer
		{
			get { return myContainer; }
			set { myContainer = value; }
		}

		public bool IsActive
		{
			get { return isActive; }
			set { isActive = value; }
		}

		#endregion

		#region Event handlers

		void OpenGLControl_LostFocus(object sender, EventArgs e)
		{
			sceneFacade.LostFocusHandler();
		}

		void OpenGLControl_Resize(object sender, EventArgs e)
		{
			sceneFacade.ResizeScene(ClientSize.Width, ClientSize.Height);
		}

		void mouseDown(object sender, MouseEventArgs e)
		{
			sceneFacade.MouseDownHandler(e.Location);
			if (e.Button == MouseButtons.Left && ActionPerformed != null)
				ActionPerformed(this, EventArgs.Empty);
		}

		void mouseUp(object sender, MouseEventArgs e)
		{
			sceneFacade.MouseUpHandler(e.Location, (MouseButton)e.Button);
			if (e.Button == MouseButtons.Left && ActionPerformed != null)
				ActionPerformed(this, EventArgs.Empty);
		}

		void mouseWheel(object sender, MouseEventArgs e)
		{
			sceneFacade.ZoomCamera(e.Location, e.Delta);
		}

		void mouseMove(object sender, MouseEventArgs e)
		{
			sceneFacade.MouseMoveHandler(e.Location, (MouseButton)e.Button);
			if (e.Button == MouseButtons.Left && ActionPerformed != null && SceneFacade.EditorMode != EditorMode.Orbit && SceneFacade.EditorMode != EditorMode.Pan)
				ActionPerformed(this, EventArgs.Empty);
		}

		void keyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.ControlKey)
			{
				sceneFacade.ControlDown = true;
				return;
			}
			if (e.KeyCode == Keys.ShiftKey)
			{
				sceneFacade.ShiftDown = true;
				return;
			}

			if (sceneFacade.ShiftDown || sceneFacade.ControlDown)
				return;

			AvailableAction action = AvailableAction.Nothing;
			object parameter = null;
			switch (e.KeyCode)
			{
				case Keys.Pause:
					ShowInternalStateInfo();
					return;

				case Keys.O:
					SceneFacade.EditorMode = EditorMode.Orbit;
					return;
				case Keys.P:
					SceneFacade.EditorMode = EditorMode.Pan;
					return;
				case Keys.L:
					SceneFacade.EditorMode = EditorMode.LookAround;
					return;
				case Keys.Z:
					SceneFacade.EditorMode = EditorMode.ZoomWindow;
					return;
				case Keys.E:
					SceneFacade.EditorMode = EditorMode.SelectElements;
					return;
				case Keys.N:
					SceneFacade.EditorMode = EditorMode.SelectNodes;
					return;
				case Keys.F:
					SceneFacade.EditorMode = EditorMode.SelectFaces;
					return;
				case Keys.G:
					SceneFacade.EditorMode = EditorMode.SelectEdges;
					return;
				case Keys.B:
					SceneFacade.EditorMode = EditorMode.SelectBeams;
					return;

				case Keys.D1:
					swapPropertyColorMode(PropertyColorsMode.Elements);
					return;
				case Keys.D2:
					swapPropertyColorMode(PropertyColorsMode.Nodes);
					return;
				case Keys.D3:
					swapPropertyColorMode(PropertyColorsMode.Faces);
					return;
				case Keys.D4:
					swapPropertyColorMode(PropertyColorsMode.Edges);
					return;
				case Keys.D5:
					swapPropertyColorMode(PropertyColorsMode.Beams);
					return;

				case Keys.D7:
					action = AvailableAction.FaceLighting;
					break;
				case Keys.D8:
					action = AvailableAction.LineSmooth;
					break;
				case Keys.D9:
					action = AvailableAction.PointSmooth;
					break;

				case Keys.D0:
					action = AvailableAction.EdgeLighting;
					break;

				//case Keys.Insert:
				//	action = AvailableAction.RestoreMesh;
				//	break;
				//case Keys.Q:
				//    action = AvailableAction.XRayVision;
				//    break;
				//case Keys.F5:
				//    action = AvailableAction.Refresh;
				//    break;
				case Keys.M: // change render mode
					action = AvailableAction.ChangeRenderMode;
					break;
				case Keys.R:
					action = AvailableAction.CameraReset;
					break;
				case Keys.Escape:
					action = AvailableAction.Storno;
					break;
				case Keys.Return:
					SetPropertyOfSelectedItems();
					return;

				default:
					e.Handled = false;
					e.SuppressKeyPress = false;
					return; // !!
			}
			sceneFacade.PerformAction(action, parameter);
		}

		void keyUp(object sender, KeyEventArgs e)
		{
			switch (e.KeyCode)
			{
				case Keys.ControlKey:
					sceneFacade.ControlDown = false;
					break;
				case Keys.ShiftKey:
					sceneFacade.ShiftDown = false;
					break;
			}
		}

		private void swapPropertyColorMode(PropertyColorsMode singleMode)
		{
			object parameter = sceneFacade.GetValue(AvailableValue.ColorMode);
			if (parameter == null)
				return;
			PropertyColorsMode mode = (PropertyColorsMode)parameter;

			if (singleMode == PropertyColorsMode.Elements)
				mode &= ~PropertyColorsMode.Faces;
			else if (singleMode == PropertyColorsMode.Faces)
				mode &= ~PropertyColorsMode.Elements;

			mode ^= singleMode;
			sceneFacade.SetValue(AvailableValue.ColorMode, mode);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			sceneFacade.DrawScene(isActive, true);
		}

		#endregion

		#region Hook events

		private void hookEvents()
		{
			hookSceneEvents();

			Resize += new EventHandler(OpenGLControl_Resize);
			Layout += delegate { OpenGLControl_Resize(null, null); };

			MouseDown += new MouseEventHandler(mouseDown);
			MouseUp += new MouseEventHandler(mouseUp);
			MouseWheel += new MouseEventHandler(mouseWheel);
			MouseMove += new MouseEventHandler(mouseMove);
			KeyDown += new KeyEventHandler(keyDown);
			KeyUp += new KeyEventHandler(keyUp);
			//MouseEnter += delegate { Focus(); };
			//GotFocus += delegate { this.Invalidate(); };
			LostFocus += new EventHandler(OpenGLControl_LostFocus);
		}

		private void hookSceneEvents()
		{
			sceneFacade.MakeCurrentNeeded += sceneFacade_MakeCurrentNeeded;
			sceneFacade.RefreshNeeded += sceneFacade_RefreshNeeded;
			sceneFacade.SwapBuffersNeeded += sceneFacade_SwapBuffersNeeded;
			sceneFacade.InvalidateNeeded += sceneFacade_InvalidateNeeded;
			//sceneFacade.ShowError += sceneFacade_ShowError;
			sceneFacade.MeshNeedRefresh += sceneFacade_MeshNeedRefresh;
			sceneFacade.ActionPerformed += sceneFacade_ActionPerformed;
			sceneFacade.ColorModeChanged += sceneFacade_ColorModeChanged;
			sceneFacade.RenderModeChanged += sceneFacade_RenderModeChanged;
			sceneFacade.ScreenshotNeeded += sceneFacade_ScreenshotNeeded;
		}

		private void unhookSceneEvents()
		{
			sceneFacade.MakeCurrentNeeded -= sceneFacade_MakeCurrentNeeded;
			sceneFacade.RefreshNeeded -= sceneFacade_RefreshNeeded;
			sceneFacade.SwapBuffersNeeded -= sceneFacade_SwapBuffersNeeded;
			sceneFacade.InvalidateNeeded -= sceneFacade_InvalidateNeeded;
			//sceneFacade.ShowError -= sceneFacade_ShowError;
			sceneFacade.MeshNeedRefresh -= sceneFacade_MeshNeedRefresh;
			sceneFacade.ActionPerformed -= sceneFacade_ActionPerformed;
			sceneFacade.ColorModeChanged -= sceneFacade_ColorModeChanged;
			sceneFacade.RenderModeChanged -= sceneFacade_RenderModeChanged;
			sceneFacade.ScreenshotNeeded -= sceneFacade_ScreenshotNeeded;
		}

		void sceneFacade_RenderModeChanged(object sender, EventArgs e)
		{
			if (RenderModeChanged != null)
				RenderModeChanged(this, e);
		}

		void sceneFacade_ColorModeChanged(object sender, EventArgs e)
		{
			if (ColorModeChanged != null)
				ColorModeChanged(this, e);
		}

		void sceneFacade_ActionPerformed(object sender, EventArgs e)
		{
			if (ActionPerformed != null)
				ActionPerformed(this, e);
		}

		void sceneFacade_MeshNeedRefresh(object sender, MeshNeedRefreshEventArgs ea)
		{
			if (MeshNeedRefresh != null)
				MeshNeedRefresh(this, ea);
		}

		private void sceneFacade_ScreenshotNeeded(object sender, ScreenshotNeededEventArgs e)
		{
			if (ScreenshotNeeded != null)
				ScreenshotNeeded(this, e);
		}

		void sceneFacade_InvalidateNeeded(object sender, EventArgs e)
		{
			this.Invalidate();
		}

		void sceneFacade_SwapBuffersNeeded(object sender, EventArgs e)
		{
			this.SwapBuffers();
		}

		void sceneFacade_RefreshNeeded(object sender, EventArgs e)
		{
			this.Refresh();
		}

		void sceneFacade_MakeCurrentNeeded(object sender, EventArgs e)
		{
			if (!Context.IsCurrent)
				this.MakeCurrent();
		}

		private void editorModeChangedHandler(object sender, EventArgs e)
		{
			/*zmenit kurzor*/
			SetCursorAccordingToEditorMode();
		}

		public void SetCursorAccordingToEditorMode()
		{
			switch (SceneFacade.EditorMode)
			{
				case EditorMode.Pan:
					this.Cursor = Cursors.SizeAll;
					break;
				case EditorMode.Orbit:
					this.Cursor = Cursors.Cross;
					break;
				case EditorMode.LookAround:
					this.Cursor = Cursors.NoMove2D;
					break;
				case EditorMode.ZoomWindow:
				case EditorMode.ScreenshotWindow:
					this.Cursor = Cursors.UpArrow;
					break;
				case EditorMode.SelectNodes:
				case EditorMode.SelectEdges:
				case EditorMode.SelectFaces:
				case EditorMode.SelectElements:
				case EditorMode.SelectBeams:
				case EditorMode.PickCuttingPlanePoint:
					this.Cursor = Cursors.Arrow;
					break;
				default:
					this.Cursor = Cursors.Default;
					break;
			}
		}

		#endregion

		#region Loading file

		public void LoadFiles(params string[] files)
		{
			if (backgroundFileLoader.IsBusy || backgroundFileSaver.IsBusy)
				return;

			Debug.Assert(files != null && files.Length > 0);

			//this.filename = files[0];
			//this.activeControl.SceneProxy.LoadMeshFromFile(this.filename);
			//this.activeControl.Invalidate();

			ioProcessCancelled = false;
			ioProcessError = false;
			backgroundFileLoader.RunWorkerAsync(files);

			System.Windows.Forms.Timer delayTimer = new System.Windows.Forms.Timer();
			delayTimer.Interval = 500;
			delayTimer.Tick += delegate
			{
				delayTimer.Stop();
				if (backgroundFileLoader.IsBusy)
				{
					progressViewForm = new ProgressViewForm("Loading " + Utilities.Functions.GetFileBatchDescription(files), enableCancellation: true);
					progressViewForm.Cancel += delegate { backgroundFileLoader.CancelAsync(); ioProcessCancelled = true; };
					progressViewForm.Show();
				}
			};
			delayTimer.Start();
		}

		/// <summary>
		/// metoda pro vlakno zajistujici nacteni site
		/// </summary>
		private void backgroundFileLoader_DoWork(object sender, DoWorkEventArgs e)
		{
			try
			{
				MeshIOEventHandler progressNotifier = delegate (object s, MeshIOEventArgs ea)
				{
					this.backgroundFileLoader.ReportProgress(ea.PercentDone);
				};
				YesNoQuestion cancelled = delegate { return backgroundFileLoader.CancellationPending; };

				SceneFacade newScene = SceneFacade.GetEmptyScene();

				newScene.LoadMeshFromFiles(e.Argument as string[], progressNotifier, cancelled);
				e.Result = newScene;
			}
			catch (Exception ex)
			{
				ioProcessError = true;
				e.Result = ex;
			}
			// ------------------------------------------
		}

		/// <summary>
		/// metoda, ktera se vola po skonceni nacitani site ze souboru
		/// </summary>
		private void backgroundFileLoader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			Cursor temp = this.Cursor;
			this.Cursor = Cursors.WaitCursor;

			if (!ioProcessCancelled && !ioProcessError && e.Result != null)
			{
				setSceneFacade((SceneFacade)e.Result);
			}
			if (progressViewForm != null)
			{
				progressViewForm.Quit();
				progressViewForm = null;
			}

			GC.Collect();

			if (ioProcessError)
			{
				Exception exception = e.Result as Exception;
				if (exception != null)
				{
					ShowErrorMessage("Error while loading mesh", Utilities.Functions.BuildErrorMessage(exception));
				}
			}
			else if (AppSettings.Instance.ShowOpenGLLowVersionMessage && !sceneFacade.CheckOpenGLVersion())
			{
				CheckMessageBox mbox = new CheckMessageBox("This application requires graphics card that supports OpenGL version 2.0+ which includes Vertex buffer object (VBO) and OpenGL shading language (GLSL) support. Check that you have the latest graphics card drivers. OpenGL version supported by your graphics drivers is now " + AppSettings.Instance.OpenGLVersion + ".", "Low OpenGL version");
				mbox.ShowDialog();
				AppSettings.Instance.ShowOpenGLLowVersionMessage = !mbox.IsChecked;
			}

			this.Cursor = temp;

			if (IOActionDone != null)
				IOActionDone(this, EventArgs.Empty);
		}

		private void setSceneFacade(SceneFacade newSceneFacade)
		{
			object colorMode = sceneFacade.GetValue(AvailableValue.ColorMode);
			object renderMode = sceneFacade.GetValue(AvailableValue.RenderMode);
			SceneFacade = newSceneFacade; // dulezite je pouzit vlastnost, ne primo field (provadi se cinnost)

			sceneFacade.SetValue(AvailableValue.ColorMode, colorMode);
			//sceneFacade.SetValue(AvailableValue.RenderMode, renderMode);
		}

		/// <summary>
		/// metoda, ktera se vola pokud doslo k pokroku pri nacitani site ze souboru
		/// </summary>
		private void backgroundFileLoader_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			if (progressViewForm != null)
			{
				progressViewForm.SetProgressState(e.ProgressPercentage);
				if (e.ProgressPercentage >= 100)
				{
					progressViewForm.Quit();
					progressViewForm = null;
				}
			}
		}

		#endregion

		#region Saving file

		// ----------------------------------------------------------------------

		public void SaveToFile(string filename, bool saveWithoutHiddenElements)
		{
			if (backgroundFileLoader.IsBusy || backgroundFileSaver.IsBusy)
				return;

			//this.filename = file;

			this.ioProcessCancelled = false;
			this.ioProcessError = false;
			this.saveWithoutHiddenElements = saveWithoutHiddenElements;
			backgroundFileSaver.RunWorkerAsync(filename);

			this.delayTimer = new System.Windows.Forms.Timer();
			delayTimer.Interval = 500;
			delayTimer.Tick += delegate
			{
				delayTimer.Stop();
				if (backgroundFileSaver.IsBusy)
				{
					progressViewForm = new ProgressViewForm("Saving " + Path.GetFileName(filename), enableCancellation: true);
					progressViewForm.Cancel += delegate { backgroundFileSaver.CancelAsync(); ioProcessCancelled = true; };
					progressViewForm.Show();
				}
			};
			delayTimer.Start();
		}

		void backgroundFileSaver_DoWork(object sender, DoWorkEventArgs e)
		{
			try
			{
				MeshIOEventHandler progressNotifier = delegate (object s, MeshIOEventArgs ea)
				{
					this.backgroundFileSaver.ReportProgress(ea.PercentDone);
				};
				YesNoQuestion cancelled = delegate { return backgroundFileSaver.CancellationPending; };
				sceneFacade.SaveMeshToFile(e.Argument as string, this.saveWithoutHiddenElements, progressNotifier, cancelled);
			}
			catch (Exception ex)
			{
				ioProcessError = true;
				e.Result = ex;
			}
		}

		void backgroundFileSaver_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if (progressViewForm != null)
			{
				progressViewForm.Quit();
				progressViewForm = null;
			}
			GC.Collect();

			if (ioProcessError)
			{
				if (e.Result is MeshSavingException)
				{
					MeshSavingException ex = e.Result as MeshSavingException;
					ShowErrorMessage("Error while saving", ex.Message + Environment.NewLine + "(line number: " + ex.LineNumber + ")");
				}
				else if (e.Result is Exception)
				{
					Exception ex = e.Result as Exception;
					ShowErrorMessage("Error while saving", ex.Message);
				}
			}
			else if (this.saveWithoutHiddenElements)
			{
				// po ulozeni smazat skryte elementy
				sceneFacade.PerformAction(AvailableAction.DeleteHiddenItems);
			}

			if (IOActionDone != null)
				IOActionDone(this, EventArgs.Empty);
		}

		// ----------------------------------------------------------------------

		#endregion

		#region Public methods

		public void ClearScene()
		{
			setSceneFacade(SceneFacade.GetEmptyScene());
		}

		public static void ShowInternalStateInfo()
		{
			float megaBytes = ((float)MemoryMeter.GetCurrentMemoryConsumption()) / 1048576f;
			StringBuilder text = new StringBuilder();

			// memory consumption
			text.Append("Current memory consumption: ");
			text.Append(Math.Round(megaBytes, 2));
			text.AppendLine(" MB");

			// opengl version
			text.Append("OpenGL version: ");
			text.AppendLine(Utilities.Functions.GetOpenGLVersionString());

			// depth buffer bits
			text.Append("Depth buffer bits: ");
			text.AppendLine(Utilities.Functions.GetDepthBufferBits().ToString());

			MessageBox.Show(text.ToString(), "Internal state info");
		}

		public void SetPropertyOfSelectedItems()
		{
			if (!sceneFacade.ContainsMesh)
				return;

			string description = (string)sceneFacade.GetValue(AvailableValue.SelectedItemsDescription);
			if (string.IsNullOrEmpty(description))
				description = "Nothing selected";
			PropertyInputForm form = new PropertyInputForm("Insert property number", description, sceneFacade);
			int value = 0;

			form.InputValueValidating += delegate (object sender, CancelEventArgs ea)
			{
				if (!int.TryParse(form.InputValue, out value))
				{
					ea.Cancel = true;
					ShowErrorMessage("Inserted value is not an integer", "Please input valid integer value" + Environment.NewLine + "in range <" + int.MinValue + "; " + int.MaxValue + ">");
				}
			};

			if (form.ShowDialog() == DialogResult.OK)
			{
				sceneFacade.SetPropertyOfSelectedItems(new MeshEditor.Data.Property(value));
			}

			if (MeshNeedRefresh != null)
				MeshNeedRefresh(this, new MeshNeedRefreshEventArgs(sceneFacade.MeshFilename));

			if (ActionPerformed != null)
				ActionPerformed(this, EventArgs.Empty);
		}

		public void SelectItemsWithProperty(string itemsName, bool addToSelection = false)
		{
			CheckedInputValueForm form = new CheckedInputValueForm("Insert property number", "Select " + itemsName + " by property:", "add to selection");
			form.IsChecked = addToSelection;

			int value = 0;
			form.InputValueValidating += delegate (object s, CancelEventArgs ea)
			{
				if (!int.TryParse(form.InputValue, out value) || value < 0)
				{
					ea.Cancel = true;
					ShowErrorMessage("Inserted value is not an integer", "Please input valid integer value" + Environment.NewLine + "in range <0; " + int.MaxValue + ">");
				}
			};

			if (form.ShowDialog() == DialogResult.OK)
			{
				AvailableAction action = form.IsChecked ? AvailableAction.SelectItemsWithPropertyAdd : AvailableAction.SelectItemsWithProperty;
				sceneFacade.PerformAction(action, new MeshEditor.Data.Property(value));
			}
		}

		public void SignalNodeByID(int[] ids)
		{
			//Cursor temp = this.Cursor;
			if (ids == null || ids.Length == 0)
			{
				//try
				//{
				//	longOpNotifier.Begin();
				//	this.Cursor = Cursors.WaitCursor;
				sceneFacade.PerformAction(AvailableAction.ClearSignalNode);
				return;
				//}
				//finally
				//{
				//	this.Cursor = temp;
				//	longOpNotifier.End();
				//}
			}

			Debug.Assert(ids.Length > 0);

			//try
			//{
			//	longOpNotifier.Begin();
			//	this.Cursor = Cursors.WaitCursor;
			sceneFacade.PerformAction(AvailableAction.SignalNode, ids);
			//}
			//finally
			//{
			//	this.Cursor = temp;
			//	longOpNotifier.End();
			//}
		}

		public void SignalNodeByID(/*, LongOpNotifier longOpNotifier*/)
		{
			InputValueForm form = new InputValueForm("Insert node ID", "Signal node(s) with ID(s):");
			List<int> values = new List<int>();
			char[] splitCharacters = { ',', ';', ' ', '\t' };
			form.InputValueValidating += delegate (object s, CancelEventArgs ea)
			{
				bool error = false;
				string[] parts = form.InputValue.Split(splitCharacters, StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length == 0)
				{
					error = true;
				}
				else
				{
					foreach (string textPart in parts)
					{
						int value;
						if (!int.TryParse(textPart, out value))
						{
							error = true;
							break;
						}
						else
						{
							values.Add(value);
						}
					}
				}
				if (error)
				{
					ea.Cancel = true;
					ShowErrorMessage("Inserted value is not an integer", "Please input valid integer value.");
				}
			};

			if (form.ShowDialog() == DialogResult.OK)
			{
				SignalNodeByID(values.ToArray());
			}
		}

		public void SignalElementByID(bool clear, LongOpNotifier longOpNotifier)
		{
			
			if (clear)
			{
				Cursor temp = this.Cursor;
				using (longOpNotifier.Begin("Removing signalled element"))
				{
					this.Cursor = Cursors.WaitCursor;
					sceneFacade.PerformAction(AvailableAction.ClearSignalElement);
				}
				this.Cursor = temp;
				return;
			}

			InputValueForm form = new InputValueForm("Insert element ID", "Signal element with ID:");
			int value = 0;

			form.InputValueValidating += delegate (object s, CancelEventArgs ea)
			{
				if (!int.TryParse(form.InputValue, out value))
				{
					ea.Cancel = true;
					ShowErrorMessage("Inserted value is not an integer", "Please input valid integer value.");
				}
			};

			if (form.ShowDialog() == DialogResult.OK)
			{
				//try
				//{
				//	longOpNotifier.Begin();					
				//	this.Cursor = Cursors.WaitCursor;
				sceneFacade.PerformAction(AvailableAction.SignalElement, value);
				//}
				//finally
				//{
				//	this.Cursor = temp;
				//	longOpNotifier.End();
				//}
			}
		}

		/// <summary>
		/// Returns a System.Drawing.Bitmap with the contents of the current framebuffer.
		/// </summary>
		/// <param name="imageWidth">Screenshot width. Zero to keep current window width.</param>
		/// <param name="imageHeight">Screenshot height. Zero to keep current window width.</param>
		/// <returns>Bitmap object containing screenshot.</returns>
		public Bitmap TakeScreenshot(int imageWidth, int imageHeight)
		{
			int tempWidth = Width;
			int tempHeight = Height;
			Width = (imageWidth <= 0) ? Width : imageWidth;
			Height = (imageHeight <= 0) ? Height : imageHeight;
			//draw(false); // draw model without calling SwapBuffers()
			sceneFacade.DrawScene(isActive, false);
			if (GraphicsContext.CurrentContext == null)
			{
				throw new GraphicsContextMissingException();
			}
			Bitmap bmp = new Bitmap(this.ClientSize.Width, this.ClientSize.Height);
			System.Drawing.Imaging.BitmapData data = bmp.LockBits(this.ClientRectangle, System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
			//GL.ReadBuffer(ReadBufferMode.ColorAttachment0);
			//GL.ReadBuffer(ReadBufferMode.Back);
			GL.ReadPixels(0, 0, this.ClientSize.Width, this.ClientSize.Height, PixelFormat.Bgr, PixelType.UnsignedByte, data.Scan0);
			bmp.UnlockBits(data);
			bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
			Width = tempWidth;
			Height = tempHeight;
			//draw(); // redraw
			//sceneFacade.DrawScene(isActive, true);
			return bmp;
		}

		public Bitmap TakeScreenshot()
		{
			return TakeScreenshot(Rectangle.Empty);
		}

		public Bitmap TakeScreenshot(Rectangle screenCropWindow)
		{
			Rectangle area = screenCropWindow;
			if (area.IsEmpty)
				area = ClientRectangle;
			else
				area.Intersect(ClientRectangle); // check bounds

			sceneFacade.DrawScene(isActive, false);

			if (GraphicsContext.CurrentContext == null)
			{
				throw new GraphicsContextMissingException();
			}

			if (area.Width <= 0 || area.Height <= 0)
			{
				throw new ArgumentException("Screen window width and height must be non-zero.", "screenWindow");
			}

			Bitmap bmp = new Bitmap(area.Width, area.Height);
			System.Drawing.Imaging.BitmapData data = bmp.LockBits(new Rectangle(0, 0, area.Width, area.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
			GL.ReadPixels(area.Left, this.Height - area.Top - area.Height, area.Width, area.Height, PixelFormat.Bgr, PixelType.UnsignedByte, data.Scan0);
			bmp.UnlockBits(data);

			bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);

			return bmp;
		}

		public static void ShowErrorMessage(string caption, string message)
		{
			MessageBox.Show(message, caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
		}

		public void DisposeScene()
		{
			sceneFacade.DisposeScene();
			SceneFacade.EditorModeChanged -= editorModeChangedHandler;
			// unhook events
			unhookSceneEvents();
		}

		#endregion

	}
}
