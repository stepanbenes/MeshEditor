using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RedSvdInterface
{
    public static class Driver
    {
		// Example of usage:
		//double[] matrix =   {   1,  -2, 3,
		//							5,  8,  -1,
		//							2,   1,   1,
		//							-1 ,  4 , -3 };
		//double[] singularValues = new double[3];
		//double[] U_VT_ColumnMajor = new double[3 * 3 + 4 * 4];
		////RedSvdInterface.Driver.ComputeSvdExact(matrix, numberOfRows: 4, numberOfColumns: 3, singularValues: singularValues, U_VT_ColumnMajor: U_VT_ColumnMajor);
		//RedSvdInterface.Driver.ComputeSvdRandomized(matrix, numberOfRows: 4, numberOfColumns: 3, rank: 3, singularValues: singularValues, U_VT_ColumnMajor: U_VT_ColumnMajor);
		//	MessageBox.Show($"singular values: [{string.Join("; ", singularValues)}]");
		//	MessageBox.Show($"U_VT_columnwise: [{string.Join("; ", U_VT_ColumnMajor)}]");

		[DllImport("redsvd_native.dll")]
		public static extern void ComputeSvdExact(double[] dataValuesRowMajor, int numberOfRows, int numberOfColumns, double[] singularValues, double[] U_VT_ColumnMajor);
		[DllImport("redsvd_native.dll")]
		public static extern void ComputeSvdRandomized(double[] dataValuesRowMajor, int numberOfRows, int numberOfColumns, int rank, double[] singularValues, double[] U_VT_ColumnMajor);
	}
}
