using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;

using MeshEditor.AvaloniaUI.Dialogs;
using MeshEditor.CoreInterface;
using MeshEditor.Graphics;
using MeshEditor.IO;
using MeshEditor.Utilities;

using DrawingPoint = System.Drawing.Point;
using DrawingRectangle = System.Drawing.Rectangle;
using CoreMouseButton = MeshEditor.CoreInterface.MouseButton;

namespace MeshEditor.AvaloniaUI.Rendering
{
	public class IOProgressEventArgs : EventArgs
	{
		public IOProgressEventArgs(string description, int percentDone)
		{
			Description = description;
			PercentDone = percentDone;
		}

		public string Description { get; }
		public int PercentDone { get; }
	}

	/// <summary>
	/// Control displaying the mesh scene (port of MeshEditor.WinUI.OpenGLControl).
	/// </summary>
	public class OpenGLViewport : Control
	{
		#region Fields

		private readonly OffscreenGlContext glContext;
		private WriteableBitmap frontBuffer;
		private int renderRequested; // 0/1, accessed from timer threads of SceneFacade
		private bool isActive;
		private SceneFacade sceneFacade;
		private double renderScaling = 1.0;

		public event EventHandler<MeshNeedRefreshEventArgs> MeshNeedRefresh;
		public event EventHandler ActionPerformed;
		public event EventHandler ColorModeChanged;
		public event EventHandler RenderModeChanged;
		public event EventHandler<ScreenshotNeededEventArgs> ScreenshotNeeded;
		public event EventHandler IOActionDone;
		public event EventHandler<IOProgressEventArgs> IOProgress;
		/// <summary>
		/// Raised on mouse down before the scene handles it (the mouseDownHandler passed to the WinForms OpenGLControl).
		/// </summary>
		public event EventHandler ActivationRequested;

		private Task ioTask;
		private CancellationTokenSource ioCancellation;
		private bool ioProcessCancelled;
		private bool ioProcessError;

		#endregion

		#region Constructors

		public OpenGLViewport(SceneFacade sceneToCopy)
		{
			Focusable = true;
			ClipToBounds = true;

			glContext = new OffscreenGlContext();
			glContext.MakeCurrent();
			SceneFacade.InitializeGL();

			if (sceneToCopy == null)
				sceneFacade = SceneFacade.GetEmptyScene();
			else
				sceneFacade = SceneFacade.GetCopyOf(sceneToCopy);

			SceneFacade.EditorModeChanged += editorModeChangedHandler;
			hookSceneEvents();
			editorModeChangedHandler(null, null);
		}

		#endregion

		#region Properties

		public bool IOActionInProgress => ioTask != null && !ioTask.IsCompleted;

		public bool IOProcessedFinishedCorrectly => !ioProcessCancelled && !ioProcessError;

		public SceneFacade SceneFacade
		{
			get { return sceneFacade; }
			set
			{
				if (sceneFacade != value)
				{
					glContext.MakeCurrent();
					unhookSceneEvents();
					sceneFacade.DisposeScene();
					sceneFacade = value;
					sceneFacade.Initialize();
					hookSceneEvents();
					resizeScene();
				}
			}
		}

		/// <summary>
		/// Panel hosting this viewport (used by the split window feature).
		/// </summary>
		public Border MyContainer { get; set; }

		public bool IsActive
		{
			get { return isActive; }
			set { isActive = value; }
		}

		/// <summary>
		/// Size of the rendered image in physical pixels (coordinates used by the scene).
		/// </summary>
		public PixelSize PixelSize => new PixelSize(glContext.Width, glContext.Height);

		#endregion

		#region Rendering

		/// <summary>
		/// Schedules a redraw (Control.Invalidate in WinForms). Can be called from any thread.
		/// </summary>
		public void Invalidate()
		{
			if (Interlocked.Exchange(ref renderRequested, 1) == 0)
				Dispatcher.UIThread.Post(renderNow, DispatcherPriority.Background);
		}

		/// <summary>
		/// Redraws immediately (Control.Refresh in WinForms).
		/// </summary>
		public void Refresh()
		{
			if (!Dispatcher.UIThread.CheckAccess())
			{
				Invalidate();
				return;
			}
			renderNow();
		}

		private void renderNow()
		{
			Interlocked.Exchange(ref renderRequested, 0);
			if (sceneFacade == null || glContext == null)
				return;
			sceneFacade.DrawScene(isActive, true);
		}

		/// <summary>
		/// Copies the rendered frame into the bitmap shown by this control (SwapBuffers in WinForms).
		/// </summary>
		private void present()
		{
			int width = glContext.Width, height = glContext.Height;
			if (DiagnosticsEnabled)
			{
				int[] viewport = new int[4];
				OpenTK.Graphics.OpenGL.GL.GetInteger(OpenTK.Graphics.OpenGL.GetPName.Viewport, viewport);
				Diag($"present msaa={glContext.Samples}x fbo={width}x{height} glViewport={string.Join(",", viewport)} bounds={Bounds}");
			}
			if (frontBuffer == null || frontBuffer.PixelSize.Width != width || frontBuffer.PixelSize.Height != height)
			{
				frontBuffer?.Dispose();
				frontBuffer = new WriteableBitmap(new PixelSize(width, height), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Opaque);
			}
			using (var locked = frontBuffer.Lock())
			{
				glContext.ReadPixelsBgra(0, 0, width, height, locked.Address, locked.RowBytes);
			}
			InvalidateVisual();
		}

		public override void Render(DrawingContext context)
		{
			if (frontBuffer != null) // the bitmap has device pixel size; stretch exactly the whole bitmap over the control
				context.DrawImage(frontBuffer, new Rect(0, 0, frontBuffer.PixelSize.Width, frontBuffer.PixelSize.Height), new Rect(Bounds.Size));
			else
				context.FillRectangle(Brushes.Black, new Rect(Bounds.Size));
		}

		protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
		{
			base.OnAttachedToVisualTree(e);
			if (TopLevel.GetTopLevel(this) is TopLevel topLevel)
			{
				topLevel.ScalingChanged += topLevel_ScalingChanged;
				renderScaling = topLevel.RenderScaling;
			}
			resizeScene();
		}

		protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
		{
			if (TopLevel.GetTopLevel(this) is TopLevel topLevel)
				topLevel.ScalingChanged -= topLevel_ScalingChanged;
			base.OnDetachedFromVisualTree(e);
		}

		private void topLevel_ScalingChanged(object sender, EventArgs e)
		{
			renderScaling = ((TopLevel)sender).RenderScaling;
			resizeScene();
		}

		protected override void OnSizeChanged(SizeChangedEventArgs e)
		{
			base.OnSizeChanged(e);
			resizeScene();
		}

		internal static readonly bool DiagnosticsEnabled = Environment.GetEnvironmentVariable("MESHEDITOR_GL_DIAG") == "1";

		internal static void Diag(string message)
		{
			if (DiagnosticsEnabled)
				Console.Error.WriteLine($"[viewport {DateTime.Now:HH:mm:ss.fff}] {message}");
		}

		private void resizeScene()
		{
			int width = Math.Max(1, (int)Math.Round(Bounds.Width * renderScaling));
			int height = Math.Max(1, (int)Math.Round(Bounds.Height * renderScaling));
			Diag($"resize bounds={Bounds} scaling={renderScaling} -> {width}x{height}");
			glContext.Resize(width, height);
			sceneFacade.ResizeScene(width, height);
		}

		#endregion

		#region Input handlers

		private DrawingPoint toScenePoint(Point position)
		{
			return new DrawingPoint((int)Math.Round(position.X * renderScaling), (int)Math.Round(position.Y * renderScaling));
		}

		private static CoreMouseButton pressedButtons(PointerPointProperties properties)
		{
			CoreMouseButton buttons = CoreMouseButton.None;
			if (properties.IsLeftButtonPressed) buttons |= CoreMouseButton.Left;
			if (properties.IsRightButtonPressed) buttons |= CoreMouseButton.Right;
			if (properties.IsMiddleButtonPressed) buttons |= CoreMouseButton.Middle;
			if (properties.IsXButton1Pressed) buttons |= CoreMouseButton.XButton1;
			if (properties.IsXButton2Pressed) buttons |= CoreMouseButton.XButton2;
			return buttons;
		}

		private static CoreMouseButton toCoreButton(global::Avalonia.Input.MouseButton button) => button switch
		{
			global::Avalonia.Input.MouseButton.Left => CoreMouseButton.Left,
			global::Avalonia.Input.MouseButton.Right => CoreMouseButton.Right,
			global::Avalonia.Input.MouseButton.Middle => CoreMouseButton.Middle,
			global::Avalonia.Input.MouseButton.XButton1 => CoreMouseButton.XButton1,
			global::Avalonia.Input.MouseButton.XButton2 => CoreMouseButton.XButton2,
			_ => CoreMouseButton.None,
		};

		protected override void OnPointerPressed(PointerPressedEventArgs e)
		{
			base.OnPointerPressed(e);
			ActivationRequested?.Invoke(this, EventArgs.Empty);
			Focus();
			e.Pointer.Capture(this); // WinForms captures the mouse while a button is down

			var point = e.GetCurrentPoint(this);
			sceneFacade.MouseDownHandler(toScenePoint(point.Position));
			if (point.Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonPressed)
				ActionPerformed?.Invoke(this, EventArgs.Empty);
		}

		protected override void OnPointerReleased(PointerReleasedEventArgs e)
		{
			base.OnPointerReleased(e);
			var point = e.GetCurrentPoint(this);
			var button = toCoreButton(e.InitialPressMouseButton);
			sceneFacade.MouseUpHandler(toScenePoint(point.Position), button);
			if (button == CoreMouseButton.Left)
				ActionPerformed?.Invoke(this, EventArgs.Empty);
			if (pressedButtons(point.Properties) == CoreMouseButton.None)
				e.Pointer.Capture(null);
		}

		protected override void OnPointerMoved(PointerEventArgs e)
		{
			base.OnPointerMoved(e);
			var point = e.GetCurrentPoint(this);
			var buttons = pressedButtons(point.Properties);
			sceneFacade.MouseMoveHandler(toScenePoint(point.Position), buttons);
			if (buttons == CoreMouseButton.Left && ActionPerformed != null && SceneFacade.EditorMode != EditorMode.Orbit && SceneFacade.EditorMode != EditorMode.Pan)
				ActionPerformed(this, EventArgs.Empty);
		}

		protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
		{
			base.OnPointerWheelChanged(e);
			// WinForms reports 120 per wheel notch
			int delta = (int)Math.Round(e.Delta.Y * 120);
			if (delta != 0)
				sceneFacade.ZoomCamera(toScenePoint(e.GetPosition(this)), delta);
			e.Handled = true;
		}

		protected override void OnLostFocus(FocusChangedEventArgs e)
		{
			base.OnLostFocus(e);
			sceneFacade.LostFocusHandler();
		}

		protected override async void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);

			if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
			{
				sceneFacade.ControlDown = true;
				return;
			}
			if (e.Key == Key.LeftShift || e.Key == Key.RightShift)
			{
				sceneFacade.ShiftDown = true;
				return;
			}

			if (sceneFacade.ShiftDown || sceneFacade.ControlDown || (e.KeyModifiers & (KeyModifiers.Alt | KeyModifiers.Meta)) != 0)
				return; // handled by menu shortcuts

			AvailableAction action = AvailableAction.Nothing;
			object parameter = null;
			e.Handled = true;
			switch (e.Key)
			{
				case Key.Pause:
					await ShowInternalStateInfo();
					return;

				case Key.O:
					SceneFacade.EditorMode = EditorMode.Orbit;
					return;
				case Key.P:
					SceneFacade.EditorMode = EditorMode.Pan;
					return;
				case Key.L:
					SceneFacade.EditorMode = EditorMode.LookAround;
					return;
				case Key.Z:
					SceneFacade.EditorMode = EditorMode.ZoomWindow;
					return;
				case Key.E:
					SceneFacade.EditorMode = EditorMode.SelectElements;
					return;
				case Key.N:
					SceneFacade.EditorMode = EditorMode.SelectNodes;
					return;
				case Key.F:
					SceneFacade.EditorMode = EditorMode.SelectFaces;
					return;
				case Key.G:
					SceneFacade.EditorMode = EditorMode.SelectEdges;
					return;
				case Key.B:
					SceneFacade.EditorMode = EditorMode.SelectBeams;
					return;

				case Key.D1:
					togglePropertyColorMode(PropertyColorsMode.Elements);
					return;
				case Key.D2:
					togglePropertyColorMode(PropertyColorsMode.Nodes);
					return;
				case Key.D3:
					togglePropertyColorMode(PropertyColorsMode.Faces);
					return;
				case Key.D4:
					togglePropertyColorMode(PropertyColorsMode.Edges);
					return;
				case Key.D5:
					togglePropertyColorMode(PropertyColorsMode.Beams);
					return;

				case Key.D7:
					action = AvailableAction.FaceLighting;
					break;
				case Key.D8:
					action = AvailableAction.LineSmooth;
					break;
				case Key.D9:
					action = AvailableAction.PointSmooth;
					break;
				case Key.D0:
					action = AvailableAction.EdgeLighting;
					break;

				case Key.M: // change render mode
					action = AvailableAction.ChangeRenderMode;
					break;
				case Key.R:
					action = AvailableAction.CameraReset;
					break;
				case Key.Escape:
					action = AvailableAction.Storno;
					break;
				case Key.Enter:
					await SetPropertyOfSelectedItems();
					return;

				case Key.Left:
					sceneFacade.MoveCamera(CameraView.Left);
					return;
				case Key.Right:
					sceneFacade.MoveCamera(CameraView.Right);
					return;
				case Key.Down:
					sceneFacade.MoveCamera(CameraView.Back);
					return;
				case Key.Up:
					sceneFacade.MoveCamera(CameraView.Front);
					return;

				default:
					e.Handled = false;
					return;
			}
			sceneFacade.PerformAction(action, parameter);
		}

		protected override void OnKeyUp(KeyEventArgs e)
		{
			base.OnKeyUp(e);
			switch (e.Key)
			{
				case Key.LeftCtrl:
				case Key.RightCtrl:
					sceneFacade.ControlDown = false;
					break;
				case Key.LeftShift:
				case Key.RightShift:
					sceneFacade.ShiftDown = false;
					break;
			}
		}

		private void togglePropertyColorMode(PropertyColorsMode singleMode)
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
			sceneFacade.PerformAction(AvailableAction.UpdateColorBuffers);
		}

		#endregion

		#region Hook events

		private void hookSceneEvents()
		{
			sceneFacade.MakeCurrentNeeded += sceneFacade_MakeCurrentNeeded;
			sceneFacade.RefreshNeeded += sceneFacade_RefreshNeeded;
			sceneFacade.SwapBuffersNeeded += sceneFacade_SwapBuffersNeeded;
			sceneFacade.FrameDrawingStarted += sceneFacade_FrameDrawingStarted;
			sceneFacade.InvalidateNeeded += sceneFacade_InvalidateNeeded;
			sceneFacade.MeshReloaded += sceneFacade_MeshReloaded;
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
			sceneFacade.FrameDrawingStarted -= sceneFacade_FrameDrawingStarted;
			sceneFacade.InvalidateNeeded -= sceneFacade_InvalidateNeeded;
			sceneFacade.MeshReloaded -= sceneFacade_MeshReloaded;
			sceneFacade.MeshNeedRefresh -= sceneFacade_MeshNeedRefresh;
			sceneFacade.ActionPerformed -= sceneFacade_ActionPerformed;
			sceneFacade.ColorModeChanged -= sceneFacade_ColorModeChanged;
			sceneFacade.RenderModeChanged -= sceneFacade_RenderModeChanged;
			sceneFacade.ScreenshotNeeded -= sceneFacade_ScreenshotNeeded;
		}

		private void sceneFacade_RenderModeChanged(object sender, EventArgs e) => onUIThread(() => RenderModeChanged?.Invoke(this, e));

		private void sceneFacade_ColorModeChanged(object sender, EventArgs e) => onUIThread(() => ColorModeChanged?.Invoke(this, e));

		private void sceneFacade_ActionPerformed(object sender, EventArgs e) => onUIThread(() => ActionPerformed?.Invoke(this, e));

		private void sceneFacade_MeshNeedRefresh(object sender, MeshNeedRefreshEventArgs ea) => onUIThread(() => MeshNeedRefresh?.Invoke(this, ea));

		private void sceneFacade_ScreenshotNeeded(object sender, ScreenshotNeededEventArgs e) => onUIThread(() => ScreenshotNeeded?.Invoke(this, e));

		private void sceneFacade_InvalidateNeeded(object sender, EventArgs e) => Invalidate();

		private void sceneFacade_MeshReloaded(object sender, EventArgs e)
		{
			onUIThread(() =>
			{
				Invalidate();
				IOActionDone?.Invoke(this, e);
			});
		}

		private void sceneFacade_SwapBuffersNeeded(object sender, EventArgs e) => present();

		private void sceneFacade_FrameDrawingStarted(object sender, EventArgs e) => glContext.BeginFrame();

		private void sceneFacade_RefreshNeeded(object sender, EventArgs e) => Refresh();

		private void sceneFacade_MakeCurrentNeeded(object sender, EventArgs e)
		{
			glContext.MakeCurrent();
			// the scene is rendered in device pixels; line widths and point sizes of the core are in logical pixels
			DeviceScaling.Factor = (float)renderScaling;
		}

		private static void onUIThread(Action action)
		{
			if (Dispatcher.UIThread.CheckAccess())
				action();
			else
				Dispatcher.UIThread.Post(action);
		}

		private void editorModeChangedHandler(object sender, EventArgs e)
		{
			onUIThread(SetCursorAccordingToEditorMode);
		}

		public void SetCursorAccordingToEditorMode()
		{
			StandardCursorType cursorType;
			switch (SceneFacade.EditorMode)
			{
				case EditorMode.Pan:
					cursorType = StandardCursorType.SizeAll;
					break;
				case EditorMode.Orbit:
					cursorType = StandardCursorType.Cross;
					break;
				case EditorMode.LookAround:
					cursorType = StandardCursorType.SizeAll; // WinForms used Cursors.NoMove2D which has no Avalonia counterpart
					break;
				case EditorMode.ZoomWindow:
				case EditorMode.ScreenshotWindow:
					cursorType = StandardCursorType.UpArrow;
					break;
				default:
					cursorType = StandardCursorType.Arrow;
					break;
			}
			Cursor = new Cursor(cursorType);
		}

		#endregion

		#region Loading file

		public void LoadFiles(params string[] files)
		{
			if (IOActionInProgress)
				return;

			Debug.Assert(files != null && files.Length > 0);

			ioProcessCancelled = false;
			ioProcessError = false;
			ioCancellation = new CancellationTokenSource();
			var cancellation = ioCancellation.Token;
			string description = "Loading " + Functions.GetFileBatchDescription(files);
			IOProgress?.Invoke(this, new IOProgressEventArgs(description, 0));

			ioTask = runIOTask(() =>
			{
				MeshIOEventHandler progressNotifier = (s, ea) => onUIThread(() => IOProgress?.Invoke(this, new IOProgressEventArgs(description, ea.PercentDone)));
				YesNoQuestion cancelled = () => cancellation.IsCancellationRequested;

				SceneFacade newScene = SceneFacade.GetEmptyScene();
				newScene.LoadMeshFromFiles(files, progressNotifier, cancelled);
				return newScene;
			}, loadingCompleted);
		}

		/// <summary>
		/// Cancels the running load/save operation (Cancel button of the WinForms ProgressViewForm).
		/// </summary>
		public void CancelIOAction()
		{
			if (IOActionInProgress)
			{
				ioProcessCancelled = true;
				ioCancellation?.Cancel();
			}
		}

		private Task runIOTask(Func<object> work, Func<object, Task> completed)
		{
			var task = Task.Run(() =>
			{
				try
				{
					return work();
				}
				catch (Exception ex)
				{
					ioProcessError = true;
					return (object)ex;
				}
			});
			var tcs = new TaskCompletionSource();
			task.ContinueWith(async t =>
			{
				try
				{
					await completed(t.Result);
				}
				catch (Exception ex)
				{
					await ShowErrorMessage("Unexpected error", Functions.BuildErrorMessage(ex));
				}
				finally
				{
					tcs.TrySetResult();
				}
			}, TaskScheduler.FromCurrentSynchronizationContext());
			return tcs.Task;
		}

		private async Task loadingCompleted(object result)
		{
			var previousCursor = Cursor;
			Cursor = new Cursor(StandardCursorType.Wait);

			if (!ioProcessCancelled && !ioProcessError && result is SceneFacade newScene)
			{
				setSceneFacade(newScene);
			}

			GC.Collect();

			if (ioProcessError)
			{
				if (result is Exception exception)
				{
					await ShowErrorMessage("Error while loading mesh", Functions.BuildErrorMessage(exception));
				}
			}
			else if (SceneSettings.Instance.ShowOpenGLLowVersionMessage && !sceneFacade.CheckOpenGLVersion())
			{
				// TODO: port CheckMessageBox ("don't show again" check box); a plain message box is shown meanwhile
				await MessageBox.Show("This application requires graphics card that supports OpenGL version 2.0+ which includes Vertex buffer object (VBO) and OpenGL shading language (GLSL) support. Check that you have the latest graphics card drivers. OpenGL version supported by your graphics drivers is now " + SceneSettings.Instance.OpenGLVersion + ".", "Low OpenGL version", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			}

			Cursor = previousCursor;

			IOActionDone?.Invoke(this, EventArgs.Empty);
		}

		private void setSceneFacade(SceneFacade newSceneFacade)
		{
			object colorMode = sceneFacade.GetValue(AvailableValue.ColorMode);
			newSceneFacade.SetValue(AvailableValue.ColorMode, colorMode);

			SceneFacade = newSceneFacade; // the property must be used, it does the initialization
		}

		#endregion

		#region Saving file

		public void SaveToFile(string filename, bool saveWithoutHiddenElements)
		{
			if (IOActionInProgress)
				return;

			ioProcessCancelled = false;
			ioProcessError = false;
			ioCancellation = new CancellationTokenSource();
			var cancellation = ioCancellation.Token;
			string description = "Saving " + Path.GetFileName(filename);
			IOProgress?.Invoke(this, new IOProgressEventArgs(description, 0));
			var scene = sceneFacade;

			ioTask = runIOTask(() =>
			{
				MeshIOEventHandler progressNotifier = (s, ea) => onUIThread(() => IOProgress?.Invoke(this, new IOProgressEventArgs(description, ea.PercentDone)));
				YesNoQuestion cancelled = () => cancellation.IsCancellationRequested;
				scene.SaveMeshToFile(filename, saveWithoutHiddenElements, progressNotifier, cancelled);
				return null;
			}, result => savingCompleted(result, saveWithoutHiddenElements));
		}

		private async Task savingCompleted(object result, bool saveWithoutHiddenElements)
		{
			GC.Collect();

			if (ioProcessError)
			{
				if (result is MeshSavingException savingException)
				{
					await ShowErrorMessage("Error while saving", savingException.Message + Environment.NewLine + "(line number: " + savingException.LineNumber + ")");
				}
				else if (result is Exception ex)
				{
					await ShowErrorMessage("Error while saving", ex.Message);
				}
			}
			else if (saveWithoutHiddenElements)
			{
				// delete hidden elements after saving
				sceneFacade.PerformAction(AvailableAction.DeleteHiddenItems);
			}

			IOActionDone?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>
		/// Waits until the running load/save operation finishes.
		/// </summary>
		public Task WaitForIOActionAsync() => ioTask ?? Task.CompletedTask;

		#endregion

		#region Public methods

		public void ClearScene()
		{
			setSceneFacade(SceneFacade.GetEmptyScene());
		}

		public void SetNewScene(SceneFacade newSceneFacade)
		{
			Debug.Assert(newSceneFacade != null);
			setSceneFacade(newSceneFacade);
		}

		public async Task ShowInternalStateInfo()
		{
			glContext.MakeCurrent();
			float megaBytes = ((float)MemoryMeter.GetCurrentMemoryConsumption()) / 1048576f;
			StringBuilder text = new StringBuilder();

			text.Append("Current memory consumption: ");
			text.Append(Math.Round(megaBytes, 2));
			text.AppendLine(" MB");

			text.Append("OpenGL version: ");
			text.AppendLine(Functions.GetOpenGLVersionString());

			text.Append("Depth buffer bits: ");
			text.AppendLine(Functions.GetDepthBufferBits().ToString());

			text.Append("Multisampling (MSAA): ");
			text.AppendLine(glContext.Samples > 0 ? glContext.Samples + "x" : "off");

			text.Append("Display scaling: ");
			text.AppendLine(Math.Round(renderScaling * 100) + " %");

			await MessageBox.Show(text.ToString(), "Internal state info");
		}

		public async Task SetPropertyOfSelectedItems()
		{
			if (!sceneFacade.ContainsMesh)
				return;

			string description = (string)sceneFacade.GetValue(AvailableValue.SelectedItemsDescription);
			if (string.IsNullOrEmpty(description))
				description = "Nothing selected";
			var form = new PropertyInputDialog("Insert property number", description, sceneFacade);
			int value = 0;

			form.InputValueValidating += (sender, ea) =>
			{
				if (!int.TryParse(form.InputValue, out value))
					ea.Reject("Inserted value is not an integer", "Please input valid integer value" + Environment.NewLine + "in range <" + int.MinValue + "; " + int.MaxValue + ">");
			};

			if (await form.ShowDialog(null))
			{
				sceneFacade.SetPropertyOfSelectedItems(new MeshEditor.Data.Property(value));
			}

			MeshNeedRefresh?.Invoke(this, new MeshNeedRefreshEventArgs(sceneFacade.ActiveMeshUniqueIdentifier.Value));
			ActionPerformed?.Invoke(this, EventArgs.Empty);
		}

		public async Task SelectItemsWithProperty(string itemsName, bool addToSelection = false)
		{
			var form = new CheckedInputValueDialog("Insert property number", "Select " + itemsName + " by property:", "add to selection");
			form.IsChecked = addToSelection;

			int value = 0;
			form.InputValueValidating += (s, ea) =>
			{
				if (!int.TryParse(form.InputValue, out value) || value < 0)
					ea.Reject("Inserted value is not an integer", "Please input valid integer value" + Environment.NewLine + "in range <0; " + int.MaxValue + ">");
			};

			if (await form.ShowDialog(null))
			{
				AvailableAction action = form.IsChecked ? AvailableAction.SelectItemsWithPropertyAdd : AvailableAction.SelectItemsWithProperty;
				sceneFacade.PerformAction(action, new MeshEditor.Data.Property(value));
			}
		}

		public void SignalNodeByID(int[] ids)
		{
			if (ids == null || ids.Length == 0)
			{
				sceneFacade.PerformAction(AvailableAction.ClearSignalNode);
				return;
			}

			sceneFacade.PerformAction(AvailableAction.SignalNode, ids);
		}

		public async Task SignalNodeByID()
		{
			var form = new InputValueDialog("Insert node ID", "Signal node(s) with ID(s):");
			List<int> values = new List<int>();
			char[] splitCharacters = { ',', ';', ' ', '\t' };
			form.InputValueValidating += (s, ea) =>
			{
				values.Clear();
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
						if (!int.TryParse(textPart, out int value))
						{
							error = true;
							break;
						}
						values.Add(value);
					}
				}
				if (error)
					ea.Reject("Inserted value is not an integer", "Please input valid integer value.");
			};

			if (await form.ShowDialog(null))
			{
				SignalNodeByID(values.ToArray());
			}
		}

		public async Task SignalElementByID(bool clear, LongOpNotifier longOpNotifier)
		{
			if (clear)
			{
				var previousCursor = Cursor;
				using (longOpNotifier.Begin("Removing signalled element"))
				{
					Cursor = new Cursor(StandardCursorType.Wait);
					sceneFacade.PerformAction(AvailableAction.ClearSignalElement);
				}
				Cursor = previousCursor;
				return;
			}

			var form = new InputValueDialog("Insert element ID", "Signal element with ID:");
			int value = 0;

			form.InputValueValidating += (s, ea) =>
			{
				if (!int.TryParse(form.InputValue, out value))
					ea.Reject("Inserted value is not an integer", "Please input valid integer value.");
			};

			if (await form.ShowDialog(null))
			{
				sceneFacade.PerformAction(AvailableAction.SignalElement, value);
			}
		}

		/// <summary>
		/// Renders the scene and returns the pixels of the given area (whole viewport when empty) as top-down BGRA rows.
		/// </summary>
		public byte[] TakeScreenshot(DrawingRectangle screenCropWindow, out int width, out int height)
		{
			var clientRectangle = new DrawingRectangle(0, 0, glContext.Width, glContext.Height);
			DrawingRectangle area = screenCropWindow;
			if (area.IsEmpty)
				area = clientRectangle;
			else
				area.Intersect(clientRectangle); // check bounds

			sceneFacade.DrawScene(isActive, false);

			if (area.Width <= 0 || area.Height <= 0)
				throw new ArgumentException("Screen window width and height must be non-zero.", nameof(screenCropWindow));

			width = area.Width;
			height = area.Height;
			byte[] pixels = new byte[width * height * 4];
			unsafe
			{
				fixed (byte* p = pixels)
				{
					glContext.ReadPixelsBgra(area.Left, area.Top, width, height, (IntPtr)p, width * 4);
				}
			}
			Refresh(); // show the regular frame again
			return pixels;
		}

		public static Task ShowErrorMessage(string caption, string message)
		{
			return MessageBox.Show(message, caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
		}

		public void DisposeScene()
		{
			glContext.MakeCurrent();
			sceneFacade.DisposeScene();
			SceneFacade.EditorModeChanged -= editorModeChangedHandler;
			unhookSceneEvents();
			frontBuffer?.Dispose();
			frontBuffer = null;
			glContext.Dispose();
		}

		#endregion
	}
}
