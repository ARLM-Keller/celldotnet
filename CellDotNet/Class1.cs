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
			TestBuildTree();
//			DisplayMetadataTokens(2);
			return;

			Action<int> a = null;
			MethodDefinition method;

			method = GetMethod(a);
			method.Body.Simplify();
			Console.WriteLine("Variables: " + method.Body.Variables.Count);
			foreach (VariableDefinition var in method.Body.Variables)
			{
				Console.WriteLine(var.Name + ": " + var.VariableType.Name);
			}
			Console.WriteLine("Max stack: " + method.Body.MaxStack);
			Console.WriteLine();
			

			foreach (Instruction inst in method.Body.Instructions)
			{
				Console.Write("Inst {0:x2}: {1}", inst.Offset, inst.OpCode.Code);
				if (inst.Operand != null)
				{
					if (inst.Operand.GetType() == typeof (VariableDefinition))
					{
						VariableDefinition var = (VariableDefinition) inst.Operand;
						Console.Write("; var type: {0} ({1})", var.VariableType.Name, var.Name);
					}
					else if (inst.OpCode.FlowControl == FlowControl.Branch || inst.OpCode.FlowControl == FlowControl.Cond_Branch)
					{
						if (inst.OpCode == OpCodes.Switch)
						{
							foreach (Instruction target in (IEnumerable) inst.Operand)
							{
								Console.Write("; target: {0:x2}", target.Offset);
							}
						}
						else
						{
							Instruction target = (Instruction) inst.Operand;
							Console.Write("; target: {0:x2}", target.Offset);
						}
					}
					else if (inst.OpCode.FlowControl == FlowControl.Call)
					{
						MethodReference cmet = (MethodReference) inst.Operand;
						Console.Write("; target: " + cmet);
					}
					else
					{
						Console.Write("; operand: {0} ({1})", inst.Operand, inst.Operand.GetType().Name);
					}	
				}

				Console.WriteLine();
			}
		}

		private static void DisplayMetadataTokens(int i)
		{
			Action<int> del = DisplayMetadataTokens;
			MethodDefinition method = GetMethod(del);


			Console.WriteLine("cecil method token: " + method.MetadataToken.ToUInt());
			Console.WriteLine("reflection method token: " + del.Method.MetadataToken);
		}

		private static void TestBuildTree()
		{
			Converter<int, long> del =
				delegate(int i)
					{
						int j = 8 + (i*5);
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
			td.DrawMethod(ci, method, td);
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
			if (inst.CilType != CilType.None)
				Console.Write(" " + inst.CilType);
			else 
				Console.Write(" -");
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

		public void DrawMethod(CompileInfo ci, MethodDefinition method, TreeDrawer td)
		{
			_branchTargets = new Set<int>();
			FindBranchTargets(ci, method);

			foreach (BasicBlock block in ci.Blocks)
			{
				Console.WriteLine(" - Basic Block:");
				td.DrawTree(method, block);
			}
		}
	}
}
