using CellDotNet;
using CellDotNet.Spe;

namespace SciMark2Cell
{
	public class MonteCarloVectorUnroled
	{
		public static float integrate(int seed, int Num_samples)
		{
			int inneriterations = 256*4;
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

				for (int i = 0; i < inneriterations; )
				{
					Float32Vector x0 = xs[i];
					Float32Vector y0 = ys[i];
					i++;

					under_curve += SpuMath.CompareGreaterThanAndSelect(unitVector, SpuMath.MultiplyAdd(y0, y0, x0 * x0), _one, _zerro);
//					under_curve += SpuMath.CompareGreaterThanAndSelect(unitVector, x0 * x0 + y0 * y0, _one, _zerro);

					Float32Vector x1 = xs[i];
					Float32Vector y1 = ys[i];
					i++;

					under_curve += SpuMath.CompareGreaterThanAndSelect(unitVector, SpuMath.MultiplyAdd(y1, y1, x1 * x1), _one, _zerro);
//					under_curve += SpuMath.CompareGreaterThanAndSelect(unitVector, x1 * x1 + y1 * y1, _one, _zerro);

					Float32Vector x2 = xs[i];
					Float32Vector y2 = ys[i];
					i++;

					under_curve += SpuMath.CompareGreaterThanAndSelect(unitVector, SpuMath.MultiplyAdd(y2, y2, x2 * x2), _one, _zerro);
//					under_curve += SpuMath.CompareGreaterThanAndSelect(unitVector, x2 * x2 + y2 * y2, _one, _zerro);

					Float32Vector x3 = xs[i];
					Float32Vector y3 = ys[i];
					i++;

					under_curve += SpuMath.CompareGreaterThanAndSelect(unitVector, SpuMath.MultiplyAdd(y3, y3, x3 * x3), _one, _zerro);
//					under_curve += SpuMath.CompareGreaterThanAndSelect(unitVector, x3 * x3 + y3 * y3, _one, _zerro);
				}
			}

			return ((float)(under_curve.E1 + under_curve.E2 + under_curve.E3 + under_curve.E4) / (float)(iterations * (4 * inneriterations))) * 4.0f;
		}
	}
}
