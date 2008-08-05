using CellDotNet;
using CellDotNet.Spe;

namespace SciMark2Cell
{
	public class MonteCarloVector
	{
		public static float integrate(int seed, int Num_samples)
		{
			int iterations = (Num_samples/4) + 1;

			RandomVector R = new RandomVector(VectorI4.Splat(seed));

			VectorI4 under_curve = VectorI4.Splat(0);

			VectorI4 _zerro = VectorI4.Splat(0);
			VectorI4 _one = VectorI4.Splat(1);
			VectorF4 unitVector = VectorF4.Splat(1f);

			for (int count = 0; count < iterations; count++)
			{
				VectorF4 x = R.nextFloat();
				VectorF4 y = R.nextFloat();

				under_curve += SpuMath.CompareGreaterThanAndSelect(unitVector, x * x + y * y, _one, _zerro);
			}

			return ((float)(under_curve.E1 + under_curve.E2 + under_curve.E3 + under_curve.E4) / (float)(iterations*4)) * 4.0f;
		}
	}
}
