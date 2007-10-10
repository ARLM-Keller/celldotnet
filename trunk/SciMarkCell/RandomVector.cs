using CellDotNet;

namespace SciMark2Cell
{
	public struct RandomVector
	{
		internal Int32Vector _seed; // readonly

		private Int32Vector[] m; // vector, (readonly)
		private int i; // scalar
		private int j; // scalar

		private Int32Vector m1; // Vector, ReadOnly

		private Float32Vector dm1; // vector, readonly
		
		private bool haveRange; // readonly
		private Float32Vector _left; // vecotr(not nessesary), readonly
//		private float _right;
		private Float32Vector _width; // vecotr(not nessesary), readonly

		private Int32Vector _zerro;

//		public RandomVector(int seed)
//		{
//			throw new NotSupportedException();
//		}

		public void initializeRandomCell(Int32Vector seed)
		{
			initializeState();
			initializeSeed(seed);
		}

		public void initializeRandomCell(Int32Vector seed, float left, float right)
		{
			initializeState();
			initializeSeed(seed);
			_left = new Float32Vector(left, left, left, left);
//			_right = right;
			float _widthscale = right - left;
			_width = new Float32Vector(_widthscale, _widthscale, _widthscale, _widthscale);
			haveRange = true;
		}
		
//		public double nextDouble()
//		{
//			throw new NotSupportedException();
//		}

		public Float32Vector nextFloat()
		{
			Int32Vector k;
		
			k = m[i] - m[j];

//			if (k < _zerro)
//				k += m1;
			k += SpuMath.CompareGreaterThanAndSelect(_zerro, k, m1, _zerro);

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
				return _left + dm1 * SpuMath.ConvertToFloat(k) * _width;
			else
				return dm1 * SpuMath.ConvertToFloat(k);
		}
		
		/*----------------------------------------------------------------------------
		PRIVATE METHODS
		------------------------------------------------------------------------ */
		
		private void initializeSeed(Int32Vector seed)
		{
			const int mdig_s = 32; // only used in initialization
			const int one_s = 1; // Only used in initialization
			Int32Vector m2; // ReadOnly, only used in initialize
			int m2_s;
			int m1_s;

			// First the initialization of the member variables;
			m1_s = (one_s << mdig_s - 2) + ((one_s << mdig_s - 2) - one_s);
			m2_s = one_s << mdig_s / 2;

			m1 = Int32Vector.Splat(m1_s);
			m2 = Int32Vector.Splat(m2_s);

			dm1 = Float32Vector.Splat(1) / SpuMath.ConvertToFloat(m1);

			Int32Vector jseed, k0, k1, j0, j1;
			int iloop;
			
			_seed = seed;
			
			m = new Int32Vector[17];
			
			jseed = SpuMath.Min(SpuMath.Abs(seed), m1);


//			if (jseed % Int32Vector.Splat(0) == 0)
//				--jseed;

			jseed -=
				SpuMath.CompareEqualsAndSelect(jseed%Int32Vector.Splat(2), Int32Vector.Splat(0),
				                               Int32Vector.Splat(1), Int32Vector.Splat(0));

			k0 = Int32Vector.Splat(9069) % m2;
			k1 = Int32Vector.Splat(9069) % m2;
			j0 = jseed % m2;
			j1 = jseed / m2;
			for (iloop = 0; iloop < 17; ++iloop)
			{
				jseed = j0*k0;
				j1 = (jseed/m2 + j0*k1 + j1*k0)%(m2/Int32Vector.Splat(2));
				j0 = jseed%m2;
				m[iloop] = j0 + m2*j1;
			}
			i = 4;
			j = 16;
		}

		private void initializeState()
		{
			_seed = Int32Vector.Splat(0);

			i = 4;
			j = 16;

			_zerro = Int32Vector.Splat(0);

			haveRange = false;
			_left = Float32Vector.Splat(0.0f);
//			_right = 1.0f;
			_width = Float32Vector.Splat(1.0f);
		}
	}
}
