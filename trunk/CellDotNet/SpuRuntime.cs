using System;
using System.Collections.Generic;

namespace CellDotNet
{
	/// <summary>
	/// Contains functionality for code running on a SPE.
	/// </summary>
	static class SpuRuntime
	{
		/// <summary>
		/// Immediately stops execution without throwing an exception.
		/// </summary>
		[IntrinsicMethod(SpuIntrinsicFunction.Runtime_Stop)]
		public static void Stop()
		{
			throw new InvalidOperationException();
		}
	}
}
