using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Nodes;
using MeshEditor.Common.Serialization;

namespace MeshEditor.Common
{
	public static class ConfigurationManager
	{
		static readonly ConcurrentDictionary<string, JsonNode> configurations = new ConcurrentDictionary<string, JsonNode>();
		static string configurationFileFullPath;

		/// <summary>
		/// Directory of the configuration file (tests point it to a temporary directory to not use the user's configuration).
		/// </summary>
		public static string ConfigurationDirectory { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MeshEditor");

		public static void LoadConfiguration()
		{
			string configurationDirectory = ConfigurationDirectory;
			if (!Directory.Exists(configurationDirectory))
			{
				Directory.CreateDirectory(configurationDirectory);
			}

			const string configFileName = "config.json";

			configurationFileFullPath = Path.Combine(configurationDirectory, configFileName);

			var currentAssemblyVersion = Assembly.GetAssembly(typeof(ConfigurationManager)).GetName().Version;

			configurations.Clear(); // nothing of a previously loaded configuration
			foreach (var pair in readConfigurations(configurationFileFullPath))
			{
				configurations[pair.Key] = pair.Value;
			}

			// check if this is updated version compared to saved version of configuration file
			if (!configurations.TryGetValue("AssemblyVersion", out JsonNode assemblyVersionNode) ||
				!(assemblyVersionNode is JsonValue assemblyVersionValue && assemblyVersionValue.TryGetValue(out string assemblyVersionText)) ||
				!Version.TryParse(assemblyVersionText, out Version previousAssemblyVersion) ||
				currentAssemblyVersion > previousAssemblyVersion)
			{
				// if configuration file is of older version, try to copy template configurations from installation directory
				string templateConfigurationFileFullPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), configFileName);
				var templateConfigurations = readConfigurations(templateConfigurationFileFullPath);
				foreach (var pair in templateConfigurations) // copy to current configurations
				{
					if (!isEmpty(pair.Value))
						configurations[pair.Key] = configurations.TryGetValue(pair.Key, out JsonNode current) ? mergeTemplate(current, pair.Value) : pair.Value.DeepClone();
				}
			}

			// write current assembly version
			configurations["AssemblyVersion"] = JsonValue.Create(currentAssemblyVersion.ToString());
		}

		/// <summary>
		/// Value of the template (installation config.json) applied to the user's value: objects are merged property by property,
		/// and empty values of the template (null, "") keep the user's ones, so a template that leaves a value empty doesn't erase it.
		/// </summary>
		private static JsonNode mergeTemplate(JsonNode current, JsonNode template)
		{
			if (current is JsonObject currentObject && template is JsonObject templateObject)
			{
				var merged = (JsonObject)currentObject.DeepClone();
				foreach (var property in templateObject)
				{
					if (!isEmpty(property.Value))
						merged[property.Key] = merged[property.Key] is JsonNode currentValue ? mergeTemplate(currentValue, property.Value) : property.Value.DeepClone();
				}
				return merged;
			}
			return template.DeepClone();
		}

		private static bool isEmpty(JsonNode value) => value == null || (value is JsonValue jsonValue && jsonValue.TryGetValue(out string text) && text.Length == 0);

		private static Dictionary<string, JsonNode> readConfigurations(string configurationFileFullPath)
		{
			if (!File.Exists(configurationFileFullPath))
				return new Dictionary<string, JsonNode>();
			string json = File.ReadAllText(configurationFileFullPath);
			if (string.IsNullOrWhiteSpace(json))
				return new Dictionary<string, JsonNode>();
			return JsonSerializer.Deserialize<Dictionary<string, JsonNode>>(json, JsonOptions.Compact) ?? new Dictionary<string, JsonNode>();
		}

		public static T GetConfigurationObject<T>(string key /*propertyName*/)
		{
			JsonNode jsonNode;
			if (!configurations.TryGetValue(key, out jsonNode) || jsonNode == null)
				return default(T);
			return jsonNode.Deserialize<T>(JsonOptions.Compact);
		}

		public static void SetConfigurationObject(string key /*propertyName*/, object configurationObject)
		{
			// serialized as its runtime type, as by Newtonsoft.Json
			configurations[key] = configurationObject == null ? null : JsonSerializer.SerializeToNode(configurationObject, configurationObject.GetType(), JsonOptions.Compact);
		}

		public static void Save()
		{
			var json = JsonSerializer.Serialize(configurations, JsonOptions.Indented);
			File.WriteAllText(configurationFileFullPath, json);
		}
	}
}
