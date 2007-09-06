namespace CellDotNet
{
	internal sealed class BitMatrix
	{
		private int width = 0;
		private int height = 0;

		private uint [,] matrix = new uint[0, 0];

		public BitMatrix(int height, int width)
		{
			resizeMatric(height, width);
		}

		//TODO overvej om den gamle matrice skal genbruges.
		public void clear()
		{
			width = 0;
			height = 0;

			matrix = new uint[0,0];
		}

		public void add(int row, int collum)
		{
			if(row < 0 || collum < 0)
				return;

			if(row >= height || collum >= width)
				resizeMatric(row+1, collum+1);

			int cbit = collum%32;
			int cindex = collum/32;

			matrix[row, cindex] |= (uint) (1 << cbit);
		}

		public void remove(int row, int collum)
		{
			if (0 <= row && row < height && 0 <= collum && collum < width)
			{
				int cbit = collum % 32;
				int cindex = collum / 32;

				matrix[row, cindex] &= ~((uint)(1 << cbit));
			}
		}

		public bool contains(int row, int collum)
		{
			if (0 <= row && row < height && 0 <= collum && collum < width)
			{
				int cbit = collum % 32;
				int cindex = collum / 32;

				return ((matrix[row, cindex] >> cbit) & 1) != 0;
			}
			return false;
		}

		public BitVector GetRow(int row)
		{
			BitVector result = new BitVector();
			for (int i = 0; i < width; i++)
				if(contains(row,i))
					result.Add(i);

			return result;
		}

		private void resizeMatric(int newHeight, int newWidth)
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
