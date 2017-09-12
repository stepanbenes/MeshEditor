using MeshEditor.LayerManager.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.LayerManager.MeshFiltering
{
	internal static class MeshFilterCreatorHelper
	{
		public static Vector3 GetPointCoordinates(GeometryDescription geometry, int pointIndex)
		{
			float x = geometry.PointCoordinates[pointIndex * geometry.NumberOfCoordinateComponents + 0];
			float y = (geometry.NumberOfCoordinateComponents > 1) ? geometry.PointCoordinates[pointIndex * geometry.NumberOfCoordinateComponents + 1] : 0f;
			float z = (geometry.NumberOfCoordinateComponents > 2) ? geometry.PointCoordinates[pointIndex * geometry.NumberOfCoordinateComponents + 2] : 0f;
			return new Vector3(x, y, z);
		}
	}
}
