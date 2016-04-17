using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.LayerManager.MeshFiltering
{
	class ComputationalGeometryMath
	{
		#region Constants

		public const double PI_DIVIDED_BY_180 = 0.017453292519943295769236907684886;
		public const double _180_DIVIDED_BY_PI = 57.295779513082320876798154814105;

		#endregion

		public static bool LinePlaneIntersection(Vector3 firstLinePoint, Vector3 secondLinePoint, ref Vector3 planeNormal, float planeOffset, out float parameter)
		{
			float nominator = planeOffset - Vector3.Dot(firstLinePoint, planeNormal);
			float denominator = Vector3.Dot(secondLinePoint - firstLinePoint, planeNormal);

			if (denominator == 0f) // usecka je rovnobezna s plochou
			{
				parameter = 0f;
				return false;
			}

			parameter = nominator / denominator;

			if (parameter < 0f || parameter > 1f) // prusecik je mimo usecku
				return false;

			return true;
		}

		public static float GetAngleInDegreesBetweenUnitVectors(Vector3 a, Vector3 b)
		{
			float dot = Vector3.Dot(a, b);
			if (dot > 1f)
				dot = 1f;
			else if (dot < -1f)
				dot = -1f;
			return (float)(Math.Acos(dot) * _180_DIVIDED_BY_PI);
		}

		public static float GetAngleInDegreesBetweenUnitVectors_0_360(Vector3 a, Vector3 b, Vector3 orientationVector)
		{
			float alpha = GetAngleInDegreesBetweenUnitVectors(a, b);
			Vector3 w;
			Vector3.Cross(ref a, ref b, out w);
			float dot;
			Vector3.Dot(ref w, ref orientationVector, out dot);
			if (dot >= 0f)
				return alpha;
			return 360f - alpha;
		}
	}
}
