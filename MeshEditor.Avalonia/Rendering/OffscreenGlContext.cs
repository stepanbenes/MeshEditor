using System;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Mathematics;

namespace MeshEditor.AvaloniaUI.Rendering
{
	/// <summary>
	/// OpenGL context backed by a hidden GLFW window, rendering into an offscreen framebuffer object.
	/// </summary>
	/// <remarks>
	/// The renderer in MeshEditor.Core uses the fixed-function pipeline (GL.Begin/End, matrix stack, GLSL without #version),
	/// so it needs a compatibility (or legacy 2.1) context. Avalonia's own OpenGL control does not guarantee that on all
	/// platforms (on Windows it defaults to ANGLE/GLES), therefore the scene is rendered here and copied into an Avalonia bitmap.
	/// Every viewport has its own context (like every GLControl had in the WinForms UI) because the core keeps per-view state
	/// (viewport, projection, lights) in the GL context. All contexts share objects (VBOs, textures) with a hidden root context.
	/// </remarks>
	internal sealed class OffscreenGlContext : IDisposable
	{
		private static NativeWindow rootWindow;

		private NativeWindow window;

		// single-sampled framebuffer: target of the auxiliary depth passes of the core (picking/visibility) and source of the read back
		private int framebuffer;
		private int colorRenderbuffer;
		private int depthRenderbuffer;

		// multisampled framebuffer: target of the visible frame (antialiasing), resolved into the single-sampled one
		private int msaaFramebuffer;
		private int msaaColorRenderbuffer;
		private int msaaDepthRenderbuffer;
		private bool msaaFrameUnresolved;

		// read back buffer, reused between frames (a new one per frame would be a large object heap allocation)
		private byte[] readBuffer;

		/// <summary>
		/// Number of MSAA samples requested (environment variable MESHEDITOR_MSAA_SAMPLES overrides, 0 disables).
		/// </summary>
		private static readonly int requestedSamples = int.TryParse(Environment.GetEnvironmentVariable("MESHEDITOR_MSAA_SAMPLES"), out int value) ? Math.Max(0, value) : 4;

		public int Width { get; private set; }
		public int Height { get; private set; }

		/// <summary>
		/// MSAA samples of the frame (0 = no multisampling).
		/// </summary>
		public int Samples { get; private set; }

		/// <summary>
		/// Root context all viewport contexts share their objects with. Created lazily on the UI thread.
		/// </summary>
		public static IGLFWGraphicsContext Root
		{
			get
			{
				if (rootWindow == null)
					rootWindow = createHiddenWindow(null);
				return rootWindow.Context;
			}
		}

		public OffscreenGlContext()
		{
			window = createHiddenWindow(Root);
			MakeCurrent();
			Resize(1, 1);
		}

		private static NativeWindow createHiddenWindow(IGLFWGraphicsContext sharedContext)
		{
			var settings = new NativeWindowSettings
			{
				API = ContextAPI.OpenGL,
				// Version 2.1 with "any" profile gives the highest compatibility profile on Windows/Linux and legacy 2.1 on macOS.
				APIVersion = new Version(2, 1),
				Profile = ContextProfile.Any,
				Flags = ContextFlags.Offscreen,
				StartVisible = false,
				StartFocused = false,
				ClientSize = new Vector2i(16, 16),
				Title = "MeshEditor offscreen GL context",
				SharedContext = sharedContext,
			};
			return new NativeWindow(settings);
		}

		public bool IsCurrent => window?.Context.IsCurrent == true;

		/// <summary>
		/// Makes the context current with the single-sampled framebuffer bound (resolving the last multisampled frame first);
		/// depth reads of the core (glReadPixels of depth) are not possible from a multisampled framebuffer.
		/// </summary>
		public void MakeCurrent()
		{
			if (!window.Context.IsCurrent)
				window.Context.MakeCurrent();
			resolveFrame();
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer);
		}

		/// <summary>
		/// Redirects drawing of the visible frame into the multisampled framebuffer.
		/// </summary>
		public void BeginFrame()
		{
			if (Samples == 0)
				return;
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, msaaFramebuffer);
			GL.Enable(EnableCap.Multisample);
			msaaFrameUnresolved = true;
		}

		private void resolveFrame()
		{
			if (!msaaFrameUnresolved)
				return;
			msaaFrameUnresolved = false;
			GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, msaaFramebuffer);
			GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, framebuffer);
			// only color: every depth read of the core (picking, point under cursor, visible nodes) is preceded
			// by its own depth pass drawn into the single-sampled framebuffer
			GL.BlitFramebuffer(0, 0, Width, Height, 0, 0, Width, Height, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer);
		}

		/// <summary>
		/// (Re)creates the offscreen framebuffer with the given size in physical pixels.
		/// </summary>
		public void Resize(int width, int height)
		{
			width = Math.Max(1, width);
			height = Math.Max(1, height);
			if (width == Width && height == Height && framebuffer != 0)
				return;

			window.Context.MakeCurrent();
			deleteFramebuffer();

			framebuffer = GL.GenFramebuffer();
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer);

			colorRenderbuffer = GL.GenRenderbuffer();
			GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, colorRenderbuffer);
			GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.Rgba8, width, height);
			GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, RenderbufferTarget.Renderbuffer, colorRenderbuffer);

			depthRenderbuffer = GL.GenRenderbuffer();
			GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, depthRenderbuffer);
			GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.Depth24Stencil8, width, height);
			GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, RenderbufferTarget.Renderbuffer, depthRenderbuffer);

			var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
			if (status != FramebufferErrorCode.FramebufferComplete)
				throw new InvalidOperationException($"Offscreen framebuffer is not complete ({status}).");

			GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
			GL.ReadBuffer(ReadBufferMode.ColorAttachment0);

			Width = width;
			Height = height;

			createMultisampledFramebuffer(width, height);
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer);
		}

		private void createMultisampledFramebuffer(int width, int height)
		{
			GL.GetInteger(GetPName.MaxSamples, out int maxSamples);
			// error flags stay set until read: clear the ones left by earlier rendering, so they are not blamed on the framebuffer
			for (int i = 0; i < 16 && GL.GetError() != ErrorCode.NoError; i++) { }
			for (int samples = Math.Min(requestedSamples, maxSamples); samples > 1; samples /= 2)
			{
				msaaFramebuffer = GL.GenFramebuffer();
				GL.BindFramebuffer(FramebufferTarget.Framebuffer, msaaFramebuffer);

				msaaColorRenderbuffer = GL.GenRenderbuffer();
				GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, msaaColorRenderbuffer);
				GL.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, samples, RenderbufferStorage.Rgba8, width, height);
				GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, RenderbufferTarget.Renderbuffer, msaaColorRenderbuffer);

				msaaDepthRenderbuffer = GL.GenRenderbuffer();
				GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, msaaDepthRenderbuffer);
				GL.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, samples, RenderbufferStorage.Depth24Stencil8, width, height);
				GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, RenderbufferTarget.Renderbuffer, msaaDepthRenderbuffer);

				if (GL.GetError() == ErrorCode.NoError && GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) == FramebufferErrorCode.FramebufferComplete)
				{
					GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
					GL.ReadBuffer(ReadBufferMode.ColorAttachment0);
					Samples = samples;
					return;
				}
				deleteMultisampledFramebuffer(); // e.g. out of memory, try fewer samples
			}
			Samples = 0;
		}

		private void deleteMultisampledFramebuffer()
		{
			if (msaaFramebuffer != 0)
			{
				GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
				GL.DeleteFramebuffer(msaaFramebuffer);
				GL.DeleteRenderbuffer(msaaColorRenderbuffer);
				GL.DeleteRenderbuffer(msaaDepthRenderbuffer);
				msaaFramebuffer = msaaColorRenderbuffer = msaaDepthRenderbuffer = 0;
			}
			msaaFrameUnresolved = false;
		}

		/// <summary>
		/// Copies a region of the rendered image (OpenGL bottom-up rows) into a top-down BGRA buffer.
		/// </summary>
		/// <param name="x">Left edge in pixels.</param>
		/// <param name="yFromTop">Top edge in pixels, measured from the top of the framebuffer.</param>
		public void ReadPixelsBgra(int x, int yFromTop, int width, int height, IntPtr destination, int destinationStride)
		{
			MakeCurrent();
			int rowBytes = width * 4;
			if (readBuffer == null || readBuffer.Length < rowBytes * height)
				readBuffer = new byte[rowBytes * height];
			byte[] buffer = readBuffer;
			GL.PixelStore(PixelStoreParameter.PackAlignment, 4);
			GL.ReadPixels(x, Height - yFromTop - height, width, height, PixelFormat.Bgra, PixelType.UnsignedByte, buffer);
			for (int row = 0; row < height; row++)
			{
				// OpenGL rows are bottom-up
				System.Runtime.InteropServices.Marshal.Copy(buffer, (height - 1 - row) * rowBytes, destination + row * destinationStride, rowBytes);
			}
		}

		private void deleteFramebuffer()
		{
			deleteMultisampledFramebuffer();
			if (framebuffer != 0)
			{
				GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
				GL.DeleteFramebuffer(framebuffer);
				GL.DeleteRenderbuffer(colorRenderbuffer);
				GL.DeleteRenderbuffer(depthRenderbuffer);
				framebuffer = colorRenderbuffer = depthRenderbuffer = 0;
			}
		}

		public void Dispose()
		{
			if (window == null)
				return;
			window.Context.MakeCurrent();
			deleteFramebuffer();
			window.Dispose();
			window = null;
		}
	}
}
