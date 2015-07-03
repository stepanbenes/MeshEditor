using System;
using System.Collections.Generic;
using System.Text;

namespace MeshEditor.Data
{
	/// <summary>
	/// trida reprezentujici parovou dvojici [typ entity; vlastnost entity]
	/// </summary>
	public struct PropertyEntityPair : IComparable<PropertyEntityPair>
	{
		private EntityType entityType;
		private Property property;

		public EntityType EntityType
		{
			get { return entityType; }
			set { entityType = value; }
		}

		public Property Property
		{
			get { return property; }
			set { property = value; }
		}

		public PropertyEntityPair(Property property, EntityType entityType)
		{
			this.property = property;
			this.entityType = entityType;
		}

		public override string ToString()
		{
			return "[" + Property + "; " + EntityType + "]";
		}

		#region IComparable<PropertyEntityPair> Members

		public int CompareTo(PropertyEntityPair other)
		{
			if (this.entityType < other.entityType)
				return -1;
			if (this.entityType > other.entityType)
				return 1;
			return property.CompareTo(other.property);
		}

		#endregion
	}
}
