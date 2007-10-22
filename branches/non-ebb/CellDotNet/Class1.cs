using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

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

		private unsafe static void RunRasmus()
		{
//			new ILOpCodeExecutionTest().Test_Beq();
			new ObjectModelTest().TestStruct_ReturnByrefArgument();
		}
	}
}
