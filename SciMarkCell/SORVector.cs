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
using CellDotNet;

namespace SciMark2Cell
{
	public class SORVector
	{
		public static float num_flops(int M, int N, int num_iterations)
		{
			float Md = M;
			float Nd = N;
			float num_iterD = num_iterations;

			return (Md - 1) * (Nd - 1) * num_iterD * 6.0f;
		}

		public static void execute(float omega, MainStorageArea G, int M, int N, int num_iterations)
		{
			Float32Vector[] Gv = new Float32Vector[M * N];

			Mfc.Get(Gv, G);

			execute_inner(omega, Gv, M, N, num_iterations);
		}

		/// <summary>
		/// Matrix mapping:
		/// Original matrix G[M][N] (N must be souch that (N-2) % 4 == 0)
		/// S = (N-2) / 4
		/// Vectro matix Gv[M][S+1]
		/// For n > 0 && n <= S
		/// Gv[m][n].E1 == G[m][n]
		/// Gv[m][n].E2 == G[m][n+S]
		/// Gv[m][n].E3 == G[m][n+2*S]
		/// Gv[m][n].E4 == G[m][n+3*S]
		/// First collum in G has values for both first and last element from original matrix layout.
		/// Gv[m][0].E1 == G[m][0], Gv[m][0].E4 == G[m][N-1]
		/// </summary>
		/// <param name="omega"></param>
		/// <param name="G"></param>
		/// <param name="M">Height of vector matrix.</param>
		/// <param name="N">Weidth of vector matrix.</param>
		/// <param name="num_iterations">Number of iterations</param>
		public static void execute_inner(float omega, Float32Vector[] G, int M, int N, int num_iterations)
		{
			Float32Vector omega_over_four = Float32Vector.Splat(omega * 0.25f);
			Float32Vector one_minus_omega = Float32Vector.Splat(1.0f - omega);

			// update interior points
			//
			int Mm1 = M - 1;
			int Nm1 = N - 1;

			for (int p = 0; p < num_iterations; p++)
			{
				for (int i = 1; i < Mm1; i++)
				{
					int Nim1 = (i - 1)*N;
					int Nip1 = (i + 1)*N;
					int Ni = i*N;

					Float32Vector Gjm1 = new Float32Vector(G[Ni].E1, G[Ni+Nm1-1].E1, G[Ni+Nm1-1].E2, G[Ni+Nm1-1].E3);
					Float32Vector Gjp1 = new Float32Vector(G[Ni+1].E2, G[Ni+1].E3, G[Ni+1].E4, G[Ni].E4);

					G[Ni + 1] = omega_over_four * (G[Nim1 + 1] + G[Nip1 + 1] + Gjm1 + G[Ni + 1 + 1]) + one_minus_omega * G[Ni + 1];

					for (int j = 2; j < Nm1; j++)
					{
						G[Ni + j] = omega_over_four * (G[Nim1 + j] + G[Nip1 + j] + G[Ni + j - 1] + G[Ni + j + 1]) + one_minus_omega * G[Ni + j];
					}

					G[Ni + Nm1] = omega_over_four * (G[Nim1 + Nm1] + G[Nip1 + Nm1] + G[Ni + Nm1 - 1] + Gjp1) + one_minus_omega * G[Ni + Nm1];
				}
			}
		}
	}
}