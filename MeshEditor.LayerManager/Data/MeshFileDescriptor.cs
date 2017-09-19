using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.LayerManager.Data
{
	public interface IMeshFileDescriptor
	{
		int Index { get; }
		IReadOnlyList<decimal> TimeSteps { get; }
		IReadOnlyList<IDataDescription> Attributes { get; }
	}

	public class MeshFileDescriptor : IMeshFileDescriptor
	{
		public int Index { get; set; }
		public decimal[] TimeSteps { get; set; }
		public DataFileDescriptor[] Attributes { get; set; }

		IReadOnlyList<decimal> IMeshFileDescriptor.TimeSteps => TimeSteps;
		IReadOnlyList<IDataDescription> IMeshFileDescriptor.Attributes => Attributes;
	}
}
