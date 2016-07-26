using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Serialization;

namespace MeshEditor.SolutionManager.Configuration
{
	class ConfigLoader
	{
		string defaultConfigFileDirectory;

		public ConfigLoader(string defaultConfigFileDirectory = null)
		{
			this.defaultConfigFileDirectory = defaultConfigFileDirectory;
		}

		public Config ReadConfiguration(string configFile = null)
		{
			using (var stream = getConfigFileStream(configFile))
			{
				ISerializationService serializer = new JsonSerializationService();
				return serializer.Deserialize<Config>(stream);
			}
		}

		public async Task<Config> ReadConfigurationAsync(CancellationToken cancellationToken, string configFile = null)
		{
			using (var stream = getConfigFileStream(configFile))
			{
				ISerializationService serializer = new JsonSerializationService();
				return await serializer.DeserializeAsync<Config>(stream, cancellationToken);
			}
		}

		private Stream getConfigFileStream(string configFile)
		{
			string configFileAbsolutePath;
			if (configFile == null)
			{
				configFileAbsolutePath = Path.Combine(defaultConfigFileDirectory ?? Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "config.json");
			}
			else
			{
				configFileAbsolutePath = (Path.IsPathRooted(configFile)) ? configFile : Path.Combine(defaultConfigFileDirectory ?? Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), configFile);
			}

			return new FileStream(configFileAbsolutePath, FileMode.Open, FileAccess.Read, FileShare.Read);
		}
	}
}
