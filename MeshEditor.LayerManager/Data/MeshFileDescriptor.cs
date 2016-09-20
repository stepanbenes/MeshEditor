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
		IReadOnlyList<IDataDescription> Attributes { get; }
		IReadOnlyList<IDataDescription> Results { get; }
	}

	public class MeshFileDescriptor : IMeshFileDescriptor
	{
		public int Index { get; set; }

		//public double[] TimeSteps { get; set; }

		public DataFileDescriptor[] Attributes { get; set; }
		public DataFileDescriptor[] Results { get; set; }

		IReadOnlyList<IDataDescription> IMeshFileDescriptor.Attributes => Attributes;
		IReadOnlyList<IDataDescription> IMeshFileDescriptor.Results => Results;
	}
}
