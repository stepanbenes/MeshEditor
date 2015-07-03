using MeshEditor.DataVisualizer.Mathematics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MeshEditor.DataVisualizer.Data
{
	public class DeformationScale : INotifyPropertyChanged
	{

		public enum Types
		{
			Absolute,
			Relative
		}

		#region Fields, constructor

		bool drawDeformed;
		float multiplier;
		private bool autoUpdateMultiplier = true;
		double maxDisplacement;
		DataIndex deformationDataIndex;
		Types type = Types.Relative;
		float relativeScale = 0.1f; // 10%
		float absoluteScale = 1.0f; // [-]

		#endregion

		#region Properties

		public bool DrawDeformed
		{
			get { return drawDeformed; }
			set
			{
				if (drawDeformed != value)
				{
					drawDeformed = value;
					if (drawDeformed)
						updateMultiplier();
					OnPropertyChanged("DrawDeformed");
				}
			}
		}

		public Types Type
		{
			get { return type; }
			set
			{
				if (type != value)
				{
					type = value;
					OnPropertyChanged("Type");
				}
			}
		}

		public float Multiplier
		{
			get { return multiplier; }
		}

		public float RelativeScale
		{
			get { return relativeScale; }
			set
			{
				if (relativeScale != value)
				{
					relativeScale = value;
					updateMultiplier();
					OnPropertyChanged("RelativeScale");
				}
			}
		}

		public float AbsoluteScale
		{
			get { return absoluteScale; }
			set
			{
				if (absoluteScale != value)
				{
					absoluteScale = value;
					updateMultiplier();
					OnPropertyChanged("AbsoluteScale");
				}
			}
		}

		public bool AutoUpdateMultiplier
		{
			get { return autoUpdateMultiplier; }
			set
			{
				if (autoUpdateMultiplier != value)
				{
					autoUpdateMultiplier = value;
					if (autoUpdateMultiplier)
						updateMultiplier();
					OnPropertyChanged("AutoUpdateMultiplier");
				}
			}
		}

		public DataIndex DeformationDataIndex
		{
			get { return deformationDataIndex; }
		}

		#endregion

		#region Public methods

		public void SetDeformationDataIndex(DataIndex dataIndex, double maxDisplacement)
		{
			if (deformationDataIndex != dataIndex)
			{
				deformationDataIndex = dataIndex;
				this.maxDisplacement = maxDisplacement;
				if (autoUpdateMultiplier)
					updateMultiplier();
				OnPropertyChanged("DeformationDataIndex");
			}
		}

		#endregion

		#region Private methods

		private void updateMultiplier()
		{
			if (type == Types.Relative)
			{
				Debug.Assert(relativeScale >= 0f && relativeScale <= 1f);

				if (maxDisplacement < Common.Epsilon)
					multiplier = 1.0f;
				else
					multiplier = (float)( /*modelSize * */ relativeScale / maxDisplacement);

				// update absolute scale
				absoluteScale = multiplier;
			}
			else
			{
				Debug.Assert(type == Types.Absolute);
				multiplier = absoluteScale;

				// update relative scale
				relativeScale = Math.Max(Math.Min((float)(multiplier * maxDisplacement), 1f), 0f);
			}
		}

		#endregion

		#region INotifyPropertyChanged

		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged(string propertyName)
		{
			if (!DrawDeformed && propertyName != "DrawDeformed") // do not update if deformations are turned off
				return;
			var handler = PropertyChanged;
			if (handler != null)
				handler(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion

	}
}
