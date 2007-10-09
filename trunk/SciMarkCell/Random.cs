using System;

namespace SciMarkCell
{
	public struct Random
	{
		internal int _seed; // readonly
		
		private int[] m; // readonly
		private int i;
		private int j;
		
		private const int mdig = 32;
		private const int one = 1;
		private int m1; // ReadOnly
		private int m2; // ReadOnly, only used in initialize
		
		private float dm1; //readonly
		
		private bool haveRange; // readonly
		private float _left; // readonly
		private float _right;
		private float _width; // readonly

		public Random(int seed)
		{
			throw new NotSupportedException();
		}

		public void initializeRandomCell(int seed)
		{
			initializeState();
			initializeSeed(seed);
		}

		public void initializeRandomCell(int seed, float left, float right)
		{
			initializeState();
			initializeSeed(seed);
			_left = left;
			_right = right;
			_width = right - left;
			haveRange = true;
		}
		
//		public double nextDouble()
//		{
//			throw new NotSupportedException();
//		}

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
				return _left + dm1 * k * _width;
			else
				return dm1 * k;
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
				
					x[count] = _left + dm1 * k * _width;
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
		
		/*----------------------------------------------------------------------------
		PRIVATE METHODS
		------------------------------------------------------------------------ */
		
		private void initializeSeed(int seed)
		{
			// First the initialization of the member variables;
			m1 = (one << mdig - 2) + ((one << mdig - 2) - one);
			m2 = one << mdig / 2;
			dm1 = 1.0f / m1;
		
			int jseed, k0, k1, j0, j1, iloop;
			
			_seed = seed;
			
			m = new int[17];
			
			jseed = Math.Min(Math.Abs(seed), m1);
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
			_seed = 0;

			i = 4;
			j = 16;

			haveRange = false;
			_left = 0.0f;
			_right = 1.0f;
			_width = 1.0f;
		}
	}
}