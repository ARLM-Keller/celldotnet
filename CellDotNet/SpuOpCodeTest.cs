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

#if UNITTEST

using System;
using System.Collections.Generic;
using NUnit.Framework;



namespace CellDotNet
{
	[TestFixture]
	public class SpuOpCodeTest : UnitTest
	{
		[Test]
		public void TestChannelOpCode()
		{
			IsFalse(SpuOpCode.rchcnt.RegisterRtNotWritten);
			IsTrue(SpuOpCode.rchcnt.HasImmediate);
			AreEqual(SpuInstructionPart.Rt | SpuInstructionPart.Ca, SpuOpCode.rchcnt.Parts);

			IsFalse(SpuOpCode.rdch.RegisterRtNotWritten);
			IsTrue(SpuOpCode.rdch.HasImmediate);
			AreEqual(SpuInstructionPart.Rt | SpuInstructionPart.Ca, SpuOpCode.rdch.Parts);

			IsTrue(SpuOpCode.wrch.RegisterRtNotWritten);
			IsTrue(SpuOpCode.wrch.HasImmediate);
			AreEqual(SpuInstructionPart.Rt | SpuInstructionPart.Ca, SpuOpCode.wrch.Parts);
		}

		[Test]
		public void TestLoad()
		{
			IsFalse(SpuOpCode.lqd.RegisterRtNotWritten);
			IsTrue(SpuOpCode.lqd.HasImmediate);
			AreEqual(SpuInstructionPart.Rt | SpuInstructionPart.Ra | SpuInstructionPart.Immediate, SpuOpCode.lqd.Parts);
		}

		[Test]
		public void TestStore()
		{
			IsTrue(SpuOpCode.stqd.RegisterRtNotWritten);
			IsTrue(SpuOpCode.stqd.HasImmediate);
			AreEqual(SpuInstructionPart.Rt | SpuInstructionPart.Ra | SpuInstructionPart.Immediate, SpuOpCode.stqd.Parts);
		}
	}
}
#endif