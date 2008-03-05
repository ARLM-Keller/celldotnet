using CellDotNet;
using CellDotNet.Spe;

namespace SciMark2Cell
{
	public class MonteCarloVectorSimpleUnroled
	{
		public static float integrate(int seed, int Num_samples)
		{
			int iterations = (Num_samples / 4) + 1;

			RandomVector R = new RandomVector(Int32Vector.Splat(seed));

			Int32Vector under_curve = Int32Vector.Splat(0);

			Int32Vector _zerro = Int32Vector.Splat(0);
			Int32Vector _one = Int32Vector.Splat(1);
			Float32Vector unitVector = Float32Vector.Splat(1f);

			for (int count = 0; count < iterations; count+=4)
			{
				Float32Vector x1 = R.nextFloat();
				Float32Vector y1 = R.nextFloat();
				Float32Vector x2 = R.nextFloat();
				Float32Vector y2 = R.nextFloat();
				Float32Vector x3 = R.nextFloat();
				Float32Vector y3 = R.nextFloat();
				Float32Vector x4 = R.nextFloat();
				Float32Vector y4 = R.nextFloat();

				Float32Vector xx1 = x1 * x1;
				Float32Vector yy1 = y1 * y1;

				Float32Vector xx2 = x2 * x2;
				Float32Vector yy2 = y2 * y2;

				Float32Vector xx3 = x3 * x3;
				Float32Vector yy3 = y3 * y3;

				Float32Vector xx4 = x4 * x4;
				Float32Vector yy4 = y4 * y4;


				Int32Vector uc1 = SpuMath.CompareGreaterThanAndSelect(unitVector, xx1 + yy1, _one, _zerro);

				Int32Vector uc2 = SpuMath.CompareGreaterThanAndSelect(unitVector, xx2 + yy2, _one, _zerro);

				Int32Vector uc3 = SpuMath.CompareGreaterThanAndSelect(unitVector, xx3 + yy3, _one, _zerro);

				Int32Vector uc4 = SpuMath.CompareGreaterThanAndSelect(unitVector, xx4 + yy4, _one, _zerro);

				under_curve = uc1 + uc2 + uc3 + uc4;
			}

			return ((float)(under_curve.E1 + under_curve.E2 + under_curve.E3 + under_curve.E4) / (float)(iterations * 4)) * 4.0f;
		}
	}
}
