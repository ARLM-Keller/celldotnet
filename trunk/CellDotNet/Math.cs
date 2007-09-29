namespace CellDotNet
{
	internal static class SpuMath
	{
		public static int Abs(int value)
		{
			if (value > 0)
				return value;
			else
				return -value;
		}

		public static int Min(int value1, int value2)
		{
			if (value1 < value2)
				return value1;
			else
				return value2;
		}

		public static int Max(int value1, int value2)
		{
			if (value1 < value2)
				return value2;
			else
				return value1;
		}
	}
}
