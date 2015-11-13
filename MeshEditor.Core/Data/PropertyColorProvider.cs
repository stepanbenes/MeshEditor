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

		public static readonly int ColorPaletteLength;
		private static readonly int[] colorPalette;
		private static readonly HashSet<Property> properties;

		private const float GOLDEN_RATIO_INV = 0.618033988749895f;

		public static readonly int ZeroColor_RGBA32 = unchecked((int)0xFFFFFFFF);

		static PropertyColorProvider()
		{
			ColorPaletteLength = 254;
			colorPalette = new int[ColorPaletteLength];
			properties = new HashSet<Property>();

			ResetToDefaults();
		}

		#endregion

		#region Public members

		public static Property[] GetAllUsedPropertiesSorted()
		{
			return properties.OrderBy(p => p.Value).ToArray();
		}

		public static int[] GetColorPalette()
		{
			return colorPalette.ToArray();
		}

		public static int GetIndexInColorPalette(Property property)
		{
			return property.Value % ColorPaletteLength;
		}

		/// <summary>
		/// Returns color in RGBA format specifing property number.
		/// </summary>
		public static int GetRGBA32(Property property)
		{
			if (property.IsZero) // zero property is special
				return ZeroColor_RGBA32;
			return colorPalette[GetIndexInColorPalette(property)];
		}

		public static Color Get(Property property)
		{
			Debug.Assert(properties.Contains(property));
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
			Debug.Assert(properties.Contains(property));
			int rgba = 0;
			rgba |= color.R;
			rgba |= color.G << 8;
			rgba |= color.B << 16;
			rgba |= color.A << 24;
			colorPalette[GetIndexInColorPalette(property)] = rgba;
		}

		public static void AddProperty(Property property)
		{
			properties.Add(property);
		}

		public static IDictionary<Property, Color> GetAllPropertyColors()
		{
			Dictionary<Property, Color> result = new Dictionary<Property, Color>();
			foreach (Property property in properties)
			{
				result.Add(property, Get(property));
			}
			return result;
		}

		#region Serialization of default property colors

		public static void LoadPropertyColors(IDictionary<Property, Color> newPropertyColors)
		{
			foreach (var pair in newPropertyColors)
			{
				if (properties.Contains(pair.Key))
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
					int index = (int)element.Attribute("index");
					int color = int.Parse(element.Value);
					colorPalette[index] = color;
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
				XElement rootElement = new XElement("ColorPalette", colorPalette.Select((color, index) =>
				{
					var propertyElement = new XElement("Color", color);
					propertyElement.SetAttributeValue("index", index);
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
			const float startHue = 0.32f; // green
			const float saturation = 1f;
			const int distinguishedHuesCount = 20;
			/*const*/ float[] availableLuminances = new float[] { 0.6f, 0.4f, 0.7f, 0.3f, 0.8f, 0.2f };

			float currentHue = startHue;
			int currentLuminanceIndex = 0;
			int currentLevelColorCount = 0;

			for (int i = 0; i < ColorPaletteLength; i++)
			{
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
				colorPalette[i] = result;
			}
		}

		#endregion

		#endregion

	}
}
