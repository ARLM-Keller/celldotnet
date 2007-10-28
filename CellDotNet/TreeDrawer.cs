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


		void DrawTree(TreeInstruction inst, int level)
		{
			Output.Write(new string(' ', level * 2));

			// Branch coloring.
/*
			bool isBranch = false;
			bool isTarget = false;
			if (inst.OpCode.FlowControl == FlowControl.Branch || inst.OpCode.FlowControl == FlowControl.Cond_Branch)
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
			Output.Write(((uint)inst.Offset & 0xffff).ToString("x4") + " " + inst.Opcode.Name);

			if (inst.Operand != null)
			{
				if (inst.Operand is IRBasicBlock)
					Output.Write(" block" + ((IRBasicBlock)inst.Operand).BlockNumber);
				else if (inst.Operand is MethodParameter)
					Output.Write(" {0} ({1})", ((MethodParameter)inst.Operand).Name, ((MethodParameter)inst.Operand).ReflectionType.Name);
				else if (inst.Operand is MethodVariable)
				{
					string typename;
					try
					{
						typename = ((MethodVariable)inst.Operand).ReflectionType.Name;
					}
					catch (InvalidOperationException)
					{
						typename = "(unknown type)";
					}
					Output.Write(" {0} ({1})", inst.Operand, typename);
				}
				else if (inst.Operand is FieldInfo)
					Output.Write(" {0} ({1})", ((FieldInfo)inst.Operand).Name, ((FieldInfo)inst.Operand).FieldType.Name);
				else if (inst.Operand is MethodCompiler)
					Output.Write(" " + ((MethodCompiler)inst.Operand).Name);
				else if (inst.Operand is int && inst.Opcode.FlowControl == FlowControl.Branch || inst.Opcode.FlowControl == FlowControl.Cond_Branch)
				{
					Output.Write(" " + ((int)inst.Operand).ToString("X4"));
				}
				else
					Output.Write(" " + inst.Operand);
			}
			if (inst.StackType != StackTypeDescription.None)
				Output.Write("   " + inst.StackType); // Fix &
			else
				Output.Write("   -");

			Output.WriteLine();

//			Console.ResetColor(); //Denne metode fejler på PS3

			if (inst.GetType() == typeof(TreeInstruction))
			{
				if (inst.Left != null)
				DrawTree(inst.Left, level + 1);
				if (inst.Right != null)
				{
					if (inst.Left == null)
						Output.Write(new string(' ', (level + 1) * 2) + "!! Only right side is non-null. -----------------");
					DrawTree(inst.Right, level + 1);
				}
			}
			else if (inst is MethodCallInstruction)
			{
				MethodCallInstruction mci = (MethodCallInstruction)inst;
				foreach (TreeInstruction param in mci.Parameters)
				{
					DrawTree(param, level + 1);
				}
			}
		}

		private void DrawTree(IRBasicBlock block)
		{
			if (Output == null)
				Output = Console.Out;
			foreach (TreeInstruction root in block.Roots)
			{
				DrawTree(root, 0);
			}
		}

		public string DrawSubTree(TreeInstruction inst)
		{
			StringWriter sw = new StringWriter();
			if (Output == null)
				Output = sw;
			DrawTree(inst, 0);
			return sw.GetStringBuilder().ToString();
		}

		private void AddBranchTargets(TreeInstruction inst)
		{
			throw new NotImplementedException();
//			// Do we still use this method?
//			if (inst.Opcode.FlowControl == FlowControl.Branch || inst.Opcode.FlowControl == FlowControl.Cond_Branch)
//			{
//				if (inst.Operand is IRBasicBlock)
//					_branchTargets.Add(((IRBasicBlock)inst.Operand).Offset);
//				else
//					_branchTargets.Add((int)inst.Operand);
//			}
//			if (inst.Left != null)
//				AddBranchTargets(inst.Left);
//			if (inst.Right != null)
//				AddBranchTargets(inst.Right);
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

		public void DrawMethod(List<IRBasicBlock> blocks)
		{
			Output = new StringWriter();

			try
			{
				DrawMethod(blocks, Output);
			}
			finally
			{
				Console.Write(((StringWriter)Output).GetStringBuilder().ToString());
			}
		}

		public static String DrawMethodToString(IEnumerable<IRBasicBlock> blocks)
		{
			StringWriter writer = new StringWriter();

			new TreeDrawer().DrawMethod(blocks, writer);

			return writer.GetStringBuilder().ToString();
		}

		public void DrawMethod(MethodCompiler ci)
		{
			DrawMethod(ci.Blocks);
		}

		public static string GetMethodDrawing(IRBasicBlock bb)
		{
			StringWriter sw = new StringWriter();
			TreeDrawer d = new TreeDrawer();

			d.Output = sw;
			d.DrawTree(bb);

			return sw.GetStringBuilder().ToString();
		}

		public static string GetMethodDrawing(MethodCompiler ci)
		{
			StringWriter sw = new StringWriter();
			TreeDrawer d = new TreeDrawer();

			d.DrawMethod(ci.MethodBase, sw, ci.Blocks);

			return sw.GetStringBuilder().ToString();
		}

		public void DrawMethod(MethodCompiler ci, MethodBase method, TextWriter output)
		{
			DrawMethod(ci);
			
		}

		public void DrawMethod(MethodBase method, TextWriter output, List<IRBasicBlock> blocks)
		{
			DrawMethod(blocks, output);
		}

		public void DrawMethod(IEnumerable<IRBasicBlock> blocks, TextWriter output)
		{
			Output = output;

			_branchTargets = new Set<int>();

			foreach (IRBasicBlock block in blocks)
			{
				Output.WriteLine(" - Basic block {0}:", block.BlockNumber);
				DrawTree(block);
			}
		}

		public void DrawMethods(CompileContext cc)
		{
			foreach (MethodCompiler mc in cc.Methods)
			{
				Console.WriteLine();
				Console.WriteLine(mc.Name);

				DrawMethod(mc);
			}
		}
	}
}
