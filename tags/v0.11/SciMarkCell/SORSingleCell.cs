using CellDotNet;
using CellDotNet.Spe;

namespace SciMark2Cell
{
	public class SORSingleCell
	{
		public static void execute(float omega, MainStorageArea G_msa, int M, int N, int iterations)
		{
			float[] G = new float[M * N];

			Mfc.Get(G, G_msa);

			execute_inner(omega, G, M, N, iterations);
		}

		public static void execute_inner(float omega, float[] G, int M, int N, int iterations)
		{
			float omega_over_four = omega * 0.25f;
			float one_minus_omega = 1.0f - omega;

			// update interior points
			int Mm1 = M - 1;
			int Nm1 = N - 1;
			for (int p = 0; p < iterations; p++)
			{
				for (int i = 1; i < Mm1; i++)
				{
					int Nim1 = (i-1)* N;
					int Nip1 = (i+1) * N;
					int Ni = i*N;
					for (int j = 1; j < Nm1; j++)
					{
						G[Ni + j] = omega_over_four * (G[Nim1 + j] + G[Nip1 + j] + G[Ni + j - 1] + G[Ni + j + 1]) + one_minus_omega * G[Ni + j];
					}
				}
			}
		}
	}
}