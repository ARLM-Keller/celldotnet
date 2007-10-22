// 
// Copyright (C) 2007 Klaus Hansen and Rasmus Halland
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Text;

namespace CellDotNet
{
	internal sealed class BitMatrix
	{
		private BitVector[] matrix = new BitVector[0];

		public void Clear()
		{
			matrix = new BitVector[0];
		}

		public void add(int row, int collum)
		{
			if (row < 0 || collum < 0)
				return;

			resizeMatric(row + 1);

			matrix[row].Add(collum);
		}

		public void remove(int row, int collum)
		{
			if(row >= 0 && row < matrix.Length)
				matrix[row].Remove(collum);
		}

		public bool contains(int row, int collum)
		{
			if (row < 0 || row >= matrix.Length)
				return false;

			return matrix[row].Contains(collum);
		}

		public bool RowEquals(int row, BitVector v)
		{
			if (row < 0)
				return true;

			if (row >= matrix.Length)
				return v.IsCountZero();

			return matrix[row].Equals(v);
	}


		public BitVector GetRow(int row)
		{
			if (row < 0 || row >= matrix.Length)
				return null; //TODO muligvis bedre håndtering af denne situation.

			return matrix[row];
		}

		public bool IsSymetric()
		{
			int maxSize = matrix.Length;
			for (int i = 0; i < matrix.Length; i++)
				maxSize = (matrix[i].Size > maxSize) ? matrix[i].Size : maxSize;

			bool result = true;
			for (int row = 0; row < maxSize; row++)
				for (int col = 0; col < maxSize; col++)
//				for (int col = 1 + row; col < maxSize; col++)
					result &= contains(row, col) == contains(col, row);

			return result;
		}

		private void resizeMatric(int newHeight)
		{
			if (newHeight <= matrix.Length)
				return;

			if (newHeight > matrix.Length)
			{
				if (newHeight < 2 * matrix.Length)
					newHeight = 2 * matrix.Length;
			}

			BitVector[] newMatrix = new BitVector[newHeight];

			for (int i = 0; i < matrix.Length; i++ )
				newMatrix[i] = matrix[i];

			for (int i = matrix.Length; i < newMatrix.Length; i++)
				newMatrix[i] = new BitVector();

			matrix = newMatrix;
		}

		public string PrintFullMatrix()
		{
			StringBuilder text = new StringBuilder();
			int maxWidth = 0;
			for(int row = 0; row < matrix.Length; row++)
			{
				if (matrix[row].SizeTrim() > maxWidth)
					maxWidth = matrix[row].SizeTrim();
			}

			for (int row = 0; row < matrix.Length; row++)
			{
				text.Append(matrix[row].PrintFullVector(maxWidth));
				text.AppendLine();
			}

			return text.ToString();
		}
	}
}
