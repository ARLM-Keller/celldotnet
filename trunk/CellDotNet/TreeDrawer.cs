using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;

namespace CellDotNet
{
	/// <summary>
	/// Used for visualising a parsed method instruction tree.
	/// </summary>
	class TreeDrawer
	{
		Set<int> _branchTargets;

		private TextWriter _output;

		void DrawTree(MethodBase method, TreeInstruction inst, int level)
		{
			_output.Write(new string(' ', level * 2));

			// Branch coloring.
/*
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
*/
			_output.Write(inst.Offset.ToString("x4") + " " + inst.Opcode.Name);

			if (inst.Operand != null)
			{
				if (inst.Operand is TreeInstruction)
					_output.Write(" " + ((TreeInstruction)inst.Operand).Offset.ToString("x4"));
				else if (inst.Operand is MethodParameter)
					_output.Write(" {0} ({1})", ((MethodParameter)inst.Operand).Name, ((MethodParameter)inst.Operand).Type.Name);
				else if (inst.Operand is MethodVariable)
					_output.Write(" {0} ({1})", inst.Operand, ((MethodVariable)inst.Operand).Type.Name);
				else if (inst.Operand is FieldInfo)
					_output.Write(" {0} ({1})", ((FieldInfo)inst.Operand).Name, ((FieldInfo)inst.Operand).FieldType.Name);
				else
					_output.Write(" " + inst.Operand);
			}
			if (inst.StackType != StackTypeDescription.None)
				_output.Write("   " + inst.StackType.CliType + (inst.StackType.IsByRef ? "&" : "")); // Fix &
			else
				_output.Write("   -");

			_output.WriteLine();

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

		public void DrawTree(MethodBase method, BasicBlock block)
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

		private void FindBranchTargets(MethodCompiler ci, MethodBase method)
		{
			foreach (BasicBlock block in ci.Blocks)
			{
				foreach (TreeInstruction inst in block.Roots)
				{
					AddBranchTargets(inst);
				}
			}
		}

		public void DrawMethod(MethodCompiler ci, MethodBase method)
		{
			_output = new StringWriter();

			try
			{
				_branchTargets = new Set<int>();
				FindBranchTargets(ci, method);

				foreach (BasicBlock block in ci.Blocks)
				{
					_output.WriteLine(" - Basic Block:");
					DrawTree(method, block);
				}
			}
			finally
			{
				Console.Write(((StringWriter)_output).GetStringBuilder().ToString());				
			}
		}
	}
}
