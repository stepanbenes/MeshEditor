using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using MeshEditor.CoreInterface;
using MeshEditor.Data;
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
	public event Action<string>? StatusChanged;

	public sealed class MeshInfoSnapshot
	{
		public bool HasMesh { get; init; }
		public bool MeshHasHiddenElements { get; init; }
		public int NodeCount { get; init; }
		public int ElementCount { get; init; }
		public int FaceCount { get; init; }
		public int EdgeCount { get; init; }
		public int BeamCount { get; init; }
		public string SelectedItemsDescription { get; init; } = string.Empty;
		public string[] PropertyRows { get; init; } = Array.Empty<string>();
	}

	public sealed class SelectedItemsSnapshot
	{
		public bool HasMesh { get; init; }
		public ItemTypeToSelect ItemType { get; init; }
		public int Count { get; init; }
		public string Description { get; init; } = string.Empty;
	}

	private sealed class MeshInfoRequest
	{
		public required TaskCompletionSource<MeshInfoSnapshot?> Completion { get; init; }
	}

	private sealed class SelectedItemsRequest
	{
		public required ItemTypeToSelect ItemType { get; init; }
		public required bool ShowCompleteInfo { get; init; }
		public required TaskCompletionSource<SelectedItemsSnapshot?> Completion { get; init; }
	}

	public enum ViewportTool
	{
		Orbit,
		Pan,
		Zoom
	}

	private readonly ConcurrentQueue<string> pendingLoads = new();
	private readonly ConcurrentQueue<string> pendingSaves = new();
	private readonly ConcurrentQueue<int[]> pendingSignalNodes = new();
	private readonly ConcurrentQueue<int> pendingSignalElements = new();
	private readonly ConcurrentQueue<(int property, bool add)> pendingSelectByProperty = new();
	private readonly ConcurrentQueue<int> pendingSetProperty = new();
	private readonly ConcurrentQueue<(int property, bool add)> pendingSelectedNodePropertyChanges = new();
	private readonly ConcurrentQueue<MeshInfoRequest> pendingMeshInfoRequests = new();
	private readonly ConcurrentQueue<SelectedItemsRequest> pendingSelectedItemsRequests = new();
	private int clearSignalNodeRequested;
	private int clearSignalElementRequested;
	private int applySettingsRequested;
	private OffscreenRenderWindow? renderWindow;
	private DispatcherTimer? renderTimer;
	private WriteableBitmap? drawingBitmap;
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

	public void SaveMesh(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
			return;

		Console.WriteLine($"[OpenGlSurface] save requested: {path}");
		EnsureRenderWindow();
		pendingSaves.Enqueue(path);
		UpdateStatus("Saving mesh...");
	}

	public void SignalNodes(int[] nodeIds)
	{
		if (nodeIds == null || nodeIds.Length == 0)
			return;

		EnsureRenderWindow();
		pendingSignalNodes.Enqueue(nodeIds);
		renderWindow?.RequestRedraw();
	}

	public void SignalElement(int elementId)
	{
		EnsureRenderWindow();
		pendingSignalElements.Enqueue(elementId);
		renderWindow?.RequestRedraw();
	}

	public void ClearSignalNode()
	{
		EnsureRenderWindow();
		Interlocked.Exchange(ref clearSignalNodeRequested, 1);
		renderWindow?.RequestRedraw();
	}

	public void ClearSignalElement()
	{
		EnsureRenderWindow();
		Interlocked.Exchange(ref clearSignalElementRequested, 1);
		renderWindow?.RequestRedraw();
	}

	public void SetPropertyOfSelectedItems(int property)
	{
		EnsureRenderWindow();
		pendingSetProperty.Enqueue(property);
		renderWindow?.RequestRedraw();
	}

	public void SelectItemsByProperty(int property, bool addToSelection)
	{
		EnsureRenderWindow();
		pendingSelectByProperty.Enqueue((property, addToSelection));
		renderWindow?.RequestRedraw();
	}

	public void AddPropertyToSelectedNodes(int property)
	{
		EnsureRenderWindow();
		pendingSelectedNodePropertyChanges.Enqueue((property, add: true));
		renderWindow?.RequestRedraw();
	}

	public void RemovePropertyFromSelectedNodes(int property)
	{
		EnsureRenderWindow();
		pendingSelectedNodePropertyChanges.Enqueue((property, add: false));
		renderWindow?.RequestRedraw();
	}

	public void ApplySceneSettings()
	{
		EnsureRenderWindow();
		Interlocked.Exchange(ref applySettingsRequested, 1);
		renderWindow?.RequestRedraw();
	}

	public Task<MeshInfoSnapshot?> GetMeshInfoAsync()
	{
		EnsureRenderWindow();
		var tcs = new TaskCompletionSource<MeshInfoSnapshot?>(TaskCreationOptions.RunContinuationsAsynchronously);
		pendingMeshInfoRequests.Enqueue(new MeshInfoRequest { Completion = tcs });
		renderWindow?.RequestRedraw();
		return tcs.Task;
	}

	public Task<SelectedItemsSnapshot?> GetSelectedItemsDescriptionAsync(ItemTypeToSelect itemType, bool showCompleteInfo)
	{
		EnsureRenderWindow();
		var tcs = new TaskCompletionSource<SelectedItemsSnapshot?>(TaskCreationOptions.RunContinuationsAsynchronously);
		pendingSelectedItemsRequests.Enqueue(new SelectedItemsRequest
		{
			ItemType = itemType,
			ShowCompleteInfo = showCompleteInfo,
			Completion = tcs
		});
		renderWindow?.RequestRedraw();
		return tcs.Task;
	}

	public bool SaveScreenshot(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
			return false;

		WriteableBitmap? snapshot = null;
		Dispatcher.UIThread.Invoke(() =>
		{
			snapshot = drawingBitmap;
		});

		if (snapshot is null)
		{
			UpdateStatus("No frame to save");
			return false;
		}

		try
		{
			using var stream = File.Create(path);
			snapshot.Save(stream);
			UpdateStatus("Screenshot saved");
			return true;
		}
		catch (Exception ex)
		{
			UpdateStatus($"Error: {ex.Message}");
			return false;
		}
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
			StatusChanged?.Invoke(text);
		});
	}

	internal void UpdateFrame(byte[] pixels, int pixelWidth, int pixelHeight)
	{
		if (pixelWidth <= 0 || pixelHeight <= 0 || pixels.Length < pixelWidth * pixelHeight * 4)
			return;

		var needsResize = drawingBitmap is null || drawingBitmap.PixelSize.Width != pixelWidth || drawingBitmap.PixelSize.Height != pixelHeight;
		if (needsResize)
		{
			drawingBitmap = new WriteableBitmap(
				new PixelSize(pixelWidth, pixelHeight),
				new Vector(96, 96),
				PixelFormats.Bgra8888,
				AlphaFormat.Unpremul);
		}

		Dispatcher.UIThread.Post(() =>
		{
			if (drawingBitmap is null)
				return;

			using (var framebuffer = drawingBitmap.Lock())
			{
				Marshal.Copy(pixels, 0, framebuffer.Address, pixels.Length);
			}

			ViewportImage.Source = drawingBitmap;
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

				while (owner.pendingSaves.TryDequeue(out var path))
				{
					try
					{
						if (!sceneFacade.ContainsMesh)
						{
							owner.UpdateStatus("No mesh loaded");
							continue;
						}

						owner.UpdateStatus("Saving mesh...");
						sceneFacade.SaveMeshToFile(path, false, null, null);
						owner.UpdateStatus("Mesh saved");
					}
					catch (Exception ex)
					{
						Console.WriteLine($"[OpenGlSurface] save failed: {ex}");
						owner.UpdateStatus($"Error: {ex.Message}");
					}
				}

				if (Interlocked.Exchange(ref owner.clearSignalNodeRequested, 0) == 1)
				{
					if (sceneFacade.ContainsMesh)
						sceneFacade.PerformAction(AvailableAction.ClearSignalNode);
					else
						owner.UpdateStatus("No mesh loaded");
				}

				if (Interlocked.Exchange(ref owner.clearSignalElementRequested, 0) == 1)
				{
					if (sceneFacade.ContainsMesh)
						sceneFacade.PerformAction(AvailableAction.ClearSignalElement);
					else
						owner.UpdateStatus("No mesh loaded");
				}

				while (owner.pendingSignalNodes.TryDequeue(out var nodeIds))
				{
					if (!sceneFacade.ContainsMesh)
					{
						owner.UpdateStatus("No mesh loaded");
						continue;
					}

					sceneFacade.PerformAction(AvailableAction.SignalNode, nodeIds);
				}

				while (owner.pendingSignalElements.TryDequeue(out var elementId))
				{
					if (!sceneFacade.ContainsMesh)
					{
						owner.UpdateStatus("No mesh loaded");
						continue;
					}

					sceneFacade.PerformAction(AvailableAction.SignalElement, elementId);
				}

				while (owner.pendingSetProperty.TryDequeue(out var property))
				{
					if (!sceneFacade.ContainsMesh)
					{
						owner.UpdateStatus("No mesh loaded");
						continue;
					}

					sceneFacade.SetPropertyOfSelectedItems(new Property(property));
					sceneFacade.PerformAction(AvailableAction.Refresh);
					owner.UpdateStatus($"Property {property} applied to selected items");
				}

				while (owner.pendingSelectByProperty.TryDequeue(out var request))
				{
					if (!sceneFacade.ContainsMesh)
					{
						owner.UpdateStatus("No mesh loaded");
						continue;
					}

					var action = request.add
						? AvailableAction.SelectItemsWithPropertyAdd
						: AvailableAction.SelectItemsWithProperty;
					sceneFacade.PerformAction(action, new Property(request.property));
					sceneFacade.PerformAction(AvailableAction.Refresh);
					owner.UpdateStatus($"Selected items by property {request.property}");
				}

				while (owner.pendingSelectedNodePropertyChanges.TryDequeue(out var propertyChange))
				{
					if (!sceneFacade.ContainsMesh)
					{
						owner.UpdateStatus("No mesh loaded");
						continue;
					}

					var action = propertyChange.add
						? AvailableAction.AddPropertyToSelectedNodes
						: AvailableAction.RemovePropertyFromSelectedNodes;
					sceneFacade.PerformAction(action, new Property(propertyChange.property));
					sceneFacade.PerformAction(AvailableAction.Refresh);
					owner.UpdateStatus(propertyChange.add
						? $"Added property {propertyChange.property} to selected nodes"
						: $"Removed property {propertyChange.property} from selected nodes");
				}

				if (Interlocked.Exchange(ref owner.applySettingsRequested, 0) == 1)
				{
					sceneFacade.PerformAction(AvailableAction.UpdateColorBuffers);
					sceneFacade.PerformAction(AvailableAction.RecreateBuffers);
					sceneFacade.PerformAction(AvailableAction.Refresh);
				}

				while (owner.pendingMeshInfoRequests.TryDequeue(out var request))
				{
					try
					{
						if (!sceneFacade.ContainsMesh)
						{
							request.Completion.TrySetResult(new MeshInfoSnapshot { HasMesh = false });
							continue;
						}

						while (owner.pendingSelectedItemsRequests.TryDequeue(out var selectedRequest))
						{
							try
							{
								if (!sceneFacade.ContainsMesh)
								{
									selectedRequest.Completion.TrySetResult(new SelectedItemsSnapshot
									{
										HasMesh = false,
										ItemType = selectedRequest.ItemType
									});
									continue;
								}

								sceneFacade.GetSelectionSummary(out var nodeCount, out var elementCount, out var faceCount, out var edgeCount);
								var count = selectedRequest.ItemType switch
								{
									ItemTypeToSelect.Node => nodeCount,
									ItemTypeToSelect.Element => elementCount,
									ItemTypeToSelect.Face => faceCount,
									ItemTypeToSelect.Edge => edgeCount,
									_ => 0
								};

								var description = sceneFacade.GetDescriptionOfSelectedItems(selectedRequest.ItemType, selectedRequest.ShowCompleteInfo) ?? string.Empty;
								selectedRequest.Completion.TrySetResult(new SelectedItemsSnapshot
								{
									HasMesh = true,
									ItemType = selectedRequest.ItemType,
									Count = count,
									Description = description
								});
							}
							catch (Exception ex)
							{
								selectedRequest.Completion.TrySetException(ex);
							}
						}

						var stats = sceneFacade.GetValue(AvailableValue.MeshStatistics) as MeshStatistics;
						var rows = new List<string>();
						if (stats != null)
						{
							foreach (var pair in stats.GetAllPropertyEntityPairs())
							{
								stats.PropertyComments.TryGetValue(pair.Property, out var comment);
								stats.PropertyCommands.TryGetValue(pair, out var commands);
								var commandText = commands is null ? string.Empty : string.Join("; ", commands);
								rows.Add($"{pair.Property} [{pair.EntityType}]  Commands: {commandText}  Comment: {comment}");
							}
						}

						request.Completion.TrySetResult(new MeshInfoSnapshot
						{
							HasMesh = true,
							MeshHasHiddenElements = (bool)(sceneFacade.GetValue(AvailableValue.MeshHasHiddenElements) ?? false),
							NodeCount = (int)(sceneFacade.GetValue(AvailableValue.NodeCount) ?? 0),
							ElementCount = (int)(sceneFacade.GetValue(AvailableValue.ElementCount) ?? 0),
							FaceCount = (int)(sceneFacade.GetValue(AvailableValue.FaceCount) ?? 0),
							EdgeCount = (int)(sceneFacade.GetValue(AvailableValue.EdgeCount) ?? 0),
							BeamCount = (int)(sceneFacade.GetValue(AvailableValue.BeamCount) ?? 0),
							SelectedItemsDescription = sceneFacade.GetValue(AvailableValue.SelectedItemsDescription)?.ToString() ?? string.Empty,
							PropertyRows = rows.ToArray()
						});
					}
					catch (Exception ex)
					{
						request.Completion.TrySetException(ex);
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
							var sourceOffset = row * rowBytes;
							var destinationOffset = (ClientSize.Y - 1 - row) * rowBytes;
							for (var columnOffset = 0; columnOffset < rowBytes; columnOffset += 4)
							{
								// RGBA -> BGRA
								flipped[destinationOffset + columnOffset + 0] = pixels[sourceOffset + columnOffset + 2];
								flipped[destinationOffset + columnOffset + 1] = pixels[sourceOffset + columnOffset + 1];
								flipped[destinationOffset + columnOffset + 2] = pixels[sourceOffset + columnOffset + 0];
								flipped[destinationOffset + columnOffset + 3] = pixels[sourceOffset + columnOffset + 3];
							}
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
