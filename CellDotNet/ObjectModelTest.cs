using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NUnit.Framework;

namespace CellDotNet
{
	[TestFixture]
	public class ObjectModelTest : UnitTest
	{
		private delegate int IntReturnDelegate();
		private delegate void SimpleDelegate();

		[Test]
		public void TestArray_Create()
		{
			SimpleDelegate del =
				delegate
				{
					int[] arr = new int[5];
					Utilities.PretendVariableIsUsed(arr);
				};

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S3InstructionSelectionDone);
			Disassembler.DisassembleUnconditionalToConsole(cc);

			cc.PerformProcessing(CompileContextState.S8Complete);

			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext sc = new SpeContext())
			{
				sc.RunProgram(cc);
			}
		}

		[Test]
		public void TestArray_Length()
		{
			IntReturnDelegate del = 
				delegate
					{
						int[] arr = new int[0];
						return arr.Length;
					};

//			IntReturnDelegate del2 = SpeDelegateRunner.CreateSpeDelegate(del);
//			SpeDelegateRunner t = (SpeDelegateRunner)del2.Target;
//			Disassembler.DisassembleToConsole(t.CompileContext);

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext sc = new SpeContext())
			{
				object ret = sc.RunProgram(cc);
				AreEqual(typeof(int), ret.GetType());
				AreEqual(0, (int) ret);
			}
		}

		[Test]
		public void TestArray_Length2()
		{
			IntReturnDelegate del =
				delegate
				{
					int[] arr = new int[5];
					return arr.Length;
				};

			IntReturnDelegate del2 = SpeDelegateRunner.CreateSpeDelegate(del);
			if (!SpeContext.HasSpeHardware)
				return;

			int val = del2();
			AreEqual(5, val);
		}

		[Test]
		public void TestArray_Int()
		{
			const int MagicNumber = 0xbababa;
			IntReturnDelegate del = 
				delegate
					{
						int[] arr = new int[10];
						arr[0] = MagicNumber;
						arr[1] = 20;
						return arr[0];
					};
			IntReturnDelegate del2 = SpeDelegateRunner.CreateSpeDelegate(del);

			if (!SpeContext.HasSpeHardware)
				return;

			int retval = del2();
			AreEqual(MagicNumber, retval);
		}

		[Test]
		public void TestArray_Int2()
		{
			const int MagicNumber = 0xbababa;
			IntReturnDelegate del =
				delegate
				{
					// Check that arr2 doesn't overwrite arr1.
					int[] arr1 = new int[1];
					arr1[0] = MagicNumber;
					int[] arr2 = new int[1];
					arr2[0] = 50;

					return arr1[0];
				};

			IntReturnDelegate del2 = SpeDelegateRunner.CreateSpeDelegate(del);

			if (!SpeContext.HasSpeHardware)
				return;

			int retval = del2();
			AreEqual(MagicNumber, retval);
		}

		[Test]
		public void TestArray_Int3()
		{
			IntReturnDelegate del =
				delegate
				{
					int[] arr1 = new int[2];
					arr1[0] = 10;
					arr1[1] = 50;

					return arr1[0] + arr1[1];
				};

			IntReturnDelegate del2 = SpeDelegateRunner.CreateSpeDelegate(del);

			if (!SpeContext.HasSpeHardware)
				return;

			int retval = del2();
			AreEqual(60, retval);
		}

		#region QWStruct

		struct QWStruct
		{
			public int i1;
			public int i2;
			public int i3;
			public int i4;

			public int ReturnArg(int i)
			{
				return i;
			}

			public int ReturnSum()
			{
				return i1+i2+i3+i4;
			}

		}

		#endregion

		[Test]
		public void TestInstanceMethod_Simple()
		{
			Converter<int, int> del =
				delegate(int i)
				{
					QWStruct s = new QWStruct();

					return s.ReturnArg(i);
				};

			int arg = 7913;

			int correctval = del(arg);

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(correctval, (int)SpeContext.UnitTestRunProgram(cc, arg));
		}

		[Test]
		public void TestInstanceMethod_FieldAccess()
		{
			Converter<int, int> del =
				delegate
				{
					QWStruct s = new QWStruct();
					s.i1 = 11;
					s.i2 = 22;
					s.i3 = 33;
					s.i4 = 44;

					return s.ReturnSum();
				};

			int arg = 0; // Is not used

			int correctval = del(arg);

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(correctval, (int)SpeContext.UnitTestRunProgram(cc, arg));
		}

		[Test]
		public void TestStruct_Field()
		{
			Converter<int, int> del = 
				delegate
					{
						QWStruct s = new QWStruct();
						s.i1 = 1;
						s.i2 = 2;
						s.i3 = 11;
						s.i4 = 12;

						return s.i1 + s.i2 + s.i3 + s.i4;
					};

			int correctval = del(0);

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(correctval, (int) SpeContext.UnitTestRunProgram(cc, 0));
		}

		[Test]
		public void TestStruct_FieldsCleared()
		{
			Converter<int, int> del =
				delegate
				{
					QWStruct s = new QWStruct();

					return s.i1 + s.i2 + s.i3 + s.i4;
				};

			int correctval = del(0);

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(correctval, (int)SpeContext.UnitTestRunProgram(cc, 0));
		}

		#region QWStruct_Big

		struct QWStruct_Big
		{
			public int i1;
			public int i2;
			public int i3;
			public int i4;
			public int i5;
			public int i6;
			public int i7;
			public int i8;
		}

		#endregion

		[Test]
		public void TestStruct_Field_Big()
		{
			Converter<int, int> del =
				delegate
				{
					QWStruct_Big s = new QWStruct_Big();
					s.i1 = 1;
					s.i2 = 2;
					s.i3 = 101;
					s.i4 = 102;
					s.i5 = 1001;
					s.i6 = 1002;
					s.i7 = 10001;
					s.i8 = 10002;

					return s.i1 + s.i2 + s.i3 + s.i4 + s.i5 + s.i6 + s.i7 + s.i8;
				};

			int correctval = del(0);

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(correctval, (int)SpeContext.UnitTestRunProgram(cc, 0));
		}

		[Test]
		public void TestStruct_FieldsCleared_Big()
		{
			Converter<int, int> del =
				delegate
				{
					QWStruct_Big s = new QWStruct_Big();

					return s.i1 + s.i2 + s.i3 + s.i4 + s.i5 + s.i6 + s.i7 + s.i8;
				};

			int correctval = del(0);

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(correctval, (int)SpeContext.UnitTestRunProgram(cc, 0));
		}


		class ClassWithInts
		{
			public int i1;
			public int i2;
			public int i3;
			public int i4;
		}

		[Test]
		public void TestClass_Field()
		{
			IntReturnDelegate del = 
				delegate
					{
						ClassWithInts c = new ClassWithInts();
						c.i1 = 1;
						c.i2 = 10;
						c.i3 = 100;
						c.i4 = 1000;

						return c.i1 + c.i2 + c.i3 + c.i4;
					};

			MethodCompiler mc = new MethodCompiler(del.Method);
			mc.PerformProcessing(MethodCompileState.S2TreeConstructionDone);
			new TreeDrawer().DrawMethod(mc);

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(del(), (int) SpeContext.UnitTestRunProgram(cc));
		}
	}
}
