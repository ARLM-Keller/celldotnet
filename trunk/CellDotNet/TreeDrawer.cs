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

		public TextWriter Output
		{
			get { return _output; }
			set { _output = value; }
		}


		void DrawTree(MethodBase method, TreeInstruction inst, int level)
		{
			Output.Write(new string(' ', level * 2));

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
			Output.Write(inst.Offset.ToString("x4") + " " + inst.Opcode.Name);

			if (inst.Operand != null)
			{
				if (inst.Operand is IRBasicBlock)
					Output.Write(" " + ((IRBasicBlock)inst.Operand).Offset.ToString("x4"));
				else if (inst.Operand is MethodParameter)
					Output.Write(" {0} ({1})", ((MethodParameter)inst.Operand).Name, ((MethodParameter)inst.Operand).Type.Name);
				else if (inst.Operand is MethodVariable)
					Output.Write(" {0} ({1})", inst.Operand, ((MethodVariable)inst.Operand).Type.Name);
				else if (inst.Operand is FieldInfo)
					Output.Write(" {0} ({1})", ((FieldInfo)inst.Operand).Name, ((FieldInfo)inst.Operand).FieldType.Name);
				else if (inst.Operand is int && inst.Opcode.FlowControl == FlowControl.Branch || inst.Opcode.FlowControl == FlowControl.Cond_Branch)
				{
					// Normally this should happen for branch instructions, but we want to handle it anyway...
					Output.Write(" " + ((int)inst.Operand).ToString("X4"));
				}
				else					
					Output.Write(" " + inst.Operand);
			}
			if (inst.StackType != StackTypeDescription.None)
				Output.Write("   " + inst.StackType.CliType + (inst.StackType.IsByRef ? "&" : "")); // Fix &
			else
				Output.Write("   -");

			Output.WriteLine();

//			Console.ResetColor(); //Denne metode fejler p� PS3

			if (inst.GetType() == typeof(TreeInstruction))
			{
				if (inst.Left != null)
				DrawTree(method, inst.Left, level + 1);
				if (inst.Right != null)
				{
					if (inst.Left == null)
						Output.Write(new string(' ', (level + 1) * 2) + "!! Only right side is non-null. -----------------");
					DrawTree(method, inst.Right, level + 1);
				}
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

		public void DrawTree(MethodBase method, IRBasicBlock block)
		{
			if (Output == null)
				Output = Console.Out;
			foreach (TreeInstruction root in block.Roots)
			{
				DrawTree(method, root, 0);
			}
		}

		private void AddBranchTargets(TreeInstruction inst)
		{
			// Do we still use this method?
			if (inst.Opcode.FlowControl == FlowControl.Branch || inst.Opcode.FlowControl == FlowControl.Cond_Branch)
			{
				if (inst.Operand is IRBasicBlock)
					_branchTargets.Add(((IRBasicBlock)inst.Operand).Offset);
				else
					_branchTargets.Add((int)inst.Operand);
			}
			if (inst.Left != null)
				AddBranchTargets(inst.Left);
			if (inst.Right != null)
				AddBranchTargets(inst.Right);
		}

		private void FindBranchTargets(MethodCompiler ci, MethodBase method)
		{
			foreach (IRBasicBlock block in ci.Blocks)
			{
				foreach (TreeInstruction inst in block.Roots)
				{
					AddBranchTargets(inst);
				}
			}
		}

		public void DrawMethod(MethodCompiler ci, MethodBase method)
		{
			Output = new StringWriter();

			try
			{
				DrawMethod(ci, method, Output);
			}
			finally
			{
				Console.Write(((StringWriter)Output).GetStringBuilder().ToString());				
			}
		}

		public string GetMethodDrawing(MethodCompiler ci)
		{
			StringWriter sw = new StringWriter();

			DrawMethod(ci, ci.MethodBase, sw);

			return sw.GetStringBuilder().ToString();
		}

		public void DrawMethod(MethodCompiler ci, MethodBase method, TextWriter output)
		{
			Output = output;

			_branchTargets = new Set<int>();
			FindBranchTargets(ci, method);

			foreach (IRBasicBlock block in ci.Blocks)
			{
				Output.WriteLine(" - Basic Block:");
				DrawTree(method, block);
			}
		}
	}
}