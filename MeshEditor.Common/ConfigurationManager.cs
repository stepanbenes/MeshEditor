using System;
using System.Collections.Concurrent;
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
	public static class ConfigurationManager
	{
		static readonly string configurationFileFullPath;
		static readonly ConcurrentDictionary<string, JToken> configurations;

		static ConfigurationManager()
		{
			try
			{
				string configurationDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MeshEditor");
				if (!Directory.Exists(configurationDirectory))
				{
					Directory.CreateDirectory(configurationDirectory);
				}

				const string configFileName = "config.json";

				configurationFileFullPath = Path.Combine(configurationDirectory, configFileName);

				var currentAssemblyVersion = Assembly.GetAssembly(typeof(ConfigurationManager)).GetName().Version;

				configurations = readConfigurations(configurationFileFullPath);

				JToken assemblyVersionToken;
				Version previousAssemblyVersion;
				if (!configurations.TryGetValue("AssemblyVersion", out assemblyVersionToken) ||
					!Version.TryParse(assemblyVersionToken.ToObject<string>(), out previousAssemblyVersion) ||
					currentAssemblyVersion > previousAssemblyVersion)
				{
					string templateConfigurationFileFullPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), configFileName);
					var templateConfigurations = readConfigurations(templateConfigurationFileFullPath);
					foreach (var pair in templateConfigurations) // copy to current configurations
					{
						configurations[pair.Key] = pair.Value;
					}
				}

				// write current assembly version
				configurations["AssemblyVersion"] = JToken.FromObject(currentAssemblyVersion.ToString());
			}
			catch
			{
				configurations = new ConcurrentDictionary<string, JToken>();
			}
		}

		private static ConcurrentDictionary<string, JToken> readConfigurations(string configurationFileFullPath)
		{
			if (!File.Exists(configurationFileFullPath))
				return new ConcurrentDictionary<string, JToken>();
			string json = File.ReadAllText(configurationFileFullPath);
			return new ConcurrentDictionary<string, JToken>(JsonConvert.DeserializeObject<Dictionary<string, JToken>>(json));
		}

		public static T ReadConfigurationObject<T>(string key /*propertyName*/)
		{
			JToken jToken;
			if (!configurations.TryGetValue(key, out jToken))
				return default(T);
			return jToken.ToObject<T>();
		}

		public static void WriteConfigurationObject(string key /*propertyName*/, object configurationObject)
		{
			configurations[key] = JToken.FromObject(configurationObject);
		}

		public static void Save()
		{
			try
			{
				var json = JsonConvert.SerializeObject(configurations, Formatting.Indented);
				File.WriteAllText(configurationFileFullPath, json);
			}
			catch { }

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
	}
}
