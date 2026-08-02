using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using MeshEditor.CoreInterface;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTKPixelFormat = OpenTK.Graphics.OpenGL.PixelFormat;
using CoreMouseButton = MeshEditor.CoreInterface.MouseButton;

namespace MeshEditor.UI;

public partial class OpenGlSurface : UserControl
{
	public enum ViewportTool
	{
		Orbit,
		Pan,
		Zoom
	}

	private readonly ConcurrentQueue<string> pendingLoads = new();
	private OffscreenRenderWindow? renderWindow;
	private DispatcherTimer? renderTimer;
	private Bitmap? drawingBitmap;
	private int width;
	private int height;
	private ViewportTool activeTool = ViewportTool.Orbit;

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		base.OnAttachedToVisualTree(e);
		EnsureRenderWindow();
	}

	public void LoadMesh(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
			return;

		Console.WriteLine($"[OpenGlSurface] load requested: {path}");
		EnsureRenderWindow();
		pendingLoads.Enqueue(path);
		UpdateStatus("Loading mesh...");
	}

	public void SetTool(ViewportTool tool)
	{
		activeTool = tool;
		renderWindow?.SetTool(tool);
	}

	private void EnsureRenderWindow()
	{
		if (renderWindow != null || renderTimer != null)
			return;

		Console.WriteLine("[OpenGlSurface] creating render window");
		GLFWProvider.EnsureInitialized();
		renderWindow = new OffscreenRenderWindow(this);
		renderWindow.SetTool(activeTool);
		renderTimer = new DispatcherTimer(DispatcherPriority.Render);
		renderTimer.Interval = TimeSpan.FromMilliseconds(16);
		renderTimer.Tick += (_, _) => renderWindow.ProcessFrame();
		renderTimer.Start();
	}

	private void UpdateViewportSize()
	{
		if (renderWindow == null)
			return;

		var pixelSize = Bounds.Size;
		width = Math.Max(1, (int)Math.Round(pixelSize.Width));
		height = Math.Max(1, (int)Math.Round(pixelSize.Height));
		if (width <= 1 && height <= 1)
		{
			width = 1280;
			height = 800;
		}
		renderWindow.SetViewportSize(width, height);
	}

	private void Viewport_PointerPressed(object? sender, PointerPressedEventArgs e)
	{
		Console.WriteLine("[OpenGlSurface] pointer pressed");
		var point = ToViewportPoint(e);
		renderWindow?.HandlePointerPressed(point, GetPressedButton(e));
		renderWindow?.RequestRedraw();
		e.Handled = true;
	}

	private void Viewport_PointerMoved(object? sender, PointerEventArgs e)
	{
		var currentPoint = e.GetCurrentPoint(this);
		if (!currentPoint.Properties.IsLeftButtonPressed)
			return;

		Console.WriteLine("[OpenGlSurface] pointer moved");
		var point = ToViewportPoint(e);
		renderWindow?.HandlePointerMoved(point, CoreMouseButton.Left);
		renderWindow?.RequestRedraw();
		e.Handled = true;
	}

	private void Viewport_PointerReleased(object? sender, PointerReleasedEventArgs e)
	{
		Console.WriteLine("[OpenGlSurface] pointer released");
		var point = ToViewportPoint(e);
		renderWindow?.HandlePointerReleased(point, CoreMouseButton.Left);
		renderWindow?.RequestRedraw();
		e.Handled = true;
	}

	private void Viewport_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
	{
		Console.WriteLine("[OpenGlSurface] wheel changed");
		var point = ToViewportPoint(e);
		var delta = (int)Math.Sign(e.Delta.Y) * 120;
		renderWindow?.ZoomCamera(point, delta);
		renderWindow?.RequestRedraw();
		e.Handled = true;
	}

	private System.Drawing.Point ToViewportPoint(PointerEventArgs e)
	{
		var position = e.GetPosition(this);
		return new System.Drawing.Point(
			Math.Max(0, Math.Min((int)Math.Round(position.X), Math.Max(1, (int)Bounds.Width))),
			Math.Max(0, Math.Min((int)Math.Round(position.Y), Math.Max(1, (int)Bounds.Height))));
	}

	private CoreMouseButton GetPressedButton(PointerPressedEventArgs e)
	{
		var properties = e.GetCurrentPoint(this).Properties;
		if (properties.IsLeftButtonPressed)
			return CoreMouseButton.Left;
		if (properties.IsRightButtonPressed)
			return CoreMouseButton.Right;
		if (properties.IsMiddleButtonPressed)
			return CoreMouseButton.Middle;
		return CoreMouseButton.None;
	}

	internal void UpdateStatus(string text)
	{
		Dispatcher.UIThread.Post(() =>
		{
			if (StatusText is null)
				return;

			StatusText.Text = text;
		});
	}

	internal void UpdateFrame(byte[] pixels, int pixelWidth, int pixelHeight)
	{
		if (pixelWidth <= 0 || pixelHeight <= 0 || pixels.Length < pixelWidth * pixelHeight * 4)
			return;

		var needsResize = drawingBitmap is null || drawingBitmap.Width != pixelWidth || drawingBitmap.Height != pixelHeight;
		if (needsResize)
		{
			drawingBitmap = new Bitmap(pixelWidth, pixelHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
		}

		var data = drawingBitmap.LockBits(new Rectangle(0, 0, pixelWidth, pixelHeight), ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
		try
		{
			Marshal.Copy(pixels, 0, data.Scan0, pixels.Length);
		}
		finally
		{
			drawingBitmap.UnlockBits(data);
		}

		Dispatcher.UIThread.Post(() =>
		{
			using var stream = new MemoryStream();
			drawingBitmap.Save(stream, ImageFormat.Png);
			stream.Position = 0;
			ViewportImage.Source = new Avalonia.Media.Imaging.Bitmap(stream);
			StatusText.IsVisible = false;
		});
	}

	private sealed class OffscreenRenderWindow : NativeWindow
	{
		private readonly OpenGlSurface owner;
		private SceneFacade? sceneFacade;
		private bool sceneReady;
		private bool initialized;
		private bool needsRedraw;

		public OffscreenRenderWindow(OpenGlSurface owner)
			: base(new NativeWindowSettings
			{
				Title = "MeshEditor Render Host",
				StartVisible = false,
				APIVersion = new Version(3, 3),
				Profile = ContextProfile.Compatability,
				Flags = ContextFlags.Default,
				WindowBorder = WindowBorder.Hidden
			})
		{
			this.owner = owner;
			Initialize();
		}

		public void SetViewportSize(int width, int height)
		{
			if (width <= 0 || height <= 0)
				return;

			if (ClientSize.X != width || ClientSize.Y != height)
			{
				ClientSize = new Vector2i(width, height);
			}
		}

		public void SetTool(ViewportTool tool)
		{
			if (sceneFacade == null)
				return;

			switch (tool)
			{
				case ViewportTool.Orbit:
					SceneFacade.EditorMode = MeshEditor.CoreInterface.EditorMode.Orbit;
					break;
				case ViewportTool.Pan:
					SceneFacade.EditorMode = MeshEditor.CoreInterface.EditorMode.Pan;
					break;
				case ViewportTool.Zoom:
					SceneFacade.EditorMode = MeshEditor.CoreInterface.EditorMode.ZoomWindow;
					break;
			}
		}

		public void HandlePointerPressed(System.Drawing.Point location, CoreMouseButton button)
		{
			if (sceneFacade != null)
				sceneFacade.MouseDownHandler(location);
		}

		public void HandlePointerMoved(System.Drawing.Point location, CoreMouseButton button)
		{
			if (sceneFacade != null)
				sceneFacade.MouseMoveHandler(location, button);
		}

		public void HandlePointerReleased(System.Drawing.Point location, CoreMouseButton button)
		{
			if (sceneFacade != null)
				sceneFacade.MouseUpHandler(location, button);
		}

		public void ZoomCamera(System.Drawing.Point location, int delta)
		{
			if (sceneFacade != null)
				sceneFacade.ZoomCamera(location, delta);
		}

		public void RequestRedraw()
		{
			needsRedraw = true;
		}

		public void ProcessFrame()
		{
			if (!initialized)
			{
				Initialize();
				return;
			}

			ProcessEvents(0.0);

			if (sceneFacade != null && sceneReady)
			{
				while (owner.pendingLoads.TryDequeue(out var path))
				{
					try
					{
						owner.UpdateStatus("Loading mesh...");
						Console.WriteLine($"[OpenGlSurface] loading mesh from file: {path}");
						sceneFacade.LoadMeshFromFiles(new[] { path }, null, null);
						Console.WriteLine($"[OpenGlSurface] mesh loaded, contains mesh: {sceneFacade.ContainsMesh}");
						sceneFacade.ResizeScene(ClientSize.X, ClientSize.Y);
						sceneFacade.PerformAction(AvailableAction.CameraReset);
						sceneFacade.PerformAction(AvailableAction.ZoomToFit);
						owner.UpdateStatus("Mesh loaded");
					}
					catch (Exception ex)
					{
						Console.WriteLine($"[OpenGlSurface] load failed: {ex}");
						owner.UpdateStatus($"Error: {ex.Message}");
					}
				}

				if (sceneFacade.ContainsMesh)
				{
					try
					{
						if (needsRedraw)
						{
							needsRedraw = false;
						}

						sceneFacade.DrawScene(isActive: true, swapBuffers: false);

						GL.Finish();
						GL.ReadBuffer(ReadBufferMode.Front);
						GL.PixelStore(PixelStoreParameter.PackAlignment, 1);
						GL.PixelStore(PixelStoreParameter.PackRowLength, 0);
						GL.PixelStore(PixelStoreParameter.PackSkipRows, 0);
						GL.PixelStore(PixelStoreParameter.PackSkipPixels, 0);

						var pixelCount = ClientSize.X * ClientSize.Y * 4;
						var pixels = new byte[pixelCount];
						GL.ReadPixels(0, 0, ClientSize.X, ClientSize.Y, OpenTKPixelFormat.Rgba, PixelType.UnsignedByte, pixels);

						var flipped = new byte[pixelCount];
						var rowBytes = ClientSize.X * 4;
						for (var row = 0; row < ClientSize.Y; row++)
						{
							System.Buffer.BlockCopy(pixels, row * rowBytes, flipped, (ClientSize.Y - 1 - row) * rowBytes, rowBytes);
						}

						owner.UpdateFrame(flipped, ClientSize.X, ClientSize.Y);
					}
					catch (Exception ex)
					{
						Console.WriteLine($"[OpenGlSurface] draw failed: {ex}");
						owner.UpdateStatus($"Error: {ex.Message}");
					}
				}
			}

			Context.SwapBuffers();
			Context.SwapInterval = 0;
		}

		private void Initialize()
		{
			if (initialized)
				return;

			Console.WriteLine("[OpenGlSurface] initializing OpenGL scene");
			MakeCurrent();
			SceneFacade.InitializeGL();
			sceneFacade = SceneFacade.GetEmptyScene();
			Console.WriteLine("[OpenGlSurface] scene facade created");
			sceneFacade.MakeCurrentNeeded += (_, _) => MakeCurrent();
			sceneFacade.Initialize();
			Console.WriteLine("[OpenGlSurface] scene facade initialized");
			sceneReady = true;
			initialized = true;
			owner.UpdateStatus("Waiting for mesh...");
		}
	}
}

