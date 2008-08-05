using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

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

			private List<MethodVariable> _currentVariableStack = new List<MethodVariable>();
			private int _currentVariableStackTop = -1;
			private int _lastStackVariableNumber = 999;

			public List<MethodVariable> GetBranchVariableStack(int branchTargetAddress)
			{
				List<MethodVariable> targetvariablestack;
				if (BranchTargetStackVariables.TryGetValue(branchTargetAddress, out targetvariablestack))
				{
					// If the branch target has been seen before, check that they agree on the stack top.
					Utilities.Assert(targetvariablestack.Count == TotalStackSize,
						"targetvariablestack.Count == TotalStackSize");
				}
				else
				{
					targetvariablestack = new List<MethodVariable>();
					BranchTargetStackVariables.Add(branchTargetAddress, targetvariablestack);
				}

				return targetvariablestack;
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
					_currentVariableStack.RemoveAt(_currentVariableStackTop);
					_currentVariableStackTop--;

					TreeInstruction inst = new TreeInstruction(IROpCodes.Ldloc, var.StackType, var, -1);

					return inst;
				}
				else
				{
					Debug.Fail("??");
					return null;
				}
			}

			public StackTypeDescription PeekType()
			{
				if (InstructionStack.Count > 0)
					return  InstructionStack[InstructionStack.Count - 1].StackType;
				else if (_currentVariableStackTop >= 0)
					return _currentVariableStack[_currentVariableStackTop].StackType;
				else
					return StackTypeDescription.None;
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
				if (stack.Count == 0 && TotalStackSize > 0)
				{
					for (int i = 0; i <= _currentVariableStackTop; i++)
					{
						_lastStackVariableNumber++;
						stack.Add(new MethodVariable(_lastStackVariableNumber, _currentVariableStack[i].StackType));
					}

					for (int i = 0; i < InstructionStack.Count; i++)
					{
						_lastStackVariableNumber++;
						stack.Add(new MethodVariable(_lastStackVariableNumber, InstructionStack[i].StackType));
					}
				}

				int j = 0;

				// Insert instructions to save current variable stack.
				for (int i = 0; i <= _currentVariableStackTop; i++, j++)
				{
					TreeInstruction loadInst = new TreeInstruction(
						IROpCodes.Ldloc, _currentVariableStack[i].StackType, _currentVariableStack[i], -1);

					TreeInstruction storeInst = new TreeInstruction(
						IROpCodes.Stloc, StackTypeDescription.None, stack[j], -1, loadInst);

					saveInstructionRoots.Add(storeInst);
				}

				// Insert instructions to save instruction stack.
				for (int i = 0; i < InstructionStack.Count; i++, j++)
				{
					TreeInstruction storeInst = new TreeInstruction(
						IROpCodes.Stloc, StackTypeDescription.None, stack[j], -1, InstructionStack[i]);

					saveInstructionRoots.Add(storeInst);
				}

				// Activate the variable stack.
				// Pop is modified to remove the element from _currentVariableStack.
				_currentVariableStack.Clear();
				_currentVariableStack.AddRange(stack);
				_currentVariableStackTop = stack.Count - 1;

				InstructionStack.Clear();

				currentBB.AddRange(saveInstructionRoots);
			}

			public int TotalStackSize
			{
				get { return _currentVariableStackTop + 1 + InstructionStack.Count; }
			}

			public void LoadVariableStack(int branchTargetAddress)
			{
				List<MethodVariable> targetvariablestack;

				if (BranchTargetStackVariables.TryGetValue(branchTargetAddress, out targetvariablestack))
					_currentVariableStack = new List<MethodVariable>(targetvariablestack);
				else
					_currentVariableStack.Clear();

				_currentVariableStackTop = _currentVariableStack.Count - 1;

				InstructionStack.Clear();
			}
		}

		#endregion

		private ParseStack _parseStack;


		private Dictionary<short, IROpCode> _irmap = GetILToIRMap();
		private static Dictionary<short, IROpCode> s_iropcodemap;

		/// <summary>
		/// Returns a map from the IL opcode subset that maps directly to the IR opcodes.
		/// </summary>
		/// <returns></returns>
		static Dictionary<short, IROpCode> GetILToIRMap()
		{
			if (s_iropcodemap == null)
			{
				Dictionary<short, IROpCode> map = new Dictionary<short, IROpCode>();

				FieldInfo[] fields = typeof(IROpCodes).GetFields(BindingFlags.Public | BindingFlags.Static);
				foreach (FieldInfo field in fields)
				{
					IROpCode oc = (IROpCode)field.GetValue(null);
					if (oc.ReflectionOpCode != null)
						map.Add(oc.ReflectionOpCode.Value.Value, oc);
				}

				s_iropcodemap = map;
			}

			return s_iropcodemap;
		}

		public List<IRBasicBlock> BuildBasicBlocks(MethodBase method, ILReader reader, 
		                                           List<MethodVariable> variables, ReadOnlyCollection<MethodParameter> parameters)
		{
			return BuildBasicBlocks(method.Name, reader, variables, parameters, (method.CallingConvention & CallingConventions.HasThis) != 0);
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
			return BuildBasicBlocks("methodXX", reader, variables, parameters, false); // FIXME antager at der kun kaldes statiske metoder.
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

			if((method.CallingConvention & CallingConventions.HasThis) != 0)
				parms.Add(new MethodParameter(new StackTypeDescription(new TypeDescription(method.DeclaringType))));

			foreach (ParameterInfo pi in method.GetParameters())
			{
				parms.Add(new MethodParameter(pi, td.GetStackTypeDescription(pi.ParameterType)));
			}

			variables = new List<MethodVariable>();
			foreach (LocalVariableInfo lvi in method.GetMethodBody().LocalVariables)
			{
				variables.Add(new MethodVariable(lvi, td.GetStackTypeDescription(lvi.LocalType)));
			}

			return BuildBasicBlocks(method.Name, reader, variables, new ReadOnlyCollection<MethodParameter>(parms), false); // FIXME antager at der kun kaldes statiske metoder.
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
		/// NOTE: <code>parameters</code> must contain the <code>this</code> argument for instance methods.
		/// </summary>
		/// <param name="methodName"></param>
		/// <param name="readerIn"></param>
		/// <param name="variables"></param>
		/// <param name="parameters"></param>
		/// <returns></returns>
		/// <param name="hasThisArgument"></param>
		private List<IRBasicBlock> BuildBasicBlocks(string methodName, ILReader readerIn, List<MethodVariable> variables, ReadOnlyCollection<MethodParameter> parameters, bool hasThisArgument)
		{
			Utilities.AssertArgumentNotNull(variables, "_variables != null");
			Utilities.AssertArgumentNotNull(parameters, "_parameters != null");

			_parseStack = new ParseStack();

			List<IRBasicBlock> blocks = new List<IRBasicBlock>();
			TypeDeriver typederiver = new TypeDeriver();

			Dictionary<int, IRBasicBlock> branchTargets = CreateBasicBlocks(readerIn);

			// Start over, this time actually reading the instructions.
			readerIn.Reset();
			IlReaderWrapper reader = new IlReaderWrapper(readerIn);

			TreeInstruction prevInst = null;
			IRBasicBlock currblock = branchTargets[0];
			while (reader.Read(_parseStack.PeekType()))
			{
				if (prevInst != null && prevInst.Opcode.FlowControl == FlowControl.Branch)
				{
					// Skip unreachable instructions.
					while (!branchTargets.ContainsKey(reader.Offset) && reader.Read(_parseStack.PeekType()))
					{
						// Nothing.
					}

					if (reader.ILReader.State == ILReader.ReadState.EOF)
						break;
				}

				IROpCode ircode;
				if (!_irmap.TryGetValue(reader.OpCode.Value, out ircode))
				{
					throw new ILParseException("Can't find IR opcode for reflection opcode " + reader.OpCode.Name +
										". The parsing or simplification probably wasn't performed correcly.");
				}

				int newinstoffset = reader.Offset;

				// Adjust variable stack if we've reached a new forward branch.
				if (branchTargets.ContainsKey(reader.Offset) && reader.Offset != 0)
				{
					if (prevInst != null && prevInst.Opcode.FlowControl == FlowControl.Branch)
					{
						_parseStack.LoadVariableStack(reader.Offset);
					}
					else
					{
						// If it's a branch target and there's contents on the instruction stack then
						// we need to save the instruction stack on the variable stack.

						_parseStack.SaveInstructionStack(reader.Offset, currblock.Roots);
					}

					blocks.Add(currblock);
					currblock = branchTargets[reader.Offset];
				}

				if (reader.LastCreatedMethodVariable != null)
					variables.Add(reader.LastCreatedMethodVariable);

				PopBehavior popbehavior = IROpCode.GetPopBehavior(reader.OpCode.StackBehaviourPop);
				int pushcount = GetPushCount(reader.OpCode);


				// Determine the operand, while replacing variable and parameter references with our own types.
				object operand;
				if (ircode.IRCode == IRCode.Ldloc || ircode.IRCode == IRCode.Stloc ||
					ircode.IRCode == IRCode.Ldloca)
				{
					LocalVariableInfo lvi = reader.Operand as LocalVariableInfo;
					if (lvi != null)
						operand = variables[lvi.LocalIndex];
					else
						operand = reader.Operand;
				}
				else if (ircode.IRCode == IRCode.Ldarg || ircode.IRCode == IRCode.Starg ||
						 ircode.IRCode == IRCode.Ldarga)
				{
					// reader.Operand is null when its represents the this parameter.
					if (reader.Operand == null)
						operand = parameters[0];
					else
					{
						ParameterInfo pi = (ParameterInfo) reader.Operand;
						operand = parameters[pi.Position + (hasThisArgument ? 1 : 0)];
					}
				}
				else if (reader.Operand is Type)
				{
					operand = typederiver.GetStackTypeDescription((Type)reader.Operand);
				}
				else
					operand = reader.Operand;

				TreeInstruction treeinst = new TreeInstruction(ircode);
				treeinst.Offset = newinstoffset;
				treeinst.Operand = operand;

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
							treeinst = CreateMethodCallInstruction(ircode, reader.OpCode, reader.Offset, (MethodBase)reader.Operand, out pushcount);
						}
						else
							throw new ILParseException("Unknown VarPop.");

						break;
					case PopBehavior.Pop3:
						if (reader.OpCode.StackBehaviourPush != StackBehaviour.Push0)
							throw new ILSemanticErrorException("Pop3 with a push != 0?");

						// Replace stelem with ldelema and stind.
						if (ircode.IRCode >= IRCode.Stelem_I && ircode.IRCode <= IRCode.Stelem_Ref)
						{
							treeinst = ReplaceStoreElement(treeinst, typederiver);
						}
						else
							throw new NotImplementedException();

						break;
					default:
						if (popbehavior != PopBehavior.Pop0)
							throw new ILParseException("Invalid PopBehavior: " + popbehavior + ". Only two-argument method calls are supported.");
						break;
				}

				typederiver.DeriveType(treeinst);

				// Push
				if (pushcount == 1)
					_parseStack.InstructionStack.Add(treeinst);
				else if (pushcount != 0)
					throw new ILParseException("Only 1-push is supported.");

				// Handle branches.
				if (reader.OpCode.FlowControl == FlowControl.Branch ||
					reader.OpCode.FlowControl == FlowControl.Cond_Branch)
				{
					// Store instruction stack associated with the target.
					int targetOffset = (int)reader.Operand;

					_parseStack.SaveInstructionStack(targetOffset, currblock.Roots);

					treeinst.Operand = branchTargets[targetOffset];
				}

				if (_parseStack.InstructionStack.Count == 0) //FIXME TODO skal det ikke v�re TotalStackSize?
				{
					// It is a root when the instruction stack is empty.
					currblock.Roots.Add(treeinst);
				}

				prevInst = treeinst;
			}

			blocks.Add(currblock);

			foreach (List<MethodVariable> methodVariables in _parseStack.BranchTargetStackVariables.Values)
			{
				variables.AddRange(methodVariables);
			}

			return blocks;
		}

		private static Dictionary<int, IRBasicBlock> CreateBasicBlocks(ILReader reader)
		{
			// Find all branch sources and targets and create basic blocks accordingly.
			Dictionary<int, IRBasicBlock> blocks = new Dictionary<int, IRBasicBlock>();
			blocks.Add(0, new IRBasicBlock(0));

			bool nextInstructionStartsBlock = false;
			while (reader.Read())
			{
				bool createBlock = false;
				int blockOffset = 0;
				if (reader.OpCode.FlowControl == FlowControl.Branch ||
				    reader.OpCode.FlowControl == FlowControl.Cond_Branch)
				{
					createBlock = true;
					blockOffset = (int) reader.Operand;
					nextInstructionStartsBlock = true;
				}
//				else if (nextInstructionStartsBlock)
//				{
//					createBlock = true;
//					blockOffset = reader.Offset;
//					nextInstructionStartsBlock = false;
//				}

				if (createBlock)
				{
					if (!blocks.ContainsKey(blockOffset)) 
						blocks.Add(blockOffset, new IRBasicBlock(blocks.Count));
				}
			}

			return blocks;
		}

		private MethodCallInstruction CreateMethodCallInstruction(IROpCode opcode, OpCode ilOpCode, int offset, MethodBase calledMethod, out int pushcount)
		{
			// Build a method call from the stack.
			MethodBase methodBase = calledMethod;
			int hasThisExtraParam = ((int)(methodBase.CallingConvention & CallingConventions.HasThis) != 0 &&
									 opcode != IROpCodes.Newobj) ? 1 : 0;

			if (_parseStack.TotalStackSize < methodBase.GetParameters().Length + hasThisExtraParam)
				throw new ILSemanticErrorException("Too few parameters on stack.");

			int paramcount = methodBase.GetParameters().Length + hasThisExtraParam;

			TreeInstruction[] arr = new TreeInstruction[paramcount];
			for (int i = 0; i < paramcount; i++)
				arr[paramcount - 1 - i] = _parseStack.Pop();

			MethodInfo methodinfo = methodBase as MethodInfo;
			if (ilOpCode == OpCodes.Newobj || (methodinfo != null && methodinfo.ReturnType != typeof(void)))
				pushcount = 1;
			else
				pushcount = 0;

			MethodCallInstruction mci;
			if (methodBase.IsDefined(typeof(IntrinsicMethodAttribute), false))
			{
				IntrinsicMethodAttribute methodAtt = (IntrinsicMethodAttribute) methodBase.GetCustomAttributes(typeof(IntrinsicMethodAttribute), false)[0];
				IROpCode oc = methodBase.IsConstructor ? IROpCodes.IntrinsicNewObj : IROpCodes.IntrinsicCall;
				mci = new MethodCallInstruction(methodBase, methodAtt.Intrinsic, oc);
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
				if (!methodBase.IsVirtual && opcode == IROpCodes.Callvirt)
					opcode = IROpCodes.Call;

				mci = new MethodCallInstruction(methodBase, opcode);
			}

			mci.Offset = offset;
			mci.Parameters.AddRange(arr);

			return mci;
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
//				case IRCode.Stelem_I8:
//					stindOpcode = IROpCodes.Stind_I8;
//					break;
				case IRCode.Stelem_R4:
					stindOpcode = IROpCodes.Stind_R4;
					break;
//				case IRCode.Stelem_R8:
//					stindOpcode = IROpCodes.Stind_R8;
//					break;
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