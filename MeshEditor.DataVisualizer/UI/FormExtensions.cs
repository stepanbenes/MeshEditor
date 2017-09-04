using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MeshEditor.DataVisualizer.UI
{
	static class FormExtensions
	{
		public static Task<DialogResult> ShowAsync(this Form form, bool modal = false)
		{
			if (modal)
			{
				var dialogResult = form.ShowDialog();
				return Task.FromResult(dialogResult);
			}

			var tcs = new TaskCompletionSource<DialogResult>();
			
			form.FormClosed += (s, e) =>
			{
				tcs.SetResult(((Form)s).DialogResult);
			};

			form.Show();

			return tcs.Task;
		}
	}
}
