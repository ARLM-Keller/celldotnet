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
				Console.WriteLine("�v");
			else
				Console.WriteLine("Selvf�lgelig :)");

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

		private static MethodDefinition GetMethod(Delegate a)
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

	class TreeDrawer
	{
		Set<int> _branchTargets;

		void DrawTree(MethodDefinition method, TreeInstruction inst, int level)
		{
			Console.Write(new string(' ', level * 2));

			// Branch coloring.
			bool isBranch = false;
			bool isTarget = false;
			if (inst.Opcode.FlowControl == FlowControl.Branch || inst.Opcode.FlowControl == FlowControl.Cond_Branch)
				isBranch = true;
			if (_branchTargets.Contains(inst.Offset))
				isTarget = true;

			if (isBranch && isTarget)
				Console.ForegroundColor = ConsoleColor.Cyan;
			else if (isBranch)
				Console.ForegroundColor = ConsoleColor.DarkCyan;
			else if (isTarget)
				Console.ForegroundColor = ConsoleColor.Green;

			Console.Write(inst.Offset.ToString("x4") + " " + inst.Opcode.Name);

			if (inst.Operand != null)
			{
				if (inst.Operand is TreeInstruction)
					Console.Write(" " + ((TreeInstruction)inst.Operand).Offset.ToString("x4"));
				else if (inst.Operand is ParameterReference)
					Console.Write(" {0} ({1})", ((ParameterReference)inst.Operand).Name, ((ParameterReference)inst.Operand).ParameterType.Name);
				else if (inst.Operand is VariableReference)
					Console.Write(" {0} ({1})", ((VariableReference)inst.Operand).Name, ((VariableReference)inst.Operand).VariableType.Name);
				else if (inst.Operand is FieldReference)
					Console.Write(" {0} ({1})", ((FieldReference)inst.Operand).Name, ((FieldReference)inst.Operand).FieldType.Name);
				else
					Console.Write(" " + inst.Operand);
			}
			if (inst.StackType != StackTypeDescription.None)
				Console.Write("   " + inst.StackType.CliType + (inst.StackType.IsByRef ? "&" : ""));
			else 
				Console.Write("   -");
			Console.WriteLine();

			Console.ResetColor();

			if (inst.Left != null)
				DrawTree(method, inst.Left, level + 1);
			if (inst.Right != null)
				DrawTree(method, inst.Right, level + 1);
		}

		public void DrawTree(MethodDefinition method, BasicBlock block)
		{
			foreach (TreeInstruction root in block.Roots)
			{
				DrawTree(method, root, 0);
			}
		}

		private void AddBranchTargets(TreeInstruction inst)
		{
			if (inst.Opcode.FlowControl == FlowControl.Branch || inst.Opcode.FlowControl == FlowControl.Cond_Branch)
			{
				_branchTargets.Add(((TreeInstruction)inst.Operand).Offset);
			}
			if (inst.Left != null)
				AddBranchTargets(inst.Left);
			if (inst.Right!= null)
				AddBranchTargets(inst.Right);
		}

		private void FindBranchTargets(CompileInfo ci, MethodDefinition method)
		{
			foreach (BasicBlock block in ci.Blocks)
			{
				foreach (TreeInstruction inst in block.Roots)
				{
					AddBranchTargets(inst);
				}
			}
		}

		public void DrawMethod(CompileInfo ci, MethodDefinition method)
		{
			_branchTargets = new Set<int>();
			FindBranchTargets(ci, method);

			foreach (BasicBlock block in ci.Blocks)
			{
				Console.WriteLine(" - Basic Block:");
				DrawTree(method, block);
			}
		}
	}
}
