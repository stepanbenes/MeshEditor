using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using MeshEditor.AvaloniaUI.Dialogs;
using MeshEditor.Graphics;
using MeshEditor.LayerManager.Import;
using MeshEditor.SolutionManager;
using NUnit.Framework;

namespace MeshEditor.AvaloniaUI.Tests
{
	/// <summary>
	/// Renders the picture of the About dialog (Assets/about.png) in the postprocessor, and a screenshot of the dialog.
	/// Explicit: run them by
	/// xvfb-run -a dotnet test MeshEditor.Avalonia.Tests --filter "FullyQualifiedName~AboutPictureTests.Render_About_picture"
	/// with ABOUT_PICTURE_DIRECTORY set to the directory of the pictures (MeshEditor.Avalonia/Assets).
	/// </summary>
	[Explicit("renders the picture of the About dialog")]
	public class AboutPictureTests
	{
		private static string outputDirectory => Environment.GetEnvironmentVariable("ABOUT_PICTURE_DIRECTORY") is string directory && directory != ""
			? directory
			: TestContext.CurrentContext.WorkDirectory;

		// a plate with a hole under tension in the y direction (and compression in the x direction: pure shear)
		private static double holeRadius => setting("ABOUT_PICTURE_HOLE", 0.55);
		private static double halfHeight => setting("ABOUT_PICTURE_HALF_HEIGHT", 1.45);
		private static double compression => setting("ABOUT_PICTURE_COMPRESSION", 0);
		private const double thickness = 0.16;
		private const int angleDivisions = 48, radialDivisions = 10, endDivisions = 5; // angleDivisions is a multiple of 8: rays go to the corners of the square

		private static async Task waitUntil(Func<bool> condition, string description, int timeoutSeconds = 60)
		{
			var timeout = DateTime.UtcNow.AddSeconds(timeoutSeconds);
			while (true)
			{
				Dispatcher.UIThread.RunJobs();
				if (condition())
					break;
				if (DateTime.UtcNow > timeout)
					Assert.Fail($"timeout: {description}");
				await Task.Delay(20);
			}
			Dispatcher.UIThread.RunJobs();
		}

		[AvaloniaTest]
		public async Task Render_About_picture()
		{
			string directory = Path.Combine(Path.GetTempPath(), "MeshEditorTests", Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(directory);
			string meshFile = Path.Combine(directory, "plate.msh"), resultFile = Path.Combine(directory, "plate.res");
			writePlate(meshFile, resultFile);

			var analysisResults = new[] { new AnalysisResult { TimeStep = null, MeshRecordNames = new[] { meshFile }, DataRecordNames = new[] { resultFile } } };
			var solutionHub = SolutionHub.CreateNewLocal(Path.Combine(directory, "plate"), analysisResults, "plate");
			solutionHub.Import(Enumerable.Empty<decimal>(), Array.Empty<string>(), "Nearest");

			// dark background (the legend text gets the contrast color), dark edges
			var settings = MeshEditor.CoreInterface.SceneSettings.Instance;
			settings.ActiveBackColor = System.Drawing.Color.FromArgb(0x1C, 0x22, 0x30);
			settings.NonActiveBackColor = settings.ActiveBackColor;
			settings.OrdinaryEdgeColor = System.Drawing.Color.FromArgb(0x10, 0x14, 0x1C);
			settings.FirstBorderColor = System.Drawing.Color.Black;
			settings.SecondBorderColor = System.Drawing.Color.Black;

			var window = new MainWindow(Array.Empty<string>()) { WindowState = WindowState.Normal, Width = 1000, Height = 600 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			await window.openFiles(solutionHub.GetSolutionDescription().Location);
			var postprocessView = window.getCurrentPostprocessView();
			var dataSelection = postprocessView.dataSelectionControl;
			await waitUntil(() => dataSelection.comboBoxField.ItemCount > 0 && window.ActiveViewport.SceneFacade.ContainsMesh, "layer is loaded");
			dataSelection.comboBoxField.SelectedItem = dataSelection.comboBoxField.Items.Single(i => i.ToString() == "von Mises stress");
			await waitUntil(() => dataSelection.linkLabelEditColorScale.IsEnabled, "scalar field is selected");

			// a portrait viewport (the picture's column of the dialog)
			var viewport = window.ActiveViewport;
			const double aspectRatio = 0.66;
			viewport.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
			viewport.Width = Math.Round(viewport.Bounds.Height * aspectRatio);
			await waitUntil(() => Math.Abs(viewport.PixelSize.Width - viewport.PixelSize.Height * aspectRatio) < 2, "viewport is resized", timeoutSeconds: 10);

			var sceneFacade = viewport.SceneFacade;
			var scene = sceneFacade.GetUnderlyingSceneObject();
			scene.RenderMode = RenderMode.FacesLines;
			scene.DrawAxes = false;
			scene.DrawAxisArrows = false;
			sceneFacade.PerformAction(MeshEditor.CoreInterface.AvailableAction.CameraStandardView, CameraView.Front);
			sceneFacade.PerformAction(MeshEditor.CoreInterface.AvailableAction.ZoomToFit);
			// iso-areas, no legend
			var dataVisualizer = (MeshEditor.DataVisualizer.LayerDataVisualizer)postprocessView.ActiveScene.GetValue(MeshEditor.CoreInterface.AvailableValue.DataVisualizer);
			dataVisualizer.Settings.DrawIsoAreas = true;
			dataVisualizer.Settings.IsoAreasSubIntervalNumber = (int)setting("ABOUT_PICTURE_ISO_SUBINTERVALS", 3);
			dataVisualizer.Settings.ShowColorScaleLegend = false;

			byte[] frame = null;
			int width = 0, height = 0;
			async Task render()
			{
				for (int i = 0; i < 5; i++) // the color buffers are updated asynchronously
				{
					viewport.Refresh();
					await Task.Delay(100);
					Dispatcher.UIThread.RunJobs();
				}
				viewport.Refresh();
				frame = viewport.TakeScreenshot(System.Drawing.Rectangle.Empty, out width, out height);
			}

			// the mesh fills the picture: move the camera until the bounding box of the mesh pixels touches the margins
			var camera = scene.Camera;
			var background = settings.ActiveBackColor;
			double margin = setting("ABOUT_PICTURE_MARGIN", 0.02);
			for (int iteration = 0; iteration < 30; iteration++)
			{
				await render();
				int left = width, right = -1, top = height, bottom = -1;
				for (int y = 0; y < height; y++)
					for (int x = 0; x < width; x++)
					{
						int k = (y * width + x) * 4;
						if (Math.Abs(frame[k] - background.B) + Math.Abs(frame[k + 1] - background.G) + Math.Abs(frame[k + 2] - background.R) > 24)
						{
							left = Math.Min(left, x);
							right = Math.Max(right, x);
							top = Math.Min(top, y);
							bottom = Math.Max(bottom, y);
						}
					}
				Assert.That(right, Is.GreaterThanOrEqualTo(0), "the mesh is visible");
				// the offset of the centre of the box from the centre of the picture, and the scale to the margins (fractions of the height)
				double offsetX = ((left + right) / 2.0 - width / 2.0) / height, offsetY = ((top + bottom) / 2.0 - height / 2.0) / height;
				double scale = Math.Min(width * (1 - 2 * margin) / (right - left + 1), height * (1 - 2 * margin) / (bottom - top + 1));
				TestContext.Out.WriteLine($"fit {iteration}: box {left}..{right} x {top}..{bottom}, offset {offsetX:0.000} {offsetY:0.000}, scale {scale:0.000}");
				if (Math.Abs(offsetX) < 0.003 && Math.Abs(offsetY) < 0.003 && Math.Abs(scale - 1) < 0.005)
					break;
				var direction = camera.GetDirection();
				float distance = (camera.Eye - scene.Mesh.CenterOfRotation).Length;
				var rightVector = OpenTK.Mathematics.Vector3.Normalize(OpenTK.Mathematics.Vector3.Cross(direction, camera.Up));
				var upVector = OpenTK.Mathematics.Vector3.Normalize(camera.Up);
				// about 0.8 of the distance is the visible height (the field of view)
				camera.Move(rightVector * (float)(offsetX * distance * 0.8) - upVector * (float)(offsetY * distance * 0.8));
				camera.MoveForward(distance * (float)(1 - 1 / Math.Max(0.5, Math.Min(scale, 2))) * 0.8f);
			}
			await render();
			string path = Path.Combine(outputDirectory, "about.png");
			MeshEditor.AvaloniaUI.Rendering.ScreenshotFile.Save(path, frame, width, height);
			TestContext.Out.WriteLine($"{path} ({width} x {height})");
		}

		[AvaloniaTest]
		public void Capture_About_dialog()
		{
			var dialog = new AboutDialog();
			dialog.Show();
			Dispatcher.UIThread.RunJobs();
			using var frame = dialog.CaptureRenderedFrame();
			string path = Path.Combine(outputDirectory, "about-dialog.png");
			frame.Save(path);
			TestContext.Out.WriteLine(path);
		}

		/// <summary>
		/// GiD mesh (hexahedra) of the plate, deformed (exaggerated) and tilted towards the camera of the front view, and the von Mises stress
		/// of the Kirsch solution (an infinite plate with a hole) in its nodes.
		/// </summary>
		private static void writePlate(string meshFile, string resultFile)
		{
			var quads = new List<(double x, double y)[]>();
			// rays from the hole to the boundary of the square [-1, 1]^2, graded towards the hole
			var rays = new (double x, double y)[radialDivisions + 1, angleDivisions];
			for (int j = 0; j < angleDivisions; j++)
			{
				double angle = 2 * Math.PI * j / angleDivisions;
				double cos = Math.Cos(angle), sin = Math.Sin(angle);
				double toSquare = 1 / Math.Max(Math.Abs(cos), Math.Abs(sin));
				for (int i = 0; i <= radialDivisions; i++)
				{
					double t = Math.Pow((double)i / radialDivisions, 1.6);
					double r = holeRadius * (1 - t) + toSquare * t;
					rays[i, j] = (r * cos, r * sin);
				}
			}
			for (int i = 0; i < radialDivisions; i++)
				for (int j = 0; j < angleDivisions; j++)
				{
					int k = (j + 1) % angleDivisions;
					quads.Add(new[] { rays[i, j], rays[i + 1, j], rays[i + 1, k], rays[i, k] });
				}
			// blocks above and below the square, with the nodes of its top and bottom edges
			foreach (int sign in new[] { 1, -1 })
			{
				var xs = Enumerable.Range(0, angleDivisions).Select(j => rays[radialDivisions, j]).Where(p => Math.Abs(p.y - sign) < 1e-9).Select(p => p.x).OrderBy(x => x).ToArray();
				for (int i = 0; i < endDivisions; i++)
				{
					double y0 = sign * (1 + (halfHeight - 1) * i / endDivisions), y1 = sign * (1 + (halfHeight - 1) * (i + 1) / endDivisions);
					for (int j = 0; j + 1 < xs.Length; j++)
					{
						var quad = new[] { (xs[j], y0), (xs[j + 1], y0), (xs[j + 1], y1), (xs[j], y1) };
						quads.Add(sign > 0 ? quad : quad.Reverse().ToArray());
					}
				}
			}

			// nodes: the plane nodes (merged) in the bottom and top layer
			var planeNodes = new Dictionary<(long, long), int>();
			var planeCoordinates = new List<(double x, double y)>();
			int planeNode((double x, double y) p)
			{
				var key = ((long)Math.Round(p.x * 1e7), (long)Math.Round(p.y * 1e7));
				if (!planeNodes.TryGetValue(key, out int index))
				{
					index = planeCoordinates.Count;
					planeNodes.Add(key, index);
					planeCoordinates.Add(p);
				}
				return index;
			}
			var elements = quads.Select(q => q.Select(planeNode).ToArray()).ToList();
			int n = planeCoordinates.Count;

			var mesh = new StringBuilder();
			mesh.AppendLine("MESH \"plate\" dimension 3 ElemType Hexahedra Nnode 8");
			mesh.AppendLine("Coordinates");
			for (int layer = 0; layer < 2; layer++)
				for (int i = 0; i < n; i++)
				{
					var (x, y, z) = deformAndTilt(planeCoordinates[i], (layer - 0.5) * thickness);
					mesh.AppendLine(string.Create(CultureInfo.InvariantCulture, $"{layer * n + i + 1} {x:R} {y:R} {z:R}"));
				}
			mesh.AppendLine("end coordinates");
			mesh.AppendLine("Elements");
			for (int e = 0; e < elements.Count; e++)
			{
				var bottom = elements[e].Select(i => i + 1);
				var top = elements[e].Select(i => n + i + 1);
				mesh.AppendLine($"{e + 1} {string.Join(" ", bottom.Concat(top))} 1");
			}
			mesh.AppendLine("end elements");
			File.WriteAllText(meshFile, mesh.ToString());

			var results = new StringBuilder();
			results.AppendLine("GiD Post Results File 1.0");
			results.AppendLine();
			results.AppendLine("Result \"von Mises stress\" \"Load Analysis\" 1 Scalar OnNodes");
			results.AppendLine("Values");
			for (int layer = 0; layer < 2; layer++)
				for (int i = 0; i < n; i++)
					results.AppendLine(string.Create(CultureInfo.InvariantCulture, $"{layer * n + i + 1} {vonMises(planeCoordinates[i]):0.0000}"));
			results.AppendLine("End Values");
			File.WriteAllText(resultFile, results.ToString());
		}

		/// <summary>
		/// Von Mises stress of an infinite plate with a hole under the unit tension in the y direction and the compression
		/// in the x direction (the sum of two Kirsch solutions).
		/// </summary>
		private static double vonMises((double x, double y) p)
		{
			double r = Math.Max(Math.Sqrt(p.x * p.x + p.y * p.y), holeRadius);
			double angle = Math.Atan2(p.y, p.x);
			var (srr1, stt1, srt1) = kirsch(r, angle - Math.PI / 2, 1); // tension along y
			var (srr2, stt2, srt2) = kirsch(r, angle, -compression); // compression along x
			double srr = srr1 + srr2, stt = stt1 + stt2, srt = srt1 + srt2;
			return Math.Sqrt(srr * srr - srr * stt + stt * stt + 3 * srt * srt);
		}

		/// <summary>
		/// Polar stresses around a hole under the uniaxial stress <paramref name="load"/>; <paramref name="angle"/> from the load direction.
		/// </summary>
		private static (double srr, double stt, double srt) kirsch(double r, double angle, double load)
		{
			double q = holeRadius * holeRadius / (r * r);
			double cos = Math.Cos(2 * angle), sin = Math.Sin(2 * angle);
			return (load / 2 * (1 - q) + load / 2 * (1 - 4 * q + 3 * q * q) * cos,
				load / 2 * (1 + q) - load / 2 * (1 + 3 * q * q) * cos,
				-load / 2 * (1 + 2 * q - 3 * q * q) * sin);
		}

		private static (double x, double y, double z) deformAndTilt((double x, double y) p, double z)
		{
			// exaggerated: stretching along the load, the necking of the ligaments, a gentle bend out of the plane
			double r = Math.Max(Math.Sqrt(p.x * p.x + p.y * p.y), holeRadius);
			double q = holeRadius * holeRadius / (r * r);
			double x = p.x - (0.06 + 0.03 * compression) * p.x * (1 + q);
			double y = p.y + 0.10 * p.y + 0.08 * p.y * q;
			z += 0.22 * (p.y / halfHeight) * (p.y / halfHeight);
			// the top tilted away from the camera, turned a bit around the vertical axis
			double tilt = setting("ABOUT_PICTURE_TILT", -25) * Math.PI / 180, turn = setting("ABOUT_PICTURE_TURN", 12) * Math.PI / 180;
			(y, z) = (y * Math.Cos(tilt) - z * Math.Sin(tilt), y * Math.Sin(tilt) + z * Math.Cos(tilt));
			(x, z) = (x * Math.Cos(turn) + z * Math.Sin(turn), -x * Math.Sin(turn) + z * Math.Cos(turn));
			return (x, y, z);
		}

		/// <summary>
		/// A parameter of the picture: the environment variable, or the default.
		/// </summary>
		private static double setting(string variable, double defaultValue) =>
			Environment.GetEnvironmentVariable(variable) is string value && value != "" ? double.Parse(value, CultureInfo.InvariantCulture) : defaultValue;
	}
}
