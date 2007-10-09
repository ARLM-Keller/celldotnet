using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace CellDotNet
{
	[TestFixture]
	public class ObjectModelTest : UnitTest
	{
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
			Func<int> del = 
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
			Func<int> del =
				delegate
				{
					int[] arr = new int[5];
					return arr.Length;
				};

			AreEqual(del(), (int)SpeContext.UnitTestRunProgram(del));
		}

		[Test]
		public void TestArray_Int()
		{
			const int MagicNumber = 0xbababa;
			Func<int> del = 
				delegate
					{
						int[] arr = new int[10];
						arr[0] = MagicNumber;
						arr[1] = 20;
						return arr[0];
					};
			Func<int> del2 = SpeDelegateRunner.CreateSpeDelegate(del);

			if (!SpeContext.HasSpeHardware)
				return;

			int retval = del2();
			AreEqual(MagicNumber, retval);
		}

		[Test]
		public void TestArray_Int2()
		{
			const int MagicNumber = 0xbababa;
			Func<int> del =
				delegate
				{
					// Check that arr2 doesn't overwrite arr1.
					int[] arr1 = new int[1];
					arr1[0] = MagicNumber;
					int[] arr2 = new int[1];
					arr2[0] = 50;

					return arr1[0];
				};

			Func<int> del2 = SpeDelegateRunner.CreateSpeDelegate(del);

			if (!SpeContext.HasSpeHardware)
				return;

			int retval = del2();
			AreEqual(MagicNumber, retval);
		}

		[Test]
		public void TestArray_Int3()
		{
			Func<int> del =
				delegate
				{
					int[] arr1 = new int[2];
					arr1[0] = 10;
					arr1[1] = 50;

					return arr1[0] + arr1[1];
				};

			Func<int> del2 = SpeDelegateRunner.CreateSpeDelegate(del);

			if (!SpeContext.HasSpeHardware)
				return;

			int retval = del2();
			AreEqual(60, retval);
		}

		[Test]
		public void TestArray_Int_4()
		{
			Converter<int, int> del =
				delegate(int index)
				{
					int[] arr = new int[8];

					for(int i = 0; i < arr.Length; i++)
						arr[i] = i;

					return arr[index];
				};

//			Converter<int, int> del2 = SpeDelegateRunner.CreateSpeDelegate(del);

			CompileContext cc = new CompileContext(del.Method);

			if (!SpeContext.HasSpeHardware)
				return;

			for(int i = 0; i < 8; i++)
			{
				int retval = (int)SpeContext.UnitTestRunProgram(cc, i);
				AreEqual(i, retval);
			}
		}

//		[Test]
//		public void TestArray_Double_1()
//		{
//			const double MagicNumber = -123455678	;
//			DoubleReturnDelegate del =
//				delegate
//				{
//					double[] arr = new double[10];
//					arr[0] = MagicNumber;
//					arr[1] = 20;
//					return arr[0];
//				};
//			DoubleReturnDelegate del2 = SpeDelegateRunner.CreateSpeDelegate(del);
//
//			if (!SpeContext.HasSpeHardware)
//				return;
//
//			double retval = del2();
//			AreEqual(MagicNumber, retval);
//		}
//
//		[Test]
//		public void TestArray_Double_2()
//		{
//			const double MagicNumber = 0xbababa;
//			DoubleReturnDelegate del =
//				delegate
//				{
//					// Check that arr2 doesn't overwrite arr1.
//					double[] arr1 = new double[1];
//					arr1[0] = MagicNumber;
//					double[] arr2 = new double[1];
//					arr2[0] = 50;
//
//					return arr1[0];
//				};
//
//			DoubleReturnDelegate del2 = SpeDelegateRunner.CreateSpeDelegate(del);
//
//			if (!SpeContext.HasSpeHardware)
//				return;
//
//			double retval = del2();
//			AreEqual(MagicNumber, retval);
//		}
//
//		[Test]
//		public void TestArray_Double_3()
//		{
//			DoubleReturnDelegate del =
//				delegate
//				{
//					double[] arr1 = new double[2];
//					arr1[0] = 10;
//					arr1[1] = 50;
//
//					return arr1[0] + arr1[1];
//				};
//
//			DoubleReturnDelegate del2 = SpeDelegateRunner.CreateSpeDelegate(del);
//
//			if (!SpeContext.HasSpeHardware)
//				return;
//
//			double retval = del2();
//			AreEqual(60.0, retval);
//		}

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

			int arg = 7913;
			AreEqual(del(arg), (int)SpeContext.UnitTestRunProgram(del, arg));
		}

		[Test]
		public void TestStruct_InstanceMethodAndFields()
		{
			Func<int> del =
				delegate
				{
					QWStruct s = new QWStruct();
					s.i1 = 11;
					s.i2 = 22;
					s.i3 = 33;
					s.i4 = 44;

					return s.ReturnSum();
				};

			AreEqual(del(), (int)SpeContext.UnitTestRunProgram(del));
		}

		[Test]
		public void TestStruct_Fields()
		{
			Func<int> del = 
				delegate
					{
						QWStruct s = new QWStruct();
						s.i1 = 1;
						s.i2 = 2;
						s.i3 = 11;
						s.i4 = 12;

						return s.i1 + s.i2 + s.i3 + s.i4;
					};

			AreEqual(del(), (int) SpeContext.UnitTestRunProgram(del));
		}

		[Test]
		public void TestStruct_FieldsCleared()
		{
			Func<int> del =
				delegate
				{
					QWStruct s = new QWStruct();

					return s.i1 + s.i2 + s.i3 + s.i4;
				};

			AreEqual(del(), (int)SpeContext.UnitTestRunProgram(del));
		}

		[Test]
		public void TestStruct_ConstructorAndFields()
		{
			Func<int> del =
				delegate
				{
					QWStruct s = new QWStruct(1, 2, 11, 12);

					return s.i1 + s.i2 + s.i3 + s.i4;
				};

			AreEqual(del(), (int)SpeContext.UnitTestRunProgram(del));
		}

		[Test]
		public void TestStruct_Fields_Multiple()
		{
			Func<int> del =
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

			AreEqual(del(), (int)SpeContext.UnitTestRunProgram(del));
		}


		#region BigStruct

		struct BigStruct
		{
			public int i1;
			public int i2;
			public int i3;
			public int i4;
			public int i5;
			public int i6;
			public int i7;
			public int i8;


			public BigStruct(int i1, int i3, int i5, int i7)
			{
				this.i1 = i1;
				this.i3 = i3;
				this.i5 = i5;
				this.i7 = i7;
				i8 = 0;
				i6 = 0;
				i4 = 0;
				i2 = 0;
			}

			public int SumElements()
			{
				return i1 + i2 + i3 + i4 + i5 + i6 + i7 + i8;
			}
		}

		#endregion

		[Test]
		public void TestStruct_Field_Big()
		{
			Func<int> del =
				delegate
				{
					BigStruct s = new BigStruct();
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
			Func<int> del =
				delegate
				{
					BigStruct s = new BigStruct();

					return s.i1 + s.i2 + s.i3 + s.i4 + s.i5 + s.i6 + s.i7 + s.i8;
				};

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(del(), (int)SpeContext.UnitTestRunProgram(cc));
		}

		#region BigFieldStructs

		struct BigFieldStruct_1
		{
			public Int32Vector v1;
		}

		struct BigFieldStruct_2
		{
			public int i1;
			public Int32Vector v1;
			public int i2;
			public Int32Vector v2;
			public int i3;
			public Int32Vector v3;
			public int i4;
			public Int32Vector v4;
		}

		#endregion


		[Test]
		public void TestStruct_BigField_1()
		{
			Converter<int, Int32Vector> del =
				delegate(int input)
				{
					BigFieldStruct_1 s = new BigFieldStruct_1();

					s.v1 = new Int32Vector(7, 9, 13, 17);

					return s.v1;
				};

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(del(0), (Int32Vector)SpeContext.UnitTestRunProgram(cc, 0));
		}

		[Test]
		public void TestStruct_BigField_2()
		{
			Converter<int, Int32Vector> del =
				delegate(int input)
				{
					BigFieldStruct_2 s = new BigFieldStruct_2();

					s.v1 = new Int32Vector(21, 22, 23, 24);
					s.v2 = new Int32Vector(5, 6, 7, 8);
					s.v3 = new Int32Vector(5, 6, 7, 8);
					s.v4 = new Int32Vector(5, 6, 7, 8);
					s.i1 = 1;
					s.i2 = 2;
					s.i3 = 3;
					s.i4 = 4;

					return s.v1;
				};

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(del(0), (Int32Vector)SpeContext.UnitTestRunProgram(cc, 0));
		}

		[Test]
		public void TestStruct_BigField_3()
		{
			Converter<int, Int32Vector> del =
				delegate(int input)
				{
					BigFieldStruct_2 s = new BigFieldStruct_2();

					s.v2 = new Int32Vector(21, 22, 23, 24);
					s.v1 = new Int32Vector(5, 6, 7, 8);
					s.v3 = new Int32Vector(5, 6, 7, 8);
					s.v4 = new Int32Vector(5, 6, 7, 8);
					s.i1 = 1;
					s.i2 = 2;
					s.i3 = 3;
					s.i4 = 4;

					return s.v2;
				};

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(del(0), (Int32Vector)SpeContext.UnitTestRunProgram(cc, 0));
		}

		[Test]
		public void TestStruct_BigField_4()
		{
			Converter<int, Int32Vector> del =
				delegate(int input)
				{
					BigFieldStruct_2 s = new BigFieldStruct_2();

					s.v3 = new Int32Vector(21, 22, 23, 24);
					s.v1 = new Int32Vector(5, 6, 7, 8);
					s.v2 = new Int32Vector(5, 6, 7, 8);
					s.v4 = new Int32Vector(5, 6, 7, 8);
					s.i1 = 1;
					s.i2 = 2;
					s.i3 = 3;
					s.i4 = 4;

					return s.v3;
				};

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(del(0), (Int32Vector)SpeContext.UnitTestRunProgram(cc, 0));
		}

		[Test]
		public void TestStruct_BigField_5()
		{
			Converter<int, Int32Vector> del =
				delegate(int input)
				{
					BigFieldStruct_2 s = new BigFieldStruct_2();

					s.v4 = new Int32Vector(21, 22, 23, 24);
					s.v1 = new Int32Vector(5, 6, 7, 8);
					s.v2 = new Int32Vector(5, 6, 7, 8);
					s.v3 = new Int32Vector(5, 6, 7, 8);
					s.i1 = 1;
					s.i2 = 2;
					s.i3 = 3;
					s.i4 = 4;

					return s.v4;
				};

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(del(0), (Int32Vector)SpeContext.UnitTestRunProgram(cc, 0));
		}

		private static void ChangeBigStructByval(BigStruct bs, int newi5Value)
		{
			bs.i5 = newi5Value;
		}

		private static void ChangeBigStructByref(ref BigStruct bs, int newi5Value)
		{
			bs.i5 = newi5Value;
		}

		private static BigStruct ReturnBigStructCopy(BigStruct bs)
		{
			return bs;
		}

		private static BigStruct ReturnBigStructCopy(ref BigStruct bs)
		{
			return bs;
		}

		private static BigStruct ReturnBigStruct(int i1, int i3, int i5, int i7)
		{
			BigStruct bs = new BigStruct(i1, i3, i5, i7);
			return bs;
		}

		[Test]
		public void TestStruct_ArgumentCantModifyCallersValue()
		{
			const int i5value = 50;
			const int i5newValue = 100;
			Func<int> del = 
				delegate
					{
						BigStruct bs = new BigStruct(10, 30, i5value, 40);
						ChangeBigStructByval(bs, i5newValue);
						return bs.i5;
					};

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(i5value, (int) SpeContext.UnitTestRunProgram(cc));
		}

		[Test]
		public void TestStruct_ArgumentByref()
		{
			const int i5value = 50;
			const int i5newValue = 100;
			Func<int> del = 
				delegate
					{
						BigStruct bs = new BigStruct(10, 30, i5value, 40);
						ChangeBigStructByref(ref bs, i5newValue);
						return bs.i5;
					};

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(i5newValue, (int) SpeContext.UnitTestRunProgram(cc));
		}

		[Test]
		public void TestStruct_ReturnArgument()
		{
			const int i5value = 50;
			const int i5newValue = 100;
			Func<int> del = 
				delegate
					{
						BigStruct bs = new BigStruct(10, 30, i5value, 40);
						BigStruct bs2 = ReturnBigStructCopy(bs);
						bs.i5 = i5newValue;

						return bs2.i5;
					};

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(i5value, (int) SpeContext.UnitTestRunProgram(cc));
		}

		[Test]
		public void TestStruct_ReturnByrefArgument()
		{
			const int i5value = 50;
			const int i5newValue = 100;
			Func<int> del = 
				delegate
					{
						BigStruct bs = new BigStruct(10, 30, i5value, 40);
						BigStruct bs2 = ReturnBigStructCopy(ref bs);
						bs.i5 = i5newValue;

						return bs2.i5;
					};

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);
			Disassembler.DisassembleToConsole(cc);

			AreEqual(i5value, (int) SpeContext.UnitTestRunProgram(cc));
		}

		[Test]
		public void TestStruct_LocalsAreCopiedByValue()
		{
			const int i5value = 50;
			const int i5newValue = 100;
			Func<int> del = 
				delegate
					{
						BigStruct bs1 = new BigStruct(10, 30, i5value, 40);
						BigStruct bs2 = bs1;

						bs1.i5 = i5newValue;
						return bs2.i5;
					};

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(i5value, (int) SpeContext.UnitTestRunProgram(cc));
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
			Func<int> del = 
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
			Func<int> del =
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
			Func<int> del =
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
			Func<int> del =
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
			Func<int> del =
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
			Func<int> del =
				delegate
				{
					ClassWithInts c = new ClassWithInts(1, 10, 100, 1000);

					return c.i1 + c.i2 + c.i3 + c.i4;
				};

			AreEqual(del(), (int)SpeContext.UnitTestRunProgram(del));
		}

		static void ReassignByrefClassArgument(ref ClassWithInts arg, int i1, int i2, int i3, int i4)
		{
			arg = new ClassWithInts();
			arg.i1 = i1;
			arg.i2 = i2;
			arg.i3 = i3;
			arg.i4 = i4;
		}

		[Test]
		public void TestClass_ArgumentByref()
		{
			Func<int> del =
				delegate
				{
					ClassWithInts c = new ClassWithInts(1, 10, 100, 1000);
					ReassignByrefClassArgument(ref c, 101, 102, 103, 104);

					return c.i1 + c.i2 + c.i3 + c.i4;
				};
			AreEqual(del(), (int)SpeContext.UnitTestRunProgram(del));
		}


		class ClassWithReferenceTypeFields
		{
			public ClassWithReferenceTypeFields ReferenceTypeField1;
			public ClassWithReferenceTypeFields ReferenceTypeField2;
		}

		[Test]
		public void TestClass_ReferenceTypeField()
		{
			Func<int> del = 
				delegate
					{
						ClassWithReferenceTypeFields c1 = new ClassWithReferenceTypeFields();
						c1.ReferenceTypeField1 = new ClassWithReferenceTypeFields();
						c1.ReferenceTypeField2 = new ClassWithReferenceTypeFields();

						int addressDiff = SpuRuntime.UnsafeGetAddress(c1.ReferenceTypeField2) - 
							SpuRuntime.UnsafeGetAddress(c1.ReferenceTypeField2);

						if (addressDiff != 16)
							return 4;

						return 0;
					};

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			if (!SpeContext.HasSpeHardware)
				return;

			AreEqual(0, (int) SpeContext.UnitTestRunProgram(cc));
		}

		class PpeClass
		{
			public const int MagicNumber1 = 0x40b0c0d0;
			public const int MagicNumber2 = 0x50;
			public const int MagicReturn = 0x0b0c0d0;

			public Int32Vector Int32VectorReturnValue;
			public BigStruct BigStructReturnValue;
			public OtherPpeClass OtherPpeClassReturnValue;

			private BigStruct _hitBigStruct;
			public BigStruct HitBigStruct
			{
				get { return _hitBigStruct; }
			}

			private object _hitObject;
			public object HitObject
			{
				get { return _hitObject; }
			}

			private Int32Vector _hitInt32Vector;
			public Int32Vector HitInt32Vector
			{
				get { return _hitInt32Vector; }
			}

			int _hitcount;
			public int Hitcount
			{
				get { return _hitcount; }
			}

			public void Hit()
			{
				_hitcount++;
			}

			public void Hit(int magic1, int magic2)
			{
				AreEqual(MagicNumber1, magic1);
				AreEqual(MagicNumber2, magic2);
				_hitcount++;
			}

			public void Hit(BigStruct bs, int magic2)
			{
				_hitBigStruct = bs;
				AreEqual(MagicNumber2, magic2);
				_hitcount++;
			}

			public void Hit(Int32Vector v)
			{
				_hitInt32Vector = v;
				_hitcount++;
			}

			public int HitWithMagicIntReturnValue()
			{
				_hitcount++;
				return MagicReturn;
			}

			public Int32Vector HitWithVectorReturn()
			{
				_hitcount++;
				return Int32VectorReturnValue;
			}

			public BigStruct HitWithBigStructReturn()
			{
				_hitcount++;
				return BigStructReturnValue;
			}

			public OtherPpeClass HitWithOtherSpeTypeReturn()
			{
				_hitcount++;
				return OtherPpeClassReturnValue;
			}

			public int SomePublicFieldWhichIsNotAccessibleFromSpu;

			public void Hit(object ppeobject)
			{
				_hitcount++;
				_hitObject = ppeobject;
			}
		}

		class OtherPpeClass
		{
		}

		[Test]
		public void TestPpeClass_InstanceMethodCall()
		{
			Action<PpeClass> del = delegate(PpeClass obj) { obj.Hit(); };

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(1, cc.Methods.Count);

			PpeClass inst = new PpeClass();
			SpeContext.UnitTestRunProgram(cc, inst);

			AreEqual(1, inst.Hitcount);
		}

		[Test]
		public void TestPpeClass_InstanceMethodCall_ArgsInt()
		{
			Action<PpeClass> del = delegate(PpeClass obj) { obj.Hit(PpeClass.MagicNumber1, PpeClass.MagicNumber2); };

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(1, cc.Methods.Count);

			PpeClass inst = new PpeClass();
			SpeContext.UnitTestRunProgram(cc, inst);

			AreEqual(1, inst.Hitcount);
		}

		[Test]
		public void TestPpeClass_InstanceMethodCall_ArgsVector()
		{
			Action<PpeClass> del = delegate(PpeClass obj) { obj.Hit(new Int32Vector(10, 11, 12, 13)); };

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(1, cc.Methods.Count);

			PpeClass inst = new PpeClass();
			SpeContext.UnitTestRunProgram(cc, inst);

			AreEqual(1, inst.Hitcount);
			AreEqual(new Int32Vector(10, 11, 12, 13), inst.HitInt32Vector);
		}

		[Test]
		public void TestPpeClass_InstanceMethodCall_ArgsBigStruct()
		{
			Action<PpeClass> del = delegate(PpeClass obj) { obj.Hit(new BigStruct(100, 200, 300, 400), PpeClass.MagicNumber2); };

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(2, cc.Methods.Count);

			PpeClass inst = new PpeClass();
			SpeContext.UnitTestRunProgram(cc, inst);

			AreEqual(1, inst.Hitcount);
			AreEqual(new BigStruct(100, 200, 300, 400), inst.HitBigStruct);
		}

		[Test]
		public void TestPpeClass_InstanceMethodCall_ArgsPpeRefType()
		{
			Action<PpeClass, object> del = delegate(PpeClass obj, object o) { obj.Hit(o); };

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(1, cc.Methods.Count);

			PpeClass inst = new PpeClass();
			string s = "hey";
			SpeContext.UnitTestRunProgram(cc, inst, s);

			AreEqual(1, inst.Hitcount);
			AreSame(s, inst.HitObject);
		}

		class SpeClass
		{
		}

		[Test]
		public void TestPpeClass_InstanceMethodCall_ArgsSpeRefTypeFailure()
		{
			Action<PpeClass> del = delegate(PpeClass obj) { obj.Hit(new SpeClass()); };

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(3, cc.Methods.Count); // call to object() included.

			PpeClass inst = new PpeClass();

			// This should fail somehow.
			SpeContext.UnitTestRunProgram(cc, inst);

			Fail();
		}

		[Test]
		public void TestPpeClass_InstanceMethodCall_ReturnInt()
		{
			Converter<PpeClass, int> del = delegate(PpeClass obj) { return obj.HitWithMagicIntReturnValue(); };

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(1, cc.Methods.Count);

			PpeClass inst = new PpeClass();
			AreEqual(PpeClass.MagicReturn, (int) SpeContext.UnitTestRunProgram(cc, inst));
			AreEqual(1, inst.Hitcount);
		}

		[Test]
		public void TestPpeClass_InstanceMethodCall_ReturnVector()
		{
			Converter<PpeClass, Int32Vector> del = delegate(PpeClass obj) { return obj.HitWithVectorReturn(); };

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(1, cc.Methods.Count);

			PpeClass inst = new PpeClass();
			inst.Int32VectorReturnValue = new Int32Vector(100, 200, 300, 400);
			AreEqual(inst.Int32VectorReturnValue, (Int32Vector) SpeContext.UnitTestRunProgram(cc, inst));
			AreEqual(1, inst.Hitcount);
		}

		[Test]
		public void TestPpeClass_InstanceMethodCall_ReturnBigStruct()
		{
			Converter<PpeClass, int> del = 
				delegate(PpeClass obj)
					{
						BigStruct bs2 = obj.HitWithBigStructReturn();
						return bs2.i1 + bs2.i3 + bs2.i5 + bs2.i7;
					};

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(1, cc.Methods.Count);

			PpeClass inst = new PpeClass();
			BigStruct bs = new BigStruct(1000, 2000, 3000, 4000);
			inst.BigStructReturnValue = bs;
			AreEqual(bs.i1 + bs.i3 + bs.i5 + bs.i7, (int)SpeContext.UnitTestRunProgram(cc, inst));
			AreEqual(1, inst.Hitcount);
		}

		[Test]
		public void TestPpeClass_InstanceMethodCall_ReturnPpeRefType()
		{
			Converter<PpeClass, OtherPpeClass> del = delegate(PpeClass obj) { return obj.HitWithOtherSpeTypeReturn(); };

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(1, cc.Methods.Count);

			PpeClass inst = new PpeClass();
			inst.OtherPpeClassReturnValue = new OtherPpeClass();
			AreSame(inst.OtherPpeClassReturnValue, (OtherPpeClass) SpeContext.UnitTestRunProgram(cc, inst));
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


		// TODO
		// new test: test af feldt i struct og object af ref typer, eks. array og ref typer
		// husk flere feldter
		// se spu runtime get address
	}
}
