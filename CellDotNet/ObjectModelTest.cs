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
	[TestFixture]
//	[Ignore("PPE calls makes mono crash when using instruction scheduling.")]
	public class ObjectModelTest : UnitTest
	{
		[Test]
		public void TestArray_Create()
		{
			Action del =
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
		public void TestArray_Int4()
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

		[Test]
		public void TestArray_Double_1()
		{
			const double MagicNumber = -123455678;
			Func<double> del =
				delegate
				{
					double[] arr = new double[10];
					arr[0] = MagicNumber;
					arr[1] = 20;
					return arr[0];
				};
			Func<double> del2 = SpeDelegateRunner.CreateSpeDelegate(del);

			if (!SpeContext.HasSpeHardware)
				return;

			double retval = del2();
			AreEqual(MagicNumber, retval);
		}

		[Test]
		public void TestArray_Double_2()
		{
			const double MagicNumber = 0xbababa;
			Func<double> del =
				delegate
				{
					// Check that arr2 doesn't overwrite arr1.
					double[] arr1 = new double[1];
					arr1[0] = MagicNumber;
					double[] arr2 = new double[1];
					arr2[0] = 50;

					return arr1[0];
				};

			Func<double> del2 = SpeDelegateRunner.CreateSpeDelegate(del);

			if (!SpeContext.HasSpeHardware)
				return;

			double retval = del2();
			AreEqual(MagicNumber, retval);
		}

		[Test]
		public void TestArray_Double_3()
		{
			Func<double> del =
				delegate
				{
					double[] arr1 = new double[2];
					arr1[0] = 10;
					arr1[1] = 50;

					return arr1[0] + arr1[1];
				};

			Func<double> del2 = SpeDelegateRunner.CreateSpeDelegate(del);

			if (!SpeContext.HasSpeHardware)
				return;

			double retval = del2();
			AreEqual(60.0, retval);
		}

		[Test]
		public void TestArray_Double_4()
		{
			Func<int> del =
				delegate
				{
					double[] arr1 = new double[2];
					double[] arr2 = new double[3];
					arr1[0] = 10;
					arr2[0] = 10;

					return arr1.Length + arr2.Length;
				};


			int expected = del();
			int actual = (int) SpeContext.UnitTestRunProgram(del);
			AreEqual(expected, actual);
		}

		[Test]
		public void TestArray_Int5()
		{
			Func<int> del =
				delegate
					{
						int[] arr1 = new int[2];
						arr1[0] = 10;
						arr1[1] = 20;

						return arr1[0] + arr1[1];
					};

			Func<int> del2 = SpeDelegateRunner.CreateSpeDelegate(del);

			if (!SpeContext.HasSpeHardware)
				return;

			AreEqual(del(), del2());
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
			public VectorI4 v1;
		}

		struct BigFieldStruct_2
		{
			public int i1;
			public VectorI4 v1;
			public int i2;
			public VectorI4 v2;
			public int i3;
			public VectorI4 v3;
			public int i4;
			public VectorI4 v4;
		}

		#endregion


		[Test]
		public void TestStruct_BigField_1()
		{
			Converter<int, VectorI4> del =
				delegate(int input)
				{
					BigFieldStruct_1 s = new BigFieldStruct_1();

					s.v1 = new VectorI4(7, 9, 13, 17);

					return s.v1;
				};

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(del(0), (VectorI4)SpeContext.UnitTestRunProgram(cc, 0));
		}

		[Test]
		public void TestStruct_BigField_2()
		{
			Func<VectorI4> del =
				delegate
				{
					BigFieldStruct_2 s = new BigFieldStruct_2();

					s.v1 = new VectorI4(21, 22, 23, 24);
					s.v2 = new VectorI4(5, 6, 7, 8);
					s.v3 = new VectorI4(5, 6, 7, 8);
					s.v4 = new VectorI4(5, 6, 7, 8);
					s.i1 = 1;
					s.i2 = 2;
					s.i3 = 3;
					s.i4 = 4;

					return s.v1;
				};

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(del(), (VectorI4)SpeContext.UnitTestRunProgram(cc));
		}

		[Test]
		public void TestStruct_BigField_3()
		{
			Func<VectorI4> del =
				delegate
					{
					BigFieldStruct_2 s = new BigFieldStruct_2();

					s.v2 = new VectorI4(21, 22, 23, 24);
					s.v1 = new VectorI4(5, 6, 7, 8);
					s.v3 = new VectorI4(5, 6, 7, 8);
					s.v4 = new VectorI4(5, 6, 7, 8);
					s.i1 = 1;
					s.i2 = 2;
					s.i3 = 3;
					s.i4 = 4;

					return s.v2;
				};

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(del(), (VectorI4)SpeContext.UnitTestRunProgram(cc));
		}

		[Test]
		public void TestStruct_BigField_4()
		{
			Func<VectorI4> del =
				delegate
					{
					BigFieldStruct_2 s = new BigFieldStruct_2();

					s.v3 = new VectorI4(21, 22, 23, 24);
					s.v1 = new VectorI4(5, 6, 7, 8);
					s.v2 = new VectorI4(5, 6, 7, 8);
					s.v4 = new VectorI4(5, 6, 7, 8);
					s.i1 = 1;
					s.i2 = 2;
					s.i3 = 3;
					s.i4 = 4;

					return s.v3;
				};

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(del(), (VectorI4)SpeContext.UnitTestRunProgram(cc));
		}

		[Test]
		public void TestStruct_BigField_5()
		{
			Func<VectorI4> del =
				delegate
				{
					BigFieldStruct_2 s = new BigFieldStruct_2();

					s.v4 = new VectorI4(21, 22, 23, 24);
					s.v1 = new VectorI4(5, 6, 7, 8);
					s.v2 = new VectorI4(5, 6, 7, 8);
					s.v3 = new VectorI4(5, 6, 7, 8);
					s.i1 = 1;
					s.i2 = 2;
					s.i3 = 3;
					s.i4 = 4;

					return s.v4;
				};

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(del(), (VectorI4)SpeContext.UnitTestRunProgram(cc));
		}

#region  dsfdsf

		struct StructWithArray
		{
			public int[] arr;
		}

		#endregion

		[Test]
		public void TestStruct_ArrayField_1()
		{
			Func<int, int> del =
				delegate(int input)
				{
					int [] array = new int[6];
					for (int i = 0; i < array.Length; i++)
						array[i] = i;

					StructWithArray s = new StructWithArray();

					s.arr = array;

					return s.arr[input];
				};

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(del(0), (int)SpeContext.UnitTestRunProgram(cc, 0));
		}

		[Test]
		public void TestStruct_ArrayField_2()
		{
			Func<int, int> del =
				delegate(int input)
				{
					StructWithArray s = new StructWithArray();

					s.arr = new int[6];
					for (int i = 0; i < s.arr.Length; i++)
						s.arr[i] = i;

					return s.arr[input];
				};

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(del(0), (int)SpeContext.UnitTestRunProgram(cc, 0));
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

		private struct DoubleStruct
		{
			public double d1;
			public double d2;
			public int i1;
			public double d3;
		}

		[Test]
		public void TestStruct_Double()
		{
			const double d1const = 11111111.76876;
			const double d2const = -777777.999;
			const int i1const = 223456789;
			const double d3const = 976.999;

			Func<double> del1;

			del1 = delegate
			       	{
			       		var s = new DoubleStruct {d1 = d1const, d2 = d2const, i1 = i1const, d3 = d3const};
			       		return s.d1;
			       	};
			AreEqual(del1(), (double)SpeContext.UnitTestRunProgram(del1), "d1");

			del1 = delegate
			       	{
			       		var s = new DoubleStruct {d1 = d1const, d2 = d2const, i1 = i1const, d3 = d3const};
			       		return s.d2;
			       	};
			AreEqual(del1(), (double)SpeContext.UnitTestRunProgram(del1), "d2");

			Func<int> del2 = delegate
			       	{
			       		var s = new DoubleStruct {d1 = d1const, d2 = d2const, i1 = i1const, d3 = d3const};
			       		return s.i1;
			       	};
			AreEqual(del2(), (int)SpeContext.UnitTestRunProgram(del2), "i1");

			del1 = delegate
			       	{
						// Make sure that d3 is not the last field writte to, so that i1 gets a "chance" to overwrite it.
						var s = new DoubleStruct { d1 = d1const, d2 = d2const, d3 = d3const, i1 = i1const };
			       		return s.d3;
			       	};
			AreEqual(del1(), (double)SpeContext.UnitTestRunProgram(del1), "d3");
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
			public int IntField;
		}

		[Test]
		public void TestClass_ReferenceTypeField()
		{
			Func<int> del =
				delegate
					{
						ClassWithReferenceTypeFields c =
							new ClassWithReferenceTypeFields
								{
									ReferenceTypeField1 = new ClassWithReferenceTypeFields {IntField = 500},
									ReferenceTypeField2 = new ClassWithReferenceTypeFields {IntField = 700}
								};

						int addressDiff = SpuRuntime.UnsafeGetAddress(c.ReferenceTypeField2) -
						                  SpuRuntime.UnsafeGetAddress(c.ReferenceTypeField1);

						if (addressDiff != 3*16)
							return addressDiff;

						if (c.ReferenceTypeField2.IntField - c.ReferenceTypeField1.IntField != 200)
							return 200;

						return 1000;
					};

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			if (!SpeContext.HasSpeHardware)
				return;

			AreEqual(1000, (int) SpeContext.UnitTestRunProgram(cc));
		}

		class PpeClass
		{
			public const int MagicNumber1 = 0x40b0c0d0;
			public const int MagicNumber2 = 0x50;
			public const int MagicReturn = 0x0b0c0d0;

			public VectorI4 VectorI4ReturnValue;
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

			private VectorI4 _hitVectorI4;
			public VectorI4 HitVectorI4
			{
				get { return _hitVectorI4; }
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

			public void Hit(VectorI4 v)
			{
				_hitVectorI4 = v;
				_hitcount++;
			}

			public int HitWithMagicIntReturnValue()
			{
				_hitcount++;
				return MagicReturn;
			}

			public VectorI4 HitWithVectorReturn()
			{
				_hitcount++;
				return VectorI4ReturnValue;
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
			Action<PpeClass> del = delegate(PpeClass obj) { obj.Hit(new VectorI4(10, 11, 12, 13)); };

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(1, cc.Methods.Count);

			PpeClass inst = new PpeClass();
			SpeContext.UnitTestRunProgram(cc, inst);

			AreEqual(1, inst.Hitcount);
			AreEqual(new VectorI4(10, 11, 12, 13), inst.HitVectorI4);
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
			AreSame(inst.HitObject, s);
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

			try
			{
				SpeContext.UnitTestRunProgram(cc, inst);
					Fail();
			}
			catch (ArgumentException)
			{
				// Ok
			}
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
			Converter<PpeClass, VectorI4> del = delegate(PpeClass obj) { return obj.HitWithVectorReturn(); };

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(1, cc.Methods.Count);

			PpeClass inst = new PpeClass();
			inst.VectorI4ReturnValue = new VectorI4(100, 200, 300, 400);
			AreEqual(inst.VectorI4ReturnValue, (VectorI4) SpeContext.UnitTestRunProgram(cc, inst));
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

#endif