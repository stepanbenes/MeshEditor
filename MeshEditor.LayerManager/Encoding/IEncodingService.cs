using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Data;

namespace MeshEditor.LayerManager.Encoding
{
	public interface IEncodingService
	{
		string Encode<T>(T[] values, TrimOptions trimOptions, out EncodingParameters encodingParameters) where T : struct;
		T[] Decode<T>(string data, TrimOptions trimOptions, EncodingParameters encodingParameters) where T : struct;
	}
}
