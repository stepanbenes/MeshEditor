using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;

namespace MeshEditor.Data
{
	class PointUnderCursorContext
	{

		#region Fields

		private Vector3 pointUnderCursor, translatedPointUnderCursor;
		private float pixelDepthUnderCursor;
		private bool mouseDownBackgroundHit;

		int[] viewport;
		double[] modelview;
		double[] projection;

		#endregion

		#region Properties

		public Vector3 PointUnderCursor
		{
			get { return pointUnderCursor; }
			set
			{
				pointUnderCursor = value;
				translatedPointUnderCursor = pointUnderCursor;
			}
		}

		public bool MouseDownBackgroundHit
		{
			get { return mouseDownBackgroundHit; }
		}

		#endregion

		#region Public methods

		public void Compute(IScene scene, Point pointLocation, bool eliminateBackgroundHit)
		{
			Vector3 meshCenter = Vector3.Zero;
			float meshRadius = Scene.RADIUS_OF_NORMALIZED_MESH;
			if (scene.Mesh != null)
			{
				meshCenter = scene.Mesh.CenterOfRotation;
				meshRadius = scene.Mesh.Radius;
			}
			// -----------------------------------------------------------------------------------------------------------
			if (scene.Mesh != null)
			{
				scene.Mesh.DrawFacesToDepthBuffer(faceDrawer: scene.Mesh.DrawFacesOnly);
			}

			computePixelDepthAndPointUnderCursor(pointLocation.X, pointLocation.Y);
			
			this.mouseDownBackgroundHit = (this.pointUnderCursor - meshCenter).Length > meshRadius;

			if (eliminateBackgroundHit && this.mouseDownBackgroundHit) // pokud jsem klepnul mimo model (nekam do dalky)
			{
				Vector3 lineA = scene.Camera.Eye;
				Vector3 lineB = this.pointUnderCursor;
				Vector3 dir = Vector3.Normalize(lineB - lineA);

				Vector3 cameraDirection = scene.Camera.GetDirection();
				Vector3 projectionCenter = meshCenter + cameraDirection * meshRadius;

				// pokud je jsem moc blizko centra, tak posunout plochu
				float projection = Vector3.Dot(projectionCenter - scene.Camera.Eye, dir);
				Vector3 add = (projection < 0.1f) ? dir * (0.1f - projection) : Vector3.Zero;
				// --------------------------------------------------

				Vector3 planeA = projectionCenter + add;
				Vector3 planeB = projectionCenter + add + Vector3.Cross(cameraDirection, scene.Camera.Up);
				Vector3 planeC = projectionCenter + add + scene.Camera.Up;
				Vector3 intersection;
				Vector3 parametres;

				if (Utilities.Functions.LinePlaneIntersection(lineA, lineB, planeA, planeB, planeC, out intersection, out parametres))
				{
					this.pointUnderCursor = intersection;
					// jeste spocist novou hloubku pixelu pomoci Glu.Project()
					this.pixelDepthUnderCursor = Scene.ProjectWorldCoordToWindowCoords(this.pointUnderCursor).Z;
				}
			}

			translatedPointUnderCursor = pointUnderCursor;
		}

		public Vector3 GetTranslationVector(int windowX, int windowY)
		{
			Vector3 windowPos = new Vector3(windowX - viewport[0], viewport[3] - windowY - viewport[1], pixelDepthUnderCursor);
			Vector3 worldPos;
			Utilities.Functions.GluUnProject(windowPos, modelview, projection, viewport, out worldPos);
			Vector3 translation = translatedPointUnderCursor - worldPos;
			translatedPointUnderCursor -= translation;
			return translation;
		}

		#endregion

		#region Private methods

		private void computePixelDepthAndPointUnderCursor(int windowX, int windowY)
		{
			Scene.ExtractMatrices(out viewport, out modelview, out projection);

			this.pixelDepthUnderCursor = Scene.GetPixelDepth(windowX, windowY, viewport);
			Vector3 windowPos = new Vector3(windowX - viewport[0], viewport[3] - windowY - viewport[1], pixelDepthUnderCursor);
			Utilities.Functions.GluUnProject(windowPos, modelview, projection, viewport, out this.pointUnderCursor);
		}

		#endregion

	}
}
