namespace SciMark2
{
	public class MonteCarloSingleUnrolled
	{
		public static float integrate(int Num_samples)
		{
			int inneriterations = 256 * 4;
			int iterations = (Num_samples / (inneriterations)) + 1;

			RandomSingle R = new RandomSingle(113);

			int under_curve = 0;

			float[] xs = new float[inneriterations];
			float[] ys = new float[inneriterations];

			for (int count = 0; count < iterations; count++)
			{
				R.nextFloats(xs);
				R.nextFloats(ys);

				for (int i = 0; i < inneriterations; i++)
				{
					float x = xs[i];
					float y = ys[i];

					if (x * x + y * y <= 1.0f)
						under_curve++;
				}
			}


//			for (int count = 0; count < Num_samples; count++)
//			{
//				float x = R.nextFloat();
//				float y = R.nextFloat();
//
//				if (x * x + y * y <= 1.0f)
//					under_curve++;
//			}
			return ((float)under_curve / Num_samples) * 4.0f;
		}
	}
}