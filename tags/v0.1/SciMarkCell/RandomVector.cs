using CellDotNet;
using CellDotNet.Spe;

namespace SciMark2Cell
{
	public class RandomVector
	{
		internal Int32Vector _seed;

		private Int32Vector m1;
		private Int32Vector m2;
		private Float32Vector dm1;

		private Float32Vector _left;
		private Float32Vector _width;

		private Int32Vector[] m;
		private int i;
		private int j;

		private bool haveRange;
		
		public RandomVector(Int32Vector seed)
		{
			i = 4;
			j = 16;

			haveRange = false;
			_left = Float32Vector.Splat(0.0f);
			_width = Float32Vector.Splat(1.0f);

			initialize(seed);
		}

		public RandomVector(Int32Vector seed, float left, float right)
		{
			i = 4;
			j = 16;

			haveRange = false;
			_left = Float32Vector.Splat(0.0f);
			_width = Float32Vector.Splat(1.0f);

			initialize(seed);
			_left = new Float32Vector(left, left, left, left);
			float _widthscale = right - left;
			_width = new Float32Vector(_widthscale, _widthscale, _widthscale, _widthscale);
			haveRange = true;
		}
		
		public Float32Vector nextFloat()
		{
			Int32Vector zerro = Int32Vector.Splat(0);

			Int32Vector k;
		
			k = m[i] - m[j];

			k += SpuMath.CompareGreaterThanAndSelect(zerro, k, m1, zerro);

			m[j] = k;

//			if (i == 0)
//				i = 16;
//			else
//				i--;

			i = SpuMath.CompareEqualsAndSelect(i, 0, 16, i - 1);

//			if (j == 0)
//				j = 16;
//			else
//				j--;

			j = SpuMath.CompareEqualsAndSelect(j, 0, 16, j - 1);
		
//			if (haveRange)
//				return _left + dm1 * SpuMath.ConvertToFloat(k) * _width;
//			else
//				return dm1 * SpuMath.ConvertToFloat(k);

			Float32Vector r = dm1 * SpuMath.ConvertToFloat(k);

			return SpuMath.ConditionalSelect(haveRange, SpuMath.MultiplyAdd(r, _width, _left), r);
		}


		public void nextFloats(Float32Vector[] x)
		{
			int N = x.Length;
			int remainder = N & 3;

			if (haveRange)
			{
				for (int count = 0; count < remainder; count++)
				{
					Int32Vector zerro = Int32Vector.Splat(0);
					Int32Vector k;
					k = m[i] - m[j];
					k += SpuMath.CompareGreaterThanAndSelect(zerro, k, m1, zerro);
					m[j] = k;
					i = SpuMath.CompareEqualsAndSelect(i, 0, 16, i - 1);
					j = SpuMath.CompareEqualsAndSelect(j, 0, 16, j - 1);
					x[count] = SpuMath.MultiplyAdd(dm1 * SpuMath.ConvertToFloat(k), _width, _left);
				}

				for (int count = remainder; count < N; count += 4)
				{
					Int32Vector zerro = Int32Vector.Splat(0);
					Int32Vector k;
					k = m[i] - m[j];
					k += SpuMath.CompareGreaterThanAndSelect(zerro, k, m1, zerro);
					m[j] = k;
					i = SpuMath.CompareEqualsAndSelect(i, 0, 16, i - 1);
					j = SpuMath.CompareEqualsAndSelect(j, 0, 16, j - 1);
					x[count] = SpuMath.MultiplyAdd(dm1 * SpuMath.ConvertToFloat(k), _width, _left);

					k = m[i] - m[j];
					k += SpuMath.CompareGreaterThanAndSelect(zerro, k, m1, zerro);
					m[j] = k;
					i = SpuMath.CompareEqualsAndSelect(i, 0, 16, i - 1);
					j = SpuMath.CompareEqualsAndSelect(j, 0, 16, j - 1);
					x[count+1] = SpuMath.MultiplyAdd(dm1 * SpuMath.ConvertToFloat(k), _width, _left);

					k = m[i] - m[j];
					k += SpuMath.CompareGreaterThanAndSelect(zerro, k, m1, zerro);
					m[j] = k;
					i = SpuMath.CompareEqualsAndSelect(i, 0, 16, i - 1);
					j = SpuMath.CompareEqualsAndSelect(j, 0, 16, j - 1);
					x[count+2] = SpuMath.MultiplyAdd(dm1 * SpuMath.ConvertToFloat(k), _width, _left);

					k = m[i] - m[j];
					k += SpuMath.CompareGreaterThanAndSelect(zerro, k, m1, zerro);
					m[j] = k;
					i = SpuMath.CompareEqualsAndSelect(i, 0, 16, i - 1);
					j = SpuMath.CompareEqualsAndSelect(j, 0, 16, j - 1);
					x[count+3] = SpuMath.MultiplyAdd(dm1 * SpuMath.ConvertToFloat(k), _width, _left);
				}
			}
			else
			{
				for (int count = 0; count < remainder; count++)
				{
					Int32Vector zerro = Int32Vector.Splat(0);
					Int32Vector k;
					k = m[i] - m[j];
					k += SpuMath.CompareGreaterThanAndSelect(zerro, k, m1, zerro);
					m[j] = k;
					i = SpuMath.CompareEqualsAndSelect(i, 0, 16, i - 1);
					j = SpuMath.CompareEqualsAndSelect(j, 0, 16, j - 1);
					x[count] = dm1 * SpuMath.ConvertToFloat(k);
				}

				for (int count = remainder; count < N; count += 4)
				{
					Int32Vector zerro = Int32Vector.Splat(0);
					Int32Vector k;
					k = m[i] - m[j];
					k += SpuMath.CompareGreaterThanAndSelect(zerro, k, m1, zerro);
					m[j] = k;
					i = SpuMath.CompareEqualsAndSelect(i, 0, 16, i - 1);
					j = SpuMath.CompareEqualsAndSelect(j, 0, 16, j - 1);
					x[count] = dm1 * SpuMath.ConvertToFloat(k);

					k = m[i] - m[j];
					k += SpuMath.CompareGreaterThanAndSelect(zerro, k, m1, zerro);
					m[j] = k;
					i = SpuMath.CompareEqualsAndSelect(i, 0, 16, i - 1);
					j = SpuMath.CompareEqualsAndSelect(j, 0, 16, j - 1);
					x[count+1] = dm1 * SpuMath.ConvertToFloat(k);

					k = m[i] - m[j];
					k += SpuMath.CompareGreaterThanAndSelect(zerro, k, m1, zerro);
					m[j] = k;
					i = SpuMath.CompareEqualsAndSelect(i, 0, 16, i - 1);
					j = SpuMath.CompareEqualsAndSelect(j, 0, 16, j - 1);
					x[count+2] = dm1 * SpuMath.ConvertToFloat(k);

					k = m[i] - m[j];
					k += SpuMath.CompareGreaterThanAndSelect(zerro, k, m1, zerro);
					m[j] = k;
					i = SpuMath.CompareEqualsAndSelect(i, 0, 16, i - 1);
					j = SpuMath.CompareEqualsAndSelect(j, 0, 16, j - 1);
					x[count+3] = dm1 * SpuMath.ConvertToFloat(k);
				}
			}
		}

		private void initialize(Int32Vector seed)
		{
			const int mdig_s = 32;
			const int one_s = 1;
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
	}
}
