using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace CellDotNet
{
	internal class Class1
	{
		static public void Main(string[] args)
		{
			new SpeContextTest().TestGetPutInt32();
//			new SpeContextTest().TestFirstCellProgram();
//			new SpuInitializerTest().TestInitialization();


			
//			GenericExperiment();
//			return;
//			RunSpu();
//			TypeExperimenalStuff(3);
//			new MethodCompilerTest().TestBuildTree();
//			DoExtremelySimpleParameterCodeGen();
			System.Console.WriteLine("Program done.");
		}

		private static void GenericExperiment()
		{
			BasicTestDelegate del  = delegate()
			                         	{
			                         		List<int> list = new List<int>();
											Array.ForEach((int[]) null, null);
			                         	};
			MethodBase method = del.Method;
		}

		delegate void RefArgumentDelegate(ref int i);

		private static void DoExtremelySimpleParameterCodeGen()
		{
			RefArgumentDelegate del = delegate(ref int i) { i = 0x1ffff; };
			MethodBase method = del.Method;

			MethodCompiler ci = new MethodCompiler(method);
			ci.PerformProcessing(MethodCompileState.S2TreeConstructionDone);
			new TreeDrawer().DrawMethod(ci, method);
			RecursiveInstructionSelector writer = new RecursiveInstructionSelector();
			SpuInstructionWriter ilist = new SpuInstructionWriter();
			writer.GenerateCode(ci, ilist);
			Console.WriteLine();
			Console.WriteLine("Disassembly: ");
			Console.WriteLine(ilist.Disassemble());
		}

		/// <summary>
		/// A delegate that takes no arguments and has void return type.
		/// </summary>
		private delegate void BasicTestDelegate();


		private unsafe static void RunSpu()
		{
//			SpeContext ctxt = new SpeContext();
//			ctxt.LoadProgram(new int[] { 13 });
//	
//			int[] lsa = ctxt.GetCopyOffLocalStorage();
//
//			if(lsa[0] != 13)
//				Console.WriteLine("øv");
//			else
//				Console.WriteLine("fint!!!");
//		
//
//			return;

			
			BasicTestDelegate del = delegate()
										{
											int* i;
											i = (int*)0x40;
											*i = 34;
										};


			/*			BasicTestDelegate del = delegate()
													{
														int a;
														int i = 42;
														if (i >= 9)
															a = 1;
														else
															a = 2;
													};
			*/
			MethodBase method = del.Method;
			MethodCompiler ci = new MethodCompiler(method);
			ci.PerformProcessing(MethodCompileState.S2TreeConstructionDone);

			new TreeDrawer().DrawMethod(ci, method);

			RecursiveInstructionSelector writer = new RecursiveInstructionSelector();
			SpuInstructionWriter ilist = new SpuInstructionWriter();
			writer.GenerateCode(ci, ilist);
			ilist.WriteStop();

			Console.WriteLine();
			Console.WriteLine("Disassembly: ");
			Console.WriteLine(ilist.Disassemble());
			

			SimpleRegAlloc regalloc = new SimpleRegAlloc();
			List<SpuInstruction> asm = ilist.GetAsList();
			regalloc.alloc(asm, 16);

			int[] bincode = SpuInstruction.emit(asm);
			int[] testbincode = new int[10];

			testbincode[0] = 0;
			testbincode[1] = 0;
			testbincode[2] = 0;
			testbincode[3] = 0;
			testbincode[4] = 0;
			testbincode[5] = 0;
			testbincode[6] = 0;
			testbincode[7] = 0;
			testbincode[8] = 0;
			testbincode[9] = 0;

//			testbincode[0] = Convert.ToInt32("010000011", 2) << (32 - 9) | (34 << 7) | 8; //ilh i rt
//			testbincode[1] = Convert.ToInt32("010000010", 2) << (32 - 9) | (0 << 7) | 8; //ilhu i rt
//			testbincode[2] = Convert.ToInt32("001000001", 2) << (32 - 9) |  (7 << 7) | 8; //stqa i rt, addr = i 
//			testbincode[3] = 34;

//			testbincode[0] = 34;

//			testbincode[0] = Convert.ToInt32("010000001", 2) << (32 - 9) | (34 << 7) | 8; //il i rt
//			testbincode[1] = Convert.ToInt32("001000001", 2) << (32 - 9) |  (15 << 7) | 8; //stqa i rt, addr = i 
//			testbincode[2] = 34;

			testbincode[0] = Convert.ToInt32("010000001", 2) << (32 - 9) | (34 << 7) | 8; //il i rt
			testbincode[1] = Convert.ToInt32("001100101", 2) << (32 - 9) | (0xf000 << 7) | 9; //fsmbi i rt
			testbincode[2] = Convert.ToInt32("00011000001", 2) << (32 - 11) | (8 << 14) | (9 << 7) | 10; //and i rt
			testbincode[3] = Convert.ToInt32("001000001", 2) << (32 - 9) | (8 << 7) | 10; //stqa i rt, addr = i 
			testbincode[4] = 34;

			SpeContext ctx = new SpeContext();

//			Console.WriteLine("LocalStorageSize = {0}", ctx.LocalStorageSize);

//			int[] ls1 = ctx.GetCopyOffLocalStorage();

			ctx.LoadProgram(bincode);
				
//			int[] ls2 = ctx.GetCopyOffLocalStorage();

			//            Buffer.BlockCopy(myspucode, 0, ctx.LocalS myspycode.Length, );
			// copy code to spu...
			int r = ctx.Run();
//			Console.WriteLine("ctx.Run(): {0}", r);

//			int[] ls3 = ctx.GetCopyOffLocalStorage();

//			Console.WriteLine("Programe code:");
//			for (int i = 0; i < bincode.Length; i++ )
//				Console.Write("{0,8:X} ", bincode[i]);
//			Console.WriteLine();


//			Console.WriteLine("ls1 == ls2");
//			WriteDiff(ls1, ls2);

//			Console.WriteLine("ls2 == ls3");
//			WriteDiff(ls2, ls3);

//			Console.WriteLine("Finde value:");

//			FindeValue(ls1, 255);
//			FindeValue(ls2, 255);
//			FindeValue(ls3, 255);

//			Console.WriteLine("DiffCount ls1 ls2: {0}", CountDiff(ls1, ls2));
//			Console.WriteLine("DiffCount ls2 ls3: {0}", CountDiff(ls2, ls3));
//			Console.WriteLine("DiffCount ls1 ls3: {0}", CountDiff(ls1, ls3));

			int[] ls = ctx.GetCopyOffLocalStorage();


//			int* i2;
//			i2 = (int*)ctx.LocalStorage + 0xff0;

//			for (int i = 0; i < 0x80/4; i++)
//			{
//				Console.WriteLine("ls[i]: {0,8:x}", ls[i]);
//				Console.WriteLine("  {0,2:x} {1,2:x} {2,2:x} {3,2:x}", (ls[i] >> 24) & 0xff, (ls[i] >> 16) & 0xff, (ls[i] >> 8) & 0xff, ls[i] & 0xff);
//				if (ls[i] == 34)
//					Console.WriteLine("i: {0}", i);
//			}

			if (ls[0x40/4] != 34)
			{
				Console.WriteLine("øv");
				Console.WriteLine("Value: {0}", ls[0xff0/4]);
			}
			else
				Console.WriteLine("Selvfølgelig :)");

			return;

		}

		private static int CountDiff(int[] a1, int[] a2)
		{
			int count = 0;
			int minsize = (a1.Length < a2.Length) ? a1.Length : a2.Length;
			for (int i = 0; i < minsize; i++)
				if (a1[i] != a2[i])
					count++;
			return count;
		}

		private static void FindeValue(int[] a, int value)
		{
			int find = 0;

			for(int i = 0; i<a.Length; i++)
			{
				if(a[i] == value)
				{
					Console.Write("{0} ", i);
					find++;
				}
			}

			Console.WriteLine("Value founde {0} places.", find);
			
		}

		private static void WriteDiff(int[] a1, int[] a2)
		{
			System.Text.StringBuilder line1 = null;
			System.Text.StringBuilder line2 = null;
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

		unsafe private static void RefIntMethod(int* i) { }
	}
}
