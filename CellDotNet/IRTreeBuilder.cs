using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Reflection.Emit;

namespace CellDotNet
{
	/// <summary>
	/// Used by <see cref="MethodCompiler"/> to construct it's IR tree.
	/// </summary>
	class IRTreeBuilder
	{
		private enum PopBehavior
		{
			Pop0 = 0,
			Pop1 = 1,
			Pop2 = 2,
			Pop3 = 3,
			PopAll = 1000,
			VarPop = 1001
		}

		private Dictionary<int, List<MethodVariable>> _branchTargetStackVariables = new Dictionary<int, List<MethodVariable>>();

		private List<TreeInstruction> _instructionStack = new List<TreeInstruction>();
		private List<MethodVariable> _currentVariableStack;
		private int _currentVariableStackTop = -1;
		private int _lastStackVariableNumber = 999;

		private List<MethodVariable> GetBranchVariableStack(int branchTargetAddress)
		{
			List<MethodVariable> targetvariablestack;
			if (_branchTargetStackVariables.TryGetValue(branchTargetAddress, out targetvariablestack))
			{
				// If the branch target has been seen before, check that they agree on the stack top.
				Utilities.Assert(targetvariablestack.Count == _instructionStack.Count,
					"targetvariablestack.Count == _instructionStack.Count");
			}
			else
			{
				targetvariablestack = new List<MethodVariable>();
				_branchTargetStackVariables.Add(branchTargetAddress, targetvariablestack);
			}

			return targetvariablestack;
		}

		private int GetNextForwardBranchAddress(int currentReaderAddress)
		{
			int min = int.MaxValue;
			foreach (int target in _branchTargetStackVariables.Keys)
			{
				if (target > currentReaderAddress && target < min)
					min = target;
			}

			return min;
		}

		/// <summary>
		/// If the instruction stack is not empty, the top of that stack is popped.
		/// Otherwise, an <see cref="IROpCodes.Ldloc"/> instruction that reads the variable 
		/// at the top of the variable stack is created and returned (and the top is popped).
		/// <para>
		/// So basically, this abstracts whether an operand comes from a stack variable or the 
		/// instruction stack.
		/// </para>
		/// </summary>
		/// <returns></returns>
		private TreeInstruction Pop()
		{
			if (_instructionStack.Count > 0)
			{
				TreeInstruction inst = _instructionStack[_instructionStack.Count - 1];
				_instructionStack.RemoveAt(_instructionStack.Count - 1);
				return inst;
			}
			else if (_currentVariableStackTop >= 0)
			{
				MethodVariable var = _currentVariableStack[_currentVariableStackTop];
				_currentVariableStackTop--;
				TreeInstruction inst = new TreeInstruction();
				inst.Opcode = IROpCodes.Ldloc;
				inst.StackType = var.StackType;
				inst.Operand = var;
				return inst;
			}
			else
				throw new Exception("??");
		}

		/// <summary>
		/// <list type="ordered">
		/// <item>
		/// Creates instructions to save the contents of the instruction stack to the variable
		/// stack associated with the branch target.
		/// </item>
		/// <item>Activates the variable stack.</item>
		/// <item>Clears the instruction stack.</item>
		/// </list>
		/// </summary>
		/// <param name="branchTarget"></param>
		/// <returns></returns>
		/// <param name="currentBB"></param>
		private void SaveInstructionStack(int branchTarget, List<TreeInstruction> currentBB)
		{
			List<MethodVariable> stack = GetBranchVariableStack(branchTarget);
			List<TreeInstruction> saveInstructionRoots = new List<TreeInstruction>();

			// If it's the first time that we save for this target, we need to create variables.
			if (stack.Count == 0 && _instructionStack.Count > 0)
			{
				for (int i = 0; i < _instructionStack.Count; i++)
				{
					_lastStackVariableNumber++;
					stack.Add(new MethodVariable(_lastStackVariableNumber, _instructionStack[i].StackType));
				}
			}

			// Insert instructions to save.
			for (int i = 0; i < _instructionStack.Count; i++)
			{
				TreeInstruction storeInst = new TreeInstruction();
				storeInst.StackType = StackTypeDescription.None;
				storeInst.Opcode = IROpCodes.Stloc;
				storeInst.Operand = stack[i];
				storeInst.Left = _instructionStack[i];

				saveInstructionRoots.Add(storeInst);
			}

			// Activate the variable stack.
			_currentVariableStack = stack;
			_currentVariableStackTop = stack.Count - 1;

			_instructionStack.Clear();

			currentBB.AddRange(saveInstructionRoots);
		}

		private int TotalStackSize
		{
			get { return _currentVariableStackTop + 1 + _instructionStack.Count; }
		}

		public List<IRBasicBlock> BuildBasicBlocks(MethodBase method, ILReader reader, 
		                                           List<MethodVariable> variables, ReadOnlyCollection<MethodParameter> parameters)
		{
			return BuildBasicBlocks(method.Name, reader, variables, parameters);
		}

		internal List<IRBasicBlock> BuildBasicBlocks(ILReader reader, List<MethodVariable> variables)
		{
			ReadOnlyCollection<MethodParameter> parameters = new ReadOnlyCollection<MethodParameter>(new MethodParameter[0]);
			return BuildBasicBlocks("methodXX", reader, variables, parameters);
		}

		/// <summary>
		/// For unit testing: It does not depend on a <see cref="MethodBase"/>.
		/// </summary>
		/// <param name="methodName"></param>
		/// <param name="reader"></param>
		/// <param name="variables"></param>
		/// <param name="parameters"></param>
		/// <returns></returns>
		private List<IRBasicBlock> BuildBasicBlocks(string methodName, ILReader reader, List<MethodVariable> variables, ReadOnlyCollection<MethodParameter> parameters)
		{
			Utilities.AssertArgumentNotNull(variables, "_variables != null");
			Utilities.AssertArgumentNotNull(parameters, "_parameters != null");

			IRBasicBlock currblock = new IRBasicBlock();
			List<TreeInstruction> branches = new List<TreeInstruction>();

			List<IRBasicBlock> blocks = new List<IRBasicBlock>();
			int nextForwardBranchTarget = int.MaxValue;
			TypeDeriver typederiver = new TypeDeriver();

			while (reader.Read())
			{
				// Adjust variable stack if we've reached a new forward branch.
				if (nextForwardBranchTarget == reader.Offset)
				{
					nextForwardBranchTarget = GetNextForwardBranchAddress(reader.Offset);

					// If it's a branch target and there's contents on the instruction stack then
					// we need to save the instruction stack on the variable stack.
					SaveInstructionStack(reader.Offset, currblock.Roots);
				}

				Utilities.Assert(nextForwardBranchTarget > reader.Offset, 
					"nextForwardBranchTarget > reader.Offset");

				PopBehavior popbehavior = GetPopBehavior(reader.OpCode);
				int pushcount = GetPushCount(reader.OpCode);


				TreeInstruction treeinst = new TreeInstruction();
				treeinst.Opcode = reader.OpCode;
				treeinst.Offset = reader.Offset;

				// Replace variable and parameter references with our own types.
				// Do not determine escapes here, since it may change later.
				if (treeinst.Opcode.IRCode == IRCode.Ldloc || treeinst.Opcode.IRCode == IRCode.Stloc ||
				    treeinst.Opcode.IRCode == IRCode.Ldloca)
				{
					LocalVariableInfo lvi = (LocalVariableInfo) reader.Operand;
					treeinst.Operand = variables[lvi.LocalIndex];
				}
				else if (treeinst.Opcode.IRCode == IRCode.Ldarg || treeinst.Opcode.IRCode == IRCode.Starg ||
				         treeinst.Opcode.IRCode == IRCode.Ldarga)
				{
					ParameterInfo pi = (ParameterInfo)reader.Operand;
					treeinst.Operand = parameters[pi.Position];
				}
				else
					treeinst.Operand = reader.Operand;

				// Pop
				switch (popbehavior)
				{
					case PopBehavior.Pop1:
						treeinst.Left = Pop();
						break;
					case PopBehavior.Pop2:
						treeinst.Right = Pop();
						treeinst.Left = Pop();
						break;
					case PopBehavior.PopAll: // "leave"
						throw new NotImplementedException("PopAll");
					case PopBehavior.VarPop: // "ret", "call"
						if (reader.OpCode == IROpCodes.Ret)
						{
							// CLI: "The evaluation stack for the current method must be empty except for the value to be returned."
							if (TotalStackSize > 1)
							{
								throw new ILException(string.Format(
								                      	"Stack.Count = {0} > 1 for VarPop opcode {1} in method {2} at offset {3:x4} ??",
								                      	TotalStackSize, reader.OpCode.Name, methodName, reader.Offset));
							}
							else if (TotalStackSize == 1)
							{
								treeinst.Left = Pop();
							}
						}
						else if (reader.OpCode.FlowControl == FlowControl.Call)
						{
							// Build a method call from the stack.
							MethodBase mr = (MethodBase) reader.Operand;
							if (TotalStackSize < mr.GetParameters().Length)
								throw new ILException("Too few parameters on stack.");

							int hasThisExtraParam = ((int) (mr.CallingConvention & CallingConventions.HasThis) != 0 && 
							                         reader.OpCode != IROpCodes.Newobj) ? 1 : 0;
							int paramcount = mr.GetParameters().Length + hasThisExtraParam;

							MethodCallInstruction mci = new MethodCallInstruction(mr, reader.OpCode);
							mci.Offset = reader.Offset;

							TreeInstruction[] arr = new TreeInstruction[paramcount];
							for (int i = 0; i < paramcount; i++)
								arr[paramcount - 1 - i] = Pop();

							mci.Parameters.AddRange(arr);

							MethodInfo methodinfo = mr as MethodInfo;
							if (mr is ConstructorInfo || (methodinfo != null && methodinfo.ReturnType != typeof(void)))
								pushcount = 1;
							else
								pushcount = 0;

							treeinst = mci;
						}
						else
							throw new Exception("Unknown VarPop.");
						//							throw new Exception("Method calls are not supported.");
						break;
					case PopBehavior.Pop3:
						if (reader.OpCode.StackBehaviourPush != StackBehaviour.Push0)
							throw new ILException("Pop3 with a push != 0?");
						throw new NotImplementedException();
					default:
						if (popbehavior != PopBehavior.Pop0)
							throw new Exception("Invalid PopBehavior: " + popbehavior + ". Only two-argument method calls are supported.");
						break;
				}

				typederiver.DeriveType(treeinst);

				// Push
				if (pushcount == 1)
					_instructionStack.Add(treeinst);
				else if (pushcount != 0)
					throw new Exception("Only 1-push is supported.");

				// Handle branches.
				if (reader.OpCode.FlowControl == FlowControl.Branch ||
					reader.OpCode.FlowControl == FlowControl.Cond_Branch)
				{
					branches.Add(treeinst);

					// Store instruction stack associated with the target.
					int targetOffset = (int)reader.Operand;
					SaveInstructionStack(targetOffset, currblock.Roots);

					if (targetOffset > reader.Offset && 
						targetOffset < nextForwardBranchTarget)
					{
						// The target is closer than the previous one, 
						// so put the old one back in line and start looking for the new one.
						nextForwardBranchTarget = GetNextForwardBranchAddress(reader.Offset);
					}
				}

				if ( _instructionStack.Count == 0)
				{
					// It is a root when the instruction stack is empty.
					currblock.Roots.Add(treeinst);
				}
			}
			blocks.Add(currblock);

			// Fix branches.
			// It is by definition only possible to branch to basic blocks.
			// So we need to create these blocks.
			Dictionary<int, IRBasicBlock> basicBlockOffsets = new Dictionary<int, IRBasicBlock>();

			foreach (TreeInstruction branchinst in branches)
			{
				int targetPos = (int) branchinst.Operand;
				IRBasicBlock target;

				if (basicBlockOffsets.TryGetValue(targetPos, out target))
				{
					branchinst.Operand = target;
					continue;
				}

				// Find root to create basic block from.
				for (int bbindex = 0; bbindex < blocks.Count; bbindex++)
				{
					IRBasicBlock bb = blocks[bbindex];
					for (int rootindex = 0; rootindex < bb.Roots.Count; rootindex++)
					{
						// The instruction with the branch target address is some instruction on the 
						// left side of the tree: It might not be the leaf, since the leafe
						// can be an inserted variable stack load instruction.
						TreeInstruction firstWithOffset = bb.Roots[rootindex].GetFirstInstructionWithOffset();

						if (firstWithOffset.Offset != targetPos)
							continue;

						TreeInstruction firstinst = firstWithOffset.GetFirstInstruction();

						// Need to create new bb from this root.
						List<TreeInstruction> newblockroots = bb.Roots.GetRange(rootindex, bb.Roots.Count - rootindex);
						bb.Roots.RemoveRange(rootindex, bb.Roots.Count - rootindex);

						IRBasicBlock newbb = new IRBasicBlock();
						newbb.Roots.AddRange(newblockroots);
						blocks.Insert(bbindex + 1, newbb);

						basicBlockOffsets.Add(firstinst.Offset, newbb);
						target = newbb;
						goto NextBranch;
					}
				}
				throw new Exception("IR tree construction error. Can't find branch target " + targetPos.ToString("x4") + " from instruction at " + branchinst.Offset.ToString("x4") + ".");

				NextBranch:
				branchinst.Operand = target;
			}

			foreach (List<MethodVariable> methodVariables in _branchTargetStackVariables.Values)
			{
				variables.AddRange(methodVariables);
			}

			return blocks;
		}

		/// <summary>
		/// Returns the number of values pushed by the opcode. -1 is returned for function calls.
		/// </summary>
		/// <param name="code"></param>
		/// <returns></returns>
		private static int GetPushCount(IROpCode code)
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
					pushCount = 1;
					break;
				case StackBehaviour.Push1_push1:
					pushCount = 2;
					break;
				case StackBehaviour.Varpush:
				default:
					pushCount = -1;
					break;
			}

			return pushCount;
		}

		private static PopBehavior GetPopBehavior(IROpCode code)
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
				case StackBehaviour.Popref_popi_pop1:
				case StackBehaviour.Popref_popi_popi:
				case StackBehaviour.Popref_popi_popi8:
				case StackBehaviour.Popref_popi_popr4:
				case StackBehaviour.Popref_popi_popr8:
				case StackBehaviour.Popref_popi_popref:
					pb = PopBehavior.Pop3;
					break;
				default:
					throw new ArgumentOutOfRangeException("code");
			}

			return pb;
		}
	}
}
