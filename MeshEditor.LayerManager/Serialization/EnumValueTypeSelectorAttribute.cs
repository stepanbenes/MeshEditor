using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.LayerManager.Serialization
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
	public class EnumValueTypeSelectorAttribute : Attribute
	{
		public object EnumValue { get; }
		public Type TargetType { get; }
		public string EnumPropertyName { get; }

		public EnumValueTypeSelectorAttribute(object enumValue, Type targetType, string enumPropertyName)
		{
			EnumValue = enumValue;
			TargetType = targetType;
			EnumPropertyName = enumPropertyName;
		}
	}
}
