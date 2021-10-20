using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace MeshEditor.Common
{
	public static class ProcessStarter
	{
		public static bool OpenBrowser(string url)
		{
			// workaround: https://github.com/mono/mono/issues/17204
			// see: https://github.com/dotnet/runtime/issues/21798


			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				//Process.Start(new ProcessStartInfo("cmd", $"/c start {url.Replace("&", "^&")}") { CreateNoWindow = true });
				Process.Start(url);
				return true;
			}
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				Process.Start("xdg-open", url);
				return true;
			}
			if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			{
				Process.Start("open", url);
				return true;
			}
			return false;
		}
	}
}
