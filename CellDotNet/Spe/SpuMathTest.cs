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
			Func<Float32Vector, Int32Vector> del = SpuMath.ConvertToInteger;

			CompileContext cc = new CompileContext(del.Method);

			cc.PerformProcessing(CompileContextState.S8Complete);

			if (!SpeContext.HasSpeHardware)
				return;

			Float32Vector arg = new Float32Vector(32, 65, 8, 3);

			Int32Vector corret = del(arg);

			Int32Vector result = (Int32Vector)SpeContext.UnitTestRunProgram(del, arg);

//			Console.WriteLine("{0}", arg);
//			Console.WriteLine("Correct: {0} result: {1}", corret, result);

			AreEqual(corret, result);
		}

		[Test]
		public void TestSpuMath_ConvertToFloat()
		{
			Func<Int32Vector, Float32Vector> del = SpuMath.ConvertToFloat;

			CompileContext cc = new CompileContext(del.Method);

			cc.PerformProcessing(CompileContextState.S8Complete);

			if (!SpeContext.HasSpeHardware)
				return;

			Int32Vector arg = new Int32Vector(32, 65, 8, 3);

			Float32Vector corret = del(arg);

			Float32Vector result = (Float32Vector)SpeContext.UnitTestRunProgram(del, arg);

			//			Console.WriteLine("{0}", arg);
			//			Console.WriteLine("Correct: {0} result: {1}", corret, result);

			AreEqual(corret, result);
		}

		[Test]
		public void TestSpuMath_CompareGreaterThanAndSelectIntInt()
		{
			CompareAndSelector<Int32Vector, Int32Vector> del = SpuMath.CompareGreaterThanAndSelect;

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			if(!SpeContext.HasSpeHardware) 
				return;

			Int32Vector arg1 = new Int32Vector(32, 65, 8, 3);
			Int32Vector arg2 = new Int32Vector(30, 65, 9, -1);
			Int32Vector arg3 = new Int32Vector(14, 1243, 324, 3);
			Int32Vector arg4 = new Int32Vector(745, 664, 82, 17);

			Int32Vector correct = del(arg1, arg2, arg3, arg4);

			Int32Vector result = (Int32Vector)SpeContext.UnitTestRunProgram(del, arg1, arg2, arg3, arg4);

//			Console.WriteLine("{0} {1} {2} {3}", arg1, arg2, arg3, arg4);
//			Console.WriteLine("Correct: {0} result: {1}", correct, result);

			AreEqual(correct, result);
		}

		[Test]
		public void TestSpuMath_CompareGreaterThanAndSelectIntFloat()
		{
			CompareAndSelector<Int32Vector, Float32Vector> del = SpuMath.CompareGreaterThanAndSelect;

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			if (!SpeContext.HasSpeHardware)
				return;

			Int32Vector arg1 = new Int32Vector(32, 65, 8, 3);
			Int32Vector arg2 = new Int32Vector(30, 65, 9, -1);
			Float32Vector arg3 = new Float32Vector(14, 1243, 324, 3);
			Float32Vector arg4 = new Float32Vector(745, 664, 82, 17);

			Float32Vector correct = del(arg1, arg2, arg3, arg4);

			Float32Vector result = (Float32Vector)SpeContext.UnitTestRunProgram(del, arg1, arg2, arg3, arg4);

//			Console.WriteLine("{0} {1} {2} {3}", arg1, arg2, arg3, arg4);
//			Console.WriteLine("Correct: {0} result: {1}", corret, result);

			AreEqual(correct, result);
		}

		[Test]
		public void TestSpuMath_CompareGreaterThanAndSelectFloatInt()
		{
			CompareAndSelector<Float32Vector, Int32Vector> del = SpuMath.CompareGreaterThanAndSelect;

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			if (!SpeContext.HasSpeHardware)
				return;

			Float32Vector arg1 = new Float32Vector(32, 65, 8, 3);
			Float32Vector arg2 = new Float32Vector(30, 65, 9, -1);
			Int32Vector arg3 = new Int32Vector(14, 1243, 324, 3);
			Int32Vector arg4 = new Int32Vector(745, 664, 82, 17);

			Int32Vector correct = del(arg1, arg2, arg3, arg4);

			Int32Vector result = (Int32Vector)SpeContext.UnitTestRunProgram(del, arg1, arg2, arg3, arg4);

//			Console.WriteLine("{0} {1} {2} {3}", arg1, arg2, arg3, arg4);
//			Console.WriteLine("Correct: {0} result: {1}", correct, result);

			AreEqual(correct, result);
		}

		[Test]
		public void TestSpuMath_CompareGreaterThanAndSelectFloatFloat()
		{
			CompareAndSelector<Float32Vector, Float32Vector> del = SpuMath.CompareGreaterThanAndSelect;

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			if (!SpeContext.HasSpeHardware)
				return;

			Float32Vector arg1 = new Float32Vector(32, 65, 8, 3);
			Float32Vector arg2 = new Float32Vector(30, 65, 9, -1);
			Float32Vector arg3 = new Float32Vector(14, 1243, 324, 3);
			Float32Vector arg4 = new Float32Vector(745, 664, 82, 17);

			Float32Vector correct = del(arg1, arg2, arg3, arg4);

			Float32Vector result = (Float32Vector)SpeContext.UnitTestRunProgram(del, arg1, arg2, arg3, arg4);

//			Console.WriteLine("{0} {1} {2} {3}", arg1, arg2, arg3, arg4);
//			Console.WriteLine("Correct: {0} result: {1}", correct, result);

			AreEqual(correct, result);
		}

		[Test]
		public void TestSpuMath_CompareEqualAndSelectInt()
		{
			CompareAndSelector<Int32Vector, Int32Vector> del = SpuMath.CompareEqualsAndSelect;

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			if (!SpeContext.HasSpeHardware)
				return;

			Int32Vector arg1 = new Int32Vector(32, 65, 8, 3);
			Int32Vector arg2 = new Int32Vector(30, 65, 9, -1);
			Int32Vector arg3 = new Int32Vector(14, 1243, 324, 3);
			Int32Vector arg4 = new Int32Vector(745, 664, 82, 17);

			Int32Vector correct = del(arg1, arg2, arg3, arg4);

			Int32Vector result = (Int32Vector)SpeContext.UnitTestRunProgram(del, arg1, arg2, arg3, arg4);

//			Console.WriteLine("{0} {1} {2} {3}", arg1, arg2, arg3, arg4);
//			Console.WriteLine("Correct: {0} result: {1}", correct, result);

			AreEqual(correct, result);
		}

		[Test]
		public void TestSpuMath_VectorInt_Abs()
		{
			Func<Int32Vector, Int32Vector> del = SpuMath.Abs;

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			if (!SpeContext.HasSpeHardware)
				return;

			Int32Vector arg = new Int32Vector(32, -65, 34568, -4374573);

			Int32Vector correct = del(arg);

			Int32Vector result = (Int32Vector)SpeContext.UnitTestRunProgram(del, arg);

			AreEqual(correct, result);
		}

		[Test]
		public void TestSpuMath_VectorInt_Min()
		{
			Operator<Int32Vector> del = SpuMath.Min;

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			if (!SpeContext.HasSpeHardware)
				return;

			Int32Vector arg1 = new Int32Vector(32, -65, 34568, -44573);
			Int32Vector arg2 = new Int32Vector(324, -60, 348, -4374573);

			Int32Vector correct = del(arg1, arg2);

			Int32Vector result = (Int32Vector)SpeContext.UnitTestRunProgram(del, arg1, arg2);

			AreEqual(correct, result);
		}

		[Test]
		public void TestSpuMath_VectorInt_Max()
		{
			Operator<Int32Vector> del = SpuMath.Max;

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			if (!SpeContext.HasSpeHardware)
				return;

			Int32Vector arg1 = new Int32Vector(32, -65, 34568, -44573);
			Int32Vector arg2 = new Int32Vector(324, -60, 348, -4374573);

			Int32Vector correct = del(arg1, arg2);

			Int32Vector result = (Int32Vector)SpeContext.UnitTestRunProgram(del, arg1, arg2);

			AreEqual(correct, result);
		}

		[Test]
		public void TestSpuMath_VectorFloat_Abs()
		{
			Func<Float32Vector, Float32Vector> del = SpuMath.Abs;

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			if (!SpeContext.HasSpeHardware)
				return;

			Float32Vector arg = new Float32Vector(32, -65, 34568, -4374573);

			Float32Vector correct = del(arg);

			Float32Vector result = (Float32Vector)SpeContext.UnitTestRunProgram(del, arg);

			AreEqual(correct, result);
		}

		[Test]
		public void TestSpuMath_VectorFloat_Min()
		{
			Operator<Float32Vector> del = SpuMath.Min;

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			if (!SpeContext.HasSpeHardware)
				return;

			Float32Vector arg1 = new Float32Vector(32, -65, 34568, -44573);
			Float32Vector arg2 = new Float32Vector(324, -60, 348, -4374573);

			Float32Vector correct = del(arg1, arg2);

			Float32Vector result = (Float32Vector)SpeContext.UnitTestRunProgram(del, arg1, arg2);

			AreEqual(correct, result);
		}

		[Test]
		public void TestSpuMath_VectorFloat_Max()
		{
			Operator<Float32Vector> del = SpuMath.Max;

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			if (!SpeContext.HasSpeHardware)
				return;

			Float32Vector arg1 = new Float32Vector(32, -65, 34568, -44573);
			Float32Vector arg2 = new Float32Vector(324, -60, 348, -4374573);

			Float32Vector correct = del(arg1, arg2);

			Float32Vector result = (Float32Vector)SpeContext.UnitTestRunProgram(del, arg1, arg2);

			AreEqual(correct, result);
		}
	}
}
#endif