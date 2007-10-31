using CellDotNet;
using CellDotNet.Spe;

namespace SciMark2Cell
{
	public class MonteCarloVector
	{
		public static float integrate(int seed, int Num_samples)
		{
			int iterations = (Num_samples/4) + 1;

			RandomVector R = new RandomVector(Int32Vector.Splat(seed));

			Int32Vector under_curve = Int32Vector.Splat(0);

			Int32Vector _zerro = Int32Vector.Splat(0);
			Int32Vector _one = Int32Vector.Splat(1);
			Float32Vector unitVector = Float32Vector.Splat(1f);

			for (int count = 0; count < iterations; count++)
			{
				Float32Vector x = R.nextFloat();
				Float32Vector y = R.nextFloat();

				under_curve += SpuMath.CompareGreaterThanAndSelect(unitVector, x * x + y * y, _one, _zerro);
			}

			return ((float)(under_curve.E1 + under_curve.E2 + under_curve.E3 + under_curve.E4) / (float)(iterations*4)) * 4.0f;
		}
	}
}
