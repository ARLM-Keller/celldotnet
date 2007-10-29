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

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using CellDotNet.Spe;

namespace CellDotNet
{
	internal class Class1
	{
		static public void Main(string[] args)
		{
			if (Environment.UserName == "kmhansen")
			{
				Console.WriteLine("Running RunKlaus...");
				RunKlaus();
			}
			else
			{
				Console.WriteLine("Running RunRasmus...");
				RunRasmus();
			}
		}

		public static int TEST()
		{
			int[] arr = new int[10];

			return arr.Length;
		}

		private static void RunKlaus()
		{
//			new ILOpCodeExecutionTest().TestRefArgumentTest();

//			new SpeContextTest().TestRecursion_StackOverflow();

//			new SpeContextTest().TestRecursion_StackOverflow_Debug();

//			new MfcTest().TestDma_GetIntArray();

//			new ILOpCodeExecutionTest().Test_Call();

//			new SpeContextTest().TestFirstCellProgram();

//			new SimpleProgramsTest().TestRecursiveSummation_Int();

//			new ILOpCodeExecutionTest().Test_Ceq_I4();

//			new SpeContextTest().TestOutOfMemory();

//			new SimpleProgramsTest().TestRecursiveSummation_Int();

//			new VectorTypeTest().TestVectorIntGetPutElement();

//			new VectorTypeTest().TestVectorIntArray();

//			new MfcTest().TestDma_PutIntArray();

//			new ObjectModelTest().TestInstanceMethod_FieldAccess();

//			new IRTreeBuilderTest().TestParseBranchesDEBUG();

//			new IRTreeBuilderTest().TestParseBranches1();

//			new ILOpCodeExecutionTest().Test_Dub();

//			new IRTreeBuilderTest().TestTypeDerivingForVariableStack();

//			new SimpleProgramsTest().TestConditionalExpresion();

//			new ILOpCodeExecutionTest().Test_Br();

//			Converter<int, uint> fun =
//				delegate(int input)
//					{
//						return CellDotNet.SpuMath.Div_Un	(14, 7);
//					};
//
//			CompileContext cc = new CompileContext(fun.Method);
//
//			cc.PerformProcessing(CompileContextState.S8Complete);
//
//			object result = SpeContext.UnitTestRunProgram(cc, 17);

//			new ILOpCodeExecutionTest().Test_Div();

//			new ILOpCodeExecutionTest().Test_Beq();

//			new SimpleProgramsTest().TestDivisionSigned();

//			new ObjectModelTest().TestArray_Int_4();

//			new ObjectModelTest().TestArray_Double_1();

//			new ILOpCodeExecutionTest().Test_Ldc_R8();

//			new ObjectModelTest().TestStruct_BigField_4();

//			new ILOpCodeExecutionTest().Test_Div_Un();

//			new VectorTypeTest().TestVectorInt_Div();

//			new SimpleProgramsTest().Test_FloatCompare();

			Test_InvaliInstruction();

			Console.WriteLine("Running RunKlaus done.");
		}

		private static void Test_InvaliInstruction()
		{
			SpeContext ct = new SpeContext();

//			1010000

			unchecked
			{
				ct.LoadProgram(new int[] {(int) 0xa0000000, 0x0});
				ct.Run();
			}
		}

		private static void WriteDiff(int[] a1, int[] a2)
		{
			StringBuilder line1 = null;
			StringBuilder line2 = null;
			int linewrites = 0;
			int maxlinewrits = 16;

			int minsize = (a1.Length < a2.Length) ? a1.Length : a2.Length;
			int maxwrites = maxlinewrits*20;
			int writs = 0;
			bool writing = false;

			for(int i = 0; i < minsize && writs <= maxwrites; i++)
			{
				if (a1[i] != a2[i])
				{
					if (!writing)
					{
						Console.WriteLine("Diff index = {0}", i);
						line1 = new StringBuilder();
						line2 = new StringBuilder();
						linewrites = 0;
						writing = true;
					} else if (linewrites >= maxlinewrits)
					{
						Console.WriteLine(line1.ToString());
						Console.WriteLine(line2.ToString());
						Console.WriteLine();

						line1 = new StringBuilder();
						line2 = new StringBuilder();

						linewrites = 0;
					}

					line1.AppendFormat("{0,8:X} ", a1[i]);
					line2.AppendFormat("{0,8:X} ", a2[i]);
					writs++;
					linewrites++;
				}
				else if(writing)
				{
					Console.WriteLine(line1.ToString());
					Console.WriteLine(line2.ToString());
					Console.WriteLine();

					line1 = new StringBuilder();
					line2 = new StringBuilder();

					linewrites = 0;

					writing = false;
				}
			}

			Console.WriteLine();
		}

		static T Allocate<T>()
		{
			return default(T);
		}

		static void Allocate<T>(out T[] rv, int elementcount) where T : struct
		{
			rv = default(T[]);
		}

		static void Allocate<T>(out T[,] rv, int elementcount1, int elementcount2) where T : struct
		{
			rv = default(T[,]);
		}

		private unsafe static void RunRasmus()
		{
			Allocate<int[,]>();

//			string s;
//			Allocate(out s);

			int[] arr;
			Allocate(out arr, 10);

			int[,] arr2d;
			Allocate(out arr2d, 40, 50);

//			new ILOpCodeExecutionTest().Test_Beq();
//			new ObjectModelTest().TestStruct_ReturnByrefArgument();

		}
	}
}
