using System;
using System.Collections.Generic;
using System.Text;

namespace MeshEditor.CoreInterface
{
	/// <summary>
	/// delegat umoznujici zobrazeni chyby
	/// </summary>
	public delegate void ShowErrorEventHandler(object sender, ShowErrorEventArgs ea);

	/// <summary>
	/// argument pro reprezentaci nastale chyby
	/// </summary>
	public class ShowErrorEventArgs : EventArgs
	{
		private string caption, message;

		public string Caption
		{
			get { return caption; }
		}

		public string Message
		{
			get { return message; }
		}
		
		public ShowErrorEventArgs(string caption, string message)
		{
			this.caption = caption;
			this.message = message;
		}
	}
}
