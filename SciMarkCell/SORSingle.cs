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

namespace SciMark2
{
	public class SORSingle
	{
		public static float num_flops(int M, int N, int num_iterations)
		{
			float Md = M;
			float Nd = N;
			float num_iterD = num_iterations;

			return (Md - 1) * (Nd - 1) * num_iterD * 6.0f;
		}

		public static void execute(float omega, float[][] G, int num_iterations)
		{
			int M = G.Length;
			int N = G[0].Length;

			float omega_over_four = omega * 0.25f;
			float one_minus_omega = 1.0f - omega;

			// update interior points
			//
			int Mm1 = M - 1;
			int Nm1 = N - 1;
			for (int p = 0; p < num_iterations; p++)
			{
				for (int i = 1; i < Mm1; i++)
				{
					float[] Gi = G[i];
					float[] Gim1 = G[i - 1];
					float[] Gip1 = G[i + 1];
					for (int j = 1; j < Nm1; j++)
						Gi[j] = omega_over_four * (Gim1[j] + Gip1[j] + Gi[j - 1] + Gi[j + 1]) + one_minus_omega * Gi[j];
				}
			}
		}
	}
}