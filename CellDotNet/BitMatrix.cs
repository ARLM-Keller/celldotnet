using System;

namespace CellDotNet
{
	internal sealed class BitMatrix
	{
		private int width = 0;
		private int height = 0;

		private uint [,] matrix = new uint[0, 0];

		public BitMatrix(int height, int width)
		{
			ResizeMatrix(height, width);
		}

		//TODO overvej om den gamle matrice skal genbruges.
		public void Clear()
		{
			width = 0;
			height = 0;

			matrix = new uint[0,0];
		}

		static void GetIndices(ref int row, ref int column, out int cindex, out int cbit)
		{
			int rowtemp = row;
			int columntemp = column;

			row = Math.Max(rowtemp, columntemp);
			column = Math.Min(rowtemp, columntemp);

			cbit = column % 32;
			cindex = column / 32;
		}

		public void Add(int row, int collum)
		{
			if(row < 0 || collum < 0)
				return;

			int cbit;
			int cindex;
			GetIndices(ref row, ref collum, out cindex, out cbit);

			if(row >= height || collum >= width)
				ResizeMatrix(row+1, collum+1);

			matrix[row, cindex] |= (uint) (1 << cbit);
		}

		public void Remove(int row, int collum)
		{
			int cbit;
			int cindex;
			GetIndices(ref row, ref collum, out cindex, out cbit);

			if (((0 <= row && row < height) && 0 <= collum) && collum < width)
				matrix[row, cindex] &= ~((uint) (1 << cbit));
		}

		public bool Contains(int row, int collum)
		{
			int cbit;
			int cindex;
			GetIndices(ref row, ref collum, out cindex, out cbit);

			if (((0 <= row && row < height) && 0 <= collum) && collum < width)
				return ((matrix[row, cindex] >> cbit) & 1) != 0;

			return false;
		}

		private void ResizeMatrix(int newHeight, int newWidth)
		{
			if (newHeight <= height && newWidth <= width)
				return;

			if (newHeight > height)
			{
				if (newHeight < 2 * height)
					height = 2 * height;
				else
					height = newHeight;
			}

			if (newWidth > width)
			{
				if (newWidth < 2 * width)
					width = 2 * width;
				else
					width = newWidth;
			}

			width = ((width - 1) / 32 + 1) * 32;

			uint[,] newMatrix = new uint[height, (width - 1) / 32 + 1];

			for (int i = 0; i < matrix.GetLength(0); i++)
			{
				for (int j = 0; j < matrix.GetLength(1); j++)
				{
					newMatrix[i, j] = matrix[i, j];
				}
			}

			matrix = newMatrix;
		}
	}
}
