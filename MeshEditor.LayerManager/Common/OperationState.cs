using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.LayerManager.Common
{
	public struct OperationState
	{
		public string State { get; }
		public float? PercentDone { get; }

		public OperationState(string state)
		{
			State = state;
			PercentDone = null;
		}

		public OperationState(string state, float percentDone)
		{
			State = state;
			PercentDone = percentDone;
		}
	}
}
