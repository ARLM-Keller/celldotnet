using CellDotNet;
using CellDotNet.Spe;

namespace SciMark2Cell
{
	public class RandomVector
	{
		internal VectorI4 _seed;

		private VectorI4 m1;
		private VectorI4 m2;
		private VectorF4 dm1;

		private VectorF4 _left;
		private VectorF4 _width;

		private VectorI4[] m;
		private int i;
		private int j;

		private bool haveRange;
		
		public RandomVector(VectorI4 seed)
		{
			i = 4;
			j = 16;

			haveRange = false;
			_left = VectorF4.Splat(0.0f);
			_width = VectorF4.Splat(1.0f);

			initialize(seed);
		}

		public RandomVector(VectorI4 seed, float left, float right)
		{
			i = 4;
			j = 16;

			haveRange = false;
			_left = VectorF4.Splat(0.0f);
			_width = VectorF4.Splat(1.0f);

			initialize(seed);
			_left = new VectorF4(left, left, left, left);
			float _widthscale = right - left;
			_width = new VectorF4(_widthscale, _widthscale, _widthscale, _widthscale);
			haveRange = true;
		}
		
		public VectorF4 nextFloat()
		{
			VectorI4 zerro = VectorI4.Splat(0);

			VectorI4 k;
		
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

			VectorF4 r = dm1 * SpuMath.ConvertToFloat(k);

			return SpuMath.ConditionalSelect(haveRange, SpuMath.MultiplyAdd(r, _width, _left), r);
		}


		public void nextFloats(VectorF4[] x)
		{
			int N = x.Length;
			int remainder = N & 3;

			if (haveRange)
			{
				for (int count = 0; count < remainder; count++)
				{
					VectorI4 zerro = VectorI4.Splat(0);
					VectorI4 k;
					k = m[i] - m[j];
					k += SpuMath.CompareGreaterThanAndSelect(zerro, k, m1, zerro);
					m[j] = k;
					i = SpuMath.CompareEqualsAndSelect(i, 0, 16, i - 1);
					j = SpuMath.CompareEqualsAndSelect(j, 0, 16, j - 1);
					x[count] = SpuMath.MultiplyAdd(dm1 * SpuMath.ConvertToFloat(k), _width, _left);
				}

				for (int count = remainder; count < N; count += 4)
				{
					VectorI4 zerro = VectorI4.Splat(0);
					VectorI4 k;
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
					VectorI4 zerro = VectorI4.Splat(0);
					VectorI4 k;
					k = m[i] - m[j];
					k += SpuMath.CompareGreaterThanAndSelect(zerro, k, m1, zerro);
					m[j] = k;
					i = SpuMath.CompareEqualsAndSelect(i, 0, 16, i - 1);
					j = SpuMath.CompareEqualsAndSelect(j, 0, 16, j - 1);
					x[count] = dm1 * SpuMath.ConvertToFloat(k);
				}

				for (int count = remainder; count < N; count += 4)
				{
					VectorI4 zerro = VectorI4.Splat(0);
					VectorI4 k;
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

		private void initialize(VectorI4 seed)
		{
			const int mdig_s = 32;
			const int one_s = 1;
			int m2_s;
			int m1_s;

			// First the initialization of the member variables;
			m1_s = (one_s << mdig_s - 2) + ((one_s << mdig_s - 2) - one_s);
			m2_s = one_s << mdig_s / 2;

			m1 = VectorI4.Splat(m1_s);
			m2 = VectorI4.Splat(m2_s);

			dm1 = VectorF4.Splat(1) / SpuMath.ConvertToFloat(m1);

			VectorI4 jseed, k0, k1, j0, j1;
			int iloop;
			
			_seed = seed;
			
			m = new VectorI4[17];
			
			jseed = SpuMath.Min(SpuMath.Abs(seed), m1);

//			if (jseed % VectorI4.Splat(0) == 0)
//				--jseed;

			jseed -=
				SpuMath.CompareEqualsAndSelect(jseed%VectorI4.Splat(2), VectorI4.Splat(0),
				                               VectorI4.Splat(1), VectorI4.Splat(0));

			k0 = VectorI4.Splat(9069) % m2;
			k1 = VectorI4.Splat(9069) % m2;
			j0 = jseed % m2;
			j1 = jseed / m2;
			for (iloop = 0; iloop < 17; ++iloop)
			{
				jseed = j0*k0;
				j1 = (jseed/m2 + j0*k1 + j1*k0)%(m2/VectorI4.Splat(2));
				j0 = jseed%m2;
				m[iloop] = j0 + m2*j1;
			}
			i = 4;
			j = 16;
		}
	}
}
