using System;
using NUnit.Framework;

namespace CellDotNet
{
	[TestFixture]
	public class SpuMathTest : UnitTest
	{
		private delegate T ComparAndSelector<S, T>(S c1, S c2, T e1, T e2);

		private delegate T Operator<T>(T e1, T e2);

		[Test]
		public void TestSpuMath_ConvertToInteger()
		{
			Converter<Float32Vector, Int32Vector> del =
				delegate(Float32Vector v) { return SpuMath.ConvertToInteger(v); };

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
			Converter<Int32Vector, Float32Vector> del =
				delegate(Int32Vector v) { return SpuMath.ConvertToFloat(v); };

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
			ComparAndSelector<Int32Vector, Int32Vector> del =
				delegate(Int32Vector c1, Int32Vector c2, Int32Vector e1, Int32Vector e2) { return SpuMath.CompareGreaterThanAndSelect(c1, c2, e1, e2); };

			CompileContext cc = new CompileContext(del.Method);

			cc.PerformProcessing(CompileContextState.S8Complete);

			if(!SpeContext.HasSpeHardware) 
				return;

			Int32Vector arg1 = new Int32Vector(32, 65, 8, 3);
			Int32Vector arg2 = new Int32Vector(30, 65, 9, -1);
			Int32Vector arg3 = new Int32Vector(14, 1243, 324, 3);
			Int32Vector arg4 = new Int32Vector(745, 664, 82, 17);

			Int32Vector corret = del(arg1, arg2, arg3, arg4);

			Int32Vector result = (Int32Vector)SpeContext.UnitTestRunProgram(del, arg1, arg2, arg3, arg4);

			Console.WriteLine("{0} {1} {2} {3}", arg1, arg2, arg3, arg4);
			Console.WriteLine("Correct: {0} result: {1}", corret, result);

			AreEqual(corret, result);
		}

		[Test]
		public void TestSpuMath_CompareGreaterThanAndSelectIntFloat()
		{
			ComparAndSelector<Int32Vector, Float32Vector> del =
				delegate(Int32Vector c1, Int32Vector c2, Float32Vector e1, Float32Vector e2) { return SpuMath.CompareGreaterThanAndSelect(c1, c2, e1, e2); };

			CompileContext cc = new CompileContext(del.Method);

			cc.PerformProcessing(CompileContextState.S8Complete);

			if (!SpeContext.HasSpeHardware)
				return;

			Int32Vector arg1 = new Int32Vector(32, 65, 8, 3);
			Int32Vector arg2 = new Int32Vector(30, 65, 9, -1);
			Float32Vector arg3 = new Float32Vector(14, 1243, 324, 3);
			Float32Vector arg4 = new Float32Vector(745, 664, 82, 17);

			Float32Vector corret = del(arg1, arg2, arg3, arg4);

			Float32Vector result = (Float32Vector)SpeContext.UnitTestRunProgram(del, arg1, arg2, arg3, arg4);

			Console.WriteLine("{0} {1} {2} {3}", arg1, arg2, arg3, arg4);
			Console.WriteLine("Correct: {0} result: {1}", corret, result);

			AreEqual(corret, result);
		}

		[Test]
		public void TestSpuMath_CompareGreaterThanAndSelectFloatInt()
		{
			ComparAndSelector<Float32Vector, Int32Vector> del =
				delegate(Float32Vector c1, Float32Vector c2, Int32Vector e1, Int32Vector e2) { return SpuMath.CompareGreaterThanAndSelect(c1, c2, e1, e2); };

			CompileContext cc = new CompileContext(del.Method);

			cc.PerformProcessing(CompileContextState.S8Complete);

			if (!SpeContext.HasSpeHardware)
				return;

			Float32Vector arg1 = new Float32Vector(32, 65, 8, 3);
			Float32Vector arg2 = new Float32Vector(30, 65, 9, -1);
			Int32Vector arg3 = new Int32Vector(14, 1243, 324, 3);
			Int32Vector arg4 = new Int32Vector(745, 664, 82, 17);

			Int32Vector corret = del(arg1, arg2, arg3, arg4);

			Int32Vector result = (Int32Vector)SpeContext.UnitTestRunProgram(del, arg1, arg2, arg3, arg4);

			Console.WriteLine("{0} {1} {2} {3}", arg1, arg2, arg3, arg4);
			Console.WriteLine("Correct: {0} result: {1}", corret, result);

			AreEqual(corret, result);
		}

		[Test]
		public void TestSpuMath_CompareGreaterThanAndSelectFloatFloat()
		{
			ComparAndSelector<Float32Vector, Float32Vector> del =
				delegate(Float32Vector c1, Float32Vector c2, Float32Vector e1, Float32Vector e2) { return SpuMath.CompareGreaterThanAndSelect(c1, c2, e1, e2); };

			CompileContext cc = new CompileContext(del.Method);

			cc.PerformProcessing(CompileContextState.S8Complete);

			if (!SpeContext.HasSpeHardware)
				return;

			Float32Vector arg1 = new Float32Vector(32, 65, 8, 3);
			Float32Vector arg2 = new Float32Vector(30, 65, 9, -1);
			Float32Vector arg3 = new Float32Vector(14, 1243, 324, 3);
			Float32Vector arg4 = new Float32Vector(745, 664, 82, 17);

			Float32Vector corret = del(arg1, arg2, arg3, arg4);

			Float32Vector result = (Float32Vector)SpeContext.UnitTestRunProgram(del, arg1, arg2, arg3, arg4);

			Console.WriteLine("{0} {1} {2} {3}", arg1, arg2, arg3, arg4);
			Console.WriteLine("Correct: {0} result: {1}", corret, result);

			AreEqual(corret, result);
		}

		[Test]
		public void TestSpuMath_CompareEqualAndSelectInt()
		{
			ComparAndSelector<Int32Vector, Int32Vector> del =
				delegate(Int32Vector c1, Int32Vector c2, Int32Vector e1, Int32Vector e2) { return SpuMath.CompareEqualsAndSelect(c1, c2, e1, e2); };

			CompileContext cc = new CompileContext(del.Method);

			cc.PerformProcessing(CompileContextState.S8Complete);

			if (!SpeContext.HasSpeHardware)
				return;

			Int32Vector arg1 = new Int32Vector(32, 65, 8, 3);
			Int32Vector arg2 = new Int32Vector(30, 65, 9, -1);
			Int32Vector arg3 = new Int32Vector(14, 1243, 324, 3);
			Int32Vector arg4 = new Int32Vector(745, 664, 82, 17);

			Int32Vector corret = del(arg1, arg2, arg3, arg4);

			Int32Vector result = (Int32Vector)SpeContext.UnitTestRunProgram(del, arg1, arg2, arg3, arg4);

			Console.WriteLine("{0} {1} {2} {3}", arg1, arg2, arg3, arg4);
			Console.WriteLine("Correct: {0} result: {1}", corret, result);

			AreEqual(corret, result);
		}

		[Test]
		public void TestSpuMath_VectorInt_Abs()
		{
			Converter<Int32Vector, Int32Vector> del =
				delegate(Int32Vector v) { return SpuMath.Abs(v); };

			CompileContext cc = new CompileContext(del.Method);

			cc.PerformProcessing(CompileContextState.S8Complete);

			if (!SpeContext.HasSpeHardware)
				return;

			Int32Vector arg = new Int32Vector(32, -65, 34568, -4374573);

			Int32Vector corret = del(arg);

			Int32Vector result = (Int32Vector)SpeContext.UnitTestRunProgram(del, arg);

			AreEqual(corret, result);
		}

		[Test]
		public void TestSpuMath_VectorInt_Min()
		{
			Operator<Int32Vector> del =
				delegate(Int32Vector v1, Int32Vector v2) { return SpuMath.Min(v1, v2); };

			CompileContext cc = new CompileContext(del.Method);

			cc.PerformProcessing(CompileContextState.S8Complete);

			if (!SpeContext.HasSpeHardware)
				return;

			Int32Vector arg1 = new Int32Vector(32, -65, 34568, -44573);
			Int32Vector arg2 = new Int32Vector(324, -60, 348, -4374573);

			Int32Vector corret = del(arg1, arg2);

			Int32Vector result = (Int32Vector)SpeContext.UnitTestRunProgram(del, arg1, arg2);

			AreEqual(corret, result);
		}

		[Test]
		public void TestSpuMath_VectorInt_Max()
		{
			Operator<Int32Vector> del =
				delegate(Int32Vector v1, Int32Vector v2) { return SpuMath.Max(v1, v2); };

			CompileContext cc = new CompileContext(del.Method);

			cc.PerformProcessing(CompileContextState.S8Complete);

			if (!SpeContext.HasSpeHardware)
				return;

			Int32Vector arg1 = new Int32Vector(32, -65, 34568, -44573);
			Int32Vector arg2 = new Int32Vector(324, -60, 348, -4374573);

			Int32Vector corret = del(arg1, arg2);

			Int32Vector result = (Int32Vector)SpeContext.UnitTestRunProgram(del, arg1, arg2);

			AreEqual(corret, result);
		}

		[Test]
		public void TestSpuMath_VectorFloat_Abs()
		{
			Converter<Float32Vector, Float32Vector> del =
				delegate(Float32Vector v) { return SpuMath.Abs(v); };

			CompileContext cc = new CompileContext(del.Method);

			cc.PerformProcessing(CompileContextState.S8Complete);

			if (!SpeContext.HasSpeHardware)
				return;

			Float32Vector arg = new Float32Vector(32, -65, 34568, -4374573);

			Float32Vector corret = del(arg);

			Float32Vector result = (Float32Vector)SpeContext.UnitTestRunProgram(del, arg);

			AreEqual(corret, result);
		}

		[Test]
		public void TestSpuMath_VectorFloat_Min()
		{
			Operator<Float32Vector> del =
				delegate(Float32Vector v1, Float32Vector v2) { return SpuMath.Min(v1, v2); };

			CompileContext cc = new CompileContext(del.Method);

			cc.PerformProcessing(CompileContextState.S8Complete);

			if (!SpeContext.HasSpeHardware)
				return;

			Float32Vector arg1 = new Float32Vector(32, -65, 34568, -44573);
			Float32Vector arg2 = new Float32Vector(324, -60, 348, -4374573);

			Float32Vector corret = del(arg1, arg2);

			Float32Vector result = (Float32Vector)SpeContext.UnitTestRunProgram(del, arg1, arg2);

			AreEqual(corret, result);
		}

		[Test]
		public void TestSpuMath_VectorFloat_Max()
		{
			Operator<Float32Vector> del =
				delegate(Float32Vector v1, Float32Vector v2) { return SpuMath.Max(v1, v2); };

			CompileContext cc = new CompileContext(del.Method);

			cc.PerformProcessing(CompileContextState.S8Complete);

			if (!SpeContext.HasSpeHardware)
				return;

			Float32Vector arg1 = new Float32Vector(32, -65, 34568, -44573);
			Float32Vector arg2 = new Float32Vector(324, -60, 348, -4374573);

			Float32Vector corret = del(arg1, arg2);

			Float32Vector result = (Float32Vector)SpeContext.UnitTestRunProgram(del, arg1, arg2);

			AreEqual(corret, result);
		}

	}
}
