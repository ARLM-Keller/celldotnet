using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Permissions;

namespace CellDotNet
{
	/// <summary>
	/// Used by <see cref="MethodCompiler"/> to construct it's IR tree.
	/// </summary>
	class IRTreeBuilder
	{
		#region Stack state

		class ParseStack
		{
			public readonly Dictionary<int, List<MethodVariable>> BranchTargetStackVariables = new Dictionary<int, List<MethodVariable>>();

			public readonly List<TreeInstruction> InstructionStack = new List<TreeInstruction>();
			private List<MethodVariable> _currentVariableStack;
			private int _currentVariableStackTop = -1;
			private int _lastStackVariableNumber = 999;

			public List<MethodVariable> GetBranchVariableStack(int branchTargetAddress)
			{
				List<MethodVariable> targetvariablestack;
				if (BranchTargetStackVariables.TryGetValue(branchTargetAddress, out targetvariablestack))
				{
					// If the branch target has been seen before, check that they agree on the stack top.
					Utilities.Assert(targetvariablestack.Count == InstructionStack.Count,
						"targetvariablestack.Count == InstructionStack.Count");
				}
				else
				{
					targetvariablestack = new List<MethodVariable>();
					BranchTargetStackVariables.Add(branchTargetAddress, targetvariablestack);
				}

				return targetvariablestack;
			}

			public int GetNextForwardBranchAddress(int currentReaderAddress)
			{
				int min = int.MaxValue;
				foreach (int target in BranchTargetStackVariables.Keys)
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
			public TreeInstruction Pop()
			{
				if (InstructionStack.Count > 0)
				{
					TreeInstruction inst = InstructionStack[InstructionStack.Count - 1];
					InstructionStack.RemoveAt(InstructionStack.Count - 1);
					return inst;
				}
				else if (_currentVariableStackTop >= 0)
				{
					MethodVariable var = _currentVariableStack[_currentVariableStackTop];
					_currentVariableStackTop--;
					TreeInstruction inst = new TreeInstruction(IROpCodes.Ldloc);
					inst.StackType = var.StackType;
					inst.Operand = var;
					return inst;
				}
				else
					throw new Exception("??");
			}

			/// <summary>
			/// Creates instructions to save the contents of the instruction stack to the variable
			/// stack associated with the branch target.
			/// <list>
			/// <item>Activates the variable stack.</item>
			/// <item>Clears the instruction stack.</item>
			/// </list>
			/// </summary>
			/// <param name="branchTarget"></param>
			/// <returns></returns>
			/// <param name="currentBB"></param>
			public void SaveInstructionStack(int branchTarget, List<TreeInstruction> currentBB)
			{
				List<MethodVariable> stack = GetBranchVariableStack(branchTarget);
				List<TreeInstruction> saveInstructionRoots = new List<TreeInstruction>();

				// If it's the first time that we save for this target, we need to create variables.
				if (stack.Count == 0 && InstructionStack.Count > 0)
				{
					for (int i = 0; i < InstructionStack.Count; i++)
					{
						_lastStackVariableNumber++;
						stack.Add(new MethodVariable(_lastStackVariableNumber, InstructionStack[i].StackType));
					}
				}

				// Insert instructions to save.
				for (int i = 0; i < InstructionStack.Count; i++)
				{
					TreeInstruction storeInst = new TreeInstruction(IROpCodes.Stloc);
					storeInst.StackType = StackTypeDescription.None;
					storeInst.Operand = stack[i];
					storeInst.Left = InstructionStack[i];

					saveInstructionRoots.Add(storeInst);
				}

				// Activate the variable stack.
				_currentVariableStack = stack;
				_currentVariableStackTop = stack.Count - 1;

				InstructionStack.Clear();

				currentBB.AddRange(saveInstructionRoots);
			}

			public int TotalStackSize
			{
				get { return _currentVariableStackTop + 1 + InstructionStack.Count; }
			}
		}


		#endregion

		private ParseStack _parseStack;


		static private object s_lock = new object();

		private Dictionary<short, IROpCode> _irmap = GetIROpCodeMap();
		private static Dictionary<short, IROpCode> s_iropcodemap;
		/// <summary>
		/// Returns a map from the IL opcode subset that maps directly to the IR opcodes.
		/// </summary>
		/// <returns></returns>
		static Dictionary<short, IROpCode> GetIROpCodeMap()
		{
			lock (s_lock)
			{
				if (s_iropcodemap == null)
				{
					s_iropcodemap = new Dictionary<short, IROpCode>();

					FieldInfo[] fields = typeof(IROpCodes).GetFields(BindingFlags.Public | BindingFlags.Static);
					foreach (FieldInfo field in fields)
					{
						IROpCode oc = (IROpCode)field.GetValue(null);
						if (oc.ReflectionOpCode != null)
							s_iropcodemap.Add(oc.ReflectionOpCode.Value.Value, oc);
					}
				}
			}

			return s_iropcodemap;
		}


		public List<IRBasicBlock> BuildBasicBlocks(MethodBase method, ILReader reader, 
		                                           List<MethodVariable> variables, ReadOnlyCollection<MethodParameter> parameters)
		{
			return BuildBasicBlocks(method.Name, reader, variables, parameters);
		}

		/// <summary>
		/// For debugging/unit tests.
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="variables"></param>
		/// <returns></returns>
		internal List<IRBasicBlock> BuildBasicBlocks(ILReader reader, List<MethodVariable> variables)
		{
			ReadOnlyCollection<MethodParameter> parameters = new ReadOnlyCollection<MethodParameter>(new MethodParameter[0]);
			return BuildBasicBlocks("methodXX", reader, variables, parameters);
		}

		/// <summary>
		/// For debugging/unit tests.
		/// </summary>
		/// <param name="method"></param>
		/// <param name="variables">
		/// This is really an out param that will contain the variables, including any that are created during the construction process.
		/// </param>
		/// <returns></returns>
		private List<IRBasicBlock> BuildBasicBlocks(MethodBase method, out List<MethodVariable> variables)
		{
			ILReader reader = new ILReader(method);
			List<MethodParameter> parms = new List<MethodParameter>();
			TypeDeriver td = new TypeDeriver();

			foreach (ParameterInfo pi in method.GetParameters())
			{
				parms.Add(new MethodParameter(pi, td.GetStackTypeDescription(pi.ParameterType)));
			}

			variables = new List<MethodVariable>();
			foreach (LocalVariableInfo lvi in method.GetMethodBody().LocalVariables)
			{
				variables.Add(new MethodVariable(lvi, td.GetStackTypeDescription(lvi.LocalType)));
			}

			return BuildBasicBlocks(method.Name, reader, variables, new ReadOnlyCollection<MethodParameter>(parms));
		}

		/// <summary>
		/// For debugging/unit tests.
		/// </summary>
		/// <param name="method"></param>
		/// <returns></returns>
		public List<IRBasicBlock> BuildBasicBlocks(MethodBase method)
		{
			List<MethodVariable> vars;
			return BuildBasicBlocks(method, out vars);
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

			_parseStack = new ParseStack();

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
					nextForwardBranchTarget = _parseStack.GetNextForwardBranchAddress(reader.Offset);

					// If it's a branch target and there's contents on the instruction stack then
					// we need to save the instruction stack on the variable stack.
					_parseStack.SaveInstructionStack(reader.Offset, currblock.Roots);
				}

				Utilities.Assert(nextForwardBranchTarget > reader.Offset, 
					"nextForwardBranchTarget > reader.Offset");



				IROpCode ircode;
				if (!_irmap.TryGetValue(reader.OpCode.Value, out ircode))
				{
					throw new Exception("Can't find IR opcode for reflection opcode " + reader.OpCode.Name +
										". The parsing or simplification probably wasn't performed correcly.");
				}



				PopBehavior popbehavior = IROpCode.GetPopBehavior(reader.OpCode.StackBehaviourPop);
				int pushcount = GetPushCount(reader.OpCode);


				TreeInstruction treeinst = new TreeInstruction(ircode);
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
				else if (reader.Operand is Type)
				{
					treeinst.Operand = typederiver.GetStackTypeDescription((Type) reader.Operand);
				}
				else
					treeinst.Operand = reader.Operand;

				// Pop
				switch (popbehavior)
				{
					case PopBehavior.Pop1:
						treeinst.Left = _parseStack.Pop();
						break;
					case PopBehavior.Pop2:
						treeinst.Right = _parseStack.Pop();
						treeinst.Left = _parseStack.Pop();
						break;
					case PopBehavior.PopAll: // "leave"
						throw new NotImplementedException("PopAll");
					case PopBehavior.VarPop: // "ret", "call"
						if (reader.OpCode == OpCodes.Ret)
						{
							// CLI: "The evaluation stack for the current method must be empty except for the value to be returned."
							if (_parseStack.TotalStackSize > 1)
							{
								throw new ILSemanticErrorException(string.Format(
								                      	"Stack.Count = {0} > 1 for VarPop opcode {1} in method {2} at offset {3:x4} ??",
								                      	_parseStack.TotalStackSize, reader.OpCode.Name, methodName, reader.Offset));
							}
							else if (_parseStack.TotalStackSize == 1)
							{
								treeinst.Left = _parseStack.Pop();
							}
						}
						else if (reader.OpCode.FlowControl == FlowControl.Call)
						{
							CreateMethodCallInstruction(reader, ircode, out pushcount, out treeinst);
						}
						else
							throw new Exception("Unknown VarPop.");
						//							throw new Exception("Method calls are not supported.");
						break;
					case PopBehavior.Pop3:
						if (reader.OpCode.StackBehaviourPush != StackBehaviour.Push0)
							throw new ILSemanticErrorException("Pop3 with a push != 0?");

						// Replace stelem with ldelema and stind.
						if (treeinst.Opcode.IRCode >= IRCode.Stelem_I && treeinst.Opcode.IRCode <= IRCode.Stelem_Ref)
						{
							treeinst = ReplaceStoreElement(treeinst, typederiver);
						}
						else
							throw new NotImplementedException();

						break;
					default:
						if (popbehavior != PopBehavior.Pop0)
							throw new Exception("Invalid PopBehavior: " + popbehavior + ". Only two-argument method calls are supported.");
						break;
				}

				typederiver.DeriveType(treeinst);

				// Push
				if (pushcount == 1)
					_parseStack.InstructionStack.Add(treeinst);
				else if (pushcount != 0)
					throw new Exception("Only 1-push is supported.");

				// Handle branches.
				if (reader.OpCode.FlowControl == FlowControl.Branch ||
					reader.OpCode.FlowControl == FlowControl.Cond_Branch)
				{
					branches.Add(treeinst);

					// Store instruction stack associated with the target.
					int targetOffset = (int)reader.Operand;
					_parseStack.SaveInstructionStack(targetOffset, currblock.Roots);

					if (targetOffset > reader.Offset && 
						targetOffset < nextForwardBranchTarget)
					{
						// The target is closer than the previous one, 
						// so put the old one back in line and start looking for the new one.
						nextForwardBranchTarget = _parseStack.GetNextForwardBranchAddress(reader.Offset);
					}
				}

				if (_parseStack.InstructionStack.Count == 0)
				{
					// It is a root when the instruction stack is empty.
					currblock.Roots.Add(treeinst);
				}
			}
			blocks.Add(currblock);

			FixBranchesAndCreateBasicBlocks(blocks, branches, variables);

			return blocks;
		}

		private void CreateMethodCallInstruction(ILReader reader, IROpCode opcode, out int pushcount, out TreeInstruction treeinst)
		{
			// Build a method call from the stack.
			MethodBase methodBase = (MethodBase) reader.Operand;
			if (_parseStack.TotalStackSize < methodBase.GetParameters().Length)
				throw new ILSemanticErrorException("Too few parameters on stack.");

			int hasThisExtraParam = ((int) (methodBase.CallingConvention & CallingConventions.HasThis) != 0 && 
			                         opcode != IROpCodes.Newobj) ? 1 : 0;
			int paramcount = methodBase.GetParameters().Length + hasThisExtraParam;

			TreeInstruction[] arr = new TreeInstruction[paramcount];
			for (int i = 0; i < paramcount; i++)
				arr[paramcount - 1 - i] = _parseStack.Pop();

			MethodInfo methodinfo = methodBase as MethodInfo;
			if (methodBase is ConstructorInfo || (methodinfo != null && methodinfo.ReturnType != typeof(void)))
				pushcount = 1;
			else
				pushcount = 0;

			MethodCallInstruction mci;
			if (methodinfo != null && methodBase.IsDefined(typeof(IntrinsicMethodAttribute), false))
			{
				IntrinsicMethodAttribute methodAtt = (IntrinsicMethodAttribute) methodBase.GetCustomAttributes(typeof(IntrinsicMethodAttribute), false)[0];
				mci = new MethodCallInstruction(methodinfo, methodAtt.Intrinsic);
			}
			else if (methodBase.IsDefined(typeof(SpuOpCodeAttribute), false))
			{
				SpuOpCodeAttribute opcodeAtt = (SpuOpCodeAttribute) methodBase.GetCustomAttributes(typeof (SpuOpCodeAttribute), false)[0];
				SpuOpCode oc = SpuOpCode.GetOpCode(opcodeAtt.SpuOpCode);
				mci = new MethodCallInstruction(methodinfo, oc);
			}
			else
			{
				// A normal method call.
				mci = new MethodCallInstruction(methodBase, opcode);
			}
			mci.Offset = reader.Offset;
			mci.Parameters.AddRange(arr);
			treeinst = mci;
		}

		private void FixBranchesAndCreateBasicBlocks(List<IRBasicBlock> blocks, List<TreeInstruction> branches, List<MethodVariable> variables)
		{
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

			foreach (List<MethodVariable> methodVariables in _parseStack.BranchTargetStackVariables.Values)
			{
				variables.AddRange(methodVariables);
			}
		}

		private TreeInstruction ReplaceStoreElement(TreeInstruction storeInst, TypeDeriver typederiver)
		{
			// stelem.* stack transition: ..., array, index, value, -> ...
			// stelem<T> stack transition: ..., array, index, value, -> ...

			IROpCode stindOpcode;

			switch (storeInst.Opcode.IRCode)
			{
				case IRCode.Stelem_I4:
					stindOpcode = IROpCodes.Stind_I4;
					break;
				case IRCode.Stelem_R4:
					stindOpcode = IROpCodes.Stind_R4;
					break;
				case IRCode.Stelem:
					stindOpcode = IROpCodes.Stobj;
					break;
				default:
					throw new NotSupportedException("Unsupported array opcode: " + storeInst.Opcode);
			}

			TreeInstruction valueInst = _parseStack.Pop(); // The value.

			TreeInstruction ldelemachild = new TreeInstruction(IROpCodes.Ldelema);
			ldelemachild.Right = _parseStack.Pop(); // The index.
			ldelemachild.Left = _parseStack.Pop(); // The array.
			ldelemachild.Operand = ldelemachild.Left.StackType.GetArrayElementType();
			ldelemachild.Offset = storeInst.Offset; // Assume the identity of the stelem.

			TreeInstruction stindParent = new TreeInstruction(stindOpcode);
			stindParent.Left = ldelemachild;
			stindParent.Right = valueInst;
			stindParent.Operand = storeInst.Operand; // Element type.

			typederiver.DeriveType(ldelemachild);
			typederiver.DeriveType(stindParent);
			storeInst = stindParent;
			return storeInst;
		}

		/// <summary>
		/// Returns the number of values pushed by the opcode. -1 is returned for function calls.
		/// </summary>
		/// <param name="code"></param>
		/// <returns></returns>
		private static int GetPushCount(OpCode code)
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

		public static void RemoveNops(List<IRBasicBlock> blocks)
		{
			for (int blocknum = 0; blocknum < blocks.Count; blocknum++)
			{
				IRBasicBlock bb = blocks[blocknum];
				bb.Roots.RemoveAll(delegate(TreeInstruction obj) { return obj.Opcode == IROpCodes.Nop; });
			}
		}
	}
}
