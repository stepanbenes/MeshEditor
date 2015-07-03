using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MeshEditor.DataVisualizer.Data
{
	public class DataInfo : IEquatable<DataInfo>
	{
		public DataType DataType { get; private set; }
		public string AnalysisName { get; private set; } /**/ // necessary?
		public double Time { get; private set; }
		public DataLocation Location { get; private set; }

		public GaussPointsInfo LocationInfo { get; set; }

		public DataInfo(DataType dataType, string analysisName, double time, DataLocation location)
		{
			this.DataType = dataType;
			this.AnalysisName = analysisName;
			this.Time = time;
			this.Location = location;
		}

		public override string ToString()
		{
			return string.Format("Result \"{0}\" \"{1}\" {2} {3} On{4}", ((DataType != null) ? DataType.Name : string.Empty), AnalysisName, Time, DataType.CompoundType, Location);
		}

		public override int GetHashCode()
		{
			int hash1 = DataType.GetHashCode();
			int hash2 = Time.GetHashCode();
			//int hash3 = Location.GetHashCode();
			//int hash4 = AnalysisName != null ? AnalysisName.GetHashCode() : 0;
			return hash1 ^ hash2;
		}

		public override bool Equals(object obj)
		{
			return this.Equals(obj as DataInfo);			
		}

		public bool Equals(DataInfo other)
		{
			if (other == null)
				return false;
			return this.DataType.Equals(other.DataType) && this.Time == other.Time && this.Location == other.Location && this.AnalysisName == other.AnalysisName;
		}

		public bool EqualsButTime(DataInfo other)
		{
			if (other == null)
				return false;
			return this.DataType.Equals(other.DataType) && this.Location == other.Location && this.AnalysisName == other.AnalysisName;
		}
	}
}
