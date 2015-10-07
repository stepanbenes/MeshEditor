using System;
using System.Collections.Generic;
using System.Text;
using OpenTK;

namespace MeshEditor.Data
{
	/// <summary>
	/// uzel site konecnych prvku (zaroven vrchol prvku, ploch a hran)
	/// </summary>
	public class Node : IItemWithSignificantPoint, ISelectable, IComparable, IEquatable<Node>
	{

		#region Fields, Constructors

		private int id;
		private Vector3 position; /**/ // Vector3d ??? (double precision)
		private PropertyEntityPair[] properties;
		//private Property property;

		//public Node(int id, Vector3 position, Property property)
		//{
		//    this.id = id;
		//    this.position = position;
		//    this.property = property;
		//}

		public Node(int id, Vector3 position, PropertyEntityPair[] properties)
		{
			this.id = id;
			this.position = position;
			this.properties = properties;
		}

		//private void setNodeProperty()
		//{
		//    this.property = Property.Zero;
		//    if (properties == null)
		//        return;
		//    for (int i = 0; i < properties.Length; i++)
		//    {
		//        if (properties[i].EntityType == EntityType.Vertex)
		//        {
		//            this.property = properties[i].Property;
		//            break;
		//        }
		//    }
		//}

		#endregion

		#region Comparing, GetHashCode

		#region IComparable Members

		public int CompareTo(object obj)
		{
			return this.id - ((Node)obj).id;
		}

		#endregion

		public bool Equals(Node other)
		{
			return this.id == other.id;
		}

		public override bool Equals(object obj)
		{
			Node other = obj as Node;
			if (other == null)
				return false;
			return this.Equals(other);
		}

		public override int GetHashCode()
		{
			return id.GetHashCode();
		}

		#endregion

		#region ToString methods

		public string ToStringWithOriginalCoordinates(float meshResizeFactor, Vector3 meshPositionOffset)
		{
			Vector3 transformedPosition = (this.position / meshResizeFactor) + meshPositionOffset;

			StringBuilder text = new StringBuilder();
			text.Append("Node ");
			text.Append(id);
			text.Append(" | Position: ");

			text.Append(Utilities.Functions.GetVector3StringRepresentation(ref transformedPosition));

			text.Append(" | Properties: ");
			text.Append(PropertyListToString());

			return text.ToString();
		}

		public string PropertyListToString()
		{
			if (properties == null)
				return "0";
			StringBuilder text = new StringBuilder();
			foreach (PropertyEntityPair pair in properties)
			{
				if (pair.EntityType == EntityType.Shell)
					text.Append("Sh");
				else
					text.Append(pair.EntityType.ToString()[0]);
				text.Append(":");
				text.Append(pair.Property.ToString());
				text.Append(" ");
			}
			return text.ToString();
		}

		public string PropertyListInDefaultFormat()
		{
			if (properties == null)
				return "0";
			StringBuilder text = new StringBuilder();
			text.Append(properties.Length.ToString());
			foreach (PropertyEntityPair pair in properties)
			{
				text.Append(" ");
				text.Append((int)pair.EntityType);
				text.Append(" ");
				text.Append(pair.Property.ToString());
			}
			return text.ToString();
		}

		#endregion

		#region IItemWithSignificantPoint Members

		public Vector3 GetSignificantPoint()
		{
			return position;
		}

		#endregion
		
		#region Other Properties

		public int ID
		{
			get { return id; }
		}

		public Vector3 Position
		{
			get { return position; }
			set { position = value; }
		}

		#endregion

		#region Node Property stuff

		#region ISelectable Members

		public Property Property
		{
			get { return getLastProperty(); }
			set { addProperty(value); }
		}

		#endregion

		public PropertyEntityPair[] Properties
		{
			get { return properties; }
		}

		public void RemoveVertexProperty(Property property)
		{
			if (properties == null)
				return;
			List<PropertyEntityPair> propertyList = new List<PropertyEntityPair>(properties.Length);
			foreach (PropertyEntityPair pair in properties)
			{
				if (pair.EntityType != EntityType.Vertex || pair.Property != property)
					propertyList.Add(pair);
			}
			this.properties = (propertyList.Count == 0) ? null : propertyList.ToArray();
		}

		private Property getLastProperty()
		{
			if (properties == null)
				return Property.Zero;
			for (int i = properties.Length - 1; i >= 0; i--)
				if (properties[i].EntityType == EntityType.Vertex)
					return properties[i].Property;
			return Property.Zero;
		}

		private void addProperty(Property property)
		{
			addProperty(property, EntityType.Vertex);
		}

		private void addProperty(Property property, EntityType entityType)
		{
			// dulezite je, aby tato funkce pridala vlastnost na konec pole

			if (this.properties == null)
			{
				this.properties = new PropertyEntityPair[] { new PropertyEntityPair(property, entityType) };
				return;
			}

			foreach (PropertyEntityPair pair in this.properties)
			{
				if (pair.EntityType == entityType && pair.Property == property)
					return; // uz je obsazena
			}
			PropertyEntityPair[] newArray = new PropertyEntityPair[this.properties.Length + 1];
			Array.Copy(this.properties, newArray, this.properties.Length);
			newArray[newArray.Length - 1] = new PropertyEntityPair(property, entityType);
			Array.Sort(newArray);
			this.properties = newArray;
		}

		public bool ContainsProperty(Property property)
		{
			if (properties == null)
				//return false; // proste zadna
				return property.IsZero; // pokud nemam zadny property, tak je to interpretovano jako nulova property
				
			foreach (PropertyEntityPair pair in this.properties)
				if (pair.EntityType == EntityType.Vertex && pair.Property == property)
					return true;
			return false;
		}

		public void RebuildEdgeSurfaceRegionProperties(IEnumerable<Property> edgeProperties, IEnumerable<Property> surfaceProperties, IEnumerable<Property> regionProperties)
		{
			List<PropertyEntityPair> propertyList = new List<PropertyEntityPair>();
			if (this.properties != null)
			{
				foreach (PropertyEntityPair pair in this.properties)
				{
					if (pair.EntityType != EntityType.Edge && pair.EntityType != EntityType.Surface && pair.EntityType != EntityType.Region)
						propertyList.Add(pair); // copy all properties except those for edges, faces and elements
				}
			}
			if (edgeProperties != null)
			{
				foreach (Property property in edgeProperties)
				{
					propertyList.Add(new PropertyEntityPair(property, EntityType.Edge));
				}
			}
			if (surfaceProperties != null)
			{
				foreach (Property property in surfaceProperties)
				{
					propertyList.Add(new PropertyEntityPair(property, EntityType.Surface));
				}
			}
			if (regionProperties != null)
			{
				foreach (Property property in regionProperties)
				{
					propertyList.Add(new PropertyEntityPair(property, EntityType.Region));
				}
			}

			// sort properties
			propertyList.Sort();

			this.properties = propertyList.ToArray();
		}

		#endregion
	
	}

	/// <summary>
	/// Uzel obohaceny o hodnotu v plovouci radove carce. 
	/// Tato hodnota muze byt vyuzita pro ulozeni vypoctenych hodnot
	/// a nasledne pouzita pro barevne zobrazeni vysledku.
	/// </summary>
	//public class NodeWithValue : Node
	//{
	//    private float value;

	//    public float Value
	//    {
	//        get { return this.value; }
	//        set { this.value = value; }
	//    }

	//    public NodeWithValue(int id, Vector3 position, PropertyPair[] properties, float value)
	//        : base(id, position, properties)
	//    {
	//        this.value = value;
	//    }
	//}

}
