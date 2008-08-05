using CellDotNet;
using CellDotNet.Spe;

namespace SciMark2Cell
{
	public class MonteCarloVectorDynamicUnroled
	{
		public static float integrate(int seed, int Num_samples)
		{
			int inneriterations = 256*4;
			int iterations = (Num_samples / (4 * inneriterations)) + 1;

			RandomVector R = new RandomVector(VectorI4.Splat(seed));

			VectorI4 under_curve = VectorI4.Splat(0);

			VectorI4 _zerro = VectorI4.Splat(0);
			VectorI4 _one = VectorI4.Splat(1);
			VectorF4 unitVector = VectorF4.Splat(1f);

			VectorF4[] xs = new VectorF4[inneriterations];
			VectorF4[] ys = new VectorF4[inneriterations];

			for (int count = 0; count < iterations; count++)
			{
				R.nextFloats(xs);
				R.nextFloats(ys);

				for (int i = 0; i < inneriterations; i++)
				{
					VectorF4 x = xs[i];
					VectorF4 y = ys[i];

					under_curve += SpuMath.CompareGreaterThanAndSelect(unitVector, x * x + y * y, _one, _zerro);
				}
			}

			return ((float)(under_curve.E1 + under_curve.E2 + under_curve.E3 + under_curve.E4) / (float)(iterations * (4 * inneriterations))) * 4.0f;
		}
	}
}
