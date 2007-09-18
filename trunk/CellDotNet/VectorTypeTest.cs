using System;
using NUnit.Framework;

namespace CellDotNet
{
	[TestFixture]
	public class VectorTypeTest : UnitTest
	{
		private delegate int IntDelegateInt32V(Int32Vector v1);

		private delegate Int32Vector Int32VDelegateInt32V(Int32Vector v1);

		private delegate Int32Vector Int32VDelegateInt32VInt32V(Int32Vector v1, Int32Vector v2);
		private delegate Float32Vector Float32VDelegateFloat32VFloat32V(Float32Vector v1, Float32Vector v2);

		private delegate bool BoolDelegateInt32VInt32V(Int32Vector v1, Int32Vector v2);
		private delegate bool BoolDelegateFloat32VFloat32V(Float32Vector v1, Float32Vector v2);

		private delegate int IntDelegateInt32VInt(Int32Vector v1, int i);

		[Test]
		public void TestSimple()
		{
			IntDelegateInt32V del = delegate(Int32Vector input) { return Int32Vector.GetE3(input); };

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			Int32Vector v1 = new Int32Vector(0, 0, 17, 0);

			int PPUresult = del(v1);

			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext sc = new SpeContext())
			{
				object rv = sc.RunProgram(cc, v1);
				AreEqual(PPUresult, (int)rv);
			}
		}

		[Test]
		public void TestVectorIntGetElement()
		{
			IntDelegateInt32VInt del = 
				delegate(Int32Vector v, int i)
					{
						if (i == 1)
							return Int32Vector.GetE1(v);
						else if (i == 2)
							return Int32Vector.GetE2(v);
						else if (i == 3)
							return Int32Vector.GetE3(v);
						else if (i == 4)
							return Int32Vector.GetE4(v);
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

		[Test]
		public void TestVectorIntGetPutElement()
		{
			Int32VDelegateInt32VInt32V del =
				delegate(Int32Vector vin, Int32Vector vout)
					{
						vout = Int32Vector.PutE1(vout, Int32Vector.GetE1(vin));
						vout = Int32Vector.PutE2(vout, Int32Vector.GetE2(vin));
						vout = Int32Vector.PutE3(vout, Int32Vector.GetE3(vin));
						vout = Int32Vector.PutE4(vout, Int32Vector.GetE4(vin));
						return vout;
					};

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			Int32Vector v1 = new Int32Vector(1, 2, 3, 4);
			Int32Vector v2 = new Int32Vector(5, 6, 7, 8);

			Int32Vector vPPU1 = del(v1, v2);

			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext sc = new SpeContext())
			{
				object vSPU1 = sc.RunProgram(cc, v1, v2);

				Console.WriteLine(vPPU1);
				Console.WriteLine(vSPU1);

				IsTrue((Int32Vector)vSPU1 == vPPU1, "First test failed.");
			}
		}

		[Test]
		public void TestVectorIntAdd()
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
		public void TestVectorIntSub()
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

		[Test, Ignore("Currently we can't handle bools.")]
		public void TestVectorIntEqual()
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
		public void TestVectorFloatAdd()
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
		public void TestVectorFloatSub()
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
		public void TestVectorFloatMul()
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

		[Test, Ignore("Currently we can't handle bools.")]
		public void TestVectorFloatEqual()
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
		public void TestVectorIntArraySimple()
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

			cc.PerformProcessing(CompileContextState.S3InstructionSelectionDone);
			Disassembler.DisassembleUnconditional(cc, Console.Out);

			cc.PerformProcessing(CompileContextState.S8Complete);
			Disassembler.DisassembleToConsole(cc);

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
		public void TestVectorIntArrayOverWrite()
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
