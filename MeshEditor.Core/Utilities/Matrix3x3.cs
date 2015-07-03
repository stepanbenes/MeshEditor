using System;
using System.Collections.Generic;
using System.Text;
using OpenTK;

namespace MeshEditor.Utilities
{
	/// <summary>
	/// trida reprezentujici ctvercovou matici cisel (3 sloupce, 3 radky)
	/// </summary>
	public class Matrix3x3
	{
		private float[,] data;

		public Matrix3x3()
		{
			data = new float[3, 3];
		}

		public float this[int r, int c]
		{
			get { return data[r, c]; }
			set { data[r, c] = value; }
		}

		public static Vector3 operator *(Matrix3x3 m, Vector3 v)
		{
			float x, y, z;
			x = m.data[0, 0] * v.X + m.data[0, 1] * v.Y + m.data[0, 2] * v.Z;
			y = m.data[1, 0] * v.X + m.data[1, 1] * v.Y + m.data[1, 2] * v.Z;
			z = m.data[2, 0] * v.X + m.data[2, 1] * v.Y + m.data[2, 2] * v.Z;
			return new Vector3(x, y, z);
		}

		public static Matrix3x3 operator *(Matrix3x3 m, float x)
		{
			Matrix3x3 result = new Matrix3x3();
			for (int i = 0; i < 3; i++)
				for (int j = 0; j < 3; j++)
					result[i, j] = m.data[i, j] * x;
			return result;
		}

		public bool TryInvert(out Matrix3x3 inverted)
		{
			float det = this.Determinant();
			if (det == 0f)
			{
				inverted = null;
				return false;
			}

			inverted = new Matrix3x3();

			inverted[0, 0] = data[1, 1] * data[2, 2] - data[2, 1] * data[1, 2];
			inverted[1, 0] = data[2, 0] * data[1, 2] - data[1, 0] * data[2, 2];
			inverted[2, 0] = data[1, 0] * data[2, 1] - data[2, 0] * data[1, 1];
			inverted[0, 1] = data[2, 1] * data[0, 2] - data[0, 1] * data[2, 2];
			inverted[1, 1] = data[0, 0] * data[2, 2] - data[0, 2] * data[2, 0];
			inverted[2, 1] = data[2, 0] * data[0, 1] - data[0, 0] * data[2, 1];
			inverted[0, 2] = data[0, 1] * data[1, 2] - data[1, 1] * data[0, 2];
			inverted[1, 2] = data[1, 0] * data[0, 2] - data[0, 0] * data[1, 2];
			inverted[2, 2] = data[0, 0] * data[1, 1] - data[1, 0] * data[0, 1];
						
			inverted *= (1f / det);
			return true;
		}

		public float Determinant()
		{
			float a = data[0, 0] * data[1, 1] * data[2, 2] + data[1, 0] * data[2, 1] * data[0, 2] + data[0, 1] * data[1, 2] * data[2, 0];
			float b = data[2, 0] * data[1, 1] * data[0, 2] + data[1, 0] * data[0, 1] * data[2, 2] + data[2, 1] * data[1, 2] * data[0, 0];
			return a - b;
		}

		public override string ToString()
		{
			StringBuilder text = new StringBuilder();
			text.AppendLine(data[0, 0] + "; " + data[0, 1] + "; " + data[0, 2]);
			text.AppendLine(data[1, 0] + "; " + data[1, 1] + "; " + data[1, 2]);
			text.AppendLine(data[2, 0] + "; " + data[2, 1] + "; " + data[2, 2]);
			return text.ToString();
		}
	}
}
