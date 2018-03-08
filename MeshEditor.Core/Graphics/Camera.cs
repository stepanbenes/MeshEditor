using System;

using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Drawing;

using MeshEditor.Data;
using System.Diagnostics;
using static MeshEditor.Utilities.Functions;

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

		#region Fields, Constructors

		private Vector3 globalViewVector, globalUpVector;
		private float yaw, pitch;

		private Vector3 eye, center, up; // center and up vectors are computed from yaw and pitch angles in method updateViewVectors()

		public Camera()
		{
			setIsoView();
		}

		/// <summary>
		/// Copy constructor
		/// </summary>
		public Camera(Camera toCopy)
		{
			eye = toCopy.eye;
			center = toCopy.center;
			up = toCopy.up;

			globalViewVector = toCopy.globalViewVector;
			globalUpVector = toCopy.globalUpVector;

			yaw = toCopy.yaw;
			pitch = toCopy.pitch;
		}

		#endregion

		#region Properties

		public Vector3 Eye
		{
			get { return eye; }
		}

		public Vector3 Up
		{
			get { return up; }
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
			GluLookAt(ref eye, ref center, ref up);
		}

		/// <summary>
		/// Look around eye
		/// </summary>
		public void RotateView(float xAngle, float yAngle)
		{
			// rotate vertically
			pitch += yAngle;

			// rotate horizontally
			yaw += xAngle * Math.Sign(Vector3.Dot(up, globalUpVector));

			// update center and up
			updateViewVectors();
		}

		public void ZoomToFit(Vector3 centerOfRotation, float radius)
		{
			Vector3 dir = GetDirection();
			center = centerOfRotation; // posunu se abych koukal do stredu
			eye = centerOfRotation - dir * (radius * Scene.DefaultCameraDistance); // vzdalim se na defaultni vzdalenost
			// up zustane stejny
		}

		public void Move(Vector3 move)
		{
			eye += move;
			center += move;
		}

		public void MoveRight(float distance)
		{
			Vector3 move = distance * getVerticalRotationAxis();
			eye += move;
			center += move;
		}

		public void MoveLeft(float distance)
		{
			MoveRight(-distance);
		}

		public void MoveForward(float distance)
		{
			Vector3 move = distance * GetDirection();
			eye += move;
			center += move;
		}

		public void MoveBack(float distance)
		{
			MoveForward(-distance);
		}

		public void Orbit(Vector3 centerOfOrbit, float xAngle, float yAngle)
		{
			Vector3 verticalRotationAxis = getVerticalRotationAxis();
			float correctedXAngle = xAngle * Math.Sign(Vector3.Dot(up, globalUpVector));

			eye = RotateVector(eye - centerOfOrbit, correctedXAngle, globalUpVector) + centerOfOrbit;
			eye = RotateVector(eye - centerOfOrbit, yAngle, verticalRotationAxis) + centerOfOrbit;

			yaw += correctedXAngle;
			pitch += yAngle;

			updateViewVectors();
		}

		public void SetNewEyePosition(Vector3 newPosition)
		{
			Vector3 direction = GetDirection();
			eye = newPosition;
			center = eye + direction;
		}

		#endregion

		#region Predefined views

		public void SetView(CameraView view)
		{
			switch (view)
			{
				case CameraView.Front:
					setFrontView();
					break;
				case CameraView.Back:
					setBackView();
					break;
				case CameraView.Left:
					setLeftView();
					break;
				case CameraView.Right:
					setRightView();
					break;
				case CameraView.Top:
					setTopView();
					break;
				case CameraView.Bottom:
					setBottomView();
					break;
				case CameraView.Iso:
					setIsoView();
					break;
				default:
					throw new NotSupportedException();
			}
		}

		private void setFrontView()
		{
			eye = Vector3.UnitZ * Scene.DefaultCameraDistance;
			globalViewVector = -Vector3.UnitZ;
			globalUpVector = Vector3.UnitY;
			yaw = 0f;
			pitch = 0f;
			updateViewVectors();
		}

		private void setBackView()
		{
			eye = Vector3.UnitZ * -Scene.DefaultCameraDistance;
			globalViewVector = Vector3.UnitZ;
			globalUpVector = Vector3.UnitY;
			yaw = 0f;
			pitch = 0f;
			updateViewVectors();
		}

		private void setLeftView()
		{
			eye = Vector3.UnitX * -Scene.DefaultCameraDistance;
			globalViewVector = Vector3.UnitX;
			globalUpVector = Vector3.UnitY;
			yaw = 0f;
			pitch = 0f;
			updateViewVectors();
		}

		private void setRightView()
		{
			eye = Vector3.UnitX * Scene.DefaultCameraDistance;
			globalViewVector = -Vector3.UnitX;
			globalUpVector = Vector3.UnitY;
			yaw = 0f;
			pitch = 0f;
			updateViewVectors();
		}

		private void setTopView()
		{
			eye = Vector3.UnitY * Scene.DefaultCameraDistance;
			globalViewVector = -Vector3.UnitY;
			globalUpVector = -Vector3.UnitZ;
			yaw = 0f;
			pitch = 0f;
			updateViewVectors();
		}

		private void setBottomView()
		{
			eye = Vector3.UnitY * -Scene.DefaultCameraDistance;
			globalViewVector = Vector3.UnitY;
			globalUpVector = Vector3.UnitZ;
			yaw = 0f;
			pitch = 0f;
			updateViewVectors();
		}

		private void setIsoView()
		{
			eye = Vector3.Normalize(new Vector3(1f, 1f, 1f)) * Scene.DefaultCameraDistance;
			globalViewVector = -Vector3.UnitZ;
			globalUpVector = Vector3.UnitY;
			yaw = (float)(Math.PI / 4.0); // 45°
			pitch = (float)-Math.Asin(1.0 / Math.Sqrt(3.0)); // the slope of the diagonal of a cube with the side of unit length
			updateViewVectors();
		}

		#endregion

		#region Private methods

		private Vector3 getVerticalRotationAxis()
		{
			Vector3 direction = GetDirection();
			Vector3 verticalRotationAxis = Vector3.Cross(direction, up);
			return verticalRotationAxis;
		}

		private void updateViewVectors()
		{
			var rotatedView = RotateVector(globalViewVector, yaw, globalUpVector);
			var pitchAxis = Vector3.Cross(rotatedView, globalUpVector);
			rotatedView = RotateVector(rotatedView, pitch, pitchAxis);
			var rotatedUp = RotateVector(globalUpVector, pitch, pitchAxis);

			center = eye + rotatedView;
			up = rotatedUp;
		}

		#endregion

	}
}
