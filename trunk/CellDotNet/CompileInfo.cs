using System;
using System.Collections.Generic;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace CellDotNet
{
	/// <summary>
	/// Data used during compilation of a method.
	/// </summary>
	class CompileInfo
	{
		private List<BasicBlock> _blocks = new List<BasicBlock>();
		public List<BasicBlock> Blocks
		{
			get { return _blocks; }
		}


		public CompileInfo(MethodDefinition	method)
		{
			method.Body.Simplify();

			BuildBasicBlocks(method);
		}

		private void BuildBasicBlocks(MethodDefinition method)
		{
			BasicBlock currblock = new BasicBlock();
			List<TreeInstruction> stack = new List<TreeInstruction>();
			List<TreeInstruction> branches = new List<TreeInstruction>();

			TreeInstruction prevroot = null;

			foreach (Instruction inst in method.Body.Instructions)
			{
				TreeInstruction treeinst;
				PopBehavior popbehavior = GetPopCount(inst.OpCode);
				int pushcount = GetPushCount(inst.OpCode);

				treeinst = new TreeInstruction();
				treeinst.Opcode = inst.OpCode;
				treeinst.Operand = inst.Operand;
				treeinst.Offset = inst.Offset;

//				if (stack.Count == 0 && prevroot != null)
//				{
//					currblock.Roots.Add(prevroot);
//					prevroot = null;
//				}

				// Pop
				switch (popbehavior)
				{
					case PopBehavior.Pop1:
						treeinst.Left = stack[stack.Count - 1];
						stack.RemoveRange(stack.Count - 1, 1);
						break;
					case PopBehavior.Pop2:
						treeinst.Left = stack[stack.Count - 2];
						treeinst.Right = stack[stack.Count - 1];
						stack.RemoveRange(stack.Count - 2, 2);
						break;
					case PopBehavior.PopAll: // "leave"
						stack.Clear();
						break;
					case PopBehavior.VarPop: // "ret"
						if (inst.OpCode != OpCodes.Ret)
							throw new Exception("Method calls are not supported.");
						break;
					default:
						if (popbehavior != PopBehavior.Pop0)
							throw new Exception("Invalid PopBehavior: " + popbehavior + ". Only two-argument method calls are supported.");
						break;
				}

				// Push
				if (pushcount == 1)
					stack.Add(treeinst);
				else if (pushcount != 0)
					throw new Exception("Only 1-push is supported.");

				bool endsblock = false;
				switch (inst.OpCode.FlowControl)
				{
					case FlowControl.Branch:
					case FlowControl.Cond_Branch:
					case FlowControl.Return:
					case FlowControl.Throw:
						endsblock = true;

						if (inst.OpCode.FlowControl == FlowControl.Branch ||
							inst.OpCode.FlowControl == FlowControl.Cond_Branch)
						{
							treeinst.Operand = ((Instruction) inst.Operand).Offset;
							branches.Add(treeinst);
						}
						break;
					case FlowControl.Call:
					case FlowControl.Meta:
					case FlowControl.Next:
					case FlowControl.Phi:
					case FlowControl.Break:
					default:
						break;
				}

				if (endsblock)
				{
					currblock.Roots.Add(treeinst);
					Blocks.Add(currblock);
					currblock = new BasicBlock();
				}
				else if (stack.Count == 0)
				{
					currblock.Roots.Add(treeinst);
				}
			}

			if (currblock.Roots.Count > 0)
				Blocks.Add(currblock);

			// Fix branches.
			foreach (TreeInstruction branchinst in branches)
			{
				int targetOffset = (int) branchinst.Operand;
				foreach (BasicBlock block in Blocks)
				{
					foreach (TreeInstruction root in block.Roots)
					{
						foreach (TreeInstruction inst in root.IterateInorder())
						{
							if (inst.Offset == targetOffset)
								branchinst.Operand = inst;
						}
					}
				}
			}
		}

		static int GetPushCount(OpCode code)
		{
			int pushCount;

			switch (code.StackBehaviourPush)
			{
				case StackBehaviour.Push0:
					pushCount = 0;
					break;
				case StackBehaviour.Push1:
				case StackBehaviour.Pushi:
				case StackBehaviour.Pushi8:
				case StackBehaviour.Pushr4:
				case StackBehaviour.Pushr8:
				case StackBehaviour.Pushref:
				case StackBehaviour.Varpush:
					pushCount = 1;
					break;
				case StackBehaviour.Push1_push1:
					pushCount = 2;
					break;
				default:
					pushCount = -1;
					break;
			}

			return pushCount;
		}

		enum PopBehavior
		{
			Pop0 = 0,
			Pop1 = 1,
			Pop2 = 2,
			Pop3 = 3,
			PopAll = 1000,
			VarPop = 1001

		}

		static PopBehavior GetPopCount(OpCode code)
		{
			PopBehavior pb;

			switch (code.StackBehaviourPop)
			{
				case StackBehaviour.Pop0:
					pb = PopBehavior.Pop0;
					break;
				case StackBehaviour.Varpop:
					pb = PopBehavior.VarPop;
					break;
				case StackBehaviour.Pop1:
				case StackBehaviour.Popi:
				case StackBehaviour.Popref:
					pb = PopBehavior.Pop1;
					break;
				case StackBehaviour.Pop1_pop1:
				case StackBehaviour.Popi_pop1:
				case StackBehaviour.Popi_popi:
				case StackBehaviour.Popi_popi8:
				case StackBehaviour.Popi_popr4:
				case StackBehaviour.Popi_popr8:
				case StackBehaviour.Popref_pop1:
				case StackBehaviour.Popref_popi:
					pb = PopBehavior.Pop2;
					break;
				case StackBehaviour.Popi_popi_popi:
				case StackBehaviour.Popref_popi_popi:
				case StackBehaviour.Popref_popi_popi8:
				case StackBehaviour.Popref_popi_popr4:
				case StackBehaviour.Popref_popi_popr8:
				case StackBehaviour.Popref_popi_popref:
					pb = PopBehavior.Pop3;
					break;
				case StackBehaviour.PopAll:
					pb = PopBehavior.PopAll; // Special...
					break;
				default:
					throw new ArgumentOutOfRangeException("code");
			}

			return pb;
		}
	}
}
