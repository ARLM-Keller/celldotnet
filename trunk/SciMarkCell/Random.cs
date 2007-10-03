using System.Runtime.CompilerServices;

namespace SciMarkCell
{
	public struct Random
	{
		internal int seed;
		
		private int[] m;
		private int i;
		private int j;
		
		private const int mdig = 32;
		private const int one = 1;
		private int m1;
		private int m2;
		
		private float dm1;
		
		private bool haveRange;
		private float left;
		private float right;
		private float width;
		
		public void initializeRandomCell(int seed)
		{
			initializeState();
			initializeSeed(seed);
		}

		public void initializeRandomCell(int seed, float left, float right)
		{
			initializeState();
			initializeSeed(seed);
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
				return left + dm1 * (float) k * width;
			else
				return dm1 * (float) k;
		}
		
		public void nextFloat(float[] x)
		{
			int N = x.Length;
			int remainder = N & 3; 
		
			if (haveRange)
			{
				for (int count = 0; count < N; count++)
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
				
					x[count] = left + dm1 * (float) k * width;
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
				
				
					x[count] = dm1 * (float) k;
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
					x[count] = dm1 * (float) k;
				
				
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
					x[count + 1] = dm1 * (float) k;
				
				
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
					x[count + 2] = dm1 * (float) k;
				
				
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
					x[count + 3] = dm1 * (float) k;
				}
			}
		}
		
		/*----------------------------------------------------------------------------
		PRIVATE METHODS
		------------------------------------------------------------------------ */
		
		private void initializeSeed(int seed)
		{
			// First the initialization of the member variables;
			m1 = (one << mdig - 2) + ((one << mdig - 2) - one);
			m2 = one << mdig / 2;
			dm1 = 1.0f / (float) m1;
		
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

		private void initializeState()
		{
			seed = 0;

			i = 4;
			j = 16;

			haveRange = false;
			left = 0.0f;
			right = 1.0f;
			width = 1.0f;
		}
	}
}