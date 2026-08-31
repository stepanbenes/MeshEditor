using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.LayerManager.Compression
{
    public static class RedSvdDriver
	{
		private const string NativeLibraryName =
#if WINDOWS
			"redsvd.dll";
#else
			"redsvd";
#endif

		[DllImport(NativeLibraryName, EntryPoint = "ComputeSvdExact", CallingConvention = CallingConvention.Cdecl)]
		private static extern void ComputeSvdExact(double[] inputMatrix_RowMajor, int numberOfRows, int numberOfColumns, double[] singularValues, double[] U_VT_ColumnMajor);

		[DllImport(NativeLibraryName, EntryPoint = "ComputeSvdRandomized", CallingConvention = CallingConvention.Cdecl)]
		private static extern void ComputeSvdRandomized(double[] inputMatrix_RowMajor, int numberOfRows, int numberOfColumns, int rank, double[] singularValues, double[] U_VT_ColumnMajor);


		public static void ComputeSvdExact(double[] inputMatrix_RowMajor, int numberOfRows, int numberOfColumns, out double[] singularValues, out double[] U_VT_ColumnMajor)
		{
			Debug.Assert(inputMatrix_RowMajor != null);
			Debug.Assert(inputMatrix_RowMajor.Length == numberOfRows * numberOfColumns);

			int rank = Math.Min(numberOfRows, numberOfColumns);
			singularValues = new double[rank];
			U_VT_ColumnMajor = new double[numberOfRows * rank + rank * numberOfColumns];

			ComputeSvdExact(inputMatrix_RowMajor, numberOfRows, numberOfColumns, singularValues, U_VT_ColumnMajor);
		}

		public static void ComputeSvdRandomized(double[] inputMatrix_RowMajor, int numberOfRows, int numberOfColumns, int rank, out double[] singularValues, out double[] U_VT_ColumnMajor)
		{
			Debug.Assert(inputMatrix_RowMajor != null);
			Debug.Assert(inputMatrix_RowMajor.Length == numberOfRows * numberOfColumns);
			Debug.Assert(rank <= Math.Min(numberOfRows, numberOfColumns));

			singularValues = new double[rank];
			U_VT_ColumnMajor = new double[numberOfRows * rank + rank * numberOfColumns];

			ComputeSvdRandomized(inputMatrix_RowMajor, numberOfRows, numberOfColumns, rank, singularValues, U_VT_ColumnMajor);
		}
	}
}
