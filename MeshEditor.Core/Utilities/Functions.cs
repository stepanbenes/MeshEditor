using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Drawing;
using System.Diagnostics;

using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;


namespace MeshEditor.Utilities
{
	/// <summary>
	/// Contains some static helper functions
	/// </summary>
	public static class Functions
	{

		#region Constants

		public const double PI_DIVIDED_BY_180 = 0.017453292519943295769236907684886;
		public const double _180_DIVIDED_BY_PI = 57.295779513082320876798154814105;

		#endregion

		#region Static Fields

		private static bool openglVersionIsComputed = false;
		private static int majorOpenGLVersion;
		private static int minorOpenGLVersion;

		public static CultureInfo EnglishCulture;

		static Functions()
		{
			EnglishCulture = new CultureInfo("en-US");
		}

		#endregion

		#region Helper functions

		/// <summary>
		/// Find out whether array contains item.
		/// </summary>
		/// <param name="array">Array of items</param>
		/// <param name="item">Item to search for</param>
		/// <returns>True if array contains item, otherwise false.</returns>
		public static bool ArrayContains<T>(T[] array, T item)
		{
			if (array == null)
				return false;
			foreach (T i in array)
			{
				if (i.Equals(item)) // using object.Equals() method
					return true;
			}
			return false;
		}

		/// <summary>
		/// Return new array that is sub-array of array.
		/// </summary>
		/// <param name="array">Original array</param>
		/// <param name="index">Start index of segment</param>
		/// <param name="length">Length of segment to copy</param>
		/// <returns></returns>
		public static T[] GetSliceOfArray<T>(T[] array, int index, int length)
		{
			T[] result = new T[length];
			Array.Copy(array, index, result, 0, length);
			return result;
		}

		/// <summary>
		/// Swaps values of two arguments.
		/// </summary>
		/// <param name="a">first argument</param>
		/// <param name="b">second argument</param>
		public static void Swap<T>(ref T a, ref T b)
		{
			T temp = a;
			a = b;
			b = temp;
		}

		/// <summary>
		/// Creates and returns shallow copy of specified array
		/// </summary>
		/// <typeparam name="T">type of array members</typeparam>
		/// <param name="source">array to copy</param>
		/// <returns>shallow copy of specified array</returns>
		public static T[] CloneArray<T>(T[] source)
		{
			if (source == null)
				return null;
			T[] copy = new T[source.Length];
			Array.Copy(source, copy, source.Length);
			return copy;
		}

		/// <summary>
		/// Creates shallow copy of specified array with length increased by one.
		/// It then put specified item to the end of the array.
		/// </summary>
		/// <typeparam name="T">type of array element</typeparam>
		/// <param name="array">array to enlarge</param>
		/// <param name="item">item to bu added</param>
		/// <returns>new array with old items copied plus the new one</returns>
		public static void AddItemToArray<T>(ref T[] array, T item)
		{
			int oldLength = array.Length;
			Array.Resize<T>(ref array, oldLength + 1);
			array[oldLength] = item;
		}

		/// <summary>
		/// Returns true if array is ascendingly ordered.
		/// </summary>
		public static bool CheckIfArrayIsSorted<T>(T[] array) where T : IComparable<T>
		{
			if (array == null)
				return true;
			for (int i = 1; i < array.Length; i++)
			{
				if (array[i - 1].CompareTo(array[i]) > 0)
					return false;
			}
			return true;
		}

		/// <summary>
		/// Returns true if values in array create strictly increasing ordered set.
		/// </summary>
		public static bool CheckIfArrayIsStrictlyIncreasing<T>(T[] array) where T : IComparable<T>
		{
			if (array == null)
				return true;
			for (int i = 1; i < array.Length; i++)
			{
				if (array[i - 1].CompareTo(array[i]) >= 0)
					return false;
			}
			return true;
		}

		/// <summary>
		/// Get vector of values which are absolute values of component of original vector.
		/// </summary>
		public static Vector3 Abs(Vector3 vector)
		{
			return new Vector3(Math.Abs(vector.X), Math.Abs(vector.Y), Math.Abs(vector.Z));
		}
		
		/// <summary>
		/// Sort already sorted list of items. Mantain previous order.
		/// </summary>
		/// <param name="comparison">comparing function</param>
		/// <param name="list">list of items to sort</param>
		public static void ThenSortBy<T>(Comparison<T> comparison, List<T> list)
		{
			for (int i = 1; i < list.Count; i++)
			{
				T insItem = list[i]; //vkládaný prvek
				int j = i;
				while (j > 0 && comparison(list[j - 1], insItem) > 0)
				{
					list[j] = list[j - 1]; //posouvám prvky,
					//dělám prostor pro vkládaný prvek
					j--;
				}
				//vložím vkládaný prvek:
				list[j] = insItem;
			}
		}

		public static T DeepCopyOf<T>(T ofWhat)
		{
			BinaryFormatter bf = new BinaryFormatter();
			MemoryStream ms = new MemoryStream();
			T result;
			try
			{
				bf.Serialize(ms, ofWhat);
				ms.Position = 0;
				result = (T)bf.Deserialize(ms);
			}
			finally
			{
				ms.Close();
			}
			return result;
		}

		public static bool XOR(bool x, bool y)
		{
			return (x && !y) || (!x && y);
		}

		public static float SQR(float n)
		{
			return n * n;
		}

		public static double Deg2Rad(double angleInDeg)
		{
			return angleInDeg * PI_DIVIDED_BY_180;
		}

		public static double Rad2Deg(double angleInRad)
		{
			return angleInRad * _180_DIVIDED_BY_PI;
		}

		public static void MMM(double[] a, double[] b, out Matrix4d result)
		{
		    //result = new Matrix4d();
			result.Row0 = new Vector4d(a[0] * b[0] + a[1] * b[4] + a[2] * b[8] + a[3] * b[12], a[0] * b[1] + a[1] * b[5] + a[2] * b[9] + a[3] * b[13], a[0] * b[2] + a[1] * b[6] + a[2] * b[10] + a[3] * b[14], a[0] * b[3] + a[1] * b[7] + a[2] * b[11] + a[3] * b[15]);
			result.Row1 = new Vector4d(a[4] * b[0] + a[5] * b[4] + a[6] * b[8] + a[7] * b[12], a[4] * b[1] + a[5] * b[5] + a[6] * b[9] + a[7] * b[13], a[4] * b[2] + a[5] * b[6] + a[6] * b[10] + a[7] * b[14], a[4] * b[3] + a[5] * b[7] + a[6] * b[11] + a[7] * b[15]);
			result.Row2 = new Vector4d(a[8] * b[0] + a[9] * b[4] + a[10] * b[8] + a[11] * b[12], a[8] * b[1] + a[9] * b[5] + a[10] * b[9] + a[11] * b[13], a[8] * b[2] + a[9] * b[6] + a[10] * b[10] + a[11] * b[14], a[8] * b[3] + a[9] * b[7] + a[10] * b[11] + a[11] * b[15]);
			result.Row3 = new Vector4d(a[12] * b[0] + a[13] * b[4] + a[14] * b[8] + a[15] * b[12], a[12] * b[1] + a[13] * b[5] + a[14] * b[9] + a[15] * b[13], a[12] * b[2] + a[13] * b[6] + a[14] * b[10] + a[15] * b[14], a[12] * b[3] + a[13] * b[7] + a[14] * b[11] + a[15] * b[15]);
		}

		public static void MVM(ref Matrix4d m, ref Vector3 v, out Vector3d result)
		{
			double x = m.M11 * v.X + m.M12 * v.Y + m.M13 * v.Z + m.M14;
			double y = m.M21 * v.X + m.M22 * v.Y + m.M23 * v.Z + m.M24;
			double z = m.M31 * v.X + m.M32 * v.Y + m.M33 * v.Z + m.M34;
			result = new Vector3d(x, y, z);
		}

		public static void MVM(double[] matrix4, ref Vector4d input, out Vector4d output)
		{
			output.X = input.X * matrix4[0] + input.Y * matrix4[4] + input.Z * matrix4[8] + input.W * matrix4[12];
			output.Y = input.X * matrix4[1] + input.Y * matrix4[5] + input.Z * matrix4[9] + input.W * matrix4[13];
			output.Z = input.X * matrix4[2] + input.Y * matrix4[6] + input.Z * matrix4[10] + input.W * matrix4[14];
			output.W = input.X * matrix4[3] + input.Y * matrix4[7] + input.Z * matrix4[11] + input.W * matrix4[15];
		}

		public static void MMM(double[] a, double[] b, double[] r)
		{
			int i, j;

			for (i = 0; i < 4; i++)
			{
				for (j = 0; j < 4; j++)
				{
					r[i * 4 + j] =
					a[i * 4] * b[0 + j] +
					a[i * 4 + 1] * b[4 + j] +
					a[i * 4 + 2] * b[8 + j] +
					a[i * 4 + 3] * b[12 + j];
				}
			}
		}

		public static bool InvertMatrix(double[] m, double[] invOut)
		{
			double[] inv = new double[16];

			inv[0] = m[5] * m[10] * m[15] - m[5] * m[11] * m[14] - m[9] * m[6] * m[15]
					 + m[9] * m[7] * m[14] + m[13] * m[6] * m[11] - m[13] * m[7] * m[10];
			inv[4] = -m[4] * m[10] * m[15] + m[4] * m[11] * m[14] + m[8] * m[6] * m[15]
					 - m[8] * m[7] * m[14] - m[12] * m[6] * m[11] + m[12] * m[7] * m[10];
			inv[8] = m[4] * m[9] * m[15] - m[4] * m[11] * m[13] - m[8] * m[5] * m[15]
					 + m[8] * m[7] * m[13] + m[12] * m[5] * m[11] - m[12] * m[7] * m[9];
			inv[12] = -m[4] * m[9] * m[14] + m[4] * m[10] * m[13] + m[8] * m[5] * m[14]
					 - m[8] * m[6] * m[13] - m[12] * m[5] * m[10] + m[12] * m[6] * m[9];
			inv[1] = -m[1] * m[10] * m[15] + m[1] * m[11] * m[14] + m[9] * m[2] * m[15]
					 - m[9] * m[3] * m[14] - m[13] * m[2] * m[11] + m[13] * m[3] * m[10];
			inv[5] = m[0] * m[10] * m[15] - m[0] * m[11] * m[14] - m[8] * m[2] * m[15]
					 + m[8] * m[3] * m[14] + m[12] * m[2] * m[11] - m[12] * m[3] * m[10];
			inv[9] = -m[0] * m[9] * m[15] + m[0] * m[11] * m[13] + m[8] * m[1] * m[15]
					 - m[8] * m[3] * m[13] - m[12] * m[1] * m[11] + m[12] * m[3] * m[9];
			inv[13] = m[0] * m[9] * m[14] - m[0] * m[10] * m[13] - m[8] * m[1] * m[14]
					 + m[8] * m[2] * m[13] + m[12] * m[1] * m[10] - m[12] * m[2] * m[9];
			inv[2] = m[1] * m[6] * m[15] - m[1] * m[7] * m[14] - m[5] * m[2] * m[15]
					 + m[5] * m[3] * m[14] + m[13] * m[2] * m[7] - m[13] * m[3] * m[6];
			inv[6] = -m[0] * m[6] * m[15] + m[0] * m[7] * m[14] + m[4] * m[2] * m[15]
					 - m[4] * m[3] * m[14] - m[12] * m[2] * m[7] + m[12] * m[3] * m[6];
			inv[10] = m[0] * m[5] * m[15] - m[0] * m[7] * m[13] - m[4] * m[1] * m[15]
					 + m[4] * m[3] * m[13] + m[12] * m[1] * m[7] - m[12] * m[3] * m[5];
			inv[14] = -m[0] * m[5] * m[14] + m[0] * m[6] * m[13] + m[4] * m[1] * m[14]
					 - m[4] * m[2] * m[13] - m[12] * m[1] * m[6] + m[12] * m[2] * m[5];
			inv[3] = -m[1] * m[6] * m[11] + m[1] * m[7] * m[10] + m[5] * m[2] * m[11]
					 - m[5] * m[3] * m[10] - m[9] * m[2] * m[7] + m[9] * m[3] * m[6];
			inv[7] = m[0] * m[6] * m[11] - m[0] * m[7] * m[10] - m[4] * m[2] * m[11]
					 + m[4] * m[3] * m[10] + m[8] * m[2] * m[7] - m[8] * m[3] * m[6];
			inv[11] = -m[0] * m[5] * m[11] + m[0] * m[7] * m[9] + m[4] * m[1] * m[11]
					 - m[4] * m[3] * m[9] - m[8] * m[1] * m[7] + m[8] * m[3] * m[5];
			inv[15] = m[0] * m[5] * m[10] - m[0] * m[6] * m[9] - m[4] * m[1] * m[10]
					 + m[4] * m[2] * m[9] + m[8] * m[1] * m[6] - m[8] * m[2] * m[5];

			double det = m[0] * inv[0] + m[1] * inv[4] + m[2] * inv[8] + m[3] * inv[12];
			if (det == 0)
				return false;

			det = 1.0 / det;

			for (int i = 0; i < 16; i++)
				invOut[i] = inv[i] * det;

			return true;
		}

		public static string GetVector3StringRepresentation(ref Vector3 position)
		{
			StringBuilder text = new StringBuilder();
			text.Append("[");
			text.Append(position.X.ToString(EnglishCulture));
			text.Append("; ");
			text.Append(position.Y.ToString(EnglishCulture));
			text.Append("; ");
			text.Append(position.Z.ToString(EnglishCulture));
			text.Append("]");
			return text.ToString();
		}

		public static byte[] ConvertStructureToByteArray<T>(T str) where T : struct
		{
			int len = Marshal.SizeOf(str);
			byte[] arr = new byte[len];
			IntPtr ptr = Marshal.AllocHGlobal(len);
			Marshal.StructureToPtr(str, ptr, true);
			Marshal.Copy(ptr, arr, 0, len);
			Marshal.FreeHGlobal(ptr);
			return arr;
		}

		public static void ConvertByteArrayToStructure<T>(byte[] byteArray, ref T str) where T : struct
		{
			int len = Marshal.SizeOf(str);
			IntPtr i = Marshal.AllocHGlobal(len);
			Marshal.Copy(byteArray, 0, i, len);
			str = (T)Marshal.PtrToStructure(i, str.GetType());
			Marshal.FreeHGlobal(i);
		}

		public static bool EnumTryParse<TEnum>(string strEnumValue, out TEnum result) where TEnum: struct
		{
			Type type = typeof(TEnum);
			if (!Enum.IsDefined(type, strEnumValue))
			{
				result = default(TEnum);
				return false;
			}

			result = (TEnum)Enum.Parse(type, strEnumValue);
			return true;
		}

		public static bool EnumTryParseIgnoreCase<TEnum>(string strType, out TEnum result, ref string[] namesCache) where TEnum : struct
		{
			string strTypeFixed = strType.Replace(' ', '_');
			if (Enum.IsDefined(typeof(TEnum), strTypeFixed))
			{
				result = (TEnum)Enum.Parse(typeof(TEnum), strTypeFixed, true);
				return true;
			}
			else
			{
				if (namesCache == null)
				{
					namesCache = Enum.GetNames(typeof(TEnum));
				}

				foreach (string value in namesCache)
				{
					if (value.Equals(strTypeFixed, StringComparison.OrdinalIgnoreCase))
					{
						result = (TEnum)Enum.Parse(typeof(TEnum), value);
						return true;
					}
				}
				result = default(TEnum);
				return false;
			}
		}

		public static string BuildErrorMessage(Exception ex)
		{
			Debug.Assert(ex != null);

			var dataException = ex as IO.FileParserException;
			if (dataException != null)
			{
				StringBuilder message = new StringBuilder();
				message.AppendLine(dataException.Message);
				if (!string.IsNullOrEmpty(dataException.FileName))
				{
					message.AppendLine();
					message.Append(string.Format("File name: \"{0}\"", System.IO.Path.GetFileName(dataException.FileName)));
				}
				if (dataException.LineNumber > 0)
				{
					message.AppendLine();
					message.Append(string.Format("Line number: {0}", dataException.LineNumber));
				}
				if (dataException.LinePosition > 0)
				{
					message.AppendLine();
					message.Append(string.Format("Line position: {0}", dataException.LinePosition));
				}
				return message.ToString();
			}
			else
			{
				return ex.Message;
			}
		}

		#endregion

		#region Color manipulation

		/// <summary>
		/// Converts a System.Drawing.Color to a System.Int32.
		/// </summary>
		/// <param name="c">The System.Drawing.Color to convert.</param>
		/// <returns>A System.Int32 containing the R, G, B, A values of the
		/// given System.Drawing.Color in the Rbga32 format.</returns>
		public static int ColorToRgba32(Color c)
		{
			return ColorToRgba32(c.R, c.G, c.B, c.A);
			//return ((int)c.A << 24) | ((int)c.B << 16) | ((int)c.G << 8) | (int)c.R;
			//return c.ToArgb();
		}

		public static int ColorToRgba32(byte red, byte green, byte blue, byte alpha)
		{
			return ((int)alpha << 24) | ((int)blue << 16) | ((int)green << 8) | (int)red;
		}

		public static int ColorToRgba32(Color c, byte alpha)
		{
			return ColorToRgba32(c.R, c.G, c.B, alpha);
		}

		/// <summary>
		/// Get byte components of color in RGBA 32bit format
		/// </summary>
		/// <param name="color">color in RGBA 32bit format</param>
		/// <param name="red">output parameter - red color component</param>
		/// <param name="green">output parameter - green color component</param>
		/// <param name="blue">output parameter - blue color component</param>
		/// <param name="alpha">output parameter - alpha color component</param>
		public static void GetColorComponents(int color, out byte red, out byte green, out byte blue, out byte alpha)
		{
			int mask = 0x000000FF;
			red = (byte)(color & mask);
			green = (byte)((color >> 8) & mask);
			blue = (byte)((color >> 16) & mask);
			alpha = (byte)((color >> 24) & mask);
		}

		/// <summary>
		/// Get float components of color in RGBA 32bit format in range <0.0f, 1.0f>
		/// </summary>
		/// <param name="color">color in RGBA 32bit format</param>
		/// <param name="red">output parameter - red color component</param>
		/// <param name="green">output parameter - green color component</param>
		/// <param name="blue">output parameter - blue color component</param>
		public static void GetColorComponents(int color, out float red, out float green, out float blue)
		{
			int mask = 0x000000FF;
			red = (float)(color & mask) / 255f;
			green = (float)((color >> 8) & mask) / 255f;
			blue = (float)((color >> 16) & mask) / 255f;
			//alpha = (float)((color >> 24) & mask) / 255f;
		}

		/// <summary>
		/// Returns inverted color. Assumes that alpha is the leftmost byte.
		/// </summary>
		/// <param name="colorRGBA32">color to invert in RGBA 32 bit format</param>
		/// <returns>Inverted color</returns>
		public static int InvertColor(int colorRGBA32)
		{
			return 0x00FFFFFF ^ colorRGBA32;
		}

		/// <summary>
		/// Returns inverted color (except alpha component). Assumes that alpha is the leftmost byte.
		/// </summary>
		/// <param name="colorRGBA32">color to invert in RGBA 32 bit format</param>
		/// <returns>Inverted color</returns>
		public static int InvertColorKeepAlpha(int colorRGBA32)
		{
			int alpha = unchecked((int)0xFF000000) & colorRGBA32;
			int result = 0x00FFFFFF ^ colorRGBA32;
			result &= 0x00FFFFFF;
			result |= alpha;
			return result;
		}

		public static int SetAlphaOfColor(int colorRGBA32, byte alpha)
		{
			int result = colorRGBA32 & 0x00FFFFFF;
			result |= alpha << 24;
			return result;
		}

		public static Color HslToColor(float hue, float saturation, float luminance)
		{
			float q = (luminance < 0.5f) ? (luminance * (1.0f + saturation)) : (luminance + saturation - (luminance * saturation));
			float p = (2.0f * luminance) - q;

			float[] T = new float[3];
			T[0] = hue + (1.0f / 3.0f);	// Tr
			T[1] = hue;				// Tb
			T[2] = hue - (1.0f / 3.0f);	// Tg

			for (int i = 0; i < 3; i++)
			{
				if (T[i] < 0) T[i] += 1.0f;
				if (T[i] > 1) T[i] -= 1.0f;

				if ((T[i] * 6) < 1)
				{
					T[i] = p + ((q - p) * 6.0f * T[i]);
				}
				else if ((T[i] * 2.0) < 1) //(1.0/6.0)<=T[i] && T[i]<0.5
				{
					T[i] = q;
				}
				else if ((T[i] * 3.0) < 2) // 0.5<=T[i] && T[i]<(2.0/3.0)
				{
					T[i] = p + (q - p) * ((2.0f / 3.0f) - T[i]) * 6.0f;
				}
				else T[i] = p;
			}

			return Color.FromArgb((int)(T[0] * 255f), (int)(T[1] * 255f), (int)(T[2] * 255f));
		}

		public static int HslToRgba32(float hue, float saturation, float luminance)
		{
			return ColorToRgba32(HslToColor(hue, saturation, luminance));
		}

		//public static float GetLuminanceOfColor(Color color)
		//{
		//	// normalizes red-green-blue values
		//	float nRed = (float)color.R / 255.0f;
		//	float nGreen = (float)color.G / 255.0f;
		//	float nBlue = (float)color.B / 255.0f;

		//	float max = Math.Max(nRed, Math.Max(nGreen, nBlue));
		//	float min = Math.Min(nRed, Math.Min(nGreen, nBlue));

		//	// luminance
		//	return (max + min) / 2.0f;
		//}

		public static Color GetContrastColor(Color color)
		{
			// Counting the perceptive luminance - human eye favors green color... 
			double a = 1 - (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255;

			int d = 0;

			if (a < 0.5)
				d = 0; // bright colors - black contrast color
			else
				d = 255; // dark colors - white contrast color

			return Color.FromArgb(d, d, d);
		}

		public static int InterpolateTwoColors(int colorMin, int colorMax, double position)
		{
			Debug.Assert(position >= 0.0 && position <= 1.0);

			//return (int)((controlPoints[index].Color - controlPoints[index - 1].Color) * position + controlPoints[index - 1].Color);

			//return ((int)alpha << 24) | ((int)blue << 16) | ((int)green << 8) | (int)red;

			byte rMin, gMin, bMin, aMin, rMax, gMax, bMax, aMax;
			GetColorComponents(colorMin, out rMin, out gMin, out bMin, out aMin);
			GetColorComponents(colorMax, out rMax, out gMax, out bMax, out aMax);

			byte r = (byte)(position * (rMax - rMin) + rMin);
			byte g = (byte)(position * (gMax - gMin) + gMin);
			byte b = (byte)(position * (bMax - bMin) + bMin);
			byte a = (byte)(position * (aMax - aMin) + aMin);

			return ColorToRgba32(r, g, b, a);
		}

		#endregion

		#region Geometric features

		public static Vector3 GetCenterOfLineSegment(Vector3 a, Vector3 b)
		{
			return a + (b - a) * 0.5f;
		}

		public static Vector3 GetCenterOfLineSegment(ref Vector3 a, ref Vector3 b)
		{
			return a + (b - a) * 0.5f;
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
		
		public static float GetAngleInDegreesBetweenUnitVectors(Vector2 a, Vector2 b)
		{
			float dot = Vector2.Dot(a, b);
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

		public static float GetAngle0_90(Vector3 a, Vector3 b)
		{
			float angle = GetAngleInDegreesBetweenUnitVectors(a, b);
			return (angle >= 90f) ? 180f - angle : angle;
		}

		public static Vector3 GetNormalVectorOfTriangle(Vector3 a, Vector3 b, Vector3 c)
		{
			Vector3 result = -Vector3.Cross(b - a, c - a); /**/ // zvolil jsem obracenou notaci, protoze to je pak prirozenejsi
			result.Normalize();
			return result;
		}

		public static float GetDistanceFromLine(Vector3 a, Vector3 b, Vector3 hitPoint)
		{
			Vector3 u = b - a;
			float x = Vector3.Cross(u, a - hitPoint).Length;
			float y = u.Length;
			return x / y;
		}

		public static bool TrimLineByPlane(ref Vector3 lineA, ref Vector3 lineB, Vector3 planePoint, Vector3 planeNormal, out bool isCompletelyBehind)
		{
			bool AisFront = (Vector3.Dot(lineA - planePoint, planeNormal) > 0f);
			bool BisFront = (Vector3.Dot(lineB - planePoint, planeNormal) > 0f);

			isCompletelyBehind = !AisFront && !BisFront;

			if (XOR(AisFront, BisFront)) // pokud je jeden vpredu a jeden vzadu, tak budu rezat
			{
				Vector3 intersection;
				if (!LinePlaneIntersection(lineA, lineB, ref planePoint, ref planeNormal, out intersection))
					return false; // toto by nemelo nastat
				if (AisFront)
					lineB = intersection; // seriznout B
				else
					lineA = intersection; // jinak seriznout A
				return true;
			}
			return false;
		}

		public static bool LinePlaneIntersection(Vector3 lineA, Vector3 lineB, ref Vector3 planePoint, ref Vector3 planeNormal, out Vector3 intersection)
		{
			float planeOffset;
			Vector3.Dot(ref planePoint, ref planeNormal, out planeOffset);
			float nominator = planeOffset - Vector3.Dot(lineA, planeNormal);
			float denominator = Vector3.Dot(lineB - lineA, planeNormal);

			if (denominator == 0f) // usecka je rovnobezna s plochou
			{
				intersection = lineA;
				return false;
			}

			float t = nominator / denominator;
			intersection = ((lineB - lineA) * t) + lineA;

			if (t < 0f || t > 1f) // prusecik je mimo usecku
				return false;

			return true;
		}

		public static bool LinePlaneIntersection(Vector3 lineA, Vector3 lineB, ref Vector3 planeNormal, float planeOffset, out float parameter)
		{
			float nominator = planeOffset - Vector3.Dot(lineA, planeNormal);
			float denominator = Vector3.Dot(lineB - lineA, planeNormal);

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
		
		public static bool LinePlaneIntersection(Vector3 lineA, Vector3 lineB, Vector3 planeA, Vector3 planeB, Vector3 planeC, out Vector3 intersection, out Vector3 parametres)
		{
			if (!tryComputeParametresOfLinePlaneIntersection(lineA, lineB, planeA, planeB, planeC, out parametres))
			{
				intersection = Vector3.Zero;
				return false;
			}
			intersection = lineA + (lineB - lineA) * parametres.X;

			// pokud budu chtit zjistit zda doslo k pruniku se ctyruhelnikem danym temito trmi body, tak tohle odkomentovat
			//return parametres.X >= 0f && parametres.X <= 1f && parametres.Y >= 0f && parametres.Y <= 1f && parametres.Z >= 0f && parametres.Z <= 1f;

			return true;
		}

		public static bool ValueIsInInterval(double value, double a, double b, out float parameter)
		{
			double range = b - a;
			if (range == 0.0)
			{
				parameter = 0.0f;
				return false; /**/ // check whether value is equal to min?
			}
			parameter = (float)((value - a) / range);
			return value.CompareTo(a) != value.CompareTo(b);
		}

		public static bool ValueIsInInterval(double value, double a, double b)
		{
			return value.CompareTo(a) != value.CompareTo(b);
		}

		private static bool tryComputeParametresOfLinePlaneIntersection(Vector3 a, Vector3 b, Vector3 planeA, Vector3 planeB, Vector3 planeC, out Vector3 parametres)
		{
			Matrix3x3 set = new Matrix3x3();
			
			set[0, 0] = a.X - b.X;
			set[0, 1] = planeB.X - planeA.X;
			set[0, 2] = planeC.X - planeA.X;
			set[1, 0] = a.Y - b.Y;
			set[1, 1] = planeB.Y - planeA.Y;
			set[1, 2] = planeC.Y - planeA.Y;
			set[2, 0] = a.Z - b.Z;
			set[2, 1] = planeB.Z - planeA.Z;
			set[2, 2] = planeC.Z - planeA.Z;

			Matrix3x3 inverted;
			if (!set.TryInvert(out inverted))
			{
				parametres = Vector3.Zero;
				return false;
			}
			parametres = inverted * new Vector3(a.X - planeA.X, a.Y - planeA.Y, a.Z - planeA.Z);
			return true;
		}

		/// <summary>
		/// Returns distance from specified point to plane
		/// </summary>
		/// <param name="point">point from which we want to calculate distance to plane</param>
		/// <param name="pointOnPlane">some point on plane</param>
		/// <param name="planeNormal">Normal vector of plane, it must have unit length.</param>
		public static float PointPlaneDistance(Vector3 point, Vector3 pointOnPlane, Vector3 planeNormal)
		{
			return Math.Abs(Vector3.Dot(point - pointOnPlane, planeNormal));
		}

		public static float PointPlaneDistanceSigned(Vector3 point, Vector3 pointOnPlane, Vector3 planeNormal)
		{
			return Vector3.Dot(point - pointOnPlane, planeNormal);
		}

		/// <summary>
		/// vraci true pokud sem se strefil do usecky
		/// </summary>
		/// <param name="lineA">prvni bod usecky</param>
		/// <param name="lineB">druhy bod usecky</param>
		/// <param name="point">bod od ktereho merime vzdalenost(treba mys, nebo jiny hlodavec)</param>
		/// <param name="distance">vzdalenost bodu od usecky</param>
		public static bool LineHit(Vector2 lineA, Vector2 lineB, Vector2 point, float limit, out float distance)
		{
			distance = Math.Abs(DistanceFromLine(lineA, lineB, point));
			float v1 = (float)Math.Sqrt(SQR(lineA.X - point.X) + SQR(lineA.Y - point.Y));
			float v2 = (float)Math.Sqrt(SQR(lineB.X - point.X) + SQR(lineB.Y - point.Y));
			float d =  (float)Math.Sqrt(SQR(lineA.X - lineB.X) + SQR(lineA.Y - lineB.Y));
			return distance <= limit && v1 < d && v2 < d;
		}

		/// <summary>
		/// vrati vzdalenost bodu point od primky ab, vysledek bude kladny pokud je nad primkou, jinak bude zaporny
		/// </summary>
		/// <param name="lineA">prvni bod primky</param>
		/// <param name="lineB">druhy bod primky</param>
		/// <param name="point">bod od ktereho chci zmerit vzdalenost k primce ab</param>
		public static float DistanceFromLine(Vector2 lineA, Vector2 lineB, Vector2 point)
		{
			Vector2 n = new Vector2(lineA.Y - lineB.Y, lineB.X - lineA.X);
			float k1 = (n.X * lineA.X + n.Y * lineA.Y);
			float k2 = (n.X * point.X + n.Y * point.Y);
			return (k2 - k1) / (float)Math.Sqrt(SQR(n.X) + SQR(n.Y));
		}

		public static Vector3 ProjectionOfPointToPlane(Vector3 point, Vector3 planePoint, Vector3 planeNormal)
		{
			Vector3 projection;
			LinePlaneIntersection(point, point + planeNormal, ref planePoint, ref planeNormal, out projection);
			return projection;
		}

		public static Vector3 RotateVector(Vector3 v, float angle, Vector3 axis)
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

		public static bool PointIsInsideFace2D(Vector2 point, Vector2[] projectedVerticesOfFace)
		{
			const float angleLimit = 359f;/**/
			List<Vector2> vectors = new List<Vector2>(projectedVerticesOfFace.Length);
			foreach (Vector2 v in projectedVerticesOfFace)
				vectors.Add(Vector2.Normalize(v - point));
			float angleSum = 0f;
			for (int i = 0; i < vectors.Count; i++)
				angleSum += GetAngleInDegreesBetweenUnitVectors(vectors[i], vectors[(i + 1) % vectors.Count]);
			return angleSum > angleLimit;
		}

		#endregion

		#region GL Helper Functions

		public static int GetDepthBufferBits()
		{
			int depth;
			GL.GetInteger(GetPName.DepthBits, out depth);
			return depth;
		}

		public static bool GetOpenGLVersion(out int major, out int minor)
		{
			if (openglVersionIsComputed)
			{
				major = majorOpenGLVersion;
				minor = minorOpenGLVersion;
				return true;
			}

			string version = GL.GetString(StringName.Version);

			const string REGEX_VERSION_PATTERN = @"(\d+)\.(\d+)\.*(\d*)";

			Regex regex = new Regex(REGEX_VERSION_PATTERN);
			Match match = regex.Match(version);
			if (!match.Success || match.Groups.Count < 3)
			{
				major = minor = 0;
				return false;
			}
			major = majorOpenGLVersion = Convert.ToInt32(match.Groups[1].Value);
			minor = minorOpenGLVersion = Convert.ToInt32(match.Groups[2].Value);
			openglVersionIsComputed = true;
			return true;
		}

		public static string GetOpenGLVersionString()
		{
			return GL.GetString(StringName.Version).Trim().Split(' ', '\t')[0];
		}

		public static void SaveTransformationMatrices()
		{
			GL.MatrixMode(MatrixMode.Projection);
			GL.PushMatrix();
			GL.MatrixMode(MatrixMode.Modelview);
			GL.PushMatrix();
		}

		public static void RestoreTransformationMatrices()
		{
			GL.MatrixMode(MatrixMode.Modelview);
			GL.PopMatrix();
			GL.MatrixMode(MatrixMode.Projection);
			GL.PopMatrix();
		}

		#endregion

		#region Text drawing

		static MeshEditor.OpenTKCompatibility.TextPrinter textPrinter = new OpenTKCompatibility.TextPrinter();
		static Font textFont = new Font(FontFamily.GenericSansSerif, 8f, FontStyle.Regular);

		public static void DrawText(string text, Vector3 position, Color color)
		{
			// NOTE: only for testing; very bad performance
			int[] viewport;
			double[] modelview;
			double[] projection;
			MeshEditor.Data.Scene.ExtractMatrices(out viewport, out modelview, out projection);

			Vector3 winPos;

			RectangleF area = new RectangleF(0f, 0f, 0f, 0f);
			textPrinter.Begin(); // sets orthografic projection

			GluProject(position, modelview, projection, viewport, out winPos);
			area.X = winPos.X + 1;
			area.Y = viewport[3] - winPos.Y + 1;
			textPrinter.Print(text, textFont, color, area);

			textPrinter.End(); // restores projection matrix
		}

		public static void DrawText(string text, Vector2 windowPosition, Color color)
		{
			// NOTE: only for testing; very bad performance

			RectangleF area = new RectangleF(windowPosition.X, windowPosition.Y, 0f, 0f);
			textPrinter.Begin(); // sets orthografic projection

			textPrinter.Print(text, textFont, color, area);

			textPrinter.End(); // restores projection matrix
		}

		public static SizeF MeasureText(string text, Vector2 windowPosition)
		{
			// NOTE: only for testing; very bad performance

			RectangleF area = new RectangleF(windowPosition.X, windowPosition.Y, 0f, 0f);
			textPrinter.Begin(); // sets orthografic projection
			RectangleF measuredArea = textPrinter.Measure(text, textFont, area);
			textPrinter.End(); // restores projection matrix
			return measuredArea.Size;
		}

		#endregion

		#region Glu replacement

		public static void GluPerspective(double fovy, double aspect, double zNear, double zFar)
		{
			//Glu.Perspective(fovy, aspect, zNear, zFar);

			if (double.IsNaN(aspect))
				return; /*chyba*/

			double xmin, xmax, ymin, ymax;
			ymax = zNear * Math.Tan(fovy * Math.PI / 360.0);
			ymin = -ymax;
			xmin = ymin * aspect;
			xmax = ymax * aspect;
			if (ymax == 0.0 || xmax == 0.0)
				return; /*chyba*/
			GL.Frustum(xmin, xmax, ymin, ymax, zNear, zFar);
		}

		public static bool GluProject(Vector3 obj, double[] modelMatrix, double[] projMatrix, int[] viewport, out Vector3 win)
		{
			//return Glu.Project(obj, modelMatrix, projMatrix, viewport, out win);
			
			// ============================================================

			Vector4d input = new Vector4d(obj.X, obj.Y, obj.Z, 1.0);
			Vector4d output;

			MVM(modelMatrix, ref input, out output);
			MVM(projMatrix, ref output, out input);

			if (input.W == 0.0)
			{
				win = Vector3.Zero;
				//throw new Exception("gluProject error");
				return false; // chyba
			}

			input.X /= input.W;
			input.Y /= input.W;
			input.Z /= input.W;

			/* Map x, y and z to range 0-1 */
			input.X = input.X * 0.5 + 0.5;
			input.Y = input.Y * 0.5 + 0.5;
			input.Z = input.Z * 0.5 + 0.5;

			/* Map x,y to viewport */
			input.X = input.X * viewport[2] + viewport[0];
			input.Y = input.Y * viewport[3] + viewport[1];

			win = (Vector3)input.Xyz;

			return true;
		}

		public static bool GluProject(Vector3 obj, double[] modelMatrix, double[] projMatrix, int[] viewport, out Vector3d win)
		{
			//return Glu.Project(obj, modelMatrix, projMatrix, viewport, out win);

			// ============================================================

			Vector4d input = new Vector4d(obj.X, obj.Y, obj.Z, 1.0);
			Vector4d output;

			MVM(modelMatrix, ref input, out output);
			MVM(projMatrix, ref output, out input);

			if (input.W == 0.0)
			{
				win = Vector3d.Zero;
				//throw new Exception("gluProject error");
				return false; // chyba
			}

			input.X /= input.W;
			input.Y /= input.W;
			input.Z /= input.W;

			/* Map x, y and z to range 0-1 */
			input.X = input.X * 0.5 + 0.5;
			input.Y = input.Y * 0.5 + 0.5;
			input.Z = input.Z * 0.5 + 0.5;

			/* Map x,y to viewport */
			input.X = input.X * viewport[2] + viewport[0];
			input.Y = input.Y * viewport[3] + viewport[1];

			win = input.Xyz;

			return true;
		}

		public static bool GluProject(Vector3d obj, double[] modelMatrix, double[] projMatrix, int[] viewport, out Vector3d win)
		{
			//return Glu.Project(obj, modelMatrix, projMatrix, viewport, out win);

			// ============================================================

			Vector4d input = new Vector4d(obj.X, obj.Y, obj.Z, 1.0);
			Vector4d output;

			MVM(modelMatrix, ref input, out output);
			MVM(projMatrix, ref output, out input);

			if (input.W == 0.0)
			{
				win = Vector3d.Zero;
				//throw new Exception("gluProject error");
				return false; // chyba
			}

			input.X /= input.W;
			input.Y /= input.W;
			input.Z /= input.W;

			/* Map x, y and z to range 0-1 */
			input.X = input.X * 0.5 + 0.5;
			input.Y = input.Y * 0.5 + 0.5;
			input.Z = input.Z * 0.5 + 0.5;

			/* Map x,y to viewport */
			input.X = input.X * viewport[2] + viewport[0];
			input.Y = input.Y * viewport[3] + viewport[1];

			win = input.Xyz;

			return true;
		}

		public static bool GluUnProject(Vector3 win, double[] modelMatrix, double[] projMatrix, int[] viewport, out Vector3 obj)
		{
			//OpenTK.Graphics.Glu.UnProject(win, modelMatrix, projMatrix, viewport, out obj);
			//return true;

			double[] finalMatrix = new double[16];

			MMM(modelMatrix, projMatrix, finalMatrix);
			if (!InvertMatrix(finalMatrix, finalMatrix))
			{
				obj = Vector3.Zero;
				return false;
			}

			Vector4d input = new Vector4d(win.X, win.Y, win.Z, 1.0);
			Vector4d output;

			/* Map x and y from window coordinates */
			input.X = (input.X - viewport[0]) / viewport[2];
			input.Y = (input.Y - viewport[1]) / viewport[3];

			/* Map to range -1 to 1 */
			input.X = input.X * 2.0 - 1.0;
			input.Y = input.Y * 2.0 - 1.0;
			input.Z = input.Z * 2.0 - 1.0;

			MVM(finalMatrix, ref input, out output);
			if (output.W == 0.0)
			{
				obj = Vector3.Zero;
				return false;
			}

			output.X /= output.W;
			output.Y /= output.W;
			output.Z /= output.W;

			obj = (Vector3)output.Xyz;

			return true;
		}

		public static void GluLookAt(ref Vector3 eye, ref Vector3 center, ref Vector3 up)
		{
			//Glu.LookAt(eye, center, up);

			// or ->

			//Vector3 f = Vector3.Normalize(center - eye);
			//Vector3 s, u;
			//Vector3.Cross(ref f, ref up, out s);
			//Vector3.Cross(ref s, ref f, out u);

			//Matrix4 m = new Matrix4(
			//    new Vector4(s.X, u.X, -f.X, 0f),
			//    new Vector4(s.Y, u.Y, -f.Y, 0f),
			//    new Vector4(s.Z, u.Z, -f.Z, 0f),
			//    new Vector4(0f, 0f, 0f, 1f));

			//GL.MultMatrix(ref m);
			//GL.Translate(-eye);

			// or ->

			Matrix4 m = Matrix4.LookAt(eye, center, up);
			GL.MultMatrix(ref m);
		}

		#endregion

		#region IO

		public static bool CheckIfFileIsInSameDirectory(string filename, string directory)
		{
			try
			{
				string directoryName = Path.GetFullPath(directory).TrimEnd('\\', '/');
				string fullpath = filename;
				if (!Path.IsPathRooted(filename)) // if is relative path
					fullpath = Path.Combine(directoryName, filename); // combine with path and then compare
				fullpath = Path.GetFullPath(fullpath);
				return Path.GetDirectoryName(fullpath) == directoryName;
			}
			catch (Exception) // probably wrong character in path
			{
				return false;
			}
		}

		public static string GetFileBatchDescription(params string[] filenames)
		{
			Debug.Assert(filenames != null && filenames.Length > 0);
			if (filenames == null || filenames.Length == 0)
				return "Empty";
			return Path.GetFileName(filenames[0]); // pick name of first file
		}

		#endregion

		#region Assembly Info

		/// <summary>
		/// Gets the assembly title.
		/// </summary>
		/// <value>The assembly title.</value>
		public static string GetAssemblyTitle()
		{
			// Get all Title attributes on this assembly
			object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
			// If there is at least one Title attribute
			if (attributes.Length > 0)
			{
				// Select the first one
				AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)attributes[0];
				// If it is not an empty string, return it
				if (titleAttribute.Title != "")
					return titleAttribute.Title;
			}
			// If there was no Title attribute, or if the Title attribute was the empty string, return the .exe name
			return System.IO.Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().CodeBase);
		}

		public static Version GetAssemblyVersion()
		{
			AssemblyName assemblyName = Assembly.GetExecutingAssembly().GetName();
			return assemblyName.Version;
		}

		#endregion

	}
}
