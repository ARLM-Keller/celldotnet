using System;
using System.Collections;
using System.Collections.Generic;

namespace CellDotNet
{
	class BitVector : IEnumerable<int>
	{
		private int size = 0;

		private uint[] vector = new uint[0];

		public BitVector()
		{
		}

		public BitVector(int capacity)
		{
			Resize(capacity);
		}

		public void Clear()
		{
			size = 0;
			vector = new uint[0];
		}

		public void Add(int elementnr)
		{
			if(elementnr < 0)
				return;

			if(elementnr >= size)
				Resize(elementnr+1);

			int index = elementnr/32;
			int bit = elementnr%32;

			vector[index] |= (uint) 1 << bit;
		}

		public void AddAll(BitVector v)
		{
			if(size < v.size)
				Resize(v.size);

			for (int i = 0; i < v.vector.Length; i++)
				vector[i] |= v.vector[i];
		}

		public void AddAll(IEnumerable<int> values)
		{
			if(values != null)
				foreach (int i in values)
					Add(i);
		}

		public void Remove(int elementnr)
		{
			if (elementnr < 0)
				throw new ArgumentOutOfRangeException();

			if (elementnr >= size)
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

		public bool Contains(int elementnr)
		{
			if(elementnr < 0)
				throw new ArgumentOutOfRangeException();

			if (elementnr >= size)
				return false;

			int index = elementnr/32;
			int bit = elementnr%32;

			return ((vector[index] >> bit) & 1) != 0;
			
		}

		public int Count()
		{
			int count=0;
			for(int i = 0; i < vector.Length; i++)
				for(int b = 0; b < 32 ; b++)
					count += ((vector[i] >> b) & 1) != 0 ? 1 : 0;
			return count;
		}

		public bool IsCountZero()
		{
			for (int i = 0; i < vector.Length; i++)
				if (vector[i] != 0)
					return false;
			return true;
		}

		private void Resize(int newSize)
		{
			if(newSize < size)
				return;

			size = (newSize > 2 * size) ? newSize : 2*size;

			size = ((size-1)/32+1) * 32;

			uint [] newVector = new uint[(size-1)/32+1];

			for(int i = 0; i < vector.Length; i++)
				newVector[i] = vector[i];

			vector = newVector;
		}

		override public bool Equals(Object o)
		{
			if (!(o is BitVector))
				return false;

			BitVector bv = (BitVector)o;

			if (vector.Length != bv.vector.Length)
				return false;

			for (int i = 0; i < vector.Length; i++)
				if (vector[i] != bv.vector[i])
					return false;
			return true;
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
//				while (++element < vector.size && !vector.Contains(element))
//				{
//				}
				while (++element < vector.size && ((vector.vector[element/32] >> (element%32)) & 1) == 0)
				{
				}

				return element < vector.size;
			}

			public void Reset()
			{
				element = -1;
			}

			public object Current
			{
				get
				{
					if (element < 0 || element >= vector.size)
						throw new InvalidOperationException();
					return element;
				}
			}

			int IEnumerator<int>.Current
			{
				get
				{
					if (element < 0 || element >= vector.size)
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
	}
}