using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using MeshEditor.Utilities;
using System.Linq;

using Utils = MeshEditor.Utilities.Functions;
using System.Xml.Linq;
using System.Diagnostics;

namespace MeshEditor.Data
{
	/// <summary>
	/// tato staticka trida slouzi jako poskytovatel barev pro nove pouzite vlastnosti
	/// </summary>
	public static class PropertyColorProvider
	{

		#region Fields, Constructor

		private static readonly Dictionary<Property, int> colorPalette;

		private const float GOLDEN_RATIO_INV = 0.618033988749895f;

		public static readonly int ZeroColor_RGBA32 = unchecked((int)0xFFFFFFFF);

		private static readonly int distinguishedHuesCount;
		private static readonly float startHue;
		private static readonly float[] availableLuminances;
		private static readonly float saturation;
		private static float currentHue;
		private static int currentLuminanceIndex;
		private static int currentLevelColorCount;

		static PropertyColorProvider()
		{
			startHue = 0.32f; // green
			saturation = 1f;
			distinguishedHuesCount = 20;
			availableLuminances = new float[] { 0.6f, 0.4f, 0.7f, 0.3f, 0.8f, 0.2f };

			colorPalette = new Dictionary<Property, int>();

			initializeColorEngine();
		}

		#endregion

		#region Private memebers

		private static void initializeColorEngine()
		{
			currentHue = startHue;
			currentLuminanceIndex = 0;
			currentLevelColorCount = 0;
		}

		private static int getNewPropertyColor(Property property)
		{
			if (property.IsZero)
			{
				return ZeroColor_RGBA32;
			}

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

		/// <summary>
		/// Returns color in RGBA format specifing property number.
		/// </summary>
		public static int GetRGBA32(Property property)
		{
			return colorPalette[property];
		}

		public static Color Get(Property property)
		{
			Debug.Assert(colorPalette.ContainsKey(property));
			// parse RGBA in big endian
			int color = GetRGBA32(property);
			int a = (color >> 24) & 0x000000FF;
			int b = (color >> 16) & 0x000000FF;
			int g = (color >> 8) & 0x000000FF;
			int r = color & 0x000000FF;
			return Color.FromArgb(a, r, g, b);
		}

		public static void Set(Property property, Color color)
		{
			Debug.Assert(colorPalette.ContainsKey(property));
			int rgba = 0;
			rgba |= color.R;
			rgba |= color.G << 8;
			rgba |= color.B << 16;
			rgba |= color.A << 24;
			colorPalette[property] = rgba;
		}

		public static void ArrangeColorForProperty(Property property)
		{
			if (!colorPalette.ContainsKey(property))
			{
				colorPalette[property] = getNewPropertyColor(property);
			}
		}

		public static IEnumerable<Property> GetAllUsedPropertiesSorted()
		{
			return colorPalette.Keys.OrderBy(p => p.Value);
		}

		public static IReadOnlyDictionary<Property, Color> GetAllPropertyColors()
		{
			Dictionary<Property, Color> result = new Dictionary<Property, Color>();
			foreach (Property property in colorPalette.Keys)
			{
				result.Add(property, Get(property));
			}
			return result;
		}

		#region Serialization of default property colors

		public static void UpdatePropertyColors(IReadOnlyDictionary<Property, Color> propertyColorsToUpdate)
		{
			foreach (var pair in propertyColorsToUpdate)
			{
				Debug.Assert(colorPalette.ContainsKey(pair.Key));
				if (colorPalette.ContainsKey(pair.Key))
				{
					Set(pair.Key, pair.Value);
				}
			}
		}

		public static void LoadPropertyColorsFromFile(string filename)
		{
			try
			{
				XElement rootElement = XElement.Load(filename);
				foreach (var element in rootElement.Elements())
				{
					Property property = new Property((int)element.Attribute("propertyId"));
					int color = int.Parse(element.Value, System.Globalization.NumberStyles.HexNumber);
					colorPalette[property] = color;
				}
			}
#if !DEBUG
			catch (Exception) { }
#endif
			finally { }
		}

		public static void SavePropertyColorsToFile(string filename)
		{
			try
			{
				XElement rootElement = new XElement("PropertyColors", GetAllUsedPropertiesSorted().Select(property =>
				{
					var propertyElement = new XElement("Color", colorPalette[property].ToString("X8"));
					propertyElement.SetAttributeValue("propertyId", property.Value.ToString());
					return propertyElement;
				}));
				rootElement.Save(filename);
			}
#if !DEBUG
			catch (Exception) { }
#endif
			finally { }
		}

		public static void ResetToDefaults()
		{
			initializeColorEngine();
			foreach (Property property in GetAllUsedPropertiesSorted().ToArray())
			{
				colorPalette[property] = getNewPropertyColor(property);
			}
		}

		#endregion

		#endregion

	}
}
