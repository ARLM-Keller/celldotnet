using System;
using System.Collections.Generic;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Metadata;

namespace CellDotNet
{
	internal class Class1
	{
		static public void Main(string[] args)
		{
//			RunSpu();
//			TypeExperimenalStuff(3);
			new CompileInfoTest().TestBuildTree();
//			DoExtremelySimpleParameterCodeGen();
		}

		delegate void RefArgumentDelegate(ref int i);

		private static void DoExtremelySimpleParameterCodeGen()
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


			RegAlloc regalloc = new RegAlloc();
			List<SpuInstruction> asm = new List<SpuInstruction>(ilist.Instructions);
			regalloc.alloc(asm, 16);
			int[] bincode = SpuInstruction.emit(asm);
			return;

			
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

		unsafe private static void RefIntMethod(int *i) {}

		private static void TypeExperimenalStuff(int i)
		{
			MethodInfo refintmethod = Array.Find(typeof(Class1).GetMethods(BindingFlags.NonPublic | BindingFlags.Static), delegate(MethodInfo mi2) { return mi2.Name == "RefIntMethod"; });
			MethodDefinition methodrefint = GetMethod(refintmethod);
			TypeReference trefint = methodrefint.Parameters[0].ParameterType;

			Action<int> delint = delegate { };
			MethodInfo intmethod = delint.Method;
			MethodDefinition methodint = GetMethod(intmethod);
			TypeReference tint = methodint.Parameters[0].ParameterType;

			CompileInfo.TypeCache tc = new CompileInfo.TypeCache();
			TypeDescription td = tc.GetTypeDescription(tint);
			TypeDescription td2 = tc.GetTypeDescription(trefint);

			AssemblyNameReference anref = (AssemblyNameReference) tint.Scope;
			AssemblyDefinition def = tint.Module.Assembly.Resolver.Resolve(anref);

//			Console.WriteLine("cecil method token: " + method.MetadataToken.ToUInt());
//			Console.WriteLine("reflection method token: " + del.Method.MetadataToken);
		}

		public static MethodDefinition GetMethod(Delegate a)
		{
			MethodInfo m = a.Method;
			return GetMethod(m);
		}

		private static MethodDefinition GetMethod(MethodInfo m)
		{
			AssemblyDefinition ass = AssemblyFactory.GetAssembly(m.DeclaringType.Assembly.Location);
			return (MethodDefinition) ass.MainModule.LookupByToken(new MetadataToken(m.MetadataToken));
		}
	}
}
