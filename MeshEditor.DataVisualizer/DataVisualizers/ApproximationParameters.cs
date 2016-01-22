using MeshEditor.CoreInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MeshEditor.DataVisualizer
{
	public class ApproximationParameters : IApproximationParameters
	{
		bool loadInternalEntities;
		bool compressTime;
		GaussPointsExtrapolationStrategy gpExptrapolationStrategy;

		public ApproximationParameters(bool loadInternalEntities, bool compressTime = false, GaussPointsExtrapolationStrategy strategy = GaussPointsExtrapolationStrategy.NearestGaussPoint)
		{
			this.loadInternalEntities = loadInternalEntities;
			this.compressTime = compressTime;
			this.gpExptrapolationStrategy = strategy;
		}

		public bool LoadInternalEntities
		{
			get { return loadInternalEntities; }
			set { loadInternalEntities = value; }
		}

		public bool CompressTime
		{
			get { return compressTime; }
			set { compressTime = value; }
		}

		public GaussPointsExtrapolationStrategy GPExptrapolationStrategy
		{
			get { return gpExptrapolationStrategy; }
			set { gpExptrapolationStrategy = value; }
		}
	}
}
