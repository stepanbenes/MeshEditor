using System;
using System.Collections.Generic;
using System.Text;
using MeshEditor.Graphics;
using System.Linq;

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

		private Dictionary<Property, List<EntityType>> allUsedProperties;
		private HashSet<ElementType> includedElementTypes;
		private Dictionary<Property, string> propertyComments;
		private Dictionary<PropertyEntityPair, List<PropertyCommand>> propertyCommands;

		private Histogram edgeAnglesHistogram;
		private float softBorderLimit;
		private float hardBorderLimit;

		private float minimalElementRadius;
		private bool minimalElementRadiusWasSetFlag;

		private string propertyCommandsFile;

		//public event EventHandler RecreateBuffersNeeded;

		public MeshStatistics()
		{
			this.allUsedProperties = null;
			this.includedElementTypes = new HashSet<ElementType>();
			this.propertyComments = new Dictionary<Property, string>();
			this.propertyCommands = new Dictionary<PropertyEntityPair, List<PropertyCommand>>();

			edgeAnglesHistogram = null;
			this.softBorderLimit = Scene.DefaultFirstBorderAngleLimit;
			this.hardBorderLimit = Scene.DefaultSecondBorderAngleLimit;

			this.minimalElementRadius = 0f;
			this.minimalElementRadiusWasSetFlag = false;

			// ================================================
			// !!!
			//propertyDescriptions[new Property(4)] = "ctyrka je nejlepsi";
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

		//public void AddProperty(Property property, bool isElementProperty)
		//{
		//    if (!propertyDescriptions.ContainsKey(property))
		//        propertyDescriptions[property] = string.Empty;
		//    if (isElementProperty)
		//        includedElementProperties.Add(property);
		//}

		public void AddProperty(Property property, EntityType targetEntityType)
		{
			if (property == Property.Zero) // zero means no property, do not add to dictionary
				return;
			if (allUsedProperties == null)
				allUsedProperties = new Dictionary<Property, List<EntityType>>();
			List<EntityType> entityTypeList;
			if (!allUsedProperties.TryGetValue(property, out entityTypeList))
				entityTypeList = allUsedProperties[property] = new List<EntityType>();

			EntityType entityType = targetEntityType;
			if (targetEntityType == EntityType.Patch || targetEntityType == EntityType.Shell) // I suppose that SURFACE is equal to PATCH is equal to SHELL
				entityType = EntityType.Surface;

			if (!entityTypeList.Contains(entityType))
				entityTypeList.Add(entityType);

			// set color for this property if not already done
			PropertyColorProvider.SetPropertyColorIfNew(property);
		}

		public void AddElementType(ElementType type)
		{
			includedElementTypes.Add(type);
		}

		public void SetBorderLimits(float soft, float hard)
		{
			this.softBorderLimit = soft;
			this.hardBorderLimit = hard;
			//if (RecreateBuffersNeeded != null)
			//    RecreateBuffersNeeded(this, EventArgs.Empty);
		}

		public ElementType[] GetIncludedElementTypesArray()
		{
			ElementType[] result = includedElementTypes.ToArray();
			Array.Sort(result);
			return result;
		}

		public IEnumerable<PropertyEntityPair> GetAllPropertyEntityPairs()
		{
			if (allUsedProperties == null)
				yield break;
			foreach (Property property in allUsedProperties.Keys)
				foreach (EntityType entityType in allUsedProperties[property])
					yield return new PropertyEntityPair(property, entityType);
		}

		#endregion

	}
}
