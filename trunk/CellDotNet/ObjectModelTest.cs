using System;
using System.Collections.Generic;
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

			public QWStruct(int i1, int i2, int i3, int i4)
			{
				this.i1 = i1;
				this.i2 = i2;
				this.i3 = i3;
				this.i4 = i4;
			}

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
		public void TestStruct_InstanceMethod()
		{
			Converter<int, int> del =
				delegate(int i)
				{
					QWStruct s = new QWStruct();

					return s.ReturnArg(i);
				};

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			int arg = 7913;
			AreEqual(del(arg), (int)SpeContext.UnitTestRunProgram(cc, arg));
		}

		[Test]
		public void TestStruct_InstanceMethodAndFields()
		{
			IntReturnDelegate del =
				delegate
				{
					QWStruct s = new QWStruct();
					s.i1 = 11;
					s.i2 = 22;
					s.i3 = 33;
					s.i4 = 44;

					return s.ReturnSum();
				};

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(del(), (int)SpeContext.UnitTestRunProgram(cc));
		}

		[Test]
		public void TestStruct_Fields()
		{
			IntReturnDelegate del = 
				delegate
					{
						QWStruct s = new QWStruct();
						s.i1 = 1;
						s.i2 = 2;
						s.i3 = 11;
						s.i4 = 12;

						return s.i1 + s.i2 + s.i3 + s.i4;
					};

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(del(), (int) SpeContext.UnitTestRunProgram(cc));
		}

		[Test]
		public void TestStruct_FieldsCleared()
		{
			IntReturnDelegate del =
				delegate
				{
					QWStruct s = new QWStruct();

					return s.i1 + s.i2 + s.i3 + s.i4;
				};

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(del(), (int)SpeContext.UnitTestRunProgram(cc));
		}

		[Test]
		public void TestStruct_ConstructorAndFields()
		{
			IntReturnDelegate del =
				delegate
				{
					QWStruct s = new QWStruct(1, 2, 11, 12);

					return s.i1 + s.i2 + s.i3 + s.i4;
				};

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(del(), (int)SpeContext.UnitTestRunProgram(cc));
		}

		[Test]
		public void TestStruct_Fields_Multiple()
		{
			IntReturnDelegate del =
				delegate
				{
					// Checks that the allocated stack positions don't overlap.
					QWStruct s1 = new QWStruct();
					s1.i1 = 1;
					s1.i2 = 2;
					s1.i3 = 11;
					s1.i4 = 12;
					QWStruct s2 = new QWStruct();
					s2.i1 = 11;
					s2.i2 = 12;
					s2.i3 = 111;
					s2.i4 = 112;

					return s1.i1 + s1.i2 + s1.i3 + s1.i4 + s2.i1 + s2.i2 + s2.i3 + s2.i4;
				};

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(del(), (int)SpeContext.UnitTestRunProgram(cc));
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
			IntReturnDelegate del =
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

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(del(), (int)SpeContext.UnitTestRunProgram(cc));
		}

		[Test]
		public void TestStruct_FieldsCleared_Big()
		{
			IntReturnDelegate del =
				delegate
				{
					QWStruct_Big s = new QWStruct_Big();

					return s.i1 + s.i2 + s.i3 + s.i4 + s.i5 + s.i6 + s.i7 + s.i8;
				};

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(del(), (int)SpeContext.UnitTestRunProgram(cc));
		}


		class ClassWithInts
		{
			public int i1;
			public int i2;
			public int i3;
			public int i4;

			public ClassWithInts() {}

			public ClassWithInts(int i1, int i2, int i3, int i4)
			{
				this.i1 = i1;
				this.i2 = i2;
				this.i3 = i3;
				this.i4 = i4;
			}

			public int ReturnArgument(int i)
			{
				return i;
			}

			public int Sum()
			{
				return i1 + i2 + i3 + i4;
			}
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

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(del(), (int) SpeContext.UnitTestRunProgram(cc));
		}

		[Test]
		public void TestClass_InstanceMethod()
		{
			IntReturnDelegate del =
				delegate
				{
					ClassWithInts c = new ClassWithInts();

					return c.ReturnArgument(10);
				};

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(del(), (int)SpeContext.UnitTestRunProgram(cc));
		}

		[Test]
		public void TestClass_InstanceMethodAndFields()
		{
			IntReturnDelegate del =
				delegate
				{
					ClassWithInts c = new ClassWithInts();
					c.i1 = 1;
					c.i2 = 10;
					c.i3 = 100;
					c.i4 = 1000;

					return c.Sum();
				};

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(del(), (int)SpeContext.UnitTestRunProgram(cc));
		}

		[Test]
		public void TestClass_AllocateMultiple()
		{
			IntReturnDelegate del =
				delegate
					{
						// This should detect if they overlap.
						ClassWithInts c = new ClassWithInts();
						c.i1 = 1;
						c.i2 = 10;
						c.i3 = 100;
						c.i4 = 1000;

						ClassWithInts c2 = new ClassWithInts();
						c2.i1 = 11;
						c2.i2 = 110;
						c2.i3 = 1100;
						c2.i4 = 11000;

						return c.i1 + c.i2 + c.i3 + c.i4 + c2.i1 + c2.i2 + c2.i3 + c2.i4;
					};

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(del(), (int) SpeContext.UnitTestRunProgram(cc));
		}

		[Test]
		public void TestClass_AllocateMultiple2()
		{
			IntReturnDelegate del =
				delegate
				{
					ClassWithInts c1 = new ClassWithInts();
					ClassWithInts c2 = new ClassWithInts();

					// With four int fields they should take up 64 bytes (and be located next to each other).
					return SpuRuntime.UnsafeGetAddress(c1) - SpuRuntime.UnsafeGetAddress(c2);
				};

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			if (!SpeContext.HasSpeHardware)
				return;

			AreEqual(64, Math.Abs((int)SpeContext.UnitTestRunProgram(cc)));
		}

		[Test]
		public void TestClass_FieldAndConstructor()
		{
			IntReturnDelegate del =
				delegate
				{
					ClassWithInts c = new ClassWithInts(1, 10, 100, 1000);

					return c.i1 + c.i2 + c.i3 + c.i4;
				};

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(del(), (int)SpeContext.UnitTestRunProgram(cc));
		}

		class PpeClass
		{
			int _hitcount;
			public int Hitcount
			{
				get { return _hitcount; }
			}

			public void Hit()
			{
//				throw new SpeExecutionException();
				_hitcount++;
			}

			public int SomePublicFieldWhichIsNotAccessibleFromSpu;
		}

		[Test]
		public void TestPpeClass_InstanceMethodCall()
		{
			Action<PpeClass> del = delegate(PpeClass obj) { obj.Hit(); };

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);
			cc.WriteAssemblyToFile("ppecall.s", 0);

			AreEqual(1, cc.Methods.Count);

			PpeClass inst = new PpeClass();
			SpeContext.UnitTestRunProgram(cc, inst);

			AreEqual(1, inst.Hitcount);
		}

		[Test]
		public void TestPpeClass_InstanceFieldAccessFailure()
		{
			Action<PpeClass> del = delegate(PpeClass obj) { obj.SomePublicFieldWhichIsNotAccessibleFromSpu = 5; };
			CompileContext cc = new CompileContext(del.Method);

			Utilities.PretendVariableIsUsed(new PpeClass().SomePublicFieldWhichIsNotAccessibleFromSpu);

			try
			{
				cc.PerformProcessing(CompileContextState.S8Complete);

				Fail("Should have failed.");
			}
			catch (NotSupportedException)
			{
				// Ok
			}
		}
	}
}
