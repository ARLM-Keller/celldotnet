namespace SciMark2
{
	public class MonteCarloSingle
	{
		internal const int SEED = 113;

		public static float integrate(int Num_samples)
		{
			RandomSingle R = new RandomSingle(SEED);

			int under_curve = 0;
			for (int count = 0; count < Num_samples; count++)
			{
				float x = R.nextFloat();
				float y = R.nextFloat();

				if (x * x + y * y <= 1.0f)
					under_curve++;
			}
			return ((float)under_curve / Num_samples) * 4.0f;
		}
	}
}