using System;
using System.Collections;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Metadata;

namespace CellDotNet
{
	internal class Class1
	{
		static public void Main(string[] args)
		{
			RunSpu();
//			TestBuildTree();
//			TestParseFunctionCall();
//			DoExtremelySimpleCodeGen();
		}

		delegate void RefArgumentDelegate(ref int i);
		delegate void NoArgumentDelegate();

		private static void DoExtremelySimpleCodeGen()
		{
			RefArgumentDelegate del = delegate(ref int i) { i = 0x1ffff; };
			MethodDefinition method = GetMethod(del);

			CompileInfo ci = new CompileInfo(method);
			new TreeDrawer().DrawMethod(ci, method);
			ILTreeSpuWriter writer = new ILTreeSpuWriter();
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
			BasicTestDelegate del = delegate()
			                        	{
			                        		int* i;
			                        		i = (int*)30000;
			                        		*i = 34;
			                        	};
			MethodDefinition method = GetMethod(del);
			CompileInfo ci = new CompileInfo(method);
			new TreeDrawer().DrawMethod(ci, method);
			ILTreeSpuWriter writer = new ILTreeSpuWriter();
			SpuInstructionWriter ilist = new SpuInstructionWriter();
			writer.GenerateCode(ci, ilist);
			ilist.WriteStop();

			Console.WriteLine();
			Console.WriteLine("Disassembly: ");
			Console.WriteLine(ilist.Disassemble());


			return;
			RegAlloc regalloc = new RegAlloc();
			List<SpuInstruction> asm = new List<SpuInstruction>(ilist.Instructions);
			regalloc.alloc(asm, 16);
			int[] bincode = SpuInstruction.emit(asm);

			
			SpeContext ctx = new SpeContext();
			ctx.LoadProgram(bincode);
//            Buffer.BlockCopy(myspucode, 0, ctx.LocalS myspycode.Length, );
			// copy code to spu...
			ctx.Run();

			int* i2;
			i2 = (int*) ctx.LocalStorage + 30000;
			if (*i2 != 34)
				Console.WriteLine("øv");
			else
				Console.WriteLine("Selvfølgelig :)");

		}

		private static void DisplayMetadataTokens(int i)
		{
			Action<int> del = DisplayMetadataTokens;
			MethodDefinition method = GetMethod(del);
			TypeReference tref = method.Parameters[0].ParameterType;

			AssemblyNameReference anref = (AssemblyNameReference) tref.Scope;
			AssemblyDefinition def = tref.Module.Assembly.Resolver.Resolve(anref);

//			Console.WriteLine("cecil method token: " + method.MetadataToken.ToUInt());
//			Console.WriteLine("reflection method token: " + del.Method.MetadataToken);
		}

		private static void TestBuildTree()
		{
			Converter<int, long> del =
				delegate(int i)
					{
						int j;

						char c1;
						char c2;

						j = 8 + (i * 5);
							c1 = (char)j;
							c2 = (char)(j + 1);
						j += c1 + c2;
//						DateTime[] arr= new DateTime[0];
						if (i > 5)
							j++;
						while (j < 0)
						{
							j--;
						}

						return j * 2;
					};
			MethodDefinition method = GetMethod(del);
			CompileInfo ci = new CompileInfo(method);


			TreeDrawer td=  new TreeDrawer();
			td.DrawMethod(ci, method);
		}

		public static MethodDefinition GetMethod(Delegate a)
		{
			AssemblyDefinition ass = AssemblyFactory.GetAssembly(a.Method.DeclaringType.Assembly.Location);
			MetadataToken token = new MetadataToken(a.Method.DeclaringType.MetadataToken);
			foreach (TypeDefinition type in ass.MainModule.Types)
			{
				if (type.MetadataToken != token)
					continue;
				foreach (MethodDefinition meth in type.Methods)
				{
					if (meth.MetadataToken == new MetadataToken(a.Method.MetadataToken))
						return meth;
				}
			}

			throw new ArgumentException("Can't find the type or method");
		}
	}
}
