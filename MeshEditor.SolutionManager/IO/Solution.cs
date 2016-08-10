using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Filters;
using Newtonsoft.Json;
using MeshEditor.LayerManager.Common;

namespace MeshEditor.SolutionManager.IO
{
	class Solution : SolutionInfo
	{
		public class Layer : ILayerInfo
		{
			public Guid Id { get; set; }
			public string Name { get; set; }
			public string FilterType { get; set; }
			[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
			public Layer[] Children { get; set; }

			string ILayerInfo.FilterType => FilterType ?? "<null>";
			IEnumerable<ILayerInfo> ILayerInfo.Children => Children ?? Enumerable.Empty<ILayerInfo>();

			public override int GetHashCode()
			{
				return Id.GetHashCode();
			}

			public override bool Equals(object obj)
			{
				var other = obj as ILayerInfo;
				if (other == null)
					return false;
				return this.Id.Equals(other.Id);
			}
		}

		public Layer[] Layers { get; set; }

		public static Solution CreateNewByAddingLayer(Solution solution, Layer layerToAdd, Guid? parentLayerId)
		{
			Solution newSolution = new Solution
			{
				Id = solution.Id,
				ProjectName = solution.ProjectName,
				Layers = parentLayerId.HasValue ?
					solution.Layers.Select(layer => cloneLayerAppend(layer, parentLayerId.Value, layerToAdd)).ToArray() :
					(solution.Layers?.Select(layer => cloneLayer(layer))).EmptyIfNull().Append(layerToAdd).ToArray()
			};
			return newSolution;
		}

		public static Solution CreateNewByDeletingLayer(Solution solution, Guid layerToDeleteId)
		{
			Solution newSolution = new Solution
			{
				Id = solution.Id,
				ProjectName = solution.ProjectName,
				Layers = solution.Layers.Where(layer => layer.Id != layerToDeleteId).Select(layer => cloneLayerExcept(layer, layerToDeleteId)).ToArray()
			};
			return newSolution;
		}

		private static Layer cloneLayer(Layer layer)
		{
			return new Layer
			{
				Id = layer.Id,
				Name = layer.Name,
				FilterType = layer.FilterType,
				Children = layer.Children?.Select(child => cloneLayer(child)).ToArray()
			};
		}

		private static Layer cloneLayerExcept(Layer layer, Guid exceptLayerId)
		{
			return new Layer
			{
				Id = layer.Id,
				Name = layer.Name,
				FilterType = layer.FilterType,
				Children = layer.Children?.Where(child => child.Id != exceptLayerId).Select(child => cloneLayerExcept(child, exceptLayerId)).NullIfEmpty()?.ToArray()
			};
		}

		private static Layer cloneLayerAppend(Layer layer, Guid parentLayerId, Layer layerToAppend)
		{
			var clone = new Layer
			{
				Id = layer.Id,
				Name = layer.Name,
				FilterType = layer.FilterType,
				Children = layer.Children?.Select(child => cloneLayerAppend(child, parentLayerId, layerToAppend)).NullIfEmpty()?.ToArray()
			};
			if (layer.Id == parentLayerId)
			{
				clone.Children = clone.Children.EmptyIfNull().Append(layerToAppend).ToArray();
			}
			return clone;
		}
	}
}
