/// <license>
/// This is a port of the SciMark2a Java Benchmark to C# by
/// Chris Re (cmr28@cornell.edu) and Werner Vogels (vogels@cs.cornell.edu)
/// 
/// For details on the original authors see http://math.nist.gov/scimark2
/// 
/// This software is likely to burn your processor, bitflip your memory chips
/// anihilate your screen and corrupt all your disks, so you it at your
/// own risk.
/// </license>

using System;

namespace SciMark2Cell
{
	public class SparseCompRowSingle
	{
		// multiple iterations used to make kernel 
		// have roughly same granulairty as other 
		// Scimark kernels	
		public static float num_flops(int N, int nz, int num_iterations)
		{
			/* Note that if nz does not divide N evenly, then the
			actual number of nonzeros used is adjusted slightly.
			*/
			int actual_nz = (nz / N) * N;
			return (actual_nz) * 2.0f * (num_iterations);
		}

		/// <summary>
		///  computes  a matrix-vector multiply with a sparse matrix
		///  held in compress-row format.  If the size of the matrix
		///  in MxN with nz nonzeros, then the val[] is the nz nonzeros,
		///  with its ith entry in column col[i].  The integer vector row[]
		///  is of size M+1 and row[i] points to the begining of the
		///  ith row in col[].  
		public static void matmult(float[] y, float[] val, int[] row, int[] col, float[] x, int NUM_ITERATIONS)
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