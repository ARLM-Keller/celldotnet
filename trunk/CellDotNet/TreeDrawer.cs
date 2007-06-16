using System;
using System.Collections.Generic;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace CellDotNet
{
	/// <summary>
	/// Used for visualising a parsed method instruction tree.
	/// </summary>
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
/*
			if (isBranch && isTarget)
				Console.ForegroundColor = ConsoleColor.Cyan;
			else if (isBranch)
				Console.ForegroundColor = ConsoleColor.DarkCyan;
			else if (isTarget)
				Console.ForegroundColor = ConsoleColor.Green;
*/
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
				Console.Write("   " + inst.StackType.CliType + (inst.StackType.IsByRef ? "&" : "")); // Fix &
			else
				Console.Write("   -");

			Console.WriteLine();

//			Console.ResetColor(); //Denne metode fejler på PS3

			if (inst.GetType() == typeof(TreeInstruction))
			{
				if (inst.Left != null)
				DrawTree(method, inst.Left, level + 1);
				if (inst.Right != null)
				DrawTree(method, inst.Right, level + 1);
			}
			else if (inst is MethodCallInstruction)
			{
				MethodCallInstruction mci = (MethodCallInstruction)inst;
				foreach (TreeInstruction param in mci.Parameters)
				{
					DrawTree(method, param, level + 1);
				}
			}
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
			if (inst.Right != null)
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
