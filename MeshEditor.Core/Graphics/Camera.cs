using System;

using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Drawing;

using Utils = MeshEditor.Utilities.Functions;
using MeshEditor.Data;

namespace MeshEditor.Graphics
{
	/// <summary>
	/// preddefinovane pohledy kamery
	/// </summary>
	public enum CameraView
	{
		Front, Back, Left, Right, Top, Bottom, Iso
	}

	/// <summary>
	/// trida reprezentujici pozici a smer pohledu na scenu
	/// </summary>
	public class Camera
	{

		#region Fields, Constructor

		private Vector3 eye, center, up;
		
        public Camera()
        {
			Reset();
		}

		#endregion

		#region Properties

		public Vector3 Eye
        {
            get { return eye; }
			set { eye = value; }
        }

        public Vector3 Center
        {
            get { return center; }
			set { center = value; }
        }

        public Vector3 Up
        {
            get { return up; }
			set { up = value; }
		}

		#endregion

		#region Public methods

		/// <summary>
		/// Returns direction vector of camera (normalized).
		/// </summary>
		public Vector3 GetDirection()
		{
			return Vector3.Normalize(center - eye);
		}

		public void LookAt()
		{
			Utils.GluLookAt(ref eye, ref center, ref up);
		}

		public void Rotate(float xAngle, float yAngle)
		{
			// horizontal
			center = rotateVector(center - eye, xAngle, up) + eye;
			//vertical
			Vector3 axis = getVerticalRotationAxis();
			center = rotateVector(center - eye, yAngle, axis) + eye;
			up = Vector3.Normalize(rotateVector(up, yAngle, axis));
		}

		public void ZoomToFit()
		{
			Vector3 dir = GetDirection();
			center = Vector3.Zero; // posunu se abych koukal do stredu
			eye = dir * -Scene.DefaultCameraDistance; // vzdalim se na defaultni vzdalenost
			// up zustane stejny
		}

		public Camera Clone()
		{
			Camera newCam = new Camera();
			newCam.eye = this.eye;
			newCam.center = this.center;
			newCam.up = this.up;
			return newCam;
		}

		public void Reset()
		{
			//Front();
			Iso();
		}

		public void Move(Vector3 move)
		{
			eye += move;
			center += move;
		}

		public void Strafe(float xAngle, float yAngle, Vector3 centerOfOrbit)
		{
			strafeHorizontal(xAngle, centerOfOrbit);
			strafeVertical(yAngle, centerOfOrbit);
		}

		public void RotateZAxis(float zAngle)
		{
			up = rotateVector(up, zAngle, this.GetDirection());
		}

		#endregion

		#region Predefined views

		public void SetView(CameraView view)
		{
			switch (view)
			{
				case CameraView.Front:
					Front();
					break;
				case CameraView.Back:
					Back();
					break;
				case CameraView.Left:
					Left();
					break;
				case CameraView.Right:
					Right();
					break;
				case CameraView.Top:
					Top();
					break;
				case CameraView.Bottom:
					Bottom();
					break;
				case CameraView.Iso:
					Iso();
					break;
			}
		}

		public void Front()
		{
			eye = new Vector3(0f, -Scene.DefaultCameraDistance, 0f);
			center = new Vector3(0f, 0f, 0f);
			up = new Vector3(0f, 0f, 1f);
		}

		public void Back()
		{
			eye = new Vector3(0f, Scene.DefaultCameraDistance, 0f);
			center = new Vector3(0f, 0f, 0f);
			up = new Vector3(0f, 0f, 1f);
		}

		public void Left()
		{
			eye = new Vector3(-Scene.DefaultCameraDistance, 0f, 0f);
			center = new Vector3(0f, 0f, 0f);
			up = new Vector3(0f, 0f, 1f);
		}

		public void Right()
		{
			eye = new Vector3(Scene.DefaultCameraDistance, 0f, 0f);
			center = new Vector3(0f, 0f, 0f);
			up = new Vector3(0f, 0f, 1f);
		}

		public void Top()
		{
			eye = new Vector3(0f, 0f, Scene.DefaultCameraDistance);
			center = new Vector3(0f, 0f, 0f);
			up = new Vector3(0f, 1f, 0f);
		}

		public void Bottom()
		{
			eye = new Vector3(0f, 0f, -Scene.DefaultCameraDistance);
			center = new Vector3(0f, 0f, 0f);
			up = new Vector3(0f, 1f, 0f);
		}

		public void Iso()
		{
			//eye = Vector3.Normalize(new Vector3(1f, 1f, 1f)) * DefaultDistance;
			//center = new Vector3(0f, 0f, 0f);
			//up = Vector3.Normalize(new Vector3(-1f, 1f, -1f));
			eye = Vector3.Normalize(new Vector3(1f, -1f, 1f)) * Scene.DefaultCameraDistance;
			center = new Vector3(0f, 0f, 0f);
			up = Vector3.Normalize(new Vector3(-1f, 1f, 1f));
		}

		#endregion

		#region Private methods

		private static Vector3 rotateVector(Vector3 v, float angle, Vector3 axis)
		{
			float cosTheta = (float)Math.Cos(angle);
			float sinTheta = (float)Math.Sin(angle);

			Vector3 rotated = new Vector3();
			rotated.X = (cosTheta + (1 - cosTheta) * axis.X * axis.X) * v.X;
			rotated.X += ((1 - cosTheta) * axis.X * axis.Y - axis.Z * sinTheta) * v.Y;
			rotated.X += ((1 - cosTheta) * axis.X * axis.Z + axis.Y * sinTheta) * v.Z;

			rotated.Y = ((1 - cosTheta) * axis.X * axis.Y + axis.Z * sinTheta) * v.X;
			rotated.Y += (cosTheta + (1 - cosTheta) * axis.Y * axis.Y) * v.Y;
			rotated.Y += ((1 - cosTheta) * axis.Y * axis.Z - axis.X * sinTheta) * v.Z;

			rotated.Z = ((1 - cosTheta) * axis.X * axis.Z - axis.Y * sinTheta) * v.X;
			rotated.Z += ((1 - cosTheta) * axis.Y * axis.Z + axis.X * sinTheta) * v.Y;
			rotated.Z += (cosTheta + (1 - cosTheta) * axis.Z * axis.Z) * v.Z;

			return rotated;
		}

		private void strafeHorizontal(float angle, Vector3 centerOfOrbit)
		{
			//float angle = (float)(Math.PI / 720.0);
			center = rotateVector(center - centerOfOrbit, angle, up) + centerOfOrbit;
			eye = rotateVector(eye - centerOfOrbit, angle, up) + centerOfOrbit;
		}

		private void strafeVertical(float angle, Vector3 centerOfOrbit)
		{
			//float angle = (float)(Math.PI / 720.0);
			Vector3 axis = getVerticalRotationAxis();

			center = rotateVector(center - centerOfOrbit, angle, axis) + centerOfOrbit;
			eye = rotateVector(eye - centerOfOrbit, angle, axis) + centerOfOrbit;
			up = Vector3.Normalize(rotateVector(up, angle, axis));
		}

		private Vector3 getVerticalRotationAxis()
		{
			Vector3 dir = center - eye;
			Vector3 axis = Vector3.Cross(dir, up);
			return Vector3.Normalize(axis);
		}

		#endregion

	}
}
