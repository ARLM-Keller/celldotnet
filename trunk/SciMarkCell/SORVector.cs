using CellDotNet;
using CellDotNet.Spe;

namespace SciMark2Cell
{
	public class SORVector
	{
		public static void execute(float omega, MainStorageArea Gv_msa, int M, int N, int iterations)
		{
			VectorF4[] Gv = new VectorF4[M * N];

			Mfc.Get(Gv, Gv_msa);

			execute_inner(omega, Gv, M, N, iterations);
		}

		/// <summary>
		/// Vectorization of SOR is acomplished by reorganising the order the values is updated.
		/// Matrix mapping:
		/// Original matrix G[M][N] (N must be souch that (N-2) % 4 == 0)
		/// S = (N-2) / 4
		/// Vectro matix Gv[M][S+1]
		/// For n > 0, n <= S, m >= 0, m < M
		/// Gv[m][n].E1 == G[m][n]
		/// Gv[m][n].E2 == G[m][n+S]
		/// Gv[m][n].E3 == G[m][n+2*S]
		/// Gv[m][n].E4 == G[m][n+3*S]
		/// First collum in Gv has values for both first and last element from original matrix layout.
		/// Gv[m][0].E1 == G[m][0], Gv[m][0].E4 == G[m][N-1]
		/// </summary>
		/// <param name="omega">omega</param>
		/// <param name="Gv">Vectorized matrix.</param>
		/// <param name="M">Height of vector matrix.</param>
		/// <param name="N">Weidth of vector matrix.</param>
		/// <param name="iterations">Number of iterations</param>
		public static void execute_inner(float omega, VectorF4[] Gv, int M, int N, int iterations)
		{
			VectorF4 omega_over_four = VectorF4.Splat(omega * 0.25f);
			VectorF4 one_minus_omega = VectorF4.Splat(1.0f - omega);

			int Mm1 = M - 1;
			int Nm1 = N - 1;

			for (int p = 0; p < iterations; p++)
			{
				for (int i = 1; i < Mm1; i++)
				{
					int Nim1 = (i - 1)*N;
					int Nip1 = (i + 1)*N;
					int Ni = i*N;

					VectorF4 Gjm1 = new VectorF4(Gv[Ni].E1, Gv[Ni+Nm1-1].E1, Gv[Ni+Nm1-1].E2, Gv[Ni+Nm1-1].E3);
					VectorF4 Gjp1 = new VectorF4(Gv[Ni+1].E2, Gv[Ni+1].E3, Gv[Ni+1].E4, Gv[Ni].E4);

					Gv[Ni + 1] = omega_over_four * (Gv[Nim1 + 1] + Gv[Nip1 + 1] + Gjm1 + Gv[Ni + 1 + 1]) + one_minus_omega * Gv[Ni + 1];

					for (int j = 2; j < Nm1; j++)
					{
						Gv[Ni + j] = omega_over_four * (Gv[Nim1 + j] + Gv[Nip1 + j] + Gv[Ni + j - 1] + Gv[Ni + j + 1]) + one_minus_omega * Gv[Ni + j];
					}

					Gv[Ni + Nm1] = omega_over_four * (Gv[Nim1 + Nm1] + Gv[Nip1 + Nm1] + Gv[Ni + Nm1 - 1] + Gjp1) + one_minus_omega * Gv[Ni + Nm1];
				}
			}
		}
	}
}