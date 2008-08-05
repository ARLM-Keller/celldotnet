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

				for (int i = 0; i < inneriterations; )
				{
					VectorF4 x0 = xs[i];
					VectorF4 y0 = ys[i];
					i++;

					under_curve += SpuMath.CompareGreaterThanAndSelect(unitVector, SpuMath.MultiplyAdd(y0, y0, x0 * x0), _one, _zerro);
//					under_curve += SpuMath.CompareGreaterThanAndSelect(unitVector, x0 * x0 + y0 * y0, _one, _zerro);

					VectorF4 x1 = xs[i];
					VectorF4 y1 = ys[i];
					i++;

					under_curve += SpuMath.CompareGreaterThanAndSelect(unitVector, SpuMath.MultiplyAdd(y1, y1, x1 * x1), _one, _zerro);
//					under_curve += SpuMath.CompareGreaterThanAndSelect(unitVector, x1 * x1 + y1 * y1, _one, _zerro);

					VectorF4 x2 = xs[i];
					VectorF4 y2 = ys[i];
					i++;

					under_curve += SpuMath.CompareGreaterThanAndSelect(unitVector, SpuMath.MultiplyAdd(y2, y2, x2 * x2), _one, _zerro);
//					under_curve += SpuMath.CompareGreaterThanAndSelect(unitVector, x2 * x2 + y2 * y2, _one, _zerro);

					VectorF4 x3 = xs[i];
					VectorF4 y3 = ys[i];
					i++;

					under_curve += SpuMath.CompareGreaterThanAndSelect(unitVector, SpuMath.MultiplyAdd(y3, y3, x3 * x3), _one, _zerro);
//					under_curve += SpuMath.CompareGreaterThanAndSelect(unitVector, x3 * x3 + y3 * y3, _one, _zerro);
				}
			}

			return ((float)(under_curve.E1 + under_curve.E2 + under_curve.E3 + under_curve.E4) / (float)(iterations * (4 * inneriterations))) * 4.0f;
		}
	}
}
