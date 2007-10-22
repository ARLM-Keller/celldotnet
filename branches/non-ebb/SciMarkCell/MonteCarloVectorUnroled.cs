using CellDotNet;

namespace SciMark2Cell
{
	public class MonteCarloVectorUnroled
	{
		public static float integrate(int seed, int Num_samples)
		{
			int inneriterations = 1024;
			int iterations = (Num_samples / (4 * inneriterations)) + 1;

			RandomVector R = new RandomVector(Int32Vector.Splat(seed));

			Int32Vector under_curve = Int32Vector.Splat(0);

			Int32Vector _zerro = Int32Vector.Splat(0);
			Int32Vector _one = Int32Vector.Splat(1);
			Float32Vector unitVector = Float32Vector.Splat(1f);

			Float32Vector[] xs = new Float32Vector[inneriterations];
			Float32Vector[] ys = new Float32Vector[inneriterations];

			for (int count = 0; count < iterations; count++)
			{
				R.nextFloats(xs);
				R.nextFloats(ys);

				for (int i = 0; i < inneriterations; i++)
				{
					Float32Vector x = xs[i];
					Float32Vector y = ys[i];

					under_curve += SpuMath.CompareGreaterThanAndSelect(unitVector, x*x + y*y, _one, _zerro);
				}
			}

			return ((float)(under_curve.E1 + under_curve.E2 + under_curve.E3 + under_curve.E4) / (float)(iterations * (4*inneriterations))) * 4.0f;
		}
	}
}
