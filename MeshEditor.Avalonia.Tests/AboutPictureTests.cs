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

		// a plate with a hole under tension in the y direction
		private const double holeRadius = 0.38, halfHeight = 1.7, thickness = 0.16;
		private const int angleDivisions = 64, radialDivisions = 14, endDivisions = 6; // angleDivisions is a multiple of 8: rays go to the corners of the square

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

			var window = new MainWindow(Array.Empty<string>()) { WindowState = WindowState.Normal, Width = 1000, Height = 780 };
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
			TestContext.Out.WriteLine($"viewport {viewport.PixelSize}, bounds {viewport.Bounds}, window {window.Bounds}");
			await waitUntil(() => Math.Abs(viewport.PixelSize.Width - viewport.PixelSize.Height * aspectRatio) < 2 || TestContext.Out.WriteLineAsync($"viewport {viewport.PixelSize}, bounds {viewport.Bounds}") == null, "viewport is resized", timeoutSeconds: 5);

			var sceneFacade = viewport.SceneFacade;
			var scene = sceneFacade.GetUnderlyingSceneObject();
			scene.RenderMode = RenderMode.FacesLines;
			scene.DrawAxes = false;
			sceneFacade.PerformAction(MeshEditor.CoreInterface.AvailableAction.CameraStandardView, CameraView.Front);
			sceneFacade.PerformAction(MeshEditor.CoreInterface.AvailableAction.ZoomToFit);
			scene.Camera.MoveForward(float.Parse(Environment.GetEnvironmentVariable("ABOUT_PICTURE_ZOOM") ?? "0", CultureInfo.InvariantCulture));

			byte[] frame = null;
			int width = 0, height = 0;
			for (int i = 0; i < 10; i++) // the color buffers are updated asynchronously
			{
				viewport.Refresh();
				await Task.Delay(100);
				Dispatcher.UIThread.RunJobs();
			}
			viewport.Refresh();
			frame = viewport.TakeScreenshot(System.Drawing.Rectangle.Empty, out width, out height);
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
		/// of the Kirsch solution (an infinite plate with a hole under the unit tension) in its nodes.
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

		private static double vonMises((double x, double y) p)
		{
			double r = Math.Max(Math.Sqrt(p.x * p.x + p.y * p.y), holeRadius);
			double angle = Math.Atan2(p.x, p.y); // from the load direction
			double q = holeRadius * holeRadius / (r * r);
			double srr = 0.5 * (1 - q) + 0.5 * (1 - 4 * q + 3 * q * q) * Math.Cos(2 * angle);
			double stt = 0.5 * (1 + q) - 0.5 * (1 + 3 * q * q) * Math.Cos(2 * angle);
			double srt = -0.5 * (1 + 2 * q - 3 * q * q) * Math.Sin(2 * angle);
			return Math.Sqrt(srr * srr - srr * stt + stt * stt + 3 * srt * srt);
		}

		private static (double x, double y, double z) deformAndTilt((double x, double y) p, double z)
		{
			// exaggerated: stretching along the load, the necking of the ligaments, a gentle bend out of the plane
			double r = Math.Max(Math.Sqrt(p.x * p.x + p.y * p.y), holeRadius);
			double q = holeRadius * holeRadius / (r * r);
			double x = p.x - 0.06 * p.x * (1 + q);
			double y = p.y + 0.10 * p.y + 0.08 * p.y * q;
			z += 0.22 * (p.y / halfHeight) * (p.y / halfHeight);
			// the top tilted away from the camera, turned a bit around the vertical axis
			double tilt = Angle("ABOUT_PICTURE_TILT", -50), turn = Angle("ABOUT_PICTURE_TURN", 18);
			(y, z) = (y * Math.Cos(tilt) - z * Math.Sin(tilt), y * Math.Sin(tilt) + z * Math.Cos(tilt));
			(x, z) = (x * Math.Cos(turn) + z * Math.Sin(turn), -x * Math.Sin(turn) + z * Math.Cos(turn));
			return (x, y, z);
		}

		private static double Angle(string variable, double defaultDegrees) =>
			double.Parse(Environment.GetEnvironmentVariable(variable) ?? defaultDegrees.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture) * Math.PI / 180;
	}
}
