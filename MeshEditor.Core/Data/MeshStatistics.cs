using System;
using System.Collections.Generic;
using System.Text;
using MeshEditor.Graphics;
using System.Linq;
using System.Collections.ObjectModel;

namespace MeshEditor.Data
{
	/// <summary>
	/// trida pro reprezentaci statistickych informaci o siti.
	/// obsahuje cisla pouzitych vlastnosti, typy prvku obsazenych v siti, 
	/// histogram uhlu hran, krome dalsich komentare k jednotlivym vlastnostem.
	/// </summary>
	public class MeshStatistics
	{

		#region Fields, constructor

		private Dictionary<EntityType, HashSet<Property>> allUsedProperties;
		private HashSet<ElementType> includedElementTypes;
		private Dictionary<Property, string> propertyComments;
		private Dictionary<PropertyEntityPair, List<PropertyCommand>> propertyCommands;

		private Histogram edgeAnglesHistogram;
		private float softBorderLimit;
		private float hardBorderLimit;

		private float minimalElementRadius;
		private bool minimalElementRadiusWasSetFlag;

		private string propertyCommandsFile;

		private Dictionary<Property, int> regionPropertyColorsPaletteIndexMap;

		public MeshStatistics()
		{
			this.allUsedProperties = new Dictionary<EntityType, HashSet<Property>>();
			this.includedElementTypes = new HashSet<ElementType>();
			this.propertyComments = new Dictionary<Property, string>();
			this.propertyCommands = new Dictionary<PropertyEntityPair, List<PropertyCommand>>();

			this.edgeAnglesHistogram = null;
			this.softBorderLimit = Scene.DefaultFirstBorderAngleLimit;
			this.hardBorderLimit = Scene.DefaultSecondBorderAngleLimit;

			this.minimalElementRadius = 0f;
			this.minimalElementRadiusWasSetFlag = false;

			this.regionPropertyColorsPaletteIndexMap = new Dictionary<Property, int>();
		}

		#endregion

		#region Public properties

		public Histogram EdgeAnglesHistogram
		{
			get { return edgeAnglesHistogram; }
			set { edgeAnglesHistogram = value; }
		}

		public float SoftBorderLimit
		{
			get { return softBorderLimit; }
		}

		public float HardBorderLimit
		{
			get { return hardBorderLimit; }
		}

		public bool MinimalElementRadiusWasSetFlag
		{
			get { return minimalElementRadiusWasSetFlag; }
		}

		public float MinimalElementRadius
		{
			get	{ return minimalElementRadius; }
			set
			{
				minimalElementRadius = value;
				minimalElementRadiusWasSetFlag = true;
			}
		}

		public Dictionary<Property, string> PropertyComments
		{
			get { return propertyComments; }
			set { propertyComments = value; }
		}

		public Dictionary<PropertyEntityPair, List<PropertyCommand>> PropertyCommands
		{
			get { return propertyCommands; }
			set { propertyCommands = value; }
		}

		public string PropertyCommandsFile
		{
			get { return propertyCommandsFile; }
			set { propertyCommandsFile = value; }
		}

		#endregion

		#region Public methods

		public void AddProperty(Property property, EntityType targetEntityType)
		{
			EntityType entityType = targetEntityType;
			if (targetEntityType == EntityType.Patch || targetEntityType == EntityType.Shell) // I suppose that SURFACE is equal to PATCH is equal to SHELL
			{
				entityType = EntityType.Surface;
			}

			if (!property.IsZero) // zero means no property, do not add to dictionary
			{
				HashSet<Property> propertyList;
				if (!allUsedProperties.TryGetValue(entityType, out propertyList))
				{
					propertyList = allUsedProperties[entityType] = new HashSet<Property>();
				}
				propertyList.Add(property);
			}

			// set color for this property if not already done
			PropertyColorProvider.ArrangeColorForProperty(property);

			if (entityType == EntityType.Region && !regionPropertyColorsPaletteIndexMap.ContainsKey(property))
			{
				int index = regionPropertyColorsPaletteIndexMap.Count;
				regionPropertyColorsPaletteIndexMap.Add(property, index);
			}
		}

		public void AddElementType(ElementType type)
		{
			includedElementTypes.Add(type);
		}

		public void SetBorderLimits(float soft, float hard)
		{
			this.softBorderLimit = soft;
			this.hardBorderLimit = hard;
		}

		public ElementType[] GetIncludedElementTypesArray()
		{
			ElementType[] result = includedElementTypes.ToArray();
			Array.Sort(result);
			return result;
		}

		public IEnumerable<PropertyEntityPair> GetAllPropertyEntityPairs()
		{
			foreach (EntityType entityType in allUsedProperties.Keys)
			{
				foreach (Property property in allUsedProperties[entityType])
				{
					yield return new PropertyEntityPair(property, entityType);
				}
			}
		}

		public /*TODO: IReadOnlyCollection<Property>*/ ICollection<Property> GetPropertiesOFEntityType(EntityType entityType)
		{
			HashSet<Property> properties;
			if (!allUsedProperties.TryGetValue(entityType, out properties))
				return new Collection<Property>();
			return properties;
		}

		public /*TODO: IReadOnlyList<int>*/ IList<int> GetElementPropertyColorsPalette()
		{
			int[] colorPalette = new int[regionPropertyColorsPaletteIndexMap.Count];
			foreach (var propertyIndex in regionPropertyColorsPaletteIndexMap)
			{
				colorPalette[propertyIndex.Value] = PropertyColorProvider.GetRGBA32(propertyIndex.Key);
			}
			return colorPalette;
		}

		public int GetIndexOfPropertyInElementPropertyColorsPalette(Property property)
		{
			return regionPropertyColorsPaletteIndexMap[property];
		}

		#endregion

	}
}
