using System;
using System.Collections.Generic;

namespace CellDotNet.Cuda
{
	public static class ThreadIndex
	{
		public static int X { get { return -1; } }
		public static int Y { get { return -1; } }
		public static int Z { get { return -1; } }
	}

	public static class BlockSize
	{
		public static int X { get { return -1; } }
		public static int Y { get { return -1; } }
		public static int Z { get { return -1; } }
	}

	public static class BlockIndex
	{
		public static int X { get { return -1; } }
		public static int Y { get { return -1; } }
	}
}
