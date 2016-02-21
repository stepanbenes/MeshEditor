using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MeshEditor.IO
{
	public abstract class JsonFileParserBase
	{
		int lineNumber;

		public JsonFileParserBase(string filename)
		{
			Filename = filename;
		}

		public string Filename { get; }

		public int CurrentLineNumber => lineNumber;

		protected TObject ParseInput<TObject>()
		{
			// deserialize JSON directly from a file
			using (var reader = File.OpenText(Filename))
			using (var jsonReader = new JsonTextReader(reader))
			{
				JsonSerializer serializer = new JsonSerializer();
				var result = serializer.Deserialize<TObject>(jsonReader);
				lineNumber = jsonReader.LineNumber;
				return result;
			}
		}

		protected static TItem[] convertBase64StringToArray<TItem>(string base64string) where TItem : struct
		{
			byte[] bytes = Convert.FromBase64String(base64string);
			TItem[] values = new TItem[bytes.Length / System.Runtime.InteropServices.Marshal.SizeOf<TItem>()];
			Buffer.BlockCopy(bytes, 0, values, 0, bytes.Length);
			return values;
		}
	}
}
