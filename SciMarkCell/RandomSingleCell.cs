namespace SciMark2Cell
{
	public class RandomSingleCell
	{
		internal int seed = 0;

		private int[] m;
		private int i = 4;
		private int j = 16;

		private const int mdig = 32;
		private const int one = 1;
		private int m1;
		private int m2;

		private float dm1;

		private bool haveRange = false;
		private float left = 0.0f;
		private float right = 1.0f;
		private float width = 1.0f;


		public RandomSingleCell(int seed)
		{
			initialize(seed);
		}

		public RandomSingleCell(int seed, float left, float right)
		{
			initialize(seed);
			this.left = left;
			this.right = right;
			width = right - left;
			haveRange = true;
		}

		public float nextFloat()
		{
			int k;

			k = m[i] - m[j];
			if (k < 0)
				k += m1;
			m[j] = k;

			if (i == 0)
				i = 16;
			else
				i--;

			if (j == 0)
				j = 16;
			else
				j--;

			if (haveRange)
				return left + dm1 * k * width;
			else
				return dm1 * k;
		}

		public void nextFloats(float[] x)
		{
			int N = x.Length;
			int remainder = N & 3;

			if (haveRange)
			{
				for (int count = 0; count < remainder; count++)
				{
					int k = m[i] - m[j];

					if (i == 0)
						i = 16;
					else
						i--;

					if (k < 0)
						k += m1;
					m[j] = k;

					if (j == 0)
						j = 16;
					else
						j--;

					x[count] = left + dm1 * k * width;
				}
				for (int count = remainder; count < N; count += 4)
				{
					int k = m[i] - m[j];
					if (i == 0)
						i = 16;
					else
						i--;
					if (k < 0)
						k += m1;
					m[j] = k;
					if (j == 0)
						j = 16;
					else
						j--;
					x[count] = left + dm1 * k * width;


					k = m[i] - m[j];
					if (i == 0)
						i = 16;
					else
						i--;
					if (k < 0)
						k += m1;
					m[j] = k;
					if (j == 0)
						j = 16;
					else
						j--;
					x[count + 1] = left + dm1 * k * width;

					k = m[i] - m[j];
					if (i == 0)
						i = 16;
					else
						i--;
					if (k < 0)
						k += m1;
					m[j] = k;
					if (j == 0)
						j = 16;
					else
						j--;
					x[count + 2] = left + dm1 * k * width;

					k = m[i] - m[j];
					if (i == 0)
						i = 16;
					else
						i--;
					if (k < 0)
						k += m1;
					m[j] = k;
					if (j == 0)
						j = 16;
					else
						j--;
					x[count + 3] = left + dm1 * k * width;
				}

			}
			else
			{
				for (int count = 0; count < remainder; count++)
				{
					int k = m[i] - m[j];

					if (i == 0)
						i = 16;
					else
						i--;

					if (k < 0)
						k += m1;
					m[j] = k;

					if (j == 0)
						j = 16;
					else
						j--;


					x[count] = dm1 * k;
				}

				for (int count = remainder; count < N; count += 4)
				{
					int k = m[i] - m[j];
					if (i == 0)
						i = 16;
					else
						i--;
					if (k < 0)
						k += m1;
					m[j] = k;
					if (j == 0)
						j = 16;
					else
						j--;
					x[count] = dm1 * k;


					k = m[i] - m[j];
					if (i == 0)
						i = 16;
					else
						i--;
					if (k < 0)
						k += m1;
					m[j] = k;
					if (j == 0)
						j = 16;
					else
						j--;
					x[count + 1] = dm1 * k;


					k = m[i] - m[j];
					if (i == 0)
						i = 16;
					else
						i--;
					if (k < 0)
						k += m1;
					m[j] = k;
					if (j == 0)
						j = 16;
					else
						j--;
					x[count + 2] = dm1 * k;


					k = m[i] - m[j];
					if (i == 0)
						i = 16;
					else
						i--;
					if (k < 0)
						k += m1;
					m[j] = k;
					if (j == 0)
						j = 16;
					else
						j--;
					x[count + 3] = dm1 * k;
				}
			}
		}

		private void initialize(int seed)
		{
			// First the initialization of the member variables;
			m1 = (one << mdig - 2) + ((one << mdig - 2) - one);
			m2 = one << mdig / 2;
			dm1 = 1.0f / m1;

			int jseed, k0, k1, j0, j1, iloop;

			this.seed = seed;

			m = new int[17];

			jseed = System.Math.Min(System.Math.Abs(seed), m1);
			if (jseed % 2 == 0)
				--jseed;
			k0 = 9069 % m2;
			k1 = 9069 / m2;
			j0 = jseed % m2;
			j1 = jseed / m2;
			for (iloop = 0; iloop < 17; ++iloop)
			{
				jseed = j0 * k0;
				j1 = (jseed / m2 + j0 * k1 + j1 * k0) % (m2 / 2);
				j0 = jseed % m2;
				m[iloop] = j0 + m2 * j1;
			}
			i = 4;
			j = 16;
		}
	}
}