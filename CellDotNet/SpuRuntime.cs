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
		[IntrinsicMethod(SpuIntrinsicMethod.Runtime_Stop)]
		public static void Stop()
		{
			throw new InvalidOperationException();
		}

		[SpuOpCode(SpuOpCodeEnum.Stop)]
		internal static void Stop(
			[SpuInstructionPart(SpuInstructionPart.Immediate)]SpuStopCode stopcode)
		{
			throw new InvalidOperationException();
		}

		public static bool IsRunningOnSpu
		{
			get 
			{
				// Dont be fooled; on the spu this property will evaluate to true.
				return false;
			}
		}

		[IntrinsicMethod(SpuIntrinsicMethod.ReturnArgument1)]
		internal static int UnsafeGetAddress<T>(T obj) where T : class
		{
			throw new InvalidOperationException();
		}
	}
}
