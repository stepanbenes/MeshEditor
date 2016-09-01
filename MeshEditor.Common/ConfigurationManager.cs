using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MeshEditor.Common
{
	public class ConfigurationManager
	{
		readonly string configurationFileFullPath;
		readonly Dictionary<string, JToken> configurations;

		public ConfigurationManager()
		{
			string configurationDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MeshEditor");
			if (!Directory.Exists(configurationDirectory))
			{
				Directory.CreateDirectory(configurationDirectory);
			}

			configurationFileFullPath = Path.Combine(configurationDirectory, "config.json");
			configurations = readConfigurations(configurationFileFullPath);
		}

		private static Dictionary<string, JToken> readConfigurations(string configurationFileFullPath)
		{
			if (!File.Exists(configurationFileFullPath))
				return new Dictionary<string, JToken>();
			return JsonConvert.DeserializeObject<Dictionary<string, JToken>>(File.ReadAllText(configurationFileFullPath));

			//var configurations = new Dictionary<string, JToken>();
			//if (File.Exists(configurationFileFullPath))
			//{
			//	using (var streamReader = new StreamReader(configurationFileFullPath))
			//	using (var jsonReader = new JsonTextReader(streamReader))
			//	{
			//		while (jsonReader.Read())
			//		{
			//			if (jsonReader.TokenType == JsonToken.PropertyName)
			//			{
			//				string key = (string)jsonReader.Value;
			//				jsonReader.Read();
			//				configurations[key] = JToken.Load(jsonReader);
			//			}
			//		}
			//	}
			//}
		}

		private void writeConfigurations()
		{
			var json = JsonConvert.SerializeObject(configurations, Formatting.Indented);
			File.WriteAllText(configurationFileFullPath, json);

			//using (var streamWriter = new StreamWriter(configurationFileFullPath))
			//using (var jsonWriter = new JsonTextWriter(streamWriter))
			//{
			//	jsonWriter.WriteStartObject();
			//	foreach (var configPair in configurations)
			//	{
			//		jsonWriter.WriteWhitespace(Environment.NewLine);
			//		jsonWriter.WritePropertyName(configPair.Key);
			//		jsonWriter.WriteWhitespace(" ");
			//		jsonWriter.WriteRawValue(configPair.Value.ToString(Formatting.Indented));
			//	}
			//	jsonWriter.WriteWhitespace(Environment.NewLine);
			//	jsonWriter.WriteEndObject();
			//}
		}

		public T ReadConfigurationObject<T>(string key /*propertyName*/)
		{
			JToken jToken;
			if (!configurations.TryGetValue(key, out jToken))
				return default(T);
			return jToken.ToObject<T>();
		}

		public void WriteConfigurationObject(string key /*propertyName*/, object configurationObject)
		{
			configurations[key] = JToken.FromObject(configurationObject);

			// write assembly version
			var assemblyVersion = Assembly.GetAssembly(typeof(ConfigurationManager)).GetName().Version;
			configurations["assemblyVersion"] = JToken.FromObject(assemblyVersion.ToString());

			writeConfigurations();
		}
	}
}
