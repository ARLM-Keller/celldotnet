using NUnit.Framework;

namespace CellDotNet
{
	[TestFixture]
	public class VectorTypeTest : UnitTest
	{
		private delegate int SimpleDelegate(Int32Vector v1);

		private delegate Int32Vector DelegateVectorOperation(Int32Vector v1, Int32Vector v2);

		private static int SimpleVectorTestFunc(Int32Vector v1)
		{
			return Int32Vector.getE3(v1);
		}


		[Test]
		public void TestSimple()
		{
			SimpleDelegate del = SimpleVectorTestFunc;

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			Int32Vector v1 = new Int32Vector();

			v1.e3 = 17;

			int PPUresult = del(v1);

			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext sc = new SpeContext())
			{
				object rv = sc.RunProgram(cc, v1);
				AreEqual(PPUresult, (int)rv);
			}
		}

		public static Int32Vector AddVectorFunc(Int32Vector v1, Int32Vector v2)
		{
			return v1 + v2;
		}

		public static Int32Vector SubVectorFunc(Int32Vector v1, Int32Vector v2)
		{
			return v1 - v2;
		}

		[Test]
		public void TestVectorIntAdd()
		{
			DelegateVectorOperation del = AddVectorFunc;

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			Int32Vector v1 = new Int32Vector(1, 2, 3, 4);
			Int32Vector v2 = new Int32Vector(5, 6, 7, 8);

			Int32Vector vPPU = del(v1, v2);

			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext sc = new SpeContext())
			{
				object vSPU = sc.RunProgram(cc, v1, v2);

				IsTrue((Int32Vector)vSPU == vPPU);
			}
		}

		public void TestVectorIntSub()
		{
			DelegateVectorOperation del = AddVectorFunc;

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			Int32Vector v1 = new Int32Vector(1, 2, 3, 4);
			Int32Vector v2 = new Int32Vector(5, 6, 7, 8);

			Int32Vector vPPU = del(v1, v2);

			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext sc = new SpeContext())
			{
				object vSPU = sc.RunProgram(cc, v1, v2);

				IsTrue((Int32Vector)vSPU == vPPU);
			}
		}
	}
}
