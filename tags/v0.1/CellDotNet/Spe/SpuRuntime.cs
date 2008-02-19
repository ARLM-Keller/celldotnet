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
using System.Collections.Generic;

namespace CellDotNet.Spe
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
