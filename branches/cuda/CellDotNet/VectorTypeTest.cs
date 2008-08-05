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
			Func<VectorI4, int> del = input => input.E3;

			var cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			var v = new VectorI4(3, 4, 5, 6);

			AreEqual(del(v), (int) SpeContext.UnitTestRunProgram(cc, v));
		}

		[Test]
		public void TestVectorInt_GetElement2()
		{
			Func<VectorI4, int, int> del =
				delegate(VectorI4 v, int i)
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

			var v1 = new VectorI4(3, 9, 13, -42);

			AreEqual(del(v1, 1), (int) SpeContext.UnitTestRunProgram(cc, v1, 1));
			AreEqual(del(v1, 2), (int) SpeContext.UnitTestRunProgram(cc, v1, 2));
			AreEqual(del(v1, 3), (int) SpeContext.UnitTestRunProgram(cc, v1, 3));
			AreEqual(del(v1, 4), (int) SpeContext.UnitTestRunProgram(cc, v1, 4));
		}

		#region TestVectorInt_RefArgument

		static private void ReplaceArgument(ref VectorI4 v1, VectorI4 v2)
		{
			v1 = v2;
		}

		/// <summary>
		/// This one is non-trivial, since the ldarg on the ref argument will be supplemented with an ldobj.
		/// </summary>
		[Test]
		public void TestVectorInt_RefArgument()
		{
			Func<VectorI4, VectorI4, VectorI4> del = 
				delegate(VectorI4 arg1, VectorI4 arg2)
					{
						ReplaceArgument(ref arg2, arg1);
						return arg2;
					};

			var cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			var v1 = new VectorI4(1, 2, 3, 4);
			var v2 = new VectorI4(5, 6, 7, 8);

			AreEqual(del(v1, v2), (VectorI4) SpeContext.UnitTestRunProgram(cc, v1, v2));
		}

		#endregion

		[Test]
		public void TestVectorInt_Copy()
		{
			Func<VectorI4, VectorI4> del = 
				delegate(VectorI4 input)
					{
						VectorI4 v2 = input;
						return v2;
					};

			var cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			var v = new VectorI4(1, 2, 3, 4);
			AreEqual(del(v), (VectorI4) SpeContext.UnitTestRunProgram(cc, v));
		}

		[Test]
		public void TestVectorInt_Add()
		{
			Func<VectorI4, VectorI4, VectorI4> del = (arg1, arg2) => arg1 + arg2;

			var cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			VectorI4 v1 = new VectorI4(1, 2, 3, 4);
			VectorI4 v2 = new VectorI4(5, 6, 7, 8);
			VectorI4 v3 = new VectorI4(-16, -32, -128, -1234567);

			AreEqual(del(v1, v2), (VectorI4) SpeContext.UnitTestRunProgram(cc, v1, v2), "First test failed.");
			AreEqual(del(v2, v3), (VectorI4) SpeContext.UnitTestRunProgram(cc, v2, v3), "Second test failed.");
		}

		[Test]
		public void TestVectorInt_Sub()
		{
			Func<VectorI4, VectorI4, VectorI4> del = (arg1, arg2) => arg1 - arg2;

			var cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			VectorI4 v1 = new VectorI4(5, 6, 7, 8);
			VectorI4 v2 = new VectorI4(1, 2, 3, 4);
			VectorI4 v3 = new VectorI4(-16, -32, -128, -1234567);

			IsTrue((VectorI4) SpeContext.UnitTestRunProgram(cc, v1, v2) == del(v1, v2), "First test failed.");
			IsTrue((VectorI4) SpeContext.UnitTestRunProgram(cc, v2, v3) == del(v2, v3), "Second test failed.");
		}

		[Test]
		public void TestVectorInt_Mul()
		{
			Func<VectorI4, VectorI4, VectorI4> del = (arg1, arg2) => arg1*arg2;

			var cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			VectorI4 v1 = new VectorI4(5, -6, 7, 8);
			VectorI4 v2 = new VectorI4(1, 2, -3, 4);
			VectorI4 v3 = new VectorI4(-16, -32, -128, -324);

			IsTrue((VectorI4) SpeContext.UnitTestRunProgram(cc, v1, v2) == del(v1, v2), "First test failed.");
			IsTrue((VectorI4) SpeContext.UnitTestRunProgram(cc, v2, v3) == del(v2, v3), "Second test failed.");
		}

		[Test]
		public void TestVectorInt_Div()
		{
			Func<VectorI4, VectorI4, VectorI4> del = (arg1, arg2) => arg1/arg2;

			var cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			var v1 = new VectorI4(5543, -654, 7345, 8553);
			var v2 = new VectorI4(5, 7, 53, 14);
			var v3 = new VectorI4(-6, -52, -128, -324);

			AreEqual(del(v1, v2), (VectorI4)SpeContext.UnitTestRunProgram(cc, v1, v2), "First test failed.");
			AreEqual(del(v2, v3), (VectorI4) SpeContext.UnitTestRunProgram(cc, v2, v3), "Second test failed.");
		}

		[Test]
		public void TestVectorInt_Rem()
		{
			Func<VectorI4, VectorI4, VectorI4> del = (arg1, arg2) => arg1%arg2;

			var cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			var v1 = new VectorI4(5543, -654, 7345, 8553);
			var v2 = new VectorI4(5, 7, 53, 14);
			var v3 = new VectorI4(-6, -52, -128, -324);

			AreEqual(del(v1, v2), (VectorI4) SpeContext.UnitTestRunProgram(cc, v1, v2), "First test failed.");
			AreEqual(del(v2, v3), (VectorI4) SpeContext.UnitTestRunProgram(cc, v2, v3), "Second test failed.");
		}

		[Test]
		public void TestVectorInt_Constructor()
		{
			// This will use newobj.
			Func<VectorI4> del = () => new VectorI4(1, 2, 3, 4);

			var cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			List<MethodVariable> vlist = cc.EntryPointAsMetodCompiler.Variables
				.Where(var => var.StackType == StackTypeDescription.VectorI4)
				.ToList();

			if (vlist.Count == 1)
				IsFalse(vlist[0].Escapes.Value);
			else
				AreEqual(0, vlist.Count);

			AreEqual(new VectorI4(1, 2, 3, 4), (VectorI4) SpeContext.UnitTestRunProgram(cc));
		}

		[Test]
		public void TestVectorInt_ConstructorLocalVariable()
		{
			// This will use call.
			Func<VectorI4> del = () =>
			{
				VectorI4 v = new VectorI4(1, 2, 3, 4);
				return v;
			};

			var cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			List<MethodVariable> vlist = cc.EntryPointAsMetodCompiler.Variables
				.Where(var => var.StackType == StackTypeDescription.VectorI4)
				.ToList();

			// allow 2 since debug mode branch might have induced an extra variable.
			IsTrue(vlist.Count == 1 || vlist.Count == 2); 
			IsFalse(vlist[0].Escapes.Value);
			if (vlist.Count == 2)
				IsFalse(vlist[1].Escapes.Value);

			AreEqual(new VectorI4(1, 2, 3, 4), (VectorI4)SpeContext.UnitTestRunProgram(cc));
		}

		[Test]
		public void TestVectorInt_Splat()
		{
			Func<int, VectorI4> del = i => VectorI4.Splat(i);

			var cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			int arg = 17;

			AreEqual(new VectorI4(arg, arg, arg, arg), VectorI4.Splat(arg));
			AreEqual(VectorI4.Splat(arg), (VectorI4)SpeContext.UnitTestRunProgram(cc, arg));
		}

		[Test]
		public void TestVectorInt_Equal()
		{
			Func<VectorI4, VectorI4, bool> del = (arg1, arg2) => arg1 == arg2;

			var cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			var v1 = new VectorI4(5, 6, 7, 8);
			var v2 = new VectorI4(1, 2, 3, 4);
			var v3 = new VectorI4(1, 2, 3, 4);

			AreEqual(del(v1, v2), (bool)SpeContext.UnitTestRunProgram(cc, v1, v2), "First test failed.");
			AreEqual(del(v2, v3), (bool)SpeContext.UnitTestRunProgram(cc, v2, v3), "Second test failed.");
		}

		[Test]
		public void TestVectorF4_Add()
		{
			Func<VectorF4, VectorF4, VectorF4> del = (arg1, arg2) => arg2 + arg1;

			var cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			var v1 = new VectorF4(1, 2, 3, 4);
			var v2 = new VectorF4(5, 6, 7, 8);
			var v3 = new VectorF4(-16, -32, -128, -1234567);

			AreEqual(del(v1, v2), (VectorF4)SpeContext.UnitTestRunProgram(cc, v1, v2), "First test failed.");
			AreEqual(del(v2, v3), (VectorF4)SpeContext.UnitTestRunProgram(cc, v2, v3), "Second test failed.");
		}

		[Test]
		public void TestVectorF4_Sub()
		{
			Func<VectorF4, VectorF4, VectorF4> del = (arg1, arg2) => arg1 - arg2;

			var cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			var v1 = new VectorF4(5, 6, 7, 8);
			var v2 = new VectorF4(1, 2, 3, 4);
			var v3 = new VectorF4(-16, -32, -128, -1234567);

			AreEqual(del(v1, v2), (VectorF4) SpeContext.UnitTestRunProgram(cc, v1, v2), "First test failed.");
			AreEqual(del(v2, v3), (VectorF4)SpeContext.UnitTestRunProgram(cc, v2, v3), "Second test failed.");
		}

		[Test]
		public void TestVectorF4_Mul()
		{
			Func<VectorF4, VectorF4, VectorF4> del = (arg1, arg2) => arg1*arg2;

			var cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			var v1 = new VectorF4(5, 6, 7, 8);
			var v2 = new VectorF4(1, 2, 3, 4);
			var v3 = new VectorF4(-16, -32, -128, -1234567);

			AreWithinLimits(del(v1, v2), (VectorF4) SpeContext.UnitTestRunProgram(cc, v1, v2), 0.00001f, "First test failed.");
			AreWithinLimits(del(v2, v3), (VectorF4) SpeContext.UnitTestRunProgram(cc, v2, v3), 0.00001f, "Second test failed.");
		}

		[Test]
		public void TestVectorF4_Div()
		{
			Func<VectorF4, VectorF4, VectorF4> del = (arg1, arg2) => arg1/arg2;

			var cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			var v1 = new VectorF4(5, -6, 7, 8);
			var v2 = new VectorF4(1, 2, -3, 4);
			var v3 = new VectorF4(-16, -32, -128, -324);

			AreWithinLimits(del(v1, v2), (VectorF4) SpeContext.UnitTestRunProgram(cc, v1, v2), 0.00001f, "First test failed.");
			AreWithinLimits(del(v2, v3), (VectorF4) SpeContext.UnitTestRunProgram(cc, v2, v3), 0.00001f, "Second test failed.");
		}

		[Test]
		public void TestVectorF4_Constructor()
		{
			Func<VectorF4> del = () => new VectorF4(1, 2, 3, 4);

			var cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(new VectorF4(1, 2, 3, 4), (VectorF4)SpeContext.UnitTestRunProgram(cc));
		}

		[Test]
		public void TestVectorF4_ConstructorLocalVariable()
		{
			Func<VectorF4> del = () => new VectorF4(1, 2, 3, 4);

			var cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(new VectorF4(1, 2, 3, 4), (VectorF4)SpeContext.UnitTestRunProgram(cc));
		}

		static private void TestVectorF4_ConstructorRefArg_RefArgMehod(out VectorF4 v)
		{
			v = new VectorF4(1, 2, 3, 4);
		}

		[Test]
		public void TestVectorF4_ConstructorRefArg()
		{
			Func<VectorF4> del = 
				delegate
					{
						VectorF4 v;
						TestVectorF4_ConstructorRefArg_RefArgMehod(out v);
						return v;
					};

			var cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(new VectorF4(1, 2, 3, 4), (VectorF4)SpeContext.UnitTestRunProgram(cc));
		}

		[Test]
		public void TestVectorF4_Splat()
		{
			Func<float, VectorF4> del = f => VectorF4.Splat(f);

			var cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			float arg = 17;

			AreEqual(new VectorF4(arg, arg, arg, arg), VectorF4.Splat(arg));
			AreEqual(VectorF4.Splat(arg), (VectorF4)SpeContext.UnitTestRunProgram(cc, arg));
		}

		[Test]
		public void TestVectorF4_GetElement()
		{
			Func<VectorF4, int, float> del =
				delegate(VectorF4 v, int i)
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

			var v1 = new VectorF4(3, 9, 13, -42);

			AreEqual(del(v1, 1), (float) SpeContext.UnitTestRunProgram(cc, v1, 1));
			AreEqual(del(v1, 2), (float) SpeContext.UnitTestRunProgram(cc, v1, 2));
			AreEqual(del(v1, 3), (float) SpeContext.UnitTestRunProgram(cc, v1, 3));
			AreEqual(del(v1, 4), (float) SpeContext.UnitTestRunProgram(cc, v1, 4));
		}

		[Test]
		public void TestVectorF4_Equal()
		{
			Func<VectorF4, VectorF4, bool> del = (arg1, arg2) => arg1 == arg2;

			var cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			var v1 = new VectorF4(5, 6, 7, 8);
			var v2 = new VectorF4(1, 2, 3, 4);
			var v3 = new VectorF4(1, 2, 3, 4);

			AreEqual(del(v1, v2), (bool) SpeContext.UnitTestRunProgram(cc, v1, v2), "First test failed.");
			AreEqual(del(v2, v3), (bool) SpeContext.UnitTestRunProgram(cc, v2, v3), "Second test failed.");
		}

		[Test]
		public void TestVectorD2_Add()
		{
			Func<VectorD2, VectorD2, VectorD2> del = (arg1, arg2) => arg2 + arg1;

			var cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			var v1 = new VectorD2(1, 2);
			var v2 = new VectorD2(5, 6);
			var v3 = new VectorD2(-16, -32);

			AreEqual(del(v1, v2), (VectorD2)SpeContext.UnitTestRunProgram(cc, v1, v2), "First test failed.");
			AreEqual(del(v2, v3), (VectorD2)SpeContext.UnitTestRunProgram(cc, v2, v3), "Second test failed.");
		}

		[Test]
		public void TestVectorD2_Sub()
		{
			Func<VectorD2, VectorD2, VectorD2> del = (arg1, arg2) => arg1 - arg2;

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			var v1 = new VectorD2(5, 6);
			var v2 = new VectorD2(1, 2);
			var v3 = new VectorD2(-16, -32);

			AreEqual(del(v1, v2), (VectorD2) SpeContext.UnitTestRunProgram(cc, v1, v2), "First test failed.");
			AreEqual(del(v2, v3), (VectorD2)SpeContext.UnitTestRunProgram(cc, v2, v3), "Second test failed.");
		}

		[Test]
		public void TestVectorD2_Mul()
		{
			Func<VectorD2, VectorD2, VectorD2> del = (arg1, arg2) => arg1*arg2;

			var cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			var v1 = new VectorD2(5, 6);
			var v2 = new VectorD2(1, 2);
			var v3 = new VectorD2(-16, -32);

			AreWithinLimits(del(v1, v2), (VectorD2) SpeContext.UnitTestRunProgram(cc, v1, v2), 0.00001f, "First test failed.");
			AreWithinLimits(del(v2, v3), (VectorD2) SpeContext.UnitTestRunProgram(cc, v2, v3), 0.00001f, "Second test failed.");
		}

		[Test]
		public void TestVectorD2_Div()
		{
			Func<VectorD2, VectorD2, VectorD2> del = (arg1, arg2) => arg1/arg2;

			var cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			var v1 = new VectorD2(5, -6);
			var v2 = new VectorD2(1, -3);
			var v3 = new VectorD2(-16, -32);

			AreWithinLimits(del(v1, v2), (VectorD2) SpeContext.UnitTestRunProgram(cc, v1, v2), 0.00001f, "First test failed.");
			AreWithinLimits(del(v2, v3), (VectorD2) SpeContext.UnitTestRunProgram(cc, v2, v3), 0.00001f, "Second test failed.");
		}

		[Test]
		public void TestVectorD2_Constructor()
		{
			Func<VectorD2> del = () => new VectorD2(1, 2);

			var cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(new VectorD2(1, 2), (VectorD2)SpeContext.UnitTestRunProgram(cc));
		}

		[Test]
		public void TestVectorD2_ConstructorLocalVariable()
		{
			Func<VectorD2> del = () => new VectorD2(1, 2);

			var cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(new VectorD2(1, 2), (VectorD2)SpeContext.UnitTestRunProgram(cc));
		}

		static private void TestVectorD2_ConstructorRefArg_RefArgMehod(out VectorD2 v)
		{
			v = new VectorD2(1, 2);
		}

		[Test]
		public void TestVectorD2_ConstructorRefArg()
		{
			Func<VectorD2> del =
				delegate
				{
					VectorD2 v;
					TestVectorD2_ConstructorRefArg_RefArgMehod(out v);
					return v;
				};

			var cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(new VectorD2(1, 2), (VectorD2)SpeContext.UnitTestRunProgram(cc));
		}

		[Test]
		public void TestVectorD2_Splat()
		{
			Func<double, VectorD2> del = d => VectorD2.Splat(d);

			var cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			double arg = 17;

			AreEqual(new VectorD2(arg, arg), VectorD2.Splat(arg));
			AreEqual(VectorD2.Splat(arg), (VectorD2)SpeContext.UnitTestRunProgram(cc, arg));
		}

		[Test]
		public void TestVectorD2_GetElement()
		{
			Func<VectorD2, int, double> del =
				delegate(VectorD2 v, int i)
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

			var v1 = new VectorD2(3, 9);

			AreEqual(del(v1, 1), (double)SpeContext.UnitTestRunProgram(cc, v1, 1));
			AreEqual(del(v1, 2), (double)SpeContext.UnitTestRunProgram(cc, v1, 2));
		}

		[Test]
		public void TestVectorD2_Equal()
		{
			Func<VectorD2, VectorD2, bool> del = (arg1, arg2) => arg1 == arg2;

			var cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			var v1 = new VectorD2(5, 6);
			var v2 = new VectorD2(1, 2);
			var v3 = new VectorD2(1, 2);

			AreEqual(del(v1, v2), (bool) SpeContext.UnitTestRunProgram(cc, v1, v2), "First test failed.");
			AreEqual(del(v2, v3), (bool) SpeContext.UnitTestRunProgram(cc, v2, v3), "Second test failed.");
		}

		private static VectorF4 ArrayIntVectorFunc2(VectorF4 v1, VectorF4 v2)
		{
			VectorF4[] varr = new VectorF4[3];

			varr[0] = v1;
			varr[1] = v2;

			varr[2] = varr[0] + varr[1];

			return varr[2];
		}

		[Test]
		public void TestVectorIntArray_Simple()
		{
			Func<VectorI4, VectorI4> del1 = delegate(VectorI4 vec)
			                                      	{
			                                      		var varr = new VectorI4[1];

			                                      		varr[0] = vec;
			                                      		return varr[0];
			                                      	};

			var cc = new CompileContext(del1.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			var v1 = new VectorI4(5, 6, 7, 8);
			var v2 = new VectorI4(1, 2, 3, 4);

			AreEqual(del1(v1), (VectorI4) SpeContext.UnitTestRunProgram(cc, v1), "First test failed.");
			AreEqual(del1(v2), (VectorI4) SpeContext.UnitTestRunProgram(cc, v2), "Second test failed.");
		}

		[Test]
		public void TestVectorIntArray()
		{
			Func<VectorI4, VectorI4, VectorI4> del =
				(arg1, arg2) =>
					{
						var varr = new VectorI4[3];

						varr[0] = arg1;
						varr[1] = arg2;

						varr[2] = varr[0] + varr[1];

						return varr[2];
					};

			var cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			var v1 = new VectorI4(5, 6, 7, 8);
			var v2 = new VectorI4(1, 2, 3, 4);
			var v3 = new VectorI4(1, 2, 3, 4);

			AreEqual(del(v1, v2), (VectorI4) SpeContext.UnitTestRunProgram(cc, v1, v2), "First test failed.");
			AreEqual(del(v2, v3), (VectorI4)SpeContext.UnitTestRunProgram(cc, v2, v3), "Second test failed.");
		}

		[Test]
		public void TestVectorIntArray_OverWrite()
		{
			Func<VectorI4, VectorI4, VectorI4> del =
				delegate(VectorI4 arg1, VectorI4 arg2)
					{
						var varr = new VectorI4[100];

						varr[14] = arg2;

						for (int i = 0; i < 100; i++)
							if (i != 14)
								varr[i] = arg1;

						return varr[14];
					};

			var cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			var v1 = new VectorI4(5, 6, 7, 8);
			var v2 = new VectorI4(1, 2, 3, 4);
			var v3 = new VectorI4(25, 64325, 23645, 435);

			AreEqual(del(v1, v2), (VectorI4) SpeContext.UnitTestRunProgram(cc, v1, v2), "First test failed. Expected");
			AreEqual(del(v2, v3), (VectorI4) SpeContext.UnitTestRunProgram(cc, v2, v3), "Second test failed.");
		}
	}
}
#endif