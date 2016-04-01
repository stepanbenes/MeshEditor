using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Data;

namespace MeshEditor.LayerManager.Infrastructure
{
	public static class SolutionBuilder
	{
		public static Solution CreateSolutionFromMasterLayer(SummaryLayerFile masterLayer, string projectName)
		{
			Solution solution = new Solution
			{
				ProjectName = projectName,
				Layers = new[]
				{
					new Solution.LayerRecord
					{
						Id = masterLayer.Id,
						Name = masterLayer.Name,
						Filter = null,
						Children = null
					}
				}
			};
			return solution;
		}

		public static Solution.LayerRecord CreateLayerRecordFromFilterLayer(SummaryLayerFile filterLayer)
		{
			var newLayerRecord = new Solution.LayerRecord
			{
				Id = filterLayer.Id,
				Name = filterLayer.Name,
				Filter = filterLayer.Filter,
				Children = null
			};
			return newLayerRecord;
		}
	}
}
