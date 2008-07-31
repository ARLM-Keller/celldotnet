using CellDotNet;
using CellDotNet.Spe;

namespace SciMark2Cell
{
	public class MonteCarloVectorSimpleUnroled
	{
		public static float integrate(int seed, int Num_samples)
		{
			int iterations = (Num_samples / 4) + 1;

			RandomVector R = new RandomVector(VectorI4.Splat(seed));

			VectorI4 under_curve = VectorI4.Splat(0);

			VectorI4 _zerro = VectorI4.Splat(0);
			VectorI4 _one = VectorI4.Splat(1);
			VectorF4 unitVector = VectorF4.Splat(1f);

			for (int count = 0; count < iterations; count+=4)
			{
				VectorF4 x1 = R.nextFloat();
				VectorF4 y1 = R.nextFloat();
				VectorF4 x2 = R.nextFloat();
				VectorF4 y2 = R.nextFloat();
				VectorF4 x3 = R.nextFloat();
				VectorF4 y3 = R.nextFloat();
				VectorF4 x4 = R.nextFloat();
				VectorF4 y4 = R.nextFloat();

				VectorF4 xx1 = x1 * x1;
				VectorF4 yy1 = y1 * y1;

				VectorF4 xx2 = x2 * x2;
				VectorF4 yy2 = y2 * y2;

				VectorF4 xx3 = x3 * x3;
				VectorF4 yy3 = y3 * y3;

				VectorF4 xx4 = x4 * x4;
				VectorF4 yy4 = y4 * y4;


				VectorI4 uc1 = SpuMath.CompareGreaterThanAndSelect(unitVector, xx1 + yy1, _one, _zerro);

				VectorI4 uc2 = SpuMath.CompareGreaterThanAndSelect(unitVector, xx2 + yy2, _one, _zerro);

				VectorI4 uc3 = SpuMath.CompareGreaterThanAndSelect(unitVector, xx3 + yy3, _one, _zerro);

				VectorI4 uc4 = SpuMath.CompareGreaterThanAndSelect(unitVector, xx4 + yy4, _one, _zerro);

				under_curve = uc1 + uc2 + uc3 + uc4;
			}

			return ((float)(under_curve.E1 + under_curve.E2 + under_curve.E3 + under_curve.E4) / (float)(iterations * 4)) * 4.0f;
		}
	}
}
