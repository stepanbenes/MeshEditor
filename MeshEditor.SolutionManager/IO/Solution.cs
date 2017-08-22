using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Filters;
using Newtonsoft.Json;
using MeshEditor.Common.Extensions;
using MeshEditor.LayerManager.Import;

namespace MeshEditor.SolutionManager.IO
{
	class Solution : SolutionInfo, ISolutionDescription
	{
		#region class Layer

		public class Layer : ILayerInfo
		{
			private static ILayerInfo[] emptyLayerInfoArray;

			public Guid Id { get; set; }
			public string Name { get; set; }
			public string FilterType { get; set; }
			[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
			public Layer[] Children { get; set; }

			string ILayerInfo.FilterType => FilterType ?? "<null>";
			IReadOnlyList<ILayerInfo> ILayerInfo.Children => Children ?? emptyLayerInfoArray ?? (emptyLayerInfoArray = new ILayerInfo[0]);

			public override int GetHashCode() => Id.GetHashCode();
			public override bool Equals(object obj) => obj is ILayerInfo other && this.Id.Equals(other.Id);
		}

		#endregion

		#region Properties

		public AnalysisResult[] Results { get; set; }
		public Layer[] Layers { get; set; }

		IReadOnlyList<ILayerInfo> ISolutionDescription.Layers => Layers;

		#endregion

		#region Public static methods

		public static Solution CreateNewByAddingLayer(Solution solution, Layer layerToAdd, Guid? parentLayerId)
		{
			Solution newSolution = createShallowCopy(solution);
			newSolution.Layers = parentLayerId.HasValue ?
					solution.Layers.Select(layer => cloneLayerAppend(layer, parentLayerId.Value, layerToAdd)).ToArray() :
					(solution.Layers?.Select(layer => cloneLayer(layer))).EmptyIfNull().Append(layerToAdd).ToArray();
			return newSolution;
		}

		public static Solution CreateNewByDeletingLayer(Solution solution, Guid layerToDeleteId)
		{
			Solution newSolution = createShallowCopy(solution);
			newSolution.Layers = solution.Layers.Where(layer => layer.Id != layerToDeleteId).Select(layer => cloneLayerExcept(layer, layerToDeleteId)).ToArray();
			return newSolution;
		}

		#endregion

		#region Private methods

		private static Solution createShallowCopy(Solution toCopy)
		{
			Solution newSolution = new Solution
			{
				Id = toCopy.Id,
				ProjectName = toCopy.ProjectName,
				Location = toCopy.Location,
				Results = toCopy.Results,
				Layers = toCopy.Layers
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

		#endregion
	}
}
