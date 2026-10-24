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

		// a spur gear driving a pinion (involute teeth, module 0.1, the pressure angle 20°)
		private const double module = 0.1, pressureAngle = 20 * Math.PI / 180;
		private const int toothDivisions = 2, gapDivisions = 1, toothLayers = 4;
		private static double lineOfCentersAngle => setting("ABOUT_PICTURE_DIRECTION", 100) * Math.PI / 180; // from the gear to the pinion

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
			string meshFile = Path.Combine(directory, "gears.msh"), resultFile = Path.Combine(directory, "gears.res");
			writeGears(meshFile, resultFile);

			var analysisResults = new[] { new AnalysisResult { TimeStep = null, MeshRecordNames = new[] { meshFile }, DataRecordNames = new[] { resultFile } } };
			var solutionHub = SolutionHub.CreateNewLocal(Path.Combine(directory, "gears"), analysisResults, "gears");
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

		private sealed class Gear
		{
			public int Teeth;
			public double CenterX, CenterY;
			public double Rotation; // the angle of the centre of the first tooth
			public double[] RingRadii; // from the bore to the root circle
			public int[] RingDivisions;
			public (double radius, double halfThickness)[] Profile; // the half of the thickness: hub, web, rim

			public double PitchRadius => module * Teeth / 2;
			public double BaseRadius => PitchRadius * Math.Cos(pressureAngle);
			public double RootRadius => PitchRadius - 1.25 * module;
			public double TipRadius => PitchRadius + module;
			public double PitchAngle => 2 * Math.PI / Teeth;

			/// <summary>
			/// The half of the angular thickness of a tooth at the radius (the involute flanks, radial below the base circle).
			/// </summary>
			public double HalfToothAngle(double r) => Math.PI / (2 * Teeth) + involute(PitchRadius) - involute(Math.Max(r, BaseRadius));

			private double involute(double r)
			{
				double angle = Math.Acos(Math.Min(BaseRadius / r, 1));
				return Math.Tan(angle) - angle;
			}

			public double HalfThickness(double r)
			{
				if (r <= Profile[0].radius)
					return Profile[0].halfThickness;
				for (int i = 1; i < Profile.Length; i++)
					if (r <= Profile[i].radius)
					{
						double t = (r - Profile[i - 1].radius) / (Profile[i].radius - Profile[i - 1].radius);
						return Profile[i - 1].halfThickness * (1 - t) + Profile[i].halfThickness * t;
					}
				return Profile[^1].halfThickness;
			}

			public (double x, double y) Point(double r, double angle) => (CenterX + r * Math.Cos(angle), CenterY + r * Math.Sin(angle));
		}

		/// <summary>
		/// GiD mesh (hexahedra) of the gear and the pinion in mesh, tilted towards the camera of the front view, and the von Mises stress
		/// in their nodes: the torsion of the webs, the bending of the loaded teeth with the concentration at the roots, the contact at the pitch point.
		/// </summary>
		private static void writeGears(string meshFile, string resultFile)
		{
			double direction = lineOfCentersAngle;
			var gear = new Gear
			{
				Teeth = 22,
				RingRadii = new[] { 0.36, 0.5, 0.58, 0.78, 0.86, 0.975 },
				RingDivisions = new[] { 2, 1, 4, 1, 2 },
				Profile = new[] { (0.5, 0.22), (0.58, 0.09), (0.78, 0.09), (0.86, 0.18) },
			};
			var pinion = new Gear
			{
				Teeth = 14,
				RingRadii = new[] { 0.19, 0.3, 0.36, 0.575 },
				RingDivisions = new[] { 2, 1, 4 },
				Profile = new[] { (0.3, 0.26), (0.36, 0.18) },
			};
			double centerDistance = gear.PitchRadius + pinion.PitchRadius;
			pinion.CenterX = centerDistance * Math.Cos(direction);
			pinion.CenterY = centerDistance * Math.Sin(direction);
			// the flanks of a tooth of the gear and a tooth of the pinion touch at the pitch point
			gear.Rotation = direction + Math.PI / (2 * gear.Teeth);
			pinion.Rotation = direction + Math.PI + Math.PI / (2 * pinion.Teeth);
			var contact = gear.Point(gear.PitchRadius, direction);

			var planeNodes = new Dictionary<(int, long, long), int>();
			var planeCoordinates = new List<(double x, double y, double halfThickness, double stress)>();
			var elements = new List<int[]>();
			foreach (var (g, index) in new[] { (gear, 0), (pinion, 1) })
			{
				int planeNode(double r, double angle)
				{
					var (x, y) = g.Point(r, angle);
					var key = (index, (long)Math.Round(x * 1e7), (long)Math.Round(y * 1e7));
					if (!planeNodes.TryGetValue(key, out int node))
					{
						node = planeCoordinates.Count;
						planeNodes.Add(key, node);
						planeCoordinates.Add((x, y, g.HalfThickness(Math.Min(r, g.RootRadius)), vonMises(g, r, angle, contact)));
					}
					return node;
				}

				// the rings of the body, the angles of its rays: the roots of the teeth, the gaps between them
				var radii = new List<double> { g.RingRadii[0] };
				for (int i = 0; i < g.RingDivisions.Length; i++)
					for (int k = 1; k <= g.RingDivisions[i]; k++)
						radii.Add(g.RingRadii[i] + (g.RingRadii[i + 1] - g.RingRadii[i]) * k / g.RingDivisions[i]);
				double rootHalfAngle = g.HalfToothAngle(g.RootRadius);
				var angles = new List<double>();
				for (int t = 0; t < g.Teeth; t++)
				{
					double center = g.Rotation + t * g.PitchAngle;
					for (int i = 0; i < toothDivisions; i++)
						angles.Add(center + rootHalfAngle * (-1 + 2.0 * i / toothDivisions));
					for (int i = 0; i < gapDivisions; i++)
						angles.Add(center + rootHalfAngle + (g.PitchAngle - 2 * rootHalfAngle) * i / gapDivisions);
				}
				for (int i = 0; i + 1 < radii.Count; i++)
					for (int j = 0; j < angles.Count; j++)
					{
						int k = (j + 1) % angles.Count;
						elements.Add(new[] { planeNode(radii[i], angles[j]), planeNode(radii[i + 1], angles[j]), planeNode(radii[i + 1], angles[k]), planeNode(radii[i], angles[k]) });
					}
				// the teeth on the root circle
				for (int t = 0; t < g.Teeth; t++)
				{
					double center = g.Rotation + t * g.PitchAngle;
					(double r, double angle) toothPoint(int layer, int i)
					{
						double r = g.RootRadius + (g.TipRadius - g.RootRadius) * layer / toothLayers;
						return (r, center + g.HalfToothAngle(r) * (-1 + 2.0 * i / toothDivisions));
					}
					for (int layer = 0; layer < toothLayers; layer++)
						for (int i = 0; i < toothDivisions; i++)
						{
							var corners = new[] { toothPoint(layer, i), toothPoint(layer + 1, i), toothPoint(layer + 1, i + 1), toothPoint(layer, i + 1) };
							elements.Add(corners.Select(p => planeNode(p.r, p.angle)).ToArray());
						}
				}
			}
			int n = planeCoordinates.Count;

			var mesh = new StringBuilder();
			mesh.AppendLine("MESH \"gears\" dimension 3 ElemType Hexahedra Nnode 8");
			mesh.AppendLine("Coordinates");
			for (int layer = 0; layer < 2; layer++)
				for (int i = 0; i < n; i++)
				{
					var p = planeCoordinates[i];
					var (x, y, z) = tilt(p.x, p.y, (2 * layer - 1) * p.halfThickness);
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
					results.AppendLine(string.Create(CultureInfo.InvariantCulture, $"{layer * n + i + 1} {planeCoordinates[i].stress:0.0000}"));
			results.AppendLine("End Values");
			File.WriteAllText(resultFile, results.ToString());
		}

		/// <summary>
		/// An engineering estimate of the von Mises stress of the gear <paramref name="g"/> under the unit tooth force at the pitch point
		/// <paramref name="contact"/>: the torsion of the body, the bending and shear of the loaded teeth (Lewis) with the concentration
		/// at the root fillets, the Hertz contact.
		/// </summary>
		private static double vonMises(Gear g, double r, double angle, (double x, double y) contact)
		{
			double faceWidth = 2 * g.HalfThickness(g.RootRadius);
			double torsion = 16 * Math.Sqrt(3) * g.PitchRadius / (2 * Math.PI * r * r * 2 * g.HalfThickness(r));
			double stress = torsion;

			// the loaded tooth (the nearest to the pitch point) carries the most of the force, its neighbours a part (the contact ratio)
			var (cx, cy) = (contact.x - g.CenterX, contact.y - g.CenterY);
			double contactAngle = Math.Atan2(cy, cx);
			int loaded = (int)Math.Round(wrap(contactAngle - g.Rotation) / g.PitchAngle);
			double rootHalfAngle = g.HalfToothAngle(g.RootRadius), rootWidth = 2 * rootHalfAngle * g.RootRadius;
			for (int offset = -2; offset <= 2; offset++)
			{
				double force = offset == 0 ? 1 : Math.Abs(offset) == 1 ? 0.5 : 0.15;
				double center = g.Rotation + (loaded + offset) * g.PitchAngle;
				double local = wrap(angle - center);
				double rootBending = 6 * force * (g.PitchRadius - g.RootRadius) / (faceWidth * rootWidth * rootWidth);
				if (r >= g.RootRadius - 1e-9 && Math.Abs(local) <= g.HalfToothAngle(r) + 1e-9)
				{
					double halfAngle = g.HalfToothAngle(r), width = 2 * halfAngle * r, s = local / halfAngle;
					double bending = 6 * force * Math.Max(g.PitchRadius - r, 0) / (faceWidth * width * width) * s;
					double shear = 1.5 * force / (faceWidth * width) * (1 - s * s) * (r < g.PitchRadius ? 1 : 0.3);
					double concentration = 1 + 0.9 * s * s * Math.Exp(-Math.Pow((r - g.RootRadius) / (0.35 * module), 2));
					stress += Math.Sqrt(Math.Pow(concentration * bending, 2) + 3 * shear * shear);
				}
				else if (r < g.RootRadius)
				{
					// the fillets spread into the rim
					foreach (int side in new[] { -1, 1 })
					{
						var (fx, fy) = (g.RootRadius * Math.Cos(center + side * rootHalfAngle), g.RootRadius * Math.Sin(center + side * rootHalfAngle));
						var (x, y) = (r * Math.Cos(angle), r * Math.Sin(angle));
						double distance = Math.Sqrt((x - fx) * (x - fx) + (y - fy) * (y - fy));
						stress += 1.9 * rootBending * Math.Exp(-Math.Pow(distance / (1.3 * module), 2));
					}
				}
			}

			// Hertz contact at the pitch point
			var (px, py) = g.Point(r, angle);
			double contactDistance = Math.Sqrt((px - contact.x) * (px - contact.x) + (py - contact.y) * (py - contact.y));
			stress += 1.6 * 6 * (g.PitchRadius - g.RootRadius) / (faceWidth * rootWidth * rootWidth) * Math.Exp(-Math.Pow(contactDistance / (module), 2));
			return stress;
		}

		private static double wrap(double angle) => Math.IEEERemainder(angle, 2 * Math.PI);

		private static (double x, double y, double z) tilt(double x, double y, double z)
		{
			// the top tilted away from the camera, turned a bit around the vertical axis
			double tilt = setting("ABOUT_PICTURE_TILT", -20) * Math.PI / 180, turn = setting("ABOUT_PICTURE_TURN", 18) * Math.PI / 180;
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
