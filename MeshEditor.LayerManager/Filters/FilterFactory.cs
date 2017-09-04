using System;
using System.Collections.Generic;
using System.Globalization;
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
						AttributeSelection = parameters.Skip(1).Select(p => int.Parse(p, NumberStyles.Integer, CultureInfo.InvariantCulture)).ToArray()
					};
				case FilterType.Slice:
					return new SliceFilter
					{
						NormalX = float.Parse(parameters.ElementAt(0), NumberStyles.Float, CultureInfo.InvariantCulture),
						NormalY = float.Parse(parameters.ElementAt(1), NumberStyles.Float, CultureInfo.InvariantCulture),
						NormalZ = float.Parse(parameters.ElementAt(2), NumberStyles.Float, CultureInfo.InvariantCulture),
						Offset = float.Parse(parameters.ElementAt(3), NumberStyles.Float, CultureInfo.InvariantCulture),
					};
				case FilterType.Surface:
					return new SurfaceFilter
					{
						EdgeAngleLimits = parameters.Select(p => float.Parse(p, NumberStyles.Float, CultureInfo.InvariantCulture)).ToArray()
					};
				case FilterType.Clip:
				case FilterType.IsoSurface:
				case FilterType.StreamLines:
					throw new NotImplementedException();
				case FilterType.TimeCompression:
					return new TimeCompressionFilter
					{
						FieldName = parameters.ElementAtOrDefault(0)
					};
				case FilterType.Deformation:
					var deformationFilter = new DeformationFilter
					{
						DeformationFieldName = parameters.ElementAt(0)
					};
					var scaleParameter = parameters.ElementAtOrDefault(1);
					if (scaleParameter != null)
					{
						deformationFilter.RelativeScale = double.Parse(scaleParameter, NumberStyles.Float, CultureInfo.InvariantCulture);
					}
					return deformationFilter;
				default:
					throw new NotSupportedException();
			}
		}
	}
}
