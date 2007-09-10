using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace CellDotNet
{
	/// <summary>
	/// A simple partial evaluator. Primarily used to cut out code that should never run on an spu.
	/// </summary>
	internal class PartialEvaluator
	{
		private Dictionary<MethodInfo, int> _fixedMethods;
		private Dictionary<MethodVariable, int> _knownVariables;

		public PartialEvaluator(Dictionary<MethodInfo, int> methods)
		{
			Utilities.AssertArgumentNotNull(methods, "methods");
			_fixedMethods = methods;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="mc"></param>
		/// A set of methods and their fixed values.
		/// </param>
		public void Evaluate(MethodCompiler mc)
		{

			// Replace methods with constants.
			_knownVariables = new Dictionary<MethodVariable, int>();
			foreach (IRBasicBlock bb in mc.Blocks)
			{
				foreach (TreeInstruction root in bb.Roots)
				{
					Evaluate(root);
				}

				// Since we haven't got a real dataflow graph, these known values are
				// only valid with a block.
				_knownVariables.Clear();
			}

			// Shortcut conditional branches with fixed outcomes.
			foreach (IRBasicBlock bb in mc.Blocks)
			{
				for (int rootnum = 0; rootnum < bb.Roots.Count; rootnum++)
				{
					TreeInstruction root = bb.Roots[rootnum];

					if (root.Opcode.FlowControl != FlowControl.Cond_Branch ||
						root.Left.Opcode != IROpCodes.Ldc_I4)
						continue;

					bool branchIsTaken;
					if (root.Opcode.IRCode == IRCode.Brtrue)
						branchIsTaken = root.Left.OperandAsInt32 != 0;
					else if (root.Opcode.IRCode == IRCode.Brfalse)
						branchIsTaken = root.Left.OperandAsInt32 == 0;
					else
					{
						// beq, ble etc.
						continue;
					}

					if (branchIsTaken)
					{
						TreeInstruction newroot = new TreeInstruction(IROpCodes.Br);
						newroot.Operand = root.OperandAsBasicBlock;
						bb.Roots[rootnum] = newroot;
						bb.Roots.RemoveRange(rootnum, bb.Roots.Count - rootnum);
					}
					else
					{
						bb.Roots.RemoveAt(rootnum);
						rootnum--;
					}
				}
			}

			EliminateUnusedCode(mc);
		}

		private static void EliminateUnusedCode(MethodCompiler mc)
		{
			Set<IRBasicBlock> reachedBlocks = new Set<IRBasicBlock>(mc.Blocks.Count);

			bool foundFirstNonEmptyBlock = false;

			for (int blocknum = 0; blocknum < mc.Blocks.Count; blocknum++)
			{
				IRBasicBlock bb = mc.Blocks[blocknum];

				// The first block might not be branched to, but it is nevertheless reachable.
				if (!foundFirstNonEmptyBlock && bb.Roots.Count > 0)
				{
					reachedBlocks.Add(bb);
					foundFirstNonEmptyBlock = true;
				}

				// Collect branch targets and remove roots which are not reached.
				for (int rootnum = 0; rootnum < bb.Roots.Count; rootnum++)
				{
					TreeInstruction root = bb.Roots[rootnum];

					if (root.Opcode.FlowControl == FlowControl.Branch)
					{
						reachedBlocks.Add(root.OperandAsBasicBlock);
						if (rootnum < bb.Roots.Count - 1)
							bb.Roots.RemoveRange(rootnum + 1, bb.Roots.Count - rootnum);
					}
					if (root.Opcode.FlowControl == FlowControl.Cond_Branch)
					{
						reachedBlocks.Add(root.OperandAsBasicBlock);
					}
				}

				// Collect other used bbs.
				if (blocknum < mc.Blocks.Count - 1 && bb.Roots.Count > 0)
				{
					TreeInstruction lastInst = bb.Roots[bb.Roots.Count - 1];
					if (lastInst.Opcode.FlowControl != FlowControl.Branch &&
						lastInst.Opcode.FlowControl != FlowControl.Cond_Branch &&
						lastInst.Opcode.FlowControl != FlowControl.Return)
					{
						reachedBlocks.Add(mc.Blocks[blocknum + 1]);
					}
				}
			}

			// Remove blocks which are not reached.
			mc.Blocks.RemoveAll(delegate(IRBasicBlock obj) { return !reachedBlocks.Contains(obj); });
		}

		private TreeInstruction Evaluate(TreeInstruction inst)
		{
			// Start with the children.
			int paramidx = 0;
			foreach (TreeInstruction child in inst.GetChildInstructions())
			{
				TreeInstruction newinst = Evaluate(child);
				if (newinst != null)
					inst.ReplaceChild(paramidx, newinst);

				paramidx++;
			}

			if (inst.Opcode.IRCode == IRCode.Call)
			{
				int fixedValue;
				// When used on a method in a CompileContext the operand is a MethodCompiler;
				// when used on a standalone MethodCompiler the operand is a MethodBase.
				MethodInfo mi = inst.OperandAsMethod as MethodInfo ?? inst.OperandAsMethodCompiler.MethodBase as MethodInfo;
				if (mi != null && _fixedMethods.TryGetValue(mi, out fixedValue))
				{
					// Replace the call inst with a constant load.
					Utilities.Assert(mi.GetParameters().Length == 0, "mi.GetParameters().Length == 0");


					TreeInstruction newInst = new TreeInstruction(IROpCodes.Ldc_I4);
					newInst.Operand = fixedValue;
					return newInst;
				}
				return null;
			}
			else if (inst.Opcode == IROpCodes.Stloc)
			{
				if (inst.Left.Opcode == IROpCodes.Ldc_I4)
					_knownVariables[inst.OperandAsVariable] = inst.Left.OperandAsInt32;
			}
			else if (inst.Opcode == IROpCodes.Ldloc)
			{
				int knownValue;
				if (_knownVariables.TryGetValue(inst.OperandAsVariable, out knownValue))
				{
					TreeInstruction newinst = new TreeInstruction(IROpCodes.Ldc_I4);
					newinst.Operand = knownValue;
					return newinst;
				}
			}

			// Arithmetic evaluation.
			if (inst.GetType() != typeof(TreeInstruction) ||
				inst.Opcode.FlowControl != FlowControl.Next)
				return null;

			if (inst.Opcode.ReflectionOpCode == null)
				return null;

			PopBehavior pb = IROpCode.GetPopBehavior(inst.Opcode.ReflectionOpCode.Value.StackBehaviourPop);
			if (pb != PopBehavior.Pop2 || inst.Left.Opcode != inst.Right.Opcode)
				return null;

			if (inst.Left.Opcode == IROpCodes.Ldc_I4)
			{
				int l = inst.Left.OperandAsInt32;
				int r = inst.Right.OperandAsInt32;

				switch (inst.Opcode.IRCode)
				{
					case IRCode.Ceq:
						TreeInstruction newinst = new TreeInstruction(IROpCodes.Ldc_I4);
						newinst.Operand = l == r ? 1 : 0;
						return newinst;
				}
			}

			return null;
		}
	}
}
