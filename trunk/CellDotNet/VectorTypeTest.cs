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
using System.Reflection.Emit;
using CellDotNet.Intermediate;
using NUnit.Framework;
using System.Linq;


namespace CellDotNet.Spe
{
	[TestFixture]
	public class VectorTypeTest : UnitTest
	{
		[Test]
		public void TestVectorInt_GetElement()
		{
			Func<Int32Vector, int> del = input => input.E3;

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			Int32Vector v = new Int32Vector(3, 4, 5, 6);

			if (!SpeContext.HasSpeHardware)
				return;

			AreEqual(del(v), (int) SpeContext.UnitTestRunProgram(cc, v));
		}

		[Test]
		public void TestVectorInt_GetElement2()
		{
			Func<Int32Vector, int, int> del =
				delegate(Int32Vector v, int i)
				{
					if (i == 1)
						return v.E1;
					else if (i == 2)
						return v.E2;
					else if (i == 3)
						return v.E3;
					else if (i == 4)
						return v.E4;
					else
						return -1;
				};

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			Int32Vector v1 = new Int32Vector(3, 9, 13, -42);

			int rPPU1 = del(v1, 1);
			int rPPU2 = del(v1, 2);
			int rPPU3 = del(v1, 3);
			int rPPU4 = del(v1, 4);

			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext sc = new SpeContext())
			{
				object rSPU1 = sc.RunProgram(cc, v1, 1);
				object rSPU2 = sc.RunProgram(cc, v1, 2);
				object rSPU3 = sc.RunProgram(cc, v1, 3);
				object rSPU4 = sc.RunProgram(cc, v1, 4);

				AreEqual(rPPU1, (int)rSPU1);
				AreEqual(rPPU2, (int)rSPU2);
				AreEqual(rPPU3, (int)rSPU3);
				AreEqual(rPPU4, (int)rSPU4);
			}
		}

#region TestVectorInt_RefArgument

		static private void ReplaceArgument(ref Int32Vector v1, Int32Vector v2)
		{
			v1 = v2;
		}

		/// <summary>
		/// This one is non-trivial, since the ldarg on the ref argument will be supplemented with an ldobj.
		/// </summary>
		[Test]
		public void TestVectorInt_RefArgument()
		{
			Func<Int32Vector, Int32Vector, Int32Vector> del = 
				delegate(Int32Vector arg1, Int32Vector arg2)
					{
						ReplaceArgument(ref arg2, arg1);
						return arg2;
					};

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			Int32Vector v1 = new Int32Vector(1, 2, 3, 4);
			Int32Vector v2 = new Int32Vector(5, 6, 7, 8);

			if (!SpeContext.HasSpeHardware)
				return;

			AreEqual(del(v1, v2), (Int32Vector) SpeContext.UnitTestRunProgram(cc, v1, v2));
		}

		#endregion

		[Test]
		public void TestVectorInt_Copy()
		{
			Func<Int32Vector, Int32Vector> del = 
				delegate(Int32Vector input)
					{
						Int32Vector v2 = input;
						return v2;
					};

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			Int32Vector v = new Int32Vector(1, 2, 3, 4);
			AreEqual(del(v), (Int32Vector) SpeContext.UnitTestRunProgram(cc, v));
		}

		[Test]
		public void TestVectorInt_Add()
		{
			Func<Int32Vector, Int32Vector, Int32Vector> del = (arg1, arg2) => arg1 + arg2;

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			Int32Vector v1 = new Int32Vector(1, 2, 3, 4);
			Int32Vector v2 = new Int32Vector(5, 6, 7, 8);
			Int32Vector v3 = new Int32Vector(-16, -32, -128, -1234567);

			Int32Vector vPPU1 = del(v1, v2);
			Int32Vector vPPU2 = del(v2, v3);

			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext sc = new SpeContext())
			{
				object vSPU1 = sc.RunProgram(cc, v1, v2);
				object vSPU2 = sc.RunProgram(cc, v2, v3);

				AreEqual(vPPU1, (Int32Vector)vSPU1, "First test failed.");
				AreEqual(vPPU2, (Int32Vector)vSPU2, "Second test failed.");
			}
		}

		[Test]
		public void TestVectorInt_Sub()
		{
			Func<Int32Vector, Int32Vector, Int32Vector> del = (arg1, arg2) => arg1 - arg2;

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			Int32Vector v1 = new Int32Vector(5, 6, 7, 8);
			Int32Vector v2 = new Int32Vector(1, 2, 3, 4);
			Int32Vector v3 = new Int32Vector(-16, -32, -128, -1234567);

			Int32Vector vPPU1 = del(v1, v2);
			Int32Vector vPPU2 = del(v2, v3);

			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext sc = new SpeContext())
			{
				object vSPU1 = sc.RunProgram(cc, v1, v2);
				object vSPU2 = sc.RunProgram(cc, v2, v3);

				IsTrue((Int32Vector)vSPU1 == vPPU1, "First test failed.");
				IsTrue((Int32Vector)vSPU2 == vPPU2, "Second test failed.");
			}
		}

		[Test]
		public void TestVectorInt_Mul()
		{
			Func<Int32Vector, Int32Vector, Int32Vector> del = (arg1, arg2) => arg1*arg2;

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			Int32Vector v1 = new Int32Vector(5, -6, 7, 8);
			Int32Vector v2 = new Int32Vector(1, 2, -3, 4);
			Int32Vector v3 = new Int32Vector(-16, -32, -128, -324);

			Int32Vector vPPU1 = del(v1, v2);
			Int32Vector vPPU2 = del(v2, v3);

			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext sc = new SpeContext())
			{
				object vSPU1 = sc.RunProgram(cc, v1, v2);
				object vSPU2 = sc.RunProgram(cc, v2, v3);

				IsTrue((Int32Vector)vSPU1 == vPPU1, "First test failed.");
				IsTrue((Int32Vector)vSPU2 == vPPU2, "Second test failed.");
			}
		}

		[Test]
		public void TestVectorInt_Div()
		{
			Func<Int32Vector, Int32Vector, Int32Vector> del = (arg1, arg2) => arg1/arg2;

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			Int32Vector v1 = new Int32Vector(5543, -654, 7345, 8553);
			Int32Vector v2 = new Int32Vector(5, 7, 53, 14);
			Int32Vector v3 = new Int32Vector(-6, -52, -128, -324);

			Int32Vector vPPU1 = del(v1, v2);
			Int32Vector vPPU2 = del(v2, v3);

			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext sc = new SpeContext())
			{
				object vSPU1 = sc.RunProgram(cc, v1, v2);
				object vSPU2 = sc.RunProgram(cc, v2, v3);

				IsTrue((Int32Vector)vSPU1 == vPPU1, "First test failed.");
				IsTrue((Int32Vector)vSPU2 == vPPU2, "Second test failed.");
			}
		}

		[Test]
		public void TestVectorInt_Rem()
		{
			Func<Int32Vector, Int32Vector, Int32Vector> del = (arg1, arg2) => arg1%arg2;

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			Int32Vector v1 = new Int32Vector(5543, -654, 7345, 8553);
			Int32Vector v2 = new Int32Vector(5, 7, 53, 14);
			Int32Vector v3 = new Int32Vector(-6, -52, -128, -324);

			Int32Vector vPPU1 = del(v1, v2);
			Int32Vector vPPU2 = del(v2, v3);

			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext sc = new SpeContext())
			{
				object vSPU1 = sc.RunProgram(cc, v1, v2);
				object vSPU2 = sc.RunProgram(cc, v2, v3);

				IsTrue((Int32Vector)vSPU1 == vPPU1, "First test failed.");
				IsTrue((Int32Vector)vSPU2 == vPPU2, "Second test failed.");
			}
		}

		[Test]
		public void TestVectorInt_Constructor()
		{
			// This will use newobj.
			Func<Int32Vector> del = () => new Int32Vector(1, 2, 3, 4);

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			List<MethodVariable> vlist = cc.EntryPointAsMetodCompiler.Variables
				.Where(var => var.StackType == StackTypeDescription.Int32Vector)
				.ToList();

			if (vlist.Count == 1)
				IsFalse(vlist[0].Escapes.Value);
			else
				AreEqual(0, vlist.Count);

			AreEqual(new Int32Vector(1, 2, 3, 4), (Int32Vector) SpeContext.UnitTestRunProgram(cc));
		}

		[Test]
		public void TestVectorInt_ConstructorLocalVariable()
		{
			// This will use call.
			Func<Int32Vector> del = () =>
			{
				Int32Vector v = new Int32Vector(1, 2, 3, 4);
				return v;
			};

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			List<MethodVariable> vlist = cc.EntryPointAsMetodCompiler.Variables
				.Where(var => var.StackType == StackTypeDescription.Int32Vector)
				.ToList();

			// allow 2 since debug mode branch might have induced an extra variable.
			IsTrue(vlist.Count == 1 || vlist.Count == 2); 
			IsFalse(vlist[0].Escapes.Value);
			if (vlist.Count == 2)
				IsFalse(vlist[1].Escapes.Value);

			AreEqual(new Int32Vector(1, 2, 3, 4), (Int32Vector)SpeContext.UnitTestRunProgram(cc));
		}

		[Test]
		public void TestVectorInt_Splat()
		{
			// This will use newobj.
			Func<int, Int32Vector> del = Int32Vector.Splat;

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			int arg = 17;

			AreEqual(new Int32Vector(arg, arg, arg, arg), Int32Vector.Splat(arg));
			AreEqual(Int32Vector.Splat(arg), (Int32Vector)SpeContext.UnitTestRunProgram(cc, arg));
		}

		[Test]
		public void TestVectorInt_Equal()
		{
			Func<Int32Vector, Int32Vector, bool> del = (arg1, arg2) => arg1 == arg2;

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			Int32Vector v1 = new Int32Vector(5, 6, 7, 8);
			Int32Vector v2 = new Int32Vector(1, 2, 3, 4);
			Int32Vector v3 = new Int32Vector(1, 2, 3, 4);

			bool vPPU1 = del(v1, v2);
			bool vPPU2 = del(v2, v3);

			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext sc = new SpeContext())
			{
				object vSPU1 = sc.RunProgram(cc, v1, v2);
				object vSPU2 = sc.RunProgram(cc, v2, v3);

				IsTrue(((bool?)vSPU1).Value == vPPU1, "First test failed.");
				IsTrue(((bool?)vSPU2).Value == vPPU2, "Second test failed.");
			}
		}

		[Test]
		public void TestVectorFloat_Add()
		{
			Func<Float32Vector, Float32Vector, Float32Vector> del = (arg1, arg2) => arg2 + arg1;

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			Float32Vector v1 = new Float32Vector(1, 2, 3, 4);
			Float32Vector v2 = new Float32Vector(5, 6, 7, 8);
			Float32Vector v3 = new Float32Vector(-16, -32, -128, -1234567);

			Float32Vector vPPU1 = del(v1, v2);
			Float32Vector vPPU2 = del(v2, v3);

			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext sc = new SpeContext())
			{
				object vSPU1 = sc.RunProgram(cc, v1, v2);
				object vSPU2 = sc.RunProgram(cc, v2, v3);


				IsTrue((Float32Vector)vSPU1 == vPPU1, "First test failed.");
				IsTrue((Float32Vector)vSPU2 == vPPU2, "Second test failed.");
			}
		}

		[Test]
		public void TestVectorFloat_Sub()
		{
			Func<Float32Vector, Float32Vector, Float32Vector> del = (arg1, arg2) => arg1 - arg2;

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			Float32Vector v1 = new Float32Vector(5, 6, 7, 8);
			Float32Vector v2 = new Float32Vector(1, 2, 3, 4);
			Float32Vector v3 = new Float32Vector(-16, -32, -128, -1234567);

			Float32Vector vPPU1 = del(v1, v2);
			Float32Vector vPPU2 = del(v2, v3);

			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext sc = new SpeContext())
			{
				object vSPU1 = sc.RunProgram(cc, v1, v2);
				object vSPU2 = sc.RunProgram(cc, v2, v3);

				IsTrue((Float32Vector)vSPU1 == vPPU1, "First test failed.");
				IsTrue((Float32Vector)vSPU2 == vPPU2, "Second test failed.");
			}
		}

		[Test]
		public void TestVectorFloat_Mul()
		{
			Func<Float32Vector, Float32Vector, Float32Vector> del = (arg1, arg2) => arg1*arg2;

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			Float32Vector v1 = new Float32Vector(5, 6, 7, 8);
			Float32Vector v2 = new Float32Vector(1, 2, 3, 4);
			Float32Vector v3 = new Float32Vector(-16, -32, -128, -1234567);

			Float32Vector vPPU1 = del(v1, v2);
			Float32Vector vPPU2 = del(v2, v3);

			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext sc = new SpeContext())
			{
				object vSPU1 = sc.RunProgram(cc, v1, v2);
				object vSPU2 = sc.RunProgram(cc, v2, v3);

				Utilities.AssertWithinLimits((Float32Vector)vSPU1, vPPU1, 0.00001f, "First test failed.");
				Utilities.AssertWithinLimits((Float32Vector)vSPU2, vPPU2, 0.00001f, "Second test failed.");

//				IsTrue((Float32Vector)vSPU1 == vPPU1, "First test failed.");
//				IsTrue((Float32Vector)vSPU2 == vPPU2, "Second test failed.");
			}
		}

		[Test]
		public void TestVectorFloat_Div()
		{
			Func<Float32Vector, Float32Vector, Float32Vector> del = (arg1, arg2) => arg1/arg2;

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			Float32Vector v1 = new Float32Vector(5, -6, 7, 8);
			Float32Vector v2 = new Float32Vector(1, 2, -3, 4);
			Float32Vector v3 = new Float32Vector(-16, -32, -128, -324);

			Float32Vector vPPU1 = del(v1, v2);
			Float32Vector vPPU2 = del(v2, v3);

			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext sc = new SpeContext())
			{
				object vSPU1 = sc.RunProgram(cc, v1, v2);
				object vSPU2 = sc.RunProgram(cc, v2, v3);

				Utilities.AssertWithinLimits((Float32Vector)vSPU1, vPPU1, 0.00001f, "First test failed.");
				Utilities.AssertWithinLimits((Float32Vector)vSPU2, vPPU2, 0.00001f, "Second test failed.");
			}
		}

		[Test]
		public void TestVectorFloat_Constructor()
		{
			Func<Float32Vector> del = () => new Float32Vector(1, 2, 3, 4);

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(new Float32Vector(1, 2, 3, 4), (Float32Vector)SpeContext.UnitTestRunProgram(cc));
		}

		[Test]
		public void TestVectorFloat_ConstructorLocalVariable()
		{
			Func<Float32Vector> del = () => new Float32Vector(1, 2, 3, 4);

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(new Float32Vector(1, 2, 3, 4), (Float32Vector)SpeContext.UnitTestRunProgram(cc));
		}

		static private void TestVectorFloat_ConstructorRefArg_RefArgMehod(out Float32Vector v)
		{
			v = new Float32Vector(1, 2, 3, 4);
		}

		[Test]
		public void TestVectorFloat_ConstructorRefArg()
		{
			Func<Float32Vector> del = 
				delegate
					{
						Float32Vector v;
						TestVectorFloat_ConstructorRefArg_RefArgMehod(out v);
						return v;
					};

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(new Float32Vector(1, 2, 3, 4), (Float32Vector)SpeContext.UnitTestRunProgram(cc));
		}

		[Test]
		public void TestVectorFloat_Splat()
		{
			// This will use newobj.
			Func<float, Float32Vector> del = Float32Vector.Splat;

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			float arg = 17;

			AreEqual(new Float32Vector(arg, arg, arg, arg), Float32Vector.Splat(arg));
			AreEqual(Float32Vector.Splat(arg), (Float32Vector)SpeContext.UnitTestRunProgram(cc, arg));
		}

		[Test]
		public void TestVectorFloat_GetElement()
		{
			Func<Float32Vector, int, float> del =
				delegate(Float32Vector v, int i)
				{
					if (i == 1)
						return v.E1;
					else if (i == 2)
						return v.E2;
					else if (i == 3)
						return v.E3;
					else if (i == 4)
						return v.E4;
					else
						return -1;
				};

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			Float32Vector v1 = new Float32Vector(3, 9, 13, -42);

			float rPPU1 = del(v1, 1);
			float rPPU2 = del(v1, 2);
			float rPPU3 = del(v1, 3);
			float rPPU4 = del(v1, 4);

			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext sc = new SpeContext())
			{
				object rSPU1 = sc.RunProgram(cc, v1, 1);
				object rSPU2 = sc.RunProgram(cc, v1, 2);
				object rSPU3 = sc.RunProgram(cc, v1, 3);
				object rSPU4 = sc.RunProgram(cc, v1, 4);

				AreEqual(rPPU1, (float)rSPU1);
				AreEqual(rPPU2, (float)rSPU2);
				AreEqual(rPPU3, (float)rSPU3);
				AreEqual(rPPU4, (float)rSPU4);
			}
		}

		[Test]
		public void TestVectorFloat_Equal()
		{
			Func<Float32Vector, Float32Vector, bool> del = (arg1, arg2) => arg1 == arg2;

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			Float32Vector v1 = new Float32Vector(5, 6, 7, 8);
			Float32Vector v2 = new Float32Vector(1, 2, 3, 4);
			Float32Vector v3 = new Float32Vector(1, 2, 3, 4);

			bool vPPU1 = del(v1, v2);
			bool vPPU2 = del(v2, v3);

			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext sc = new SpeContext())
			{
				object vSPU1 = sc.RunProgram(cc, v1, v2);
				object vSPU2 = sc.RunProgram(cc, v2, v3);

				IsTrue((bool)vSPU1 == vPPU1, "First test failed.");
				IsTrue((bool)vSPU2 == vPPU2, "Second test failed.");
			}
		}

		private static Float32Vector ArrayIntVectorFunc2(Float32Vector v1, Float32Vector v2)
		{
			Float32Vector[] varr = new Float32Vector[3];

			varr[0] = v1;
			varr[1] = v2;

			varr[2] = varr[0] + varr[1];

			return varr[2];
		}

		[Test]
		public void TestVectorIntArray_Simple()
		{
			Func<Int32Vector, Int32Vector> del1 = delegate(Int32Vector vec)
			{
				Int32Vector[] varr = new Int32Vector[1];

				varr[0] = vec;
				return varr[0];
			};
			
			CompileContext cc = new CompileContext(del1.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);


			Int32Vector v1 = new Int32Vector(5, 6, 7, 8);
			Int32Vector v2 = new Int32Vector(1, 2, 3, 4);

			Int32Vector vPPU1 = del1(v1);
			Int32Vector vPPU2 = del1(v2);

			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext sc = new SpeContext())
			{
				object vSPU1 = sc.RunProgram(cc, v1);
				object vSPU2 = sc.RunProgram(cc, v2);

				IsTrue((Int32Vector)vSPU1 == vPPU1, "First test failed.");
				IsTrue((Int32Vector)vSPU2 == vPPU2, "Second test failed.");
			}
		}

		[Test]
		public void TestVectorIntArray()
		{
			Func<Int32Vector, Int32Vector, Int32Vector> del =
				(arg1, arg2) =>
					{
						Int32Vector[] varr = new Int32Vector[3];

						varr[0] = arg1;
						varr[1] = arg2;

						varr[2] = varr[0] + varr[1];

						return varr[2];
					};

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			Int32Vector v1 = new Int32Vector(5, 6, 7, 8);
			Int32Vector v2 = new Int32Vector(1, 2, 3, 4);
			Int32Vector v3 = new Int32Vector(1, 2, 3, 4);

			Int32Vector vPPU1 = del(v1, v2);
			Int32Vector vPPU2 = del(v2, v3);

			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext sc = new SpeContext())
			{
				object vSPU1 = sc.RunProgram(cc, v1, v2);
				object vSPU2 = sc.RunProgram(cc, v2, v3);

				IsTrue((Int32Vector)vSPU1 == vPPU1, "First test failed.");
				IsTrue((Int32Vector)vSPU2 == vPPU2, "Second test failed.");
			}
		}

		[Test]
		public void TestVectorIntArray_OverWrite()
		{
			Func<Int32Vector, Int32Vector, Int32Vector> del =
				delegate(Int32Vector arg1, Int32Vector arg2)
					{
						Int32Vector[] varr = new Int32Vector[100];

						varr[14] = arg2;

						for (int i = 0; i < 100; i++)
							if (i != 14)
								varr[i] = arg1;

						return varr[14];
					};

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			Int32Vector v1 = new Int32Vector(5, 6, 7, 8);
			Int32Vector v2 = new Int32Vector(1, 2, 3, 4);
			Int32Vector v3 = new Int32Vector(25, 64325, 23645, 435);

			Int32Vector vPPU1 = del(v1, v2);
			Int32Vector vPPU2 = del(v2, v3);

			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext sc = new SpeContext())
			{
				object vSPU1 = sc.RunProgram(cc, v1, v2);
				object vSPU2 = sc.RunProgram(cc, v2, v3);

				IsTrue((Int32Vector)vSPU1 == vPPU1, "First test failed. Expected");
				IsTrue((Int32Vector)vSPU2 == vPPU2, "Second test failed.");
			}
		}
	}
}
#endif