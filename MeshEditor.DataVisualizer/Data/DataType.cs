using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MeshEditor.DataVisualizer.Data
{
	public class DataType : IEquatable<DataType>
	{
		public enum CompoundTypes
		{
			Scalar,
			Vector,
			Matrix,
			PlainDeformationMatrix,
			MainMatrix,
			LocalAxes
		}

		public class Component
		{
			public string Name { get; private set; }
			//public int Index { get; private set; }
			public Component(string name)
			{
				this.Name = name;
			}
			public override string ToString()
			{
				return Name;
			}
		}
		
		public string Name { get; private set; }
		public int ComponentCount { get { return (Components != null) ? Components.Length : 0; } }
		public Component[] Components { get; private set; }
		public CompoundTypes CompoundType { get; private set; }

		public string FileName { get; private set; }

		public long FilePosition { get; private set; }

		public DataType(string name, string filename, long filePosition, CompoundTypes componentComposition, params string[] componentNames)
		{
			Debug.Assert(!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(filename));

			this.Name = name;
			this.FileName = filename;
			this.FilePosition = filePosition;
			this.CompoundType = componentComposition;
            SetComponents(componentNames);
		}

        public void SetComponents(params string[] componentNames)
        {
            this.Components = new Component[componentNames.Length];
            for (int i = 0; i < componentNames.Length; i++)
            {
                this.Components[i] = new Component(componentNames[i]);
            }
        }

		public override string ToString()
		{
			return Name;
		}

		public override int GetHashCode()
		{
			int hash1 = (Name != null) ? Name.GetHashCode() : 0;
			int hash2 = (FileName != null) ? FileName.GetHashCode() : 0;
			return hash1 ^ hash2;
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as DataType);
		}

		public bool Equals(DataType other)
		{
			if (other == null)
				return false;
			return this.Name == other.Name && this.FileName == other.FileName;
		}

		public void AddGenericComponentNames()
		{
			string[] names = null;
			switch (CompoundType)
			{
				case DataType.CompoundTypes.Scalar:
					names = new[] { "value" };
					break;
				case DataType.CompoundTypes.Vector:
					names = new[] {"X", "Y", "Z"}; // optional fourth component signed_module_value !!
					break;
				case DataType.CompoundTypes.Matrix:
					names = new[] { "Sxx", "Syy", "Szz", "Sxy", "Syz", "Sxz" }; // in 2D only four components !!
					break;
				case DataType.CompoundTypes.PlainDeformationMatrix:
					names = new[] { "Sxx", "Syy", "Sxy", "Szz" };
					break;
				case DataType.CompoundTypes.MainMatrix:
					names = new[] { "Si", "Sii", "Siii", "Vix", "Viy", "Viz", "Viix", "Viiy", "Viiz", "Viiix", "Viiiy", "Viiiz" };
					break;
				case DataType.CompoundTypes.LocalAxes:
					names = new[] { "euler_ang_1", "euler_ang_2", "euler_ang_3" };
					break;
				default:
					break;
			}
			Debug.Assert(names != null);
			if (names != null)
				SetComponents(names);
		}
	}
}
