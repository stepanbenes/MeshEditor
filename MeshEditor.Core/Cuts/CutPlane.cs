using System;
using System.Collections.Generic;
using System.Text;
using MeshEditor.Data;
using OpenTK;
using MeshEditor.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Graphics;

using Utils = MeshEditor.Utilities.Functions;
using System.Drawing;


namespace MeshEditor.Cuts
{
	/// <summary>
	/// trida reprezentujici reznou rovinu
	/// </summary>
	public class CutPlane
	{

		#region Fields

		// =========================================================
		// static
		public static Color4 FrontColor = new Color4(0.8f, 0f, 0f, 0.4f);
		public static Color4 BackColor = new Color4(0f, 0.8f, 0f, 0.4f);
		public static Color4 SelectedColor = new Color4(1f, 1f, 0f, 1f);
		//public static Color4 FrontColorSelected = new Color4(0.6f, 0.8f, 0f, 0.4f);
		//public static Color4 BackColorSelected = new Color4(0.8f, 0.6f, 0f, 0.4f);
		public static float PlaneRadiusFactor = 1.5f;
		
		public static float TransparencyFactor
		{
			get { return FrontColor.A; }
			set
			{
				if (value < 0f || value > 1f)
					throw new ArgumentException("Value must be in range <0,1>.");
				FrontColor.A = value;
				BackColor.A = value;
			}
		}

		// =========================================================

		private Vector3 point1, point2, point3;

		private Vector3 pointOnPlane;
		private Vector3 normalVector;
		private float dParameter;
		private float epsilon;
		private float shownDParameter;

		private Vector3 vertex1, vertex2, vertex3, vertex4;

		private bool specifiedByThreePoints;
		private bool isSelected;

		#endregion

		#region Properties

		public Vector3 PointOnPlane => pointOnPlane;

		public Vector3 NormalVector => normalVector;

		public float Offset => -shownDParameter;

		public bool IsSelected
		{
			get { return isSelected; }
			set { isSelected = value; }
		}

		#endregion

		#region Constructors

		public CutPlane(Vector3 pointOnPlane, Vector3 pointOnNormal, Vector3 centerOfMesh, float meshRadius, Vector3 meshLowerBound, Vector3 meshUpperBound, float meshMinimalElementRadius, float meshResizeFactor, Vector3 meshPositionOffset)
		{
			this.isSelected = false;

			point1 = pointOnPlane;
			point2 = pointOnNormal;
			point3 = Vector3.Zero;
			specifiedByThreePoints = false;

			this.pointOnPlane = pointOnPlane;
			this.normalVector = -Vector3.Normalize(pointOnNormal - pointOnPlane);
			computeDParameter(meshMinimalElementRadius);
			computeShownDParameter(meshResizeFactor, meshPositionOffset);

			initPoints(centerOfMesh, meshRadius, PlaneRadiusFactor);
		}

		public CutPlane(Vector3 point1, Vector3 point2, Vector3 point3, Vector3 centerOfMesh, float meshRadius, Vector3 meshLowerBound, Vector3 meshUpperBound, float meshMinimalElementRadius, float meshResizeFactor, Vector3 meshPositionOffset)
		{
			this.isSelected = false;

			this.point1 = point1;
			this.point2 = point2;
			this.point3 = point3;
			specifiedByThreePoints = true;

			pointOnPlane = point1;
			normalVector = Utilities.Functions.GetNormalVectorOfTriangle(point1, point2, point3);
			computeDParameter(meshMinimalElementRadius);
			computeShownDParameter(meshResizeFactor, meshPositionOffset);

			initPoints(centerOfMesh, meshRadius, PlaneRadiusFactor);
		}

		#endregion
		
		#region Private methods

		private void initPoints(Vector3 centerOfMesh, float meshRadius, float planeRadiusFactor)
		{
			Vector3 centerOfQuad = Utilities.Functions.ProjectionOfPointToPlane(centerOfMesh, this.pointOnPlane, this.normalVector);
			Vector3 dir1;
			
			Vector3 projection = Utils.ProjectionOfPointToPlane(this.normalVector, Vector3.Zero, Vector3.UnitY);
			if (projection == Vector3.Zero || projection == this.normalVector)
			{
				if (Vector3.Cross(normalVector, Vector3.UnitY) == Vector3.Zero)
					projection = Vector3.UnitZ;
				else
					projection = Vector3.UnitY;
			}

			dir1 = Vector3.Cross(this.normalVector, projection);
			dir1.Normalize();
			dir1 = dir1 + Vector3.Cross(dir1, this.normalVector);
			dir1.Normalize();
			Vector3 dir2 = Vector3.Cross(dir1, this.normalVector);

			vertex1 = centerOfQuad + dir1 * (meshRadius * planeRadiusFactor);
			vertex2 = centerOfQuad + dir2 * (meshRadius * planeRadiusFactor);
			vertex3 = centerOfQuad + dir1 * (meshRadius * -planeRadiusFactor);
			vertex4 = centerOfQuad + dir2 * (meshRadius * -planeRadiusFactor);

		}

		private void drawOutline()
		{
			bool blendEnabled = GL.IsEnabled(EnableCap.Blend);
			GL.LineWidth(2f);
			if (Scene.LineSmooth)
			{
				GL.Enable(EnableCap.LineSmooth);
				if (!blendEnabled)
					GL.Enable(EnableCap.Blend);
			}
			GL.Color4(ref SelectedColor.R);
			GL.Begin(PrimitiveType.LineLoop);
			GL.Vertex3(ref vertex1.X);
			GL.Vertex3(ref vertex2.X);
			GL.Vertex3(ref vertex3.X);
			GL.Vertex3(ref vertex4.X);
			GL.End();
			if (Scene.LineSmooth)
			{
				GL.Disable(EnableCap.LineSmooth);
				if (!blendEnabled)
					GL.Disable(EnableCap.Blend);
			}
		}

		private void computeDParameter(float meshMinimalElementRadius)
		{
			this.epsilon = meshMinimalElementRadius * 0.01f; // zde se vypocte minimalni velikost prvku (pokud jiz nebyla spocitana drive) a vezme se setina - to je muj epsilon a ktery posunu rezne plochy dozadu, aby se neurizly prvky, ktere lezi svou plochou primo v rovine
			//this.epsilon = float.Epsilon;
			this.dParameter = -Vector3.Dot(this.normalVector, this.pointOnPlane);
		}

		private void computeShownDParameter(float meshResizeFactor, Vector3 meshPositionOffset)
		{
			Vector3 p = this.pointOnPlane / meshResizeFactor + meshPositionOffset;
			this.shownDParameter = -Vector3.Dot(this.normalVector, p);
		}
		
		#endregion

		#region Public methods

		public static void DrawDefinitionPoints(IEnumerable<Vector3> points)
		{
			bool blendEnabled = GL.IsEnabled(EnableCap.Blend);
			GL.PointSize(Scene.PointSize * 1.5f);

			GL.Enable(EnableCap.PointSmooth);
			if (!blendEnabled)
				GL.Enable(EnableCap.Blend);

			GL.Color4(ref SelectedColor.R);
			GL.Begin(PrimitiveType.Points);
			foreach (Vector3 point in points)
				GL.Vertex3(point);
			GL.End();

			if (!blendEnabled)
				GL.Disable(EnableCap.Blend);
			GL.Disable(EnableCap.PointSmooth);
		}

		public void Invert()
		{
			normalVector = -normalVector;

			//dParameter -= epsilon;
			dParameter = -dParameter;
			//dParameter += epsilon;

			shownDParameter = -shownDParameter;
		}

		public float GetDParameter(CutInfo cutInfo)
		{
			//if (cutInfo.Action == CutInfo.ActionType.SelectNodes) // u uzlu to musim udelat opacne
			//{
			//    if (cutInfo.HitDecision == CutInfo.ItemHitDecision.AllNodes)
			//        return dParameter + epsilon;
			//    else if (cutInfo.HitDecision == CutInfo.ItemHitDecision.SomeNodes)
			//        return dParameter - epsilon;
			//    else
			//        throw new NotSupportedException();
			//}
			//else
			//{
				if (cutInfo.HitDecision == CutInfo.ItemHitDecision.AllNodes)
					return dParameter - epsilon;
				else if (cutInfo.HitDecision == CutInfo.ItemHitDecision.SomeNodes)
					return dParameter + epsilon;
				else
					throw new NotSupportedException();
			//}
		}

		public void Draw(Vector3 cameraPosition)
		{
			// vyber barvy podle orientace plochy, nebo vybrani
			if (Vector3.Dot(cameraPosition - this.pointOnPlane, this.normalVector) >= 0)
				GL.Color4(ref FrontColor.R);
			else
				GL.Color4(ref BackColor.R);

			// nastaveni normaly plochy
			GL.Normal3(ref normalVector.X);

			GL.Begin(PrimitiveType.Quads);
			GL.Vertex3(ref vertex1.X);
			GL.Vertex3(ref vertex2.X);
			GL.Vertex3(ref vertex3.X);
			GL.Vertex3(ref vertex4.X);
			GL.End();

			if (isSelected)
			{
				if (specifiedByThreePoints)
					DrawDefinitionPoints(new Vector3[] { point1, point2, point3 });
				else
					DrawDefinitionPoints(new Vector3[] { point1, point2 });
			
				drawOutline();
			}
		}

		public override string ToString()
		{
			string sign = normalVector.X >= 0f ? string.Empty : "-";
			StringBuilder text = new StringBuilder();
			// n.X * x + n.Y * y + n.Z * z + d < 0

			if (normalVector.X != 0f)
			{
				text.Append(sign);
				if (Math.Abs(normalVector.X) != 1f)
					text.Append(Math.Abs(normalVector.X));
				text.Append("x");
			}
			
			if (normalVector.X == 0f)
				sign = normalVector.Y >= 0f ? string.Empty : "-";
			else
				sign = normalVector.Y >= 0f ? " + " : " - ";

			if (normalVector.Y != 0f)
			{
				text.Append(sign);
				if (Math.Abs(normalVector.Y) != 1f)
					text.Append(Math.Abs(normalVector.Y));
				text.Append("y");
			}

			if (normalVector.X == 0f && normalVector.Y == 0f)
				sign = normalVector.Z >= 0f ? string.Empty : "-";
			else
				sign = normalVector.Z >= 0f ? " + " : " - ";

			if (normalVector.Z != 0f)
			{
				text.Append(sign);
				if (Math.Abs(normalVector.Z) != 1f)
					text.Append(Math.Abs(normalVector.Z));
				text.Append("z");
			}

			if (normalVector.X == 0f && normalVector.Y == 0f && normalVector.Z == 0f)
				sign = shownDParameter >= 0f ? string.Empty : "-";
			else
				sign = shownDParameter >= 0f ? " + " : " - ";

			if (shownDParameter != 0f)
			{
				text.Append(sign);
				text.Append(Math.Abs(shownDParameter));
			}

			text.Append(" < 0");

			return text.ToString();
		}

		#endregion

	}
}
