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
		static readonly ConcurrentDictionary<string, JToken> configurations = new ConcurrentDictionary<string, JToken>();
		static string configurationFileFullPath;

		public static void LoadConfiguration()
		{
			string configurationDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MeshEditor");
			if (!Directory.Exists(configurationDirectory))
			{
				Directory.CreateDirectory(configurationDirectory);
			}

			const string configFileName = "config.json";

			configurationFileFullPath = Path.Combine(configurationDirectory, configFileName);

			var currentAssemblyVersion = Assembly.GetAssembly(typeof(ConfigurationManager)).GetName().Version;

			foreach (var pair in readConfigurations(configurationFileFullPath))
			{
				configurations[pair.Key] = pair.Value;
			}

			// check if this is updated version compared to saved version of configuration file
			if (!configurations.TryGetValue("AssemblyVersion", out JToken assemblyVersionToken) ||
				!Version.TryParse(assemblyVersionToken.ToObject<string>(), out Version previousAssemblyVersion) ||
				currentAssemblyVersion > previousAssemblyVersion)
			{
				// if configuration file is of older version, try to copy template configurations from installation directory
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

		private static Dictionary<string, JToken> readConfigurations(string configurationFileFullPath)
		{
			if (!File.Exists(configurationFileFullPath))
				return new Dictionary<string, JToken>();
			string json = File.ReadAllText(configurationFileFullPath);
			return JsonConvert.DeserializeObject<Dictionary<string, JToken>>(json);
		}

		public static T GetConfigurationObject<T>(string key /*propertyName*/)
		{
			JToken jToken;
			if (!configurations.TryGetValue(key, out jToken))
				return default(T);
			return jToken.ToObject<T>();
		}

		public static void SetConfigurationObject(string key /*propertyName*/, object configurationObject)
		{
			configurations[key] = JToken.FromObject(configurationObject);
		}

		public static void Save()
		{
			var json = JsonConvert.SerializeObject(configurations, Formatting.Indented);
			File.WriteAllText(configurationFileFullPath, json);
		}
	}
}
