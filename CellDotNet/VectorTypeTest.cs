using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using NUnit.Framework;

namespace CellDotNet
{
	[TestFixture]
	public class VectorTypeTest : UnitTest
	{
		private delegate Int32Vector Int32VDelegateInt32V(Int32Vector v1);

		private delegate T Creator<T>();

		private delegate Int32Vector Int32VDelegateInt32VInt32V(Int32Vector v1, Int32Vector v2);
		private delegate Float32Vector Float32VDelegateFloat32VFloat32V(Float32Vector v1, Float32Vector v2);

		private delegate bool BoolDelegateInt32VInt32V(Int32Vector v1, Int32Vector v2);
		private delegate bool BoolDelegateFloat32VFloat32V(Float32Vector v1, Float32Vector v2);

		private delegate int IntDelegateInt32VInt(Int32Vector v1, int i);

		[Test]
		public void TestVectorInt_GetElement()
		{
			Converter<Int32Vector, int> del = delegate(Int32Vector input) { return input.E3; };

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
			IntDelegateInt32VInt del =
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

		private delegate TReturn Func<T1, T2, TReturn>(T1 arg1, T2 arg2);

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
			Converter<Int32Vector, Int32Vector> del = 
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
			Int32VDelegateInt32VInt32V del = delegate(Int32Vector arg1, Int32Vector arg2) { return arg1 + arg2; };

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

				Console.WriteLine(vSPU1);
				Console.WriteLine(vPPU1);
				Console.WriteLine(vSPU2);
				Console.WriteLine(vPPU2);

				IsTrue((Int32Vector)vSPU1 == vPPU1, "First test failed.");
				IsTrue((Int32Vector)vSPU2 == vPPU2, "Second test failed.");
			}
		}

		[Test]
		public void TestVectorInt_Sub()
		{
			Int32VDelegateInt32VInt32V del = delegate(Int32Vector arg1, Int32Vector arg2) { return arg1 - arg2; };

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
		public void TestVectorInt_Constructor()
		{
			// This will use newobj.
			Creator<Int32Vector> del = delegate { return new Int32Vector(1, 2, 3, 4); };

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			List<MethodVariable> vlist = Utilities.FindAll(cc.EntryPointAsMetodCompiler.Variables,
				delegate(MethodVariable var) { return var.StackType == StackTypeDescription.Int32Vector; });
			AreEqual(1, vlist.Count);
			IsFalse(vlist[0].Escapes.Value);

			cc.WriteAssemblyToFile("vector.s");

			AreEqual(new Int32Vector(1, 2, 3, 4), (Int32Vector) SpeContext.UnitTestRunProgram(cc));
		}

		[Test]
		public void TestVectorInt_ConstructorLocalVariable()
		{
			// This will use call.
			Creator<Int32Vector> del = delegate
			                           	{
			                           		Int32Vector v = new Int32Vector(1, 2, 3, 4);
			                           		return v;
			                           	};

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			new TreeDrawer().DrawMethod(cc.EntryPointAsMetodCompiler);

			List<MethodVariable> vlist = Utilities.FindAll(cc.EntryPointAsMetodCompiler.Variables,
				delegate(MethodVariable var) { return var.StackType == StackTypeDescription.Int32Vector; });

			// allow 2 since debug mode branch might have induced an extra variable.
			IsTrue(vlist.Count == 1 || vlist.Count == 2); 
			IsFalse(vlist[0].Escapes.Value);
			if (vlist.Count == 2)
				IsFalse(vlist[1].Escapes.Value);

			AreEqual(new Int32Vector(1, 2, 3, 4), (Int32Vector)SpeContext.UnitTestRunProgram(cc));
		}

		[Test, Ignore("Currently we can't handle bools.")]
		public void TestVectorInt_Equal()
		{
			BoolDelegateInt32VInt32V del = delegate(Int32Vector arg1, Int32Vector arg2) { return arg1 == arg2; };

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
			Float32VDelegateFloat32VFloat32V del = delegate(Float32Vector arg1, Float32Vector arg2) { return arg2 + arg1; };

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

				Console.WriteLine(vSPU1);
				Console.WriteLine(vPPU1);
				Console.WriteLine(vSPU2);
				Console.WriteLine(vPPU2);

				IsTrue((Float32Vector)vSPU1 == vPPU1, "First test failed.");
				IsTrue((Float32Vector)vSPU2 == vPPU2, "Second test failed.");
			}
		}

		[Test]
		public void TestVectorFloat_Sub()
		{
			Float32VDelegateFloat32VFloat32V del = delegate(Float32Vector arg1, Float32Vector arg2) { return arg1 - arg2; };

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
			Float32VDelegateFloat32VFloat32V del = delegate(Float32Vector arg1, Float32Vector arg2) { return arg1 * arg2; };

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
		public void TestVectorFloat_Constructor()
		{
			Creator<Float32Vector> del = delegate { return new Float32Vector(1, 2, 3, 4); };

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(new Float32Vector(1, 2, 3, 4), (Float32Vector)SpeContext.UnitTestRunProgram(cc));
		}

		[Test]
		public void TestVectorFloat_ConstructorLocalVariable()
		{
			Creator<Float32Vector> del =
				delegate
					{
						Float32Vector v = new Float32Vector(1, 2, 3, 4);
						return v;
					};

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(new Float32Vector(1, 2, 3, 4), (Float32Vector)SpeContext.UnitTestRunProgram(cc));
		}


		private delegate float FloatElementDelegate(Float32Vector v, int elementno);

		[Test]
		public void TestVectorFloat_GetElement()
		{
			FloatElementDelegate del =
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

		[Test, Ignore("Currently we can't handle bools.")]
		public void TestVectorFloat_Equal()
		{
			BoolDelegateFloat32VFloat32V del =
				delegate(Float32Vector arg1, Float32Vector arg2)
				{
					return arg1 == arg2;
				};

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
			Int32VDelegateInt32V del1 = delegate(Int32Vector vec)
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
			Int32VDelegateInt32VInt32V del =
				delegate(Int32Vector arg1, Int32Vector arg2)
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
			Int32VDelegateInt32VInt32V del =
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
