using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Data;

namespace MeshEditor.LayerManager.Infrastructure
{
	public class SolutionBuilder
	{
		public static Solution CreateFromMasterLayer(SummaryLayerFile masterLayer, string projectName)
		{
			Solution solution = new Solution
			{
				ProjectName = projectName,
				Layers = new[] { new Solution.LayerRecord { Id = masterLayer.Id, Name = masterLayer.Name } }
			};
			return solution;
		}

		public static void AddFilterLayer(Solution.LayerRecord parentLayer, SummaryLayerFile filterLayer)
		{
			var children = parentLayer.Children?.ToList() ?? new List<Solution.LayerRecord>();
			var newLayerRecord = new Solution.LayerRecord
			{
				Id = filterLayer.Id,
				Name = filterLayer.Name,
				Filter = filterLayer.Filter,
			};
			children.Add(newLayerRecord);
			parentLayer.Children = children.ToArray();
		}
	}
}
