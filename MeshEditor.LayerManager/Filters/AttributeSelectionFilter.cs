using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.LayerManager.Filters
{
	internal class AttributeSelectionFilter : Filter
	{
		public override FilterType Type => FilterType.AttributeSelection;

		public string AttributeName { get; set; }

		public int[] AttributeSelection { get; set; }
	}
}
