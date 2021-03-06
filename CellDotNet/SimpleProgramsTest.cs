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


namespace CellDotNet.Spe
{
	/// <summary>
	/// For simple programs. Test focused on arrays and object should go into <see cref="ObjectModelTest"/>.
	/// </summary>
	[TestFixture]
	public class SimpleProgramsTest : UnitTest
	{
		[Test]
		public void TestDump()
		{
			Func<int, int> del = input => input + 0xf0f0;
			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			cc.DisassembleToFile("dumpx.s", 0xaeae);
		}

		[Test]
		public void TestLoop_SumInt()
		{
			Func<int> del =
				delegate
					{
						const int count = 5;
						int sum = 0;
						for (int i = 0; i < count; i++)
							sum += i*i;
						return sum;
					};

			int correctVal = del();

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext sc = new SpeContext())
			{
				object rv = sc.RunProgram(cc);
				AreEqual(correctVal, (int)rv);
			}
		}

		[Test]
		public void TestLoop_SumFloat()
		{
			Func<float> del =
				delegate
					{
						const int count = 5;
						float sum = 0;
						for (int i = 0; i < count; i++)
							sum += (float) i*(float) i;
						return sum;
					};

			float correctVal = del();

			Func<float> del2 = SpeDelegateRunner.CreateSpeDelegate(del);

			if (!SpeContext.HasSpeHardware)
				return;

			AreEqual(correctVal, del2());
		}

		[Test]
		public void TestLoop_SumFloat2()
		{
			Func<float> del =
				delegate
					{
						const int count = 5;
						float sum = 0;
						int[] arr = new int[count];

						for (int i = 0; i < count; i++)
							arr[i] = i;
						for (int i = 0; i < count; i++)
							sum += arr[i];

						return sum;
					};

			float correctVal = del();
			Func<float> del2 = SpeDelegateRunner.CreateSpeDelegate(del);
			if (!SpeContext.HasSpeHardware)
				return;

			AreEqual(correctVal, del2());
		}

//		[Test]
//		public void TestReturnConditional()
//		{
//			Func<int, int> del = 
//				delegate(int input)
//					{
//						// Tests that 
//						if (input == 1)
//							return 10;
//						else
//							return 20;
//					};
//			int arg = 1;
//			CompileContext cc = new CompileContext(del.Method);
//
//			AreEqual(del(arg), (int) SpeContext.UnitTestRunProgram(cc, arg));
//		}

#region Method call

		[Test]
		public void TestMethodCall()
		{
			Func<int, int> del = MethodCallMethod;

			const int arg = 15;
			int correctval = del(arg);

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext sc = new SpeContext())
			{
				object rv = sc.RunProgram(cc, arg);
				AreEqual(correctval, (int) rv);
			}
		}

		private static int ReturnInt(int i)
		{
			return i;
		}

		private static int MethodCallMethod(int level)
		{
			return 3 + ReturnInt(10);
		}

		#endregion

		// **************************************************
		// TestRecursiveSummation_Int
		// **************************************************

		[Test]
		public void TestRecursiveSummation_Int()
		{
			Func<int, int> del = RecursiveSummation_Int;

			const int arg = 15;
			int correctval = del(arg);
			AreNotEqual(0, correctval, "Zero isn't good.");

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext sc = new SpeContext())
			{
				object rv = sc.RunProgram(cc, arg);
				AreEqual(correctval, (int) rv);
			}
		}

		/// <summary>
		/// This one just does some weird recursion.
		/// </summary>
		/// <param name="level"></param>
		/// <returns></returns>
		private static int RecursiveSummation_Int(int level)
		{
			if (level == 0)
				return 0;
			else if ((level & 3) == 1)
				return level + RecursiveSummation_Int(level - 1);
			else
				return RecursiveSummation_Int(level - 1);
		}

		// **************************************************
		// TestRecursiveSummation_Float
		// **************************************************

		[Test]
		public void TestRecursiveSummation_Float()
		{
			Func<int, float> del = RecursiveSummation_Float;

			const int arg = 15;
			float correctval = del(arg);
			AreNotEqual(0f, correctval, "Zero isn't good.");

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext sc = new SpeContext())
			{
				object rv = sc.RunProgram(cc, arg);
				AreEqual(correctval, (float) rv);
			}
		}

		/// <summary>
		/// This one just does some weird recursion.
		/// </summary>
		/// <param name="level"></param>
		/// <returns></returns>
		private static float RecursiveSummation_Float(int level)
		{
			if (level == 0)
				return 0;
			else if ((level & 3) == 1)
				return level + RecursiveSummation_Float(level - 1);
			else
				return RecursiveSummation_Float(level - 1);
		}

		[Test]
		public void TestConditionalExpression()
		{
			Func<int, int> fun =
				delegate(int input)
					{
						return input + (input == 17 ? 0 : input);
					};

			CompileContext cc = new CompileContext(fun.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			object result1 = SpeContext.UnitTestRunProgram(cc, 14);
			object result2 = SpeContext.UnitTestRunProgram(cc, 17);

			AreEqual(28, (int)result1);
			AreEqual(17, (int)result2);
		}

		[Test]
		public void TestDivisionSigned()
		{
			Func<int, int> fun =
				delegate(int input)
				{
					return input / 7;
				};

			CompileContext cc = new CompileContext(fun.Method);

			cc.PerformProcessing(CompileContextState.S8Complete);

			object result1 = SpeContext.UnitTestRunProgram(cc, 14);
			object result2 = SpeContext.UnitTestRunProgram(cc, 17);
			object result3 = SpeContext.UnitTestRunProgram(cc, 1952);

			AreEqual(14/7, (int)result1);
			AreEqual(17/7, (int)result2);
			AreEqual(1952/7, (int)result3);
		}

		[Test]
		public void TestDivisionFloat()
		{
			Func<float, float> fun =
				delegate(float input)
				{
					return input / 3.14f;
				};

			CompileContext cc = new CompileContext(fun.Method);

			cc.PerformProcessing(CompileContextState.S8Complete);

			object result1 = SpeContext.UnitTestRunProgram(cc, 6.28f);
			object result2 = SpeContext.UnitTestRunProgram(cc, 123.45f);
			object result3 = SpeContext.UnitTestRunProgram(cc, 435643.23f);

			AreWithinLimits(6.28f / 3.14f, (float)result1, 0.000001f, "6.28f / 3.14f failed.");
			AreWithinLimits(123.45f / 3.14f, (float)result2, 0.000001f, "123.45f / 3.14f failed.");
			AreWithinLimits(435643.23f / 3.14f, (float)result3, 0.000001f, "435643.23f / 3.14f failed.");
		}

		[Test]
		public void TestImplementationDivisionUnsigned()
		{
			Assert.AreEqual(SpuMath.Div_Un(14, 7), ((uint)14)/((uint)7), "SpuMath.Div_Un failed.");
			Assert.AreEqual(SpuMath.Div_Un(33, 7), ((uint)33) / ((uint)7), "SpuMath.Div_Un failed.");
			Assert.AreEqual(SpuMath.Div_Un(12345, 54), ((uint)12345) / ((uint)54), "SpuMath.Div_Un failed.");
			Assert.AreEqual(SpuMath.Div_Un(987536, 664), ((uint)987536) / ((uint)664), "SpuMath.Div_Un failed.");
			Assert.AreEqual(SpuMath.Div_Un(9675, 745), ((uint)9675) / ((uint)745), "SpuMath.Div_Un failed.");
			Assert.AreEqual(SpuMath.Div_Un(123454, 3), ((uint)123454) / ((uint)3), "SpuMath.Div_Un failed.");
			Assert.AreEqual(SpuMath.Div_Un(16524, 23), ((uint)16524) / ((uint)23), "SpuMath.Div_Un failed.");
		}

		[Test]
		public void TestImplementationDivisionSigned()
		{
			Assert.AreEqual(SpuMath.Div(14, 7), 14 / 7, "SpuMath.Div failed.");
			Assert.AreEqual(SpuMath.Div(33, 7), 33 / 7, "SpuMath.Div failed.");
			Assert.AreEqual(SpuMath.Div(12345, 54), 12345 / 54, "SpuMath.Div failed.");
			Assert.AreEqual(SpuMath.Div(987536, 664), 987536 / 664, "SpuMath.Div failed.");
			Assert.AreEqual(SpuMath.Div(9675, 745), 9675 / 745, "SpuMath.Div failed.");
			Assert.AreEqual(SpuMath.Div(123454, 3), 123454 / 3, "SpuMath.Div failed.");
			Assert.AreEqual(SpuMath.Div(16524, 23), 16524 / 23, "SpuMath.Div failed.");

			Assert.AreEqual(SpuMath.Div(-14, 7), -14 / 7, "SpuMath.Div failed.");
			Assert.AreEqual(SpuMath.Div(33, -7), 33 / -7, "SpuMath.Div failed.");
			Assert.AreEqual(SpuMath.Div(-12345, -54), -12345 / -54, "SpuMath.Div failed.");
			Assert.AreEqual(SpuMath.Div(-987536, -664), -987536 / -664, "SpuMath.Div failed.");
			Assert.AreEqual(SpuMath.Div(9675, -745), 9675 / -745, "SpuMath.Div failed.");
			Assert.AreEqual(SpuMath.Div(123454, -3), 123454 / -3, "SpuMath.Div failed.");
			Assert.AreEqual(SpuMath.Div(-16524, 23), -16524 / 23, "SpuMath.Div failed.");
		}

		[Test]
		public void Test_FloatCompare()
		{
			Func<int, float> fun = delegate(int input)
											{
												float a = 0;

												if ((float)input * (float)input <= 1.0f)
													a++;

												return a;
											};
			CompileContext cc = new CompileContext(fun.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			int n = 10;

			float result = (float)SpeContext.UnitTestRunProgram(cc, n);

			AreEqual(0f, result);
		}

		[Test]
		public void TestAssignmentExpression()
		{
			Func<int, int> fun =
				delegate(int input) { return input + (++input) + input; };

			CompileContext cc = new CompileContext(fun.Method);

			cc.PerformProcessing(CompileContextState.S8Complete);

			int n = 17;

			int result = (int)SpeContext.UnitTestRunProgram(cc, n);

			AreEqual(fun(n), result);

		}

		static void TestIntRefArgument_RefFunction(ref int val)
		{
			val++;
		}

		[Test]
		public void TestIntRefArgument()
		{
			Func<int, int> del = val =>
			{
				TestIntRefArgument_RefFunction(ref val);
				return val;
			};

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);
//			cc.DisassembleToFile("intref.s");

			int expected = del(5);
			int actual = (int) SpeContext.UnitTestRunProgram(del, 5);

			AreEqual(expected, actual);
		}

		static void TestFloatRefArgument_RefFunction(ref float val)
		{
			val++;
		}

		[Test]
		public void TestFloatRefArgument()
		{
			Func<float, float> del = val =>
			{
				TestFloatRefArgument_RefFunction(ref val);
				return val;
			};

			float expected = del(5);
			float actual = (float) SpeContext.UnitTestRunProgram(del, 5f);

			AreEqual(expected, actual);
		}

		static void TestDoubleRefArgument_RefFunction(ref double val)
		{
			val++;
		}

		[Test]
		public void TestDoubleRefArgument()
		{
			Func<double, double> del = val =>
			{
				TestDoubleRefArgument_RefFunction(ref val);
				return val;
			};

			double expected = del(5);
			double actual = (double)SpeContext.UnitTestRunProgram(del, 5d);

			AreEqual(expected, actual);
		}

		static double TestDoubleArgument_Function(double val)
		{
			return val + 1;
		}

		[Test]
		public void TestDoubleArgument()
		{
			// this shouldn' be replaced with a method group, even if r# would like to do so.
			Func<double, double> del = val => TestDoubleArgument_Function(val);

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);
			cc.DisassembleToFile(@"doublearg.s");

			double expected = del(5);
			double actual = (double)SpeContext.UnitTestRunProgram(del, 5d);

			AreEqual(expected, actual);
		}
	}
}

#endif