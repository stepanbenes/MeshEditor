using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.LayerManager.Filters
{
	public static class FilterFactory
	{
		public static Filter Create(FilterType type, IEnumerable<string> parameters)
		{
			switch (type)
			{
				case FilterType.AttributeSelection:
					return new AttributeSelectionFilter
					{
						AttributeName = parameters.First(),
						AttributeSelection = parameters.Skip(1).Select(p => int.Parse(p)).ToArray()
					};
				case FilterType.Slice:
					return new SliceFilter
					{
						NormalX = float.Parse(parameters.ElementAt(0)),
						NormalY = float.Parse(parameters.ElementAt(1)),
						NormalZ = float.Parse(parameters.ElementAt(2)),
						Offset = float.Parse(parameters.ElementAt(3)),
					};
				case FilterType.Surface:
					return new SurfaceFilter
					{
						EdgeAngleLimits = parameters.Select(p => float.Parse(p)).ToArray()
					};
				case FilterType.Clip:
				case FilterType.IsoSurface:
				case FilterType.StreamLines:
					throw new NotImplementedException();
				case FilterType.TimeCompression:
					return new TimeCompressionFilter
					{
						FieldName = parameters.ElementAtOrDefault(0),
						ComponentName = parameters.ElementAtOrDefault(1)
					};
				default:
					throw new NotSupportedException();
			}
		}
	}
}
