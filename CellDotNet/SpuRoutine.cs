using System;
using System.Collections.Generic;

namespace CellDotNet
{
	/// <summary>
	/// Represents anything that can be emitted as SPU code.
	/// </summary>
	abstract class SpuRoutine : ObjectWithAddress
	{
		/// <summary>
		/// Implementations of this method should return the emitted binary code.
		/// </summary>
		/// <returns></returns>
		public abstract int[] Emit();
	}
}
