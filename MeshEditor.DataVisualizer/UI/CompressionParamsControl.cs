using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Globalization;

namespace MeshEditor.DataVisualizer.UI
{
	public partial class CompressionParamsControl : UserControl
	{
		public CompressionParamsControl()
		{
			InitializeComponent();
			comboBoxCompressionMethod.SelectedIndex = 0;
			radioButtonError.Checked = true;
			textBoxNRMSD.Text = 1.0E-4.ToString("0.0E0");
			trackBarCompressionFactor.Value = 10;
			updateUI();
		}

		private void comboBoxCompressionMethod_SelectedIndexChanged(object sender, EventArgs e)
		{
			checkBoxMergeTimeSteps.Checked = comboBoxCompressionMethod.SelectedIndex > 0;
			updateUI();
		}

		private void checkBoxMergeTimeSteps_CheckedChanged(object sender, EventArgs e)
		{
			updateUI();
		}

		private void trackBarCompressionFactor_ValueChanged(object sender, EventArgs e)
		{
			labelCompressionFactor.Text = $"Compression factor: {trackBarCompressionFactor.Value} %";
			updateUI();
		}

		private void updateUI()
		{
			groupBoxSVDCompressionParameters.Enabled = comboBoxCompressionMethod.SelectedIndex > 0;
			textBoxKeyTimeSteps.Enabled = checkBoxMergeTimeSteps.Checked;
			labelCompressionFactor.Enabled = trackBarCompressionFactor.Enabled = radioButtonSize.Checked;
			labelNRMSD.Enabled = textBoxNRMSD.Enabled = radioButtonError.Checked;
		}

		public IEnumerable<string> GetCompressionParameters()
		{
			List<string> parameters = new List<string>();
			if (comboBoxCompressionMethod.SelectedIndex > 0)
			{
				parameters.Add((string)comboBoxCompressionMethod.SelectedItem);

				// add compression factor parameters
				if (radioButtonError.Checked)
				{
					parameters.Add("error");
					parameters.Add(textBoxNRMSD.Text);
				}
				else if (radioButtonSize.Checked)
				{
					parameters.Add("size");
					parameters.Add((trackBarCompressionFactor.Value * 0.01).ToString());
				}
				if (checkBoxSVDParameterRandomized.Checked)
				{
					parameters.Add("randomized");
				}
			}
			return parameters;
		}

		public IEnumerable<decimal> GetKeyTimeSteps()
		{
			if (checkBoxMergeTimeSteps.Checked)
			{
				string[] tokens = textBoxKeyTimeSteps.Text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
				if (!tokens.Any())
					return Enumerable.Repeat(decimal.MaxValue, 1); // NOTE: was double.PositiveInfinity, but decimal does not have infinity
				return tokens.Select(token => decimal.Parse(token, NumberStyles.Float, CultureInfo.InvariantCulture)); // TODO: handle parsing errors better
			}
			else
			{
				return Enumerable.Empty<decimal>();
			}
		}

		private void radioButtonPreference_CheckedChanged(object sender, EventArgs e)
		{
			updateUI();
		}
	}
}
