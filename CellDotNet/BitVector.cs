using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace CellDotNet
{
	internal class BitVector : IEnumerable<int>
	{
		public int Size
		{
			get { return _size; }
		}

		private int _size = 0;

		private uint[] vector = new uint[0];

		public BitVector()
		{
		}

		public BitVector(int capacity)
		{
			Resize(capacity);
		}

		public BitVector(BitVector v)
		{
			Resize(v._size);

			Buffer.BlockCopy(v.vector, 0, vector, 0, v.vector.Length*4);
		}

		public void Clear()
		{
			_size = 0;
			vector = new uint[0];
		}

		public void Add(int elementnr)
		{
			if(elementnr < 0)
				return;

			if(elementnr >= _size)
				Resize(elementnr+1);

			int index = elementnr/32;
			int bit = elementnr%32;

			vector[index] |= (uint) 1 << bit;
		}

		public void AddAll(BitVector v)
		{
			if(_size < v._size)
				Resize(v._size);

			for (int i = 0; i < v.vector.Length; i++)
				vector[i] |= v.vector[i];
		}

		public void AddAll(IEnumerable<int> values)
		{
			if(values != null)
				foreach (int i in values)
					Add(i);
		}

		public void AddAll(IEnumerable<uint> values)
		{
			if (values != null)
				foreach (uint i in values)
					Add((int)i);
		}

		public void Remove(int elementnr)
		{
			if (elementnr < 0)
				throw new ArgumentOutOfRangeException();

			if (elementnr >= _size)
				return;

			int index = elementnr / 32;
			int bit = elementnr % 32;

			vector[index] &= ~((uint) (1 << bit));

		}

		public void RemoveAll(IEnumerable<int> values)
		{
			if (values != null)
				foreach (int i in values)
					Remove(i);
		}

		public void RemoveAll(IEnumerable<uint> values)
		{
			if (values != null)
				foreach (int i in values)
					Remove(i);
		}

		public void RemoveAll(BitVector v)
		{
			for (int i = 0; i < vector.Length && i < v.vector.Length; i++)
			{
				vector[i] &= ~v.vector[i];
			}
		}

		public bool Contains(int elementnr)
		{
			if (elementnr >= vector.Length*32)
				return false;

			return ((vector[elementnr / 32] >> elementnr % 32) & 1) != 0;
		}

		public int Count
		{
			get
			{
				int count = 0;
				for (int i = 0; i < vector.Length; i++)
					for (int b = 0; b < 32; b++)
						count += ((vector[i] >> b) & 1) != 0 ? 1 : 0;
				return count;
			}
		}

		public bool IsCountZero()
		{
			for (int i = 0; i < vector.Length; i++)
				if (vector[i] != 0)
					return false;
			return true;
		}

		public int SizeTrim()
		{
			for(int i = Size-1; i >= 0; i--)
				if (Contains(i))
					return i+1;

			return 0;
		}

		private void Resize(int newSize)
		{
			if(newSize < _size)
				return;

			_size = (newSize > 2 * _size) ? newSize : 2*_size;

			_size = ((_size-1)/32+1) * 32;

			uint [] newVector = new uint[(_size-1)/32+1];

			for(int i = 0; i < vector.Length; i++)
				newVector[i] = vector[i];

			vector = newVector;
		}

		// Returns the first item in the vector. NOTE if the vector is empty uint.MaxVaue is returned.
		public uint getItem()
		{
			int element = 0;
			while (element < _size && ((vector[element / 32] >> (element % 32)) & 1) == 0)
			{
				element++;
			}

			return element < _size ? (uint) element : uint.MaxValue;
		}

		public void And(BitVector v)
		{
			int min = vector.Length < v.vector.Length ? vector.Length : v.vector.Length;

			for (int i = 0; i < min; i++)
				vector[i] &= v.vector[i];

			for (int i = min; i < vector.Length; i++)
			{
				vector[i] = 0;
			}
		}

		// AddAll(v1 & v2)
		public void AddAllAnd(BitVector v1, BitVector v2)
		{
			int minLength = Math.Min(v1.vector.Length, v2.vector.Length);

			if(minLength > vector.Length)
				Resize(minLength*32);

			for (int i = 0; i < minLength; i++)
				vector[i] |= (v1.vector[i] & v2.vector[i]);
		}

		// RemoveAll(v1 & v2)
		public void RemoveAllAnd(BitVector v1, BitVector v2)
		{
			int minLength = Math.Min(v1.vector.Length, v2.vector.Length);
			minLength = Math.Min(minLength, vector.Length);

			for (int i = 0; i < minLength; i++)
				vector[i] &= ~(v2.vector[i] & v2.vector[i]);
		}

		public static BitVector operator &(BitVector v1, BitVector v2)
		{
			BitVector vmin = v1._size < v2._size ? v1 : v2;
			BitVector vmax = v1._size > v2._size ? v1 : v2;

			BitVector result = new BitVector(vmin);
			for (int i = 0; i < result.vector.Length; i++)
				result.vector[i] &= vmax.vector[i];

			return result;
		}

		public static BitVector operator |(BitVector v1, BitVector v2)
		{
			BitVector vmin = v1._size < v2._size ? v1 : v2;
			BitVector vmax = v1._size >= v2._size ? v1 : v2;

			BitVector result = new BitVector(vmax);
			result.AddAll(vmin);

			return result;
		}

		public bool Equals(BitVector v)
		{
			if (v == null)
				return IsCountZero();

			int min = Math.Min(vector.Length, v.vector.Length);
			int max = Math.Max(vector.Length, v.vector.Length);

			uint[] vmax = vector.Length == max ? vector : v.vector;

			for (int i = 0; i < min; i++)
				if (vector[i] != v.vector[i])
					return false;
			for (int i = min; i < max; i++)
				if (vmax[i] != 0)
					return false;
			return true;
		}

		public override string ToString()
		{
			String result = "";

			foreach (int i in this)
			{
				result = result + " " + i;
			}
			return result;
		}

		IEnumerator<int> IEnumerable<int>.GetEnumerator()
		{
			return new Enumerator(this);
		}

		public IEnumerator GetEnumerator()
		{
			return new Enumerator(this);
		}

		private class Enumerator : IEnumerator, IEnumerator<int>
		{
			private BitVector vector;

			private int element = -1;

			public Enumerator(BitVector vector)
			{
				this.vector = vector;
			}

			public bool MoveNext()
			{
				while (++element % 32 != 0 && ((vector.vector[element / 32] >> (element % 32)) & 1) == 0)
				{
					
				}
				if (element % 32 != 0)
					return true;

				for(int i = element/32; i< vector.vector.Length; i++)
				{
					if(vector.vector[i] != 0)
					{
						element = i*32;
						while (((vector.vector[i] >> (element % 32)) & 1) == 0)
						{
							element++;
						}
						return true;
					}
				}

				return false;

//				while (++element < vector.size && ((vector.vector[element/32] >> (element%32)) & 1) == 0)
//				{
//				}
//
//				return element < vector.size;
			}

			public void Reset()
			{
				element = -1;
			}

			public object Current
			{
				get
				{
					if (element < 0 || element >= vector._size)
						throw new InvalidOperationException();
					return element;
				}
			}

			int IEnumerator<int>.Current
			{
				get
				{
					if (element < 0 || element >= vector._size)
						throw new InvalidOperationException();
					return element;
				}
			}

			public void Dispose()
			{
				// TODO skal der foretages noget her?
//				throw new NotImplementedException();
			}
		}

		public string PrintFullVector(int maxWidth)
		{
			StringBuilder text = new StringBuilder();

//			if (maxWidth < _size)
//				maxWidth = _size;

			for (int i = 0; i < _size && i < maxWidth; i++)
			{
				text.Append(Contains(i) ? "1" : " ");
			}

			for (int i = _size; i < maxWidth; i++)
				text.Append(" ");

			return text.ToString();
		}

	}
}