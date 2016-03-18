using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Data;

namespace MeshEditor.LayerManager.Compression
{
	public interface ICompressionService
	{
		string TrimAndEncode<T>(T[] values) where T : struct;
		T[] DecodeAndExpand<T>(string data, int requestedLength) where T : struct;
		string Encode<T>(T[] values) where T : struct;
		T[] Decode<T>(string data) where T : struct;

		string CompressAndEncode(double[] values, out CompressionDescriptor compressionParameters);
		double[] DecodeAndDecompress(string data, CompressionDescriptor compressionParameters);
	}
}
