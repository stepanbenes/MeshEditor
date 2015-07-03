using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using MeshEditor.Utilities;

using Utils = MeshEditor.Utilities.Functions;

namespace MeshEditor.Data
{
	/// <summary>
	/// tato staticka trida slouzi jako poskytovatel barev pro nove pouzite vlastnosti
	/// </summary>
	public static class PropertyColorProvider
	{

		#region Fields, Constructor

		private static readonly Dictionary<Property, int> propertyColors;

		private const float GOLDEN_RATIO_INV = 0.618033988749895f;
		
		private static readonly int distinguishedHuesCount;
		private static readonly float startHue;
		private static readonly float[] availableLuminances;
		private static readonly float saturation;
		private static float currentHue;
		private static int currentLuminanceIndex;
		private static int currentLevelColorCount;

		static PropertyColorProvider()
		{
			propertyColors = new Dictionary<Property, int>();
			setZeroPropertyColor();

			startHue = 0.32f; // green
			saturation = 1f;
			distinguishedHuesCount = 20;
			availableLuminances = new float[] { 0.6f, 0.4f, 0.7f, 0.3f, 0.8f, 0.2f };
			currentHue = startHue;
			currentLuminanceIndex = 0;
			currentLevelColorCount = 0;
		}

		#endregion

		#region Private memebers

		private static void setZeroPropertyColor()
		{
			propertyColors[Property.Zero] = Utils.ColorToRgba32(Color.White);
			// ...
		}
		
		private static int getNewPropertyColor(Property property)
		{
			//return Utils.ColorToRgba32(Color.FromArgb(RandomNumber.GetRandomByte(), RandomNumber.GetRandomByte(), RandomNumber.GetRandomByte()));
			//Console.WriteLine("hue: " + currentHue);
			int result = Utils.HslToRgba32(currentHue, saturation, availableLuminances[currentLuminanceIndex]);
			currentLevelColorCount++;
			if (currentLevelColorCount >= distinguishedHuesCount)
			{
				//currentHue = startHue;
				currentLuminanceIndex = (currentLuminanceIndex + 1) % availableLuminances.Length;
				currentLevelColorCount = 0;
			}
			else
			{
				currentHue += GOLDEN_RATIO_INV;
				currentHue %= 1f;
			}
			return result;
		}

		#endregion

		#region Public members

		public static IEnumerable<Property> AllUsedProperties
		{
			get { return propertyColors.Keys; }
		}

		/// <summary>
		/// Returns color in RGBA format specifing property number.
		/// </summary>
		public static int GetRGBA32(Property property)
		{
			//int result;
			//if (!propertyColors.TryGetValue(property, out result))
			//    result = propertyColors[property] = getNewPropertyColor(property);
			//return result;
			return propertyColors[property];
		}

		public static Color Get(Property property)
		{
			// ABGR -> ARGB
			int color = GetRGBA32(property);
			int a = (color >> 24) & 0x000000FF;
			int b = (color >> 16) & 0x000000FF;
			int g = (color >> 8) & 0x000000FF;
			int r = color & 0x000000FF;
			return Color.FromArgb(a, r, g, b);
		}

		public static void SetPropertyColorIfNew(Property property)
		{
			if (!propertyColors.ContainsKey(property))
				propertyColors[property] = getNewPropertyColor(property);
		}

		#endregion

	}
}
