using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
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
			string configFileAbsolutePath;
			if (configFile == null)
			{
				configFileAbsolutePath = Path.Combine(defaultConfigFileDirectory ?? Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "config.json");
			}
			else
			{
				configFileAbsolutePath = (Path.IsPathRooted(configFile)) ? configFile : Path.Combine(defaultConfigFileDirectory ?? Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), configFile);
			}

			using (var stream = new FileStream(configFileAbsolutePath, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				ISerializationService serializer = new JsonSerializationService();
				return serializer.Deserialize<Config>(stream);
			}
		}
	}
}
