using MeshEditor.CoreInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MeshEditor.DataVisualizer
{
	public class ApproximationParameters : IApproximationParameters
	{
		ApproximationMethod method;
		bool loadInternalEntities;
		bool compressTime;
		double[] fixedTimes;
		GaussPointsExtrapolationStrategy gpExptrapolationStrategy;

		public ApproximationParameters(ApproximationMethod method, bool loadInternalEntities, bool compressTime = false, double[] fixedTimes = null, GaussPointsExtrapolationStrategy strategy = GaussPointsExtrapolationStrategy.NearestGaussPoint)
		{
			this.method = method;
			this.loadInternalEntities = loadInternalEntities;
			this.compressTime = compressTime;
			this.fixedTimes = fixedTimes;
			this.gpExptrapolationStrategy = strategy;
		}

		public ApproximationMethod Method
		{
			get { return method; }
			set { method = value; }
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

		public double[] FixedTimes
		{
			get { return fixedTimes; }
			set { fixedTimes = value; }
		}

		public GaussPointsExtrapolationStrategy GPExptrapolationStrategy
		{
			get { return gpExptrapolationStrategy; }
			set { gpExptrapolationStrategy = value; }
		}
	}
}
