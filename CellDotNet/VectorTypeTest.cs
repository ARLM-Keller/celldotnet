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
using CellDotNet.Intermediate;
using NUnit.Framework;
using System.Linq;


namespace CellDotNet
{
	[TestFixture]
	public class VectorTypeTest : UnitTest
	{
		[Test]
		public void TestVectorInt_GetElement()
		{
			Func<Int32Vector, int> del = input => input.E3;

			var cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			var v = new Int32Vector(3, 4, 5, 6);

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

			var cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			var v1 = new Int32Vector(3, 9, 13, -42);

			AreEqual(del(v1, 1), (int) SpeContext.UnitTestRunProgram(cc, v1, 1));
			AreEqual(del(v1, 2), (int) SpeContext.UnitTestRunProgram(cc, v1, 2));
			AreEqual(del(v1, 3), (int) SpeContext.UnitTestRunProgram(cc, v1, 3));
			AreEqual(del(v1, 4), (int) SpeContext.UnitTestRunProgram(cc, v1, 4));
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

			var cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			var v1 = new Int32Vector(1, 2, 3, 4);
			var v2 = new Int32Vector(5, 6, 7, 8);

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

			var cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			var v = new Int32Vector(1, 2, 3, 4);
			AreEqual(del(v), (Int32Vector) SpeContext.UnitTestRunProgram(cc, v));
		}

		[Test]
		public void TestVectorInt_Add()
		{
			Func<Int32Vector, Int32Vector, Int32Vector> del = (arg1, arg2) => arg1 + arg2;

			var cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			Int32Vector v1 = new Int32Vector(1, 2, 3, 4);
			Int32Vector v2 = new Int32Vector(5, 6, 7, 8);
			Int32Vector v3 = new Int32Vector(-16, -32, -128, -1234567);

			AreEqual(del(v1, v2), (Int32Vector) SpeContext.UnitTestRunProgram(cc, v1, v2), "First test failed.");
			AreEqual(del(v2, v3), (Int32Vector) SpeContext.UnitTestRunProgram(cc, v2, v3), "Second test failed.");
		}

		[Test]
		public void TestVectorInt_Sub()
		{
			Func<Int32Vector, Int32Vector, Int32Vector> del = (arg1, arg2) => arg1 - arg2;

			var cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			Int32Vector v1 = new Int32Vector(5, 6, 7, 8);
			Int32Vector v2 = new Int32Vector(1, 2, 3, 4);
			Int32Vector v3 = new Int32Vector(-16, -32, -128, -1234567);

			IsTrue((Int32Vector) SpeContext.UnitTestRunProgram(cc, v1, v2) == del(v1, v2), "First test failed.");
			IsTrue((Int32Vector) SpeContext.UnitTestRunProgram(cc, v2, v3) == del(v2, v3), "Second test failed.");
		}

		[Test]
		public void TestVectorInt_Mul()
		{
			Func<Int32Vector, Int32Vector, Int32Vector> del = (arg1, arg2) => arg1*arg2;

			var cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			Int32Vector v1 = new Int32Vector(5, -6, 7, 8);
			Int32Vector v2 = new Int32Vector(1, 2, -3, 4);
			Int32Vector v3 = new Int32Vector(-16, -32, -128, -324);

			IsTrue((Int32Vector) SpeContext.UnitTestRunProgram(cc, v1, v2) == del(v1, v2), "First test failed.");
			IsTrue((Int32Vector) SpeContext.UnitTestRunProgram(cc, v2, v3) == del(v2, v3), "Second test failed.");
		}

		[Test]
		public void TestVectorInt_Div()
		{
			Func<Int32Vector, Int32Vector, Int32Vector> del = (arg1, arg2) => arg1/arg2;

			var cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			var v1 = new Int32Vector(5543, -654, 7345, 8553);
			var v2 = new Int32Vector(5, 7, 53, 14);
			var v3 = new Int32Vector(-6, -52, -128, -324);

			AreEqual(del(v1, v2), (Int32Vector)SpeContext.UnitTestRunProgram(cc, v1, v2), "First test failed.");
			AreEqual(del(v2, v3), (Int32Vector) SpeContext.UnitTestRunProgram(cc, v2, v3), "Second test failed.");
		}

		[Test]
		public void TestVectorInt_Rem()
		{
			Func<Int32Vector, Int32Vector, Int32Vector> del = (arg1, arg2) => arg1%arg2;

			var cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			var v1 = new Int32Vector(5543, -654, 7345, 8553);
			var v2 = new Int32Vector(5, 7, 53, 14);
			var v3 = new Int32Vector(-6, -52, -128, -324);

			AreEqual(del(v1, v2), (Int32Vector) SpeContext.UnitTestRunProgram(cc, v1, v2), "First test failed.");
			AreEqual(del(v2, v3), (Int32Vector) SpeContext.UnitTestRunProgram(cc, v2, v3), "Second test failed.");
		}

		[Test]
		public void TestVectorInt_Constructor()
		{
			// This will use newobj.
			Func<Int32Vector> del = () => new Int32Vector(1, 2, 3, 4);

			var cc = new CompileContext(del.Method);
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

			var cc = new CompileContext(del.Method);
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
			Func<int, Int32Vector> del = i => Int32Vector.Splat(i);

			var cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			int arg = 17;

			AreEqual(new Int32Vector(arg, arg, arg, arg), Int32Vector.Splat(arg));
			AreEqual(Int32Vector.Splat(arg), (Int32Vector)SpeContext.UnitTestRunProgram(cc, arg));
		}

		[Test]
		public void TestVectorInt_Equal()
		{
			Func<Int32Vector, Int32Vector, bool> del = (arg1, arg2) => arg1 == arg2;

			var cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			var v1 = new Int32Vector(5, 6, 7, 8);
			var v2 = new Int32Vector(1, 2, 3, 4);
			var v3 = new Int32Vector(1, 2, 3, 4);

			AreEqual(del(v1, v2), (bool)SpeContext.UnitTestRunProgram(cc, v1, v2), "First test failed.");
			AreEqual(del(v2, v3), (bool)SpeContext.UnitTestRunProgram(cc, v2, v3), "Second test failed.");
		}

		[Test]
		public void TestVectorFloat_Add()
		{
			Func<Float32Vector, Float32Vector, Float32Vector> del = (arg1, arg2) => arg2 + arg1;

			var cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			var v1 = new Float32Vector(1, 2, 3, 4);
			var v2 = new Float32Vector(5, 6, 7, 8);
			var v3 = new Float32Vector(-16, -32, -128, -1234567);

			AreEqual(del(v1, v2), (Float32Vector)SpeContext.UnitTestRunProgram(cc, v1, v2), "First test failed.");
			AreEqual(del(v2, v3), (Float32Vector)SpeContext.UnitTestRunProgram(cc, v2, v3), "Second test failed.");
		}

		[Test]
		public void TestVectorFloat_Sub()
		{
			Func<Float32Vector, Float32Vector, Float32Vector> del = (arg1, arg2) => arg1 - arg2;

			var cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			var v1 = new Float32Vector(5, 6, 7, 8);
			var v2 = new Float32Vector(1, 2, 3, 4);
			var v3 = new Float32Vector(-16, -32, -128, -1234567);

			AreEqual(del(v1, v2), (Float32Vector) SpeContext.UnitTestRunProgram(cc, v1, v2), "First test failed.");
			AreEqual(del(v2, v3), (Float32Vector)SpeContext.UnitTestRunProgram(cc, v2, v3), "Second test failed.");
		}

		[Test]
		public void TestVectorFloat_Mul()
		{
			Func<Float32Vector, Float32Vector, Float32Vector> del = (arg1, arg2) => arg1*arg2;

			var cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			var v1 = new Float32Vector(5, 6, 7, 8);
			var v2 = new Float32Vector(1, 2, 3, 4);
			var v3 = new Float32Vector(-16, -32, -128, -1234567);

			AreWithinLimits(del(v1, v2), (Float32Vector) SpeContext.UnitTestRunProgram(cc, v1, v2), 0.00001f, "First test failed.");
			AreWithinLimits(del(v2, v3), (Float32Vector) SpeContext.UnitTestRunProgram(cc, v2, v3), 0.00001f, "Second test failed.");
		}

		[Test]
		public void TestVectorFloat_Div()
		{
			Func<Float32Vector, Float32Vector, Float32Vector> del = (arg1, arg2) => arg1/arg2;

			var cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			var v1 = new Float32Vector(5, -6, 7, 8);
			var v2 = new Float32Vector(1, 2, -3, 4);
			var v3 = new Float32Vector(-16, -32, -128, -324);

			AreWithinLimits(del(v1, v2), (Float32Vector) SpeContext.UnitTestRunProgram(cc, v1, v2), 0.00001f, "First test failed.");
			AreWithinLimits(del(v2, v3), (Float32Vector) SpeContext.UnitTestRunProgram(cc, v2, v3), 0.00001f, "Second test failed.");
		}

		[Test]
		public void TestVectorFloat_Constructor()
		{
			Func<Float32Vector> del = () => new Float32Vector(1, 2, 3, 4);

			var cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(new Float32Vector(1, 2, 3, 4), (Float32Vector)SpeContext.UnitTestRunProgram(cc));
		}

		[Test]
		public void TestVectorFloat_ConstructorLocalVariable()
		{
			Func<Float32Vector> del = () => new Float32Vector(1, 2, 3, 4);

			var cc = new CompileContext(del.Method);
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

			var cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(new Float32Vector(1, 2, 3, 4), (Float32Vector)SpeContext.UnitTestRunProgram(cc));
		}

		[Test]
		public void TestVectorFloat_Splat()
		{
			Func<float, Float32Vector> del = f => Float32Vector.Splat(f);

			var cc = new CompileContext(del.Method);
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

			var cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			var v1 = new Float32Vector(3, 9, 13, -42);

			AreEqual(del(v1, 1), (float) SpeContext.UnitTestRunProgram(cc, v1, 1));
			AreEqual(del(v1, 2), (float) SpeContext.UnitTestRunProgram(cc, v1, 2));
			AreEqual(del(v1, 3), (float) SpeContext.UnitTestRunProgram(cc, v1, 3));
			AreEqual(del(v1, 4), (float) SpeContext.UnitTestRunProgram(cc, v1, 4));
		}

		[Test]
		public void TestVectorFloat_Equal()
		{
			Func<Float32Vector, Float32Vector, bool> del = (arg1, arg2) => arg1 == arg2;

			var cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			var v1 = new Float32Vector(5, 6, 7, 8);
			var v2 = new Float32Vector(1, 2, 3, 4);
			var v3 = new Float32Vector(1, 2, 3, 4);

			AreEqual(del(v1, v2), (bool) SpeContext.UnitTestRunProgram(cc, v1, v2), "First test failed.");
			AreEqual(del(v2, v3), (bool) SpeContext.UnitTestRunProgram(cc, v2, v3), "Second test failed.");
		}

		[Test]
		public void TestVectorDouble_Add()
		{
			Func<Float64Vector, Float64Vector, Float64Vector> del = (arg1, arg2) => arg2 + arg1;

			var cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			var v1 = new Float64Vector(1, 2);
			var v2 = new Float64Vector(5, 6);
			var v3 = new Float64Vector(-16, -32);

			AreEqual(del(v1, v2), (Float64Vector)SpeContext.UnitTestRunProgram(cc, v1, v2), "First test failed.");
			AreEqual(del(v2, v3), (Float64Vector)SpeContext.UnitTestRunProgram(cc, v2, v3), "Second test failed.");
		}

		[Test]
		public void TestVectorDouble_Sub()
		{
			Func<Float64Vector, Float64Vector, Float64Vector> del = (arg1, arg2) => arg1 - arg2;

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			var v1 = new Float64Vector(5, 6);
			var v2 = new Float64Vector(1, 2);
			var v3 = new Float64Vector(-16, -32);

			AreEqual(del(v1, v2), (Float64Vector) SpeContext.UnitTestRunProgram(cc, v1, v2), "First test failed.");
			AreEqual(del(v2, v3), (Float64Vector)SpeContext.UnitTestRunProgram(cc, v2, v3), "Second test failed.");
		}

		[Test]
		public void TestVectorDouble_Mul()
		{
			Func<Float64Vector, Float64Vector, Float64Vector> del = (arg1, arg2) => arg1*arg2;

			var cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			var v1 = new Float64Vector(5, 6);
			var v2 = new Float64Vector(1, 2);
			var v3 = new Float64Vector(-16, -32);

			AreWithinLimits(del(v1, v2), (Float64Vector) SpeContext.UnitTestRunProgram(cc, v1, v2), 0.00001f, "First test failed.");
			AreWithinLimits(del(v2, v3), (Float64Vector) SpeContext.UnitTestRunProgram(cc, v2, v3), 0.00001f, "Second test failed.");
		}

		[Test]
		public void TestVectorDouble_Div()
		{
			Func<Float64Vector, Float64Vector, Float64Vector> del = (arg1, arg2) => arg1/arg2;

			var cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			var v1 = new Float64Vector(5, -6);
			var v2 = new Float64Vector(1, -3);
			var v3 = new Float64Vector(-16, -32);

			AreWithinLimits(del(v1, v2), (Float64Vector) SpeContext.UnitTestRunProgram(cc, v1, v2), 0.00001f, "First test failed.");
			AreWithinLimits(del(v2, v3), (Float64Vector) SpeContext.UnitTestRunProgram(cc, v2, v3), 0.00001f, "Second test failed.");
		}

		[Test]
		public void TestVectorDouble_Constructor()
		{
			Func<Float64Vector> del = () => new Float64Vector(1, 2);

			var cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(new Float64Vector(1, 2), (Float64Vector)SpeContext.UnitTestRunProgram(cc));
		}

		[Test]
		public void TestVectorDouble_ConstructorLocalVariable()
		{
			Func<Float64Vector> del = () => new Float64Vector(1, 2);

			var cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(new Float64Vector(1, 2), (Float64Vector)SpeContext.UnitTestRunProgram(cc));
		}

		static private void TestVectorDouble_ConstructorRefArg_RefArgMehod(out Float64Vector v)
		{
			v = new Float64Vector(1, 2);
		}

		[Test]
		public void TestVectorDouble_ConstructorRefArg()
		{
			Func<Float64Vector> del =
				delegate
				{
					Float64Vector v;
					TestVectorDouble_ConstructorRefArg_RefArgMehod(out v);
					return v;
				};

			var cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(new Float64Vector(1, 2), (Float64Vector)SpeContext.UnitTestRunProgram(cc));
		}

		[Test]
		public void TestVectorDouble_Splat()
		{
			Func<double, Float64Vector> del = d => Float64Vector.Splat(d);

			var cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			double arg = 17;

			AreEqual(new Float64Vector(arg, arg), Float64Vector.Splat(arg));
			AreEqual(Float64Vector.Splat(arg), (Float64Vector)SpeContext.UnitTestRunProgram(cc, arg));
		}

		[Test]
		public void TestVectorDouble_GetElement()
		{
			Func<Float64Vector, int, double> del =
				delegate(Float64Vector v, int i)
					{
						if (i == 1)
							return v.E1;
						else if (i == 2)
							return v.E2;
						else
							return -1;
					};

			var cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			var v1 = new Float64Vector(3, 9);

			AreEqual(del(v1, 1), (double)SpeContext.UnitTestRunProgram(cc, v1, 1));
			AreEqual(del(v1, 2), (double)SpeContext.UnitTestRunProgram(cc, v1, 2));
		}

		[Test]
		public void TestVectorDouble_Equal()
		{
			Func<Float64Vector, Float64Vector, bool> del = (arg1, arg2) => arg1 == arg2;

			var cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			var v1 = new Float64Vector(5, 6);
			var v2 = new Float64Vector(1, 2);
			var v3 = new Float64Vector(1, 2);

			AreEqual(del(v1, v2), (bool) SpeContext.UnitTestRunProgram(cc, v1, v2), "First test failed.");
			AreEqual(del(v2, v3), (bool) SpeContext.UnitTestRunProgram(cc, v2, v3), "Second test failed.");
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
			                                      		var varr = new Int32Vector[1];

			                                      		varr[0] = vec;
			                                      		return varr[0];
			                                      	};

			var cc = new CompileContext(del1.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			var v1 = new Int32Vector(5, 6, 7, 8);
			var v2 = new Int32Vector(1, 2, 3, 4);

			AreEqual(del1(v1), (Int32Vector) SpeContext.UnitTestRunProgram(cc, v1), "First test failed.");
			AreEqual(del1(v2), (Int32Vector) SpeContext.UnitTestRunProgram(cc, v2), "Second test failed.");
		}

		[Test]
		public void TestVectorIntArray()
		{
			Func<Int32Vector, Int32Vector, Int32Vector> del =
				(arg1, arg2) =>
					{
						var varr = new Int32Vector[3];

						varr[0] = arg1;
						varr[1] = arg2;

						varr[2] = varr[0] + varr[1];

						return varr[2];
					};

			var cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			var v1 = new Int32Vector(5, 6, 7, 8);
			var v2 = new Int32Vector(1, 2, 3, 4);
			var v3 = new Int32Vector(1, 2, 3, 4);

			AreEqual(del(v1, v2), (Int32Vector) SpeContext.UnitTestRunProgram(cc, v1, v2), "First test failed.");
			AreEqual(del(v2, v3), (Int32Vector)SpeContext.UnitTestRunProgram(cc, v2, v3), "Second test failed.");
		}

		[Test]
		public void TestVectorIntArray_OverWrite()
		{
			Func<Int32Vector, Int32Vector, Int32Vector> del =
				delegate(Int32Vector arg1, Int32Vector arg2)
					{
						var varr = new Int32Vector[100];

						varr[14] = arg2;

						for (int i = 0; i < 100; i++)
							if (i != 14)
								varr[i] = arg1;

						return varr[14];
					};

			var cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			var v1 = new Int32Vector(5, 6, 7, 8);
			var v2 = new Int32Vector(1, 2, 3, 4);
			var v3 = new Int32Vector(25, 64325, 23645, 435);

			AreEqual(del(v1, v2), (Int32Vector) SpeContext.UnitTestRunProgram(cc, v1, v2), "First test failed. Expected");
			AreEqual(del(v2, v3), (Int32Vector) SpeContext.UnitTestRunProgram(cc, v2, v3), "Second test failed.");
		}
	}
}
#endif