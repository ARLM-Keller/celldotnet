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
using NUnit.Framework;



namespace CellDotNet.Spe
{
	[TestFixture]
	public class SpuMathTest : UnitTest
	{
		private delegate T CompareAndSelector<S, T>(S c1, S c2, T e1, T e2);

		private delegate T Operator<T>(T e1, T e2);

		[Test]
		public void TestSpuMath_ConvertToInteger()
		{
			Func<VectorF4, VectorI4> del = SpuMath.ConvertToInteger;

			CompileContext cc = new CompileContext(del.Method);

			cc.PerformProcessing(CompileContextState.S8Complete);

			if (!SpeContext.HasSpeHardware)
				return;

			VectorF4 arg = new VectorF4(32, 65, 8, 3);

			VectorI4 corret = del(arg);

			VectorI4 result = (VectorI4)SpeContext.UnitTestRunProgram(del, arg);

			AreEqual(corret, result);
		}

		[Test]
		public void TestSpuMath_ConvertToFloat()
		{
			Func<VectorI4, VectorF4> del = SpuMath.ConvertToFloat;

			CompileContext cc = new CompileContext(del.Method);

			cc.PerformProcessing(CompileContextState.S8Complete);

			if (!SpeContext.HasSpeHardware)
				return;

			VectorI4 arg = new VectorI4(32, 65, 8, 3);

			VectorF4 corret = del(arg);

			VectorF4 result = (VectorF4)SpeContext.UnitTestRunProgram(del, arg);

			AreEqual(corret, result);
		}

		[Test]
		public void TestSpuMath_CompareGreaterThanAndSelectIntInt()
		{
			CompareAndSelector<VectorI4, VectorI4> del = SpuMath.CompareGreaterThanAndSelect;

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			if(!SpeContext.HasSpeHardware) 
				return;

			VectorI4 arg1 = new VectorI4(32, 65, 8, 3);
			VectorI4 arg2 = new VectorI4(30, 65, 9, -1);
			VectorI4 arg3 = new VectorI4(14, 1243, 324, 3);
			VectorI4 arg4 = new VectorI4(745, 664, 82, 17);

			VectorI4 correct = del(arg1, arg2, arg3, arg4);

			VectorI4 result = (VectorI4)SpeContext.UnitTestRunProgram(del, arg1, arg2, arg3, arg4);

			AreEqual(correct, result);
		}

		[Test]
		public void TestSpuMath_CompareGreaterThanAndSelectIntFloat()
		{
			CompareAndSelector<VectorI4, VectorF4> del = SpuMath.CompareGreaterThanAndSelect;

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			if (!SpeContext.HasSpeHardware)
				return;

			VectorI4 arg1 = new VectorI4(32, 65, 8, 3);
			VectorI4 arg2 = new VectorI4(30, 65, 9, -1);
			VectorF4 arg3 = new VectorF4(14, 1243, 324, 3);
			VectorF4 arg4 = new VectorF4(745, 664, 82, 17);

			VectorF4 correct = del(arg1, arg2, arg3, arg4);

			VectorF4 result = (VectorF4)SpeContext.UnitTestRunProgram(del, arg1, arg2, arg3, arg4);

			AreEqual(correct, result);
		}

		[Test]
		public void TestSpuMath_CompareGreaterThanAndSelectFloatInt()
		{
			CompareAndSelector<VectorF4, VectorI4> del = SpuMath.CompareGreaterThanAndSelect;

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			if (!SpeContext.HasSpeHardware)
				return;

			VectorF4 arg1 = new VectorF4(32, 65, 8, 3);
			VectorF4 arg2 = new VectorF4(30, 65, 9, -1);
			VectorI4 arg3 = new VectorI4(14, 1243, 324, 3);
			VectorI4 arg4 = new VectorI4(745, 664, 82, 17);

			VectorI4 correct = del(arg1, arg2, arg3, arg4);

			VectorI4 result = (VectorI4)SpeContext.UnitTestRunProgram(del, arg1, arg2, arg3, arg4);

			AreEqual(correct, result);
		}

		[Test]
		public void TestSpuMath_CompareGreaterThanAndSelectFloatFloat()
		{
			CompareAndSelector<VectorF4, VectorF4> del = SpuMath.CompareGreaterThanAndSelect;

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			if (!SpeContext.HasSpeHardware)
				return;

			VectorF4 arg1 = new VectorF4(32, 65, 8, 3);
			VectorF4 arg2 = new VectorF4(30, 65, 9, -1);
			VectorF4 arg3 = new VectorF4(14, 1243, 324, 3);
			VectorF4 arg4 = new VectorF4(745, 664, 82, 17);

			VectorF4 correct = del(arg1, arg2, arg3, arg4);

			VectorF4 result = (VectorF4)SpeContext.UnitTestRunProgram(del, arg1, arg2, arg3, arg4);

			AreEqual(correct, result);
		}

		[Test]
		public void TestSpuMath_CompareEqualAndSelectInt()
		{
			CompareAndSelector<VectorI4, VectorI4> del = SpuMath.CompareEqualsAndSelect;

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			if (!SpeContext.HasSpeHardware)
				return;

			VectorI4 arg1 = new VectorI4(32, 65, 8, 3);
			VectorI4 arg2 = new VectorI4(30, 65, 9, -1);
			VectorI4 arg3 = new VectorI4(14, 1243, 324, 3);
			VectorI4 arg4 = new VectorI4(745, 664, 82, 17);

			VectorI4 correct = del(arg1, arg2, arg3, arg4);

			VectorI4 result = (VectorI4)SpeContext.UnitTestRunProgram(del, arg1, arg2, arg3, arg4);

			AreEqual(correct, result);
		}

		[Test]
		public void TestSpuMath_VectorInt_Abs()
		{
			Func<VectorI4, VectorI4> del = SpuMath.Abs;

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			if (!SpeContext.HasSpeHardware)
				return;

			VectorI4 arg = new VectorI4(32, -65, 34568, -4374573);

			VectorI4 correct = del(arg);

			VectorI4 result = (VectorI4)SpeContext.UnitTestRunProgram(del, arg);

			AreEqual(correct, result);
		}

		[Test]
		public void TestSpuMath_VectorInt_Min()
		{
			Operator<VectorI4> del = SpuMath.Min;

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			if (!SpeContext.HasSpeHardware)
				return;

			VectorI4 arg1 = new VectorI4(32, -65, 34568, -44573);
			VectorI4 arg2 = new VectorI4(324, -60, 348, -4374573);

			VectorI4 correct = del(arg1, arg2);

			VectorI4 result = (VectorI4)SpeContext.UnitTestRunProgram(del, arg1, arg2);

			AreEqual(correct, result);
		}

		[Test]
		public void TestSpuMath_VectorInt_Max()
		{
			Operator<VectorI4> del = SpuMath.Max;

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			if (!SpeContext.HasSpeHardware)
				return;

			VectorI4 arg1 = new VectorI4(32, -65, 34568, -44573);
			VectorI4 arg2 = new VectorI4(324, -60, 348, -4374573);

			VectorI4 correct = del(arg1, arg2);

			VectorI4 result = (VectorI4)SpeContext.UnitTestRunProgram(del, arg1, arg2);

			AreEqual(correct, result);
		}

		[Test]
		public void TestSpuMath_VectorF4_Abs()
		{
			Func<VectorF4, VectorF4> del = SpuMath.Abs;

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			if (!SpeContext.HasSpeHardware)
				return;

			VectorF4 arg = new VectorF4(32, -65, 34568, -4374573);

			VectorF4 correct = del(arg);

			VectorF4 result = (VectorF4)SpeContext.UnitTestRunProgram(del, arg);

			AreEqual(correct, result);
		}

		[Test]
		public void TestSpuMath_VectorF4_Min()
		{
			Operator<VectorF4> del = SpuMath.Min;

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			if (!SpeContext.HasSpeHardware)
				return;

			VectorF4 arg1 = new VectorF4(32, -65, 34568, -44573);
			VectorF4 arg2 = new VectorF4(324, -60, 348, -4374573);

			VectorF4 correct = del(arg1, arg2);

			VectorF4 result = (VectorF4)SpeContext.UnitTestRunProgram(del, arg1, arg2);

			AreEqual(correct, result);
		}

		[Test]
		public void TestSpuMath_VectorF4_Max()
		{
			Operator<VectorF4> del = SpuMath.Max;

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			if (!SpeContext.HasSpeHardware)
				return;

			VectorF4 arg1 = new VectorF4(32, -65, 34568, -44573);
			VectorF4 arg2 = new VectorF4(324, -60, 348, -4374573);

			VectorF4 correct = del(arg1, arg2);

			VectorF4 result = (VectorF4)SpeContext.UnitTestRunProgram(del, arg1, arg2);

			AreEqual(correct, result);
		}

		[Test]
		public void TestSpuMath_VectorF4_Sin()
		{
			Func<VectorF4, VectorF4> del = v =>  SpuMath.Sin(v);

			var arg = new VectorF4(1, 2, 3, 4);
			AreWithinLimits(del(arg), (VectorF4) SpeContext.UnitTestRunProgram(del, arg), 0.00001f, null);
		}

		[Test]
		public void TestSpuMath_VectorF4_Cos()
		{
			Func<VectorF4, VectorF4> del = v =>  SpuMath.Cos(v);

			var arg = new VectorF4(1, 2, 3, 4);
			AreWithinLimits(del(arg), (VectorF4) SpeContext.UnitTestRunProgram(del, arg), 0.00001f, null);
		}

		[Test]
		public void TestSpuMath_VectorF4_Tan()
		{
			Func<VectorF4, VectorF4> del = v =>  SpuMath.Tan(v);

			var arg = new VectorF4(1, 2, 3, 4);
			AreWithinLimits(del(arg), (VectorF4) SpeContext.UnitTestRunProgram(del, arg), 0.00001f, null);
		}

		[Test]
		public void TestSpuMath_VectorF4_Asin()
		{
			Func<VectorF4, VectorF4> del = v => SpuMath.Asin(v);

			var arg = new VectorF4(-.5f, 0, .6f, .9f);
			VectorF4 expected = del(arg);
			VectorF4 actual = (VectorF4)SpeContext.UnitTestRunProgram(del, arg);
			AreWithinLimits(expected, actual, 0.00001f, null);
		}

		[Test]
		public void TestSpuMath_VectorF4_Acos()
		{
			Func<VectorF4, VectorF4> del = v => SpuMath.Acos(v);

			var arg = new VectorF4(-.5f, 0, .6f, .9f);
			AreWithinLimits(del(arg), (VectorF4)SpeContext.UnitTestRunProgram(del, arg), 0.00001f, null);
		}

		[Test]
		public void TestSpuMath_VectorF4_Atan()
		{
			Func<VectorF4, VectorF4> del = v =>  SpuMath.Atan(v);

			var arg = new VectorF4(-.5f, 0, .6f, 2);
			AreWithinLimits(del(arg), (VectorF4)SpeContext.UnitTestRunProgram(del, arg), 0.00001f, null);
		}

		[Test]
		public void TestSpuMath_VectorF4_Atan2()
		{
			Func<VectorF4, VectorF4, VectorF4> del = (x, y) => SpuMath.Atan2(x, y);

			var arg1 = new VectorF4(-.5f, 0, .6f, 2);
			var arg2 = new VectorF4(.6f, 2, -.5f, 0);
			AreWithinLimits(del(arg1, arg2), (VectorF4)SpeContext.UnitTestRunProgram(del, arg1, arg2), 0.00001f, null);
		}

		[Test]
		public void TestSpuMath_VectorF4_Log()
		{
			Func<VectorF4, VectorF4> del = v => SpuMath.Log(v);

			var arg = new VectorF4(1, 2, 12.5f, 100);
			AreWithinLimits(del(arg), (VectorF4)SpeContext.UnitTestRunProgram(del, arg), 0.00001f, null);
		}

		[Test]
		public void TestSpuMath_VectorF4_Sqrt()
		{
			Func<VectorF4, VectorF4> del = v => SpuMath.Sqrt(v);

			var arg = new VectorF4(1, 2, 12.5f, 100);
			AreWithinLimits(del(arg), (VectorF4)SpeContext.UnitTestRunProgram(del, arg), 0.00001f, null);
		}

		[Test]
		public void TestSpuMath_VectorD2_Sin()
		{
			Func<VectorD2, VectorD2> del = v => SpuMath.Sin(v);

			var arg = new VectorD2(1, 2);
			AreWithinLimits(del(arg), (VectorD2)SpeContext.UnitTestRunProgram(del, arg), 0.00001f, null);
		}

		[Test]
		public void TestSpuMath_VectorD2_Cos()
		{
			Func<VectorD2, VectorD2> del = v => SpuMath.Cos(v);

			var arg = new VectorD2(1, 2);
			AreWithinLimits(del(arg), (VectorD2)SpeContext.UnitTestRunProgram(del, arg), 0.00001d, null);
		}

		[Test]
		public void TestSpuMath_VectorD2_Tan()
		{
			Func<VectorD2, VectorD2> del = v => SpuMath.Tan(v);

			var arg = new VectorD2(1, 2);
			AreWithinLimits(del(arg), (VectorD2)SpeContext.UnitTestRunProgram(del, arg), 0.00001f, null);
		}

		[Test]
		public void TestSpuMath_VectorD2_Asin()
		{
			Func<VectorD2, VectorD2> del = v => SpuMath.Asin(v);

			var arg = new VectorD2(-.5f, .6f);
			AreWithinLimits(del(arg), (VectorD2)SpeContext.UnitTestRunProgram(del, arg), 0.00001f, null);
		}

		[Test]
		public void TestSpuMath_VectorD2_Acos()
		{
			Func<VectorD2, VectorD2> del = v => SpuMath.Acos(v);

			var arg = new VectorD2(-.5f, .6f);
			AreWithinLimits(del(arg), (VectorD2)SpeContext.UnitTestRunProgram(del, arg), 0.00001f, null);
		}

		[Test]
		public void TestSpuMath_VectorD2_Atan()
		{
			Func<VectorD2, VectorD2> del = v => SpuMath.Atan(v);

			var arg = new VectorD2(-.5f, .6f);
			AreWithinLimits(del(arg), (VectorD2)SpeContext.UnitTestRunProgram(del, arg), 0.00001f, null);
		}

		[Test]
		public void TestSpuMath_VectorD2_Atan2()
		{
			Func<VectorD2, VectorD2, VectorD2> del = (x, y) => SpuMath.Atan2(x, y);

			var arg1 = new VectorD2(-.5f, .6f);
			var arg2 = new VectorD2(.6f, -.5f);
			AreWithinLimits(del(arg1, arg2), (VectorD2)SpeContext.UnitTestRunProgram(del, arg1, arg2), 0.00001f, null);
		}

		[Test]
		public void TestSpuMath_VectorD2_Log()
		{
			Func<VectorD2, VectorD2> del = v => SpuMath.Log(v);

			var arg = new VectorD2(1, 12.5);
			AreWithinLimits(del(arg), (VectorD2)SpeContext.UnitTestRunProgram(del, arg), 0.00001f, null);
		}

		[Test]
		public void TestSpuMath_VectorD2_Sqrt()
		{
			Func<VectorD2, VectorD2> del = v => SpuMath.Sqrt(v);

			var arg = new VectorD2(1, 12.5);
			AreWithinLimits(del(arg), (VectorD2)SpeContext.UnitTestRunProgram(del, arg), 0.00001f, null);
		}

	}
}
#endif