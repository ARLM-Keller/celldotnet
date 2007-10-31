using System;
using CellDotNet;
using CellDotNet.Spe;

namespace SciMark2Cell
{
	public class SparseCompRowSingleCell
	{
		public static void matmult(MainStorageArea y, int ysize, MainStorageArea val, int valsize, MainStorageArea row, int rowsize, MainStorageArea col, int colsize, MainStorageArea x, int xsize, int NUM_ITERATIONS)
		{
			float[] yarr = new float[ysize];
			float[] valarr = new float[valsize];
			int[] rowarr = new int[rowsize];
			int[] colarr = new int[colsize];
			float[] xarr = new float[xsize];

			Console.WriteLine(11);

			Mfc.Get(yarr, y);
			Console.WriteLine(12);
			Mfc.Get(valarr, val);
			Console.WriteLine(13);
			Mfc.Get(rowarr, row);
			Console.WriteLine(14);
			Mfc.Get(colarr, col);
			Console.WriteLine(15);
			Mfc.Get(xarr, x);

			Console.WriteLine(19);

			matmult_inner(yarr, valarr, rowarr, colarr, xarr, NUM_ITERATIONS);
		}

		/// <summary>
		///  computes  a matrix-vector multiply with a sparse matrix
		///  held in compress-row format.  If the size of the matrix
		///  in MxN with nz nonzeros, then the val[] is the nz nonzeros,
		///  with its ith entry in column col[i].  The integer vector row[]
		///  is of size M+1 and row[i] points to the begining of the
		///  ith row in col[].  
		public static void matmult_inner(float[] y, float[] val, int[] row, int[] col, float[] x, int NUM_ITERATIONS)
		{
			int M = row.Length - 1;

			for (int reps = 0; reps < NUM_ITERATIONS; reps++)
			{

				for (int r = 0; r < M; r++)
				{
					float sum = 0.0f;
					int rowR = row[r];
					int rowRp1 = row[r + 1];
					for (int i = rowR; i < rowRp1; i++)
						sum += x[col[i]] * val[i];
					y[r] = sum;
				}
			}
		}
	}
}