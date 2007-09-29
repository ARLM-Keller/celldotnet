using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;

namespace CellDotNet
{
	/// <summary>
	/// The progressive states that <see cref="MethodCompiler"/> goes through.
	/// </summary>
	internal enum MethodCompileState
	{
		S0None,
		/// <summary>
		/// Before any real processing has been performed.
		/// </summary>
		S1Initial,
		S2TreeConstructionDone,
		/// <summary>
		/// Determine escaping arguments and variables.
		/// </summary>
		S3InstructionSelectionPreparationsDone,
		S4InstructionSelectionDone,
		S5RegisterAllocationDone,
		S6PrologAndEpilogDone,
		S7RemoveRedundantMoves,
		/// <summary>
		/// At this point the only changes that must be done to the code
		/// is address changes.
		/// </summary>
		S8AddressPatchingDone,
		S9Complete
	}

	/// <summary>
	/// Data used during compilation of a method.
	/// </summary>
	internal class MethodCompiler : SpuDynamicRoutine
	{
		private MethodCompileState _state;

		public MethodCompileState State
		{
			get { return _state; }
			private set { _state = value; }
		}

		private List<IRBasicBlock> _blocks = new List<IRBasicBlock>();

		public List<SpuBasicBlock> SpuBasicBlocks
		{
			get
			{
				return GetBodyWriter().BasicBlocks;
			}
		}

		public List<IRBasicBlock> Blocks
		{
			get { return _blocks; }
		}

		public override string Name
		{
			get { return _methodBase.Name; }
		}

		private MethodBase _methodBase;
		public MethodBase MethodBase
		{
			get { return _methodBase; }
		}

		private ReadOnlyCollection<MethodParameter> _parameters;
		public override ReadOnlyCollection<MethodParameter> Parameters
		{
			get { return _parameters; }
		}

		private List<MethodVariable> _variablesMutable;
		private ReadOnlyCollection<MethodVariable> _variables;
		/// <summary>
		/// Local variables.
		/// </summary>
		public ReadOnlyCollection<MethodVariable> Variables
		{
			get { return _variables; }
		}

		public override int Size
		{
			get
			{
				AssertMinimumState(MethodCompileState.S6PrologAndEpilogDone);
				return GetSpuInstructionCount()*4;
			}
		}

		public override StackTypeDescription ReturnType
		{
			get { return _returnType; }
		}

		private bool _naked = true;

		public bool Naked
		{
			get { return _naked; }
			set { _naked = value; }
		}

		private Dictionary<VirtualRegister, int> registerWeight = new Dictionary<VirtualRegister, int>();

		public MethodCompiler(MethodBase method)
		{
			_methodBase = method;
			State = MethodCompileState.S1Initial;

			Dictionary<MethodInfo, int> fixedMethods = new Dictionary<MethodInfo, int>();
			fixedMethods.Add(typeof(SpuRuntime).GetProperty("IsRunningOnSpu").GetGetMethod(), 1);
			_partialEvaluator = new PartialEvaluator(fixedMethods);

			PerformIRTreeConstruction();
		}

		public void ForeachTreeInstruction(Action<TreeInstruction> action)
		{
			IRBasicBlock.ForeachTreeInstruction(Blocks, action);
		}

		public IEnumerable<TreeInstruction> EnumerateTreeInstructions()
		{
			return IRBasicBlock.EnumerateTreeInstructions(Blocks);
		}

		private void AssertMinimumState(MethodCompileState requiredState)
		{
			if (State < requiredState)
				throw new InvalidOperationException(string.Format("Operation is invalid for the current state. " +
					"Current state: {0}; required minimum state: {1}.", State, requiredState));
		}


		private void AssertState(MethodCompileState requiredState)
		{
			if (State != requiredState)
				throw new InvalidOperationException(string.Format("Operation is invalid for the current state. " +
					"Current state: {0}; required state: {1}.", State, requiredState));
		}

		private SpecialSpeObjects _specialSpeObjects;
		public void SetRuntimeSettings(SpecialSpeObjects specialSpeObjects)
		{
			Utilities.AssertArgumentNotNull(specialSpeObjects, "specialSpeObjects");
			if (_specialSpeObjects != null)
				throw new InvalidOperationException("specialSpeObjects != null");
			_specialSpeObjects = specialSpeObjects;
		}

		#region IR tree construction

		private void PerformIRTreeConstruction()
		{
			AssertState(MethodCompileState.S2TreeConstructionDone - 1);

			TypeDeriver typederiver = new TypeDeriver();


			// Build Parameters.
			List<MethodParameter> parlist = new List<MethodParameter>();
			int i = 0;

			if ((_methodBase.CallingConvention & CallingConventions.HasThis) != 0)
			{
				StackTypeDescription type = new StackTypeDescription(new TypeDescription(_methodBase.DeclaringType));

				parlist.Add(new MethodParameter(type.GetManagedPointer()));
				i++;
			}

			foreach (ParameterInfo pi in _methodBase.GetParameters())
			{
				//Not true in instance methods.
//				Utilities.Assert(pi.Position == i, "pi.Index == i");

				Utilities.Assert(pi.Position == i - ((_methodBase.CallingConvention & CallingConventions.HasThis) != 0 ? 1 : 0), "pi.Index == i");
				i++;
					
				parlist.Add(new MethodParameter(pi, typederiver.GetStackTypeDescription(pi.ParameterType)));
			}
			_parameters = new ReadOnlyCollection<MethodParameter>(parlist);


			// Build Variables.
			List<MethodVariable> varlist = new List<MethodVariable>();
			i = 0;
			foreach (LocalVariableInfo lv in _methodBase.GetMethodBody().LocalVariables)
			{
				Utilities.Assert(lv.LocalIndex == i, "lv.LocalIndex == i");
				i++;

				varlist.Add(new MethodVariable(lv, typederiver.GetStackTypeDescription(lv.LocalType)));
			}
			_variables = new ReadOnlyCollection<MethodVariable>(varlist);
			_variablesMutable = varlist;

			if (_methodBase is MethodInfo)
			{
				MethodInfo mi = (MethodInfo) _methodBase;
				_returnType = typederiver.GetStackTypeDescription(mi.ReturnType);
			}



			ILReader reader = new ILReader(_methodBase);
			try
			{
				_blocks = new IRTreeBuilder().BuildBasicBlocks(MethodBase, reader, _variablesMutable, _parameters);
			}
			catch (ILParseException e)
			{
				throw new ILParseException(string.Format("An error occurred while parsing method '{0}.{1}'.", 
					_methodBase.DeclaringType.Name, _methodBase.Name), e);
			}
			CheckTreeInstructionCountIsMinimum(reader.InstructionsRead);

			PatchSystemLib();

			// This one should be before escape determination, since some of the address ops might be removed.
			RemoveAddressOperations();

			DetermineEscapes();

			_partialEvaluator.Evaluate(this);


			State = MethodCompileState.S2TreeConstructionDone;
		}

		/// <summary>
		/// Removes opcodes which take addresses of instances of well-known immutable structs which we would like
		/// to stay in registers: The vector types.
		/// </summary>
		private void RemoveAddressOperations()
		{
			ForeachTreeInstruction(
				delegate(TreeInstruction obj)
					{
						// Replace instance method calls (ld*, call) on vector types with (ldarg, ldobj, call)
						// so that vector instance methods operate on values instead of pointers.

						MethodCallInstruction mci = obj as MethodCallInstruction;
						if (mci != null && (mci.Opcode == IROpCodes.IntrinsicMethod || mci.Opcode == IROpCodes.SpuInstructionMethod) &&
							!mci.IntrinsicMethod.IsStatic && !mci.IntrinsicMethod.IsConstructor)
						{
							MethodBase method = mci.IntrinsicMethod;
							TreeInstruction ldthis = mci.Parameters[0];
							StackTypeDescription thistype = ldthis.StackType;

							bool isDefiningTypeInstanceMethodCall = method.DeclaringType == ldthis.OperandAsVariable.ReflectionType;
							bool canRemoveAddressOp = isDefiningTypeInstanceMethodCall && ldthis.OperandAsVariable != null && 
								thistype.IsManagedPointer && thistype.Dereference().IsImmutableSingleRegisterType;

							if (!canRemoveAddressOp)
								return;

							if (ldthis.Opcode == IROpCodes.Ldloca || ldthis.Opcode == IROpCodes.Ldarga)
							{
								// Load value instead of address.
								MethodVariable var = ldthis.OperandAsVariable;
								if (ldthis.Opcode == IROpCodes.Ldloca)
									ldthis = new TreeInstruction(IROpCodes.Ldloc);
								else
									ldthis = new TreeInstruction(IROpCodes.Ldarg);
								ldthis.Operand = var;

								mci.Parameters[0] = ldthis;
							}
							else
							{
								Utilities.Assert(ldthis.Opcode == IROpCodes.Ldloc || ldthis.Opcode == IROpCodes.Ldarg,
									"ldthis.Opcode == IROpCodes.Ldloc || ldthis.Opcode == IROpCodes.Ldarg");

								// Insert an ldobj before the method call.
								TreeInstruction ldobj = new TreeInstruction(IROpCodes.Ldobj);
								ldobj.Operand = thistype.Dereference();
								ldobj.Left = ldthis;

								mci.Parameters[0] = ldobj;
							}
						}
					});
		}

		/// <summary>
		/// Checks that the number of instructions in the constructed tree is equal to the number of IL instructions in the cecil model.
		/// </summary>
		/// <param name="minimumCount"></param>
		private void CheckTreeInstructionCountIsMinimum(int minimumCount)
		{
			int count = 0;
			ForeachTreeInstruction(delegate
			{
				count += 1;
			});

			if (count < minimumCount)
			{
				TreeDrawer td= new TreeDrawer();
				td.DrawMethod(this);
				string msg = string.Format("Invalid tree instruction count of {0} for method {2}. Should have been {1}.", 
					count, minimumCount, MethodBase.Name);
				throw new Exception(msg);
			}
		}

		//TODO nameing
		private void PatchSystemLib()
		{
			ForeachTreeInstruction(
				delegate(TreeInstruction obj)
					{
						MethodBase mb = obj.Operand as MethodBase;
						if(mb != null)
						{
							obj.Operand = SystemLibMap.GetUseableMethodBase(mb);
						}

					});
		}

		#endregion

		#region Instruction selection preparations

		// TODO to be removed
		private void PerformInstructionSelectionPreparations()
		{
			if (State != MethodCompileState.S2TreeConstructionDone)
				throw new InvalidOperationException("State != MethodCompileState.S2TreeConstructionDone");

			State = MethodCompileState.S3InstructionSelectionPreparationsDone;
		}

		/// <summary>
		/// Determines escapes in the tree and allocates virtual registers to them.
		/// </summary>
		private void DetermineEscapes()
		{
			foreach (MethodVariable var in Variables)
			{
				var.Escapes = false;
				if (var.VirtualRegister == null)
					var.VirtualRegister = NextRegister();

				// Custom mutable structs always go on the stack.
				if (var.StackType == StackTypeDescription.ValueType && !var.StackType.IsImmutableSingleRegisterType)
				{
					var.Escapes = true;
					var.StackLocation = GetNewSpillQuadOffset(var.StackType.ComplexType.QuadWordCount);
				}
			}

			foreach (MethodParameter p in Parameters)
			{
				p.Escapes = false;

				// The linear register allocator will move physical argument registers into these virtual registers.
				p.VirtualRegister = NextRegister();
			}

			Action<TreeInstruction> action =
				delegate(TreeInstruction obj)
					{
						if (obj.Opcode.IRCode == IRCode.Ldarga)
						{
							((MethodParameter) obj.Operand).Escapes = true;
							if (((MethodParameter) obj.Operand).StackLocation == 0)
								((MethodParameter) obj.Operand).StackLocation = GetNewSpillQuadOffset();
						}
						else if (obj.Opcode.IRCode == IRCode.Ldloca)
						{
							((MethodVariable) obj.Operand).Escapes = true;
							if (((MethodVariable)obj.Operand).StackLocation == 0)
								((MethodVariable) obj.Operand).StackLocation = GetNewSpillQuadOffset();
						}
					};
			ForeachTreeInstruction(action);
		}

		#endregion

		private SpuBasicBlock _innerEpilog = new SpuBasicBlock();

		private int _virtualRegisterNum = -1000; // Arbitrary...
		private VirtualRegister NextRegister()
		{
			return new VirtualRegister(_virtualRegisterNum++);
		}

		private void PerformInstructionSelection()
		{
			AssertState(MethodCompileState.S4InstructionSelectionDone - 1);

			_instructions = new SpuInstructionWriter();

			// Move calle-saves regs to virtual regs.
			int calleeSavesRegisterCount = HardwareRegister.CalleeSavesRegisters.Count;
			List<VirtualRegister> calleTemps = new List<VirtualRegister>(calleeSavesRegisterCount);
			if (!_naked)
			{
				_instructions.BeginNewBasicBlock();
				foreach (VirtualRegister register in HardwareRegister.CalleeSavesRegisters)
				{
					VirtualRegister temp = NextRegister();
					registerWeight.Add(temp, 5);
					calleTemps.Add(temp);
					_instructions.WriteMove(register, temp);
				}

//				for (int regnum = 80; regnum <= 127; regnum++)
//				{
//					VirtualRegister temp = NextRegister();
//					calleTemps.Add(temp);
//					_instructions.WriteMove(HardwareRegister.GetHardwareRegister(regnum), temp);
//				}
			}

			// Generate the body.
			RecursiveInstructionSelector selector;
			if (_specialSpeObjects != null)
				selector = new RecursiveInstructionSelector(_specialSpeObjects);
			else
				selector = new RecursiveInstructionSelector();

			selector.GenerateCode(Blocks, Parameters, _instructions);

			// Move callee saves temps back to physical regs.
			if (!_naked)
			{
				_instructions.BeginNewBasicBlock();
				for (int i = calleeSavesRegisterCount-1; i >= 0; i--)
				{
					_instructions.WriteMove(calleTemps[i], HardwareRegister.GetHardwareRegister(i+80));
				}
//				for (int regnum = 80; regnum <= 127; regnum++)
//				{
//					_instructions.WriteMove(calleTemps[regnum - 80], HardwareRegister.GetHardwareRegister(regnum));
//				}
			}

			State = MethodCompileState.S4InstructionSelectionDone;
		}

		private void PerformInstructionOptimazation()
		{
			List<SpuInstruction> jumpInsts = new List<SpuInstruction>();

			Dictionary<SpuBasicBlock, Set<SpuInstruction>> branchSourcesForConditionalBranch = new Dictionary<SpuBasicBlock, Set<SpuInstruction>>();
			Dictionary<SpuBasicBlock, List<SpuInstruction>> branchSources = new Dictionary<SpuBasicBlock, List<SpuInstruction>>();

			// Maps to basic block number.
			List<int> posibleRemoveableBranche = new List<int>();


			for (int i = 0; i < SpuBasicBlocks.Count; i++ )
			{
				SpuBasicBlock block = SpuBasicBlocks[i];

				SpuInstruction inst = block.Head;

				while (inst != null)
				{
					if(inst.JumpTarget != null)
					{
						int target = SpuBasicBlocks.IndexOf(inst.JumpTarget);
						// Looks for the firste non empty basic block
						while (target < SpuBasicBlocks.Count && SpuBasicBlocks[target].Head == null)
							target++;

						if (i < SpuBasicBlocks.Count)
						{
							SpuInstruction targetInst = SpuBasicBlocks[target].Head;

							if(targetInst.OpCode == SpuOpCode.br || targetInst.OpCode == SpuOpCode.ret)
							{
								posibleRemoveableBranche.Add(target);

								if (targetInst.OpCode == SpuOpCode.ret)
										targetInst.JumpTarget = _innerEpilog;

								if(!branchSourcesForConditionalBranch.ContainsKey(SpuBasicBlocks[target]))
									branchSourcesForConditionalBranch[SpuBasicBlocks[target]] = new Set<SpuInstruction>();

								branchSourcesForConditionalBranch[SpuBasicBlocks[target]].Add(inst);
							}
						}
					}
				}
			}

			while (branchSourcesForConditionalBranch.Count != 0)
			{
				SpuBasicBlock targetBlock = Utilities.GetFirst(branchSourcesForConditionalBranch.Keys);

				while(branchSourcesForConditionalBranch[targetBlock].Count != 0)
				{
					SpuInstruction srcInst = Utilities.GetFirst(branchSourcesForConditionalBranch[targetBlock]);

					SpuBasicBlock newTarget = targetBlock.Head.JumpTarget;

					srcInst.JumpTarget = newTarget;

					branchSourcesForConditionalBranch[targetBlock].Remove(srcInst);

					if (branchSourcesForConditionalBranch[targetBlock].Count == 0)
						branchSourcesForConditionalBranch.Remove(targetBlock);

					if (branchSourcesForConditionalBranch.ContainsKey(newTarget))
						branchSourcesForConditionalBranch[newTarget].Add(srcInst);
				}
			}

			posibleRemoveableBranche.Sort();
			posibleRemoveableBranche.Reverse();

			foreach (int i in posibleRemoveableBranche)
				if(i + 1 < SpuBasicBlocks.Count && SpuBasicBlocks[i].Head.JumpTarget == SpuBasicBlocks[i+1])
					SpuBasicBlocks.RemoveAt(i);
		}


		/// <summary>
		/// This is only for unit test
		/// </summary>
		/// <returns></returns>
		public SpuInstructionWriter GetBodyWriter()
		{
			if (State < MethodCompileState.S4InstructionSelectionDone)
				throw new InvalidOperationException("State < MethodCompileState.S4InstructionSelectionDone");

			return _instructions;
		}

		/// <summary>
		/// Return the number of instructions currently in the prolog, body and epilog.
		/// </summary>
		/// <returns></returns>
		public int GetSpuInstructionCount()
		{
			if (State < MethodCompileState.S4InstructionSelectionDone)
				throw new InvalidOperationException("Too early. State: " + State);

			int count = 
				_prolog.GetInstructionCount() + 
				_instructions.GetInstructionCount() + 
				_epilog.GetInstructionCount();

			return count;
		}

		/// <summary>
		/// Brings the compiler process up to the specified state.
		/// </summary>
		/// <param name="targetState"></param>
		public void PerformProcessing(MethodCompileState targetState)
		{
			if (State >= targetState)
				return; // Already there...

			if (State < MethodCompileState.S2TreeConstructionDone && targetState >= MethodCompileState.S2TreeConstructionDone)
				PerformIRTreeConstruction();

			if (State < MethodCompileState.S3InstructionSelectionPreparationsDone && targetState >= MethodCompileState.S3InstructionSelectionPreparationsDone)
				PerformInstructionSelectionPreparations();

			if (State < MethodCompileState.S4InstructionSelectionDone && targetState >= MethodCompileState.S4InstructionSelectionDone)
				PerformInstructionSelection();

			if (State < MethodCompileState.S5RegisterAllocationDone && targetState >= MethodCompileState.S5RegisterAllocationDone)
				PerformRegisterAllocation();

			if (State < MethodCompileState.S6PrologAndEpilogDone && targetState >= MethodCompileState.S6PrologAndEpilogDone)
				PerformPrologAndEpilogGeneration();

			if (State < MethodCompileState.S7RemoveRedundantMoves && targetState >= MethodCompileState.S7RemoveRedundantMoves)
				PerformRemoveRedundantMoves();

			if (State < MethodCompileState.S8AddressPatchingDone && targetState >= MethodCompileState.S8AddressPatchingDone)
				PerformAddressPatching();

			if (targetState >= MethodCompileState.S9Complete)
			{
				if (targetState <= MethodCompileState.S9Complete) 
					throw new NotImplementedException("Target state: " + targetState);
				else 
					throw new ArgumentException("Invalid state: " + targetState, "targetState");
			}
		}

		#region Register allocation

		private void PerformRegisterAllocation()
		{
			AssertState(MethodCompileState.S5RegisterAllocationDone - 1);


//			RegAllocGraphColloring regalloc = new RegAllocGraphColloring();
//			regalloc.Alloc(SpuBasicBlocks, GetNewSpillQuadOffset, registerWeight);

//			Console.WriteLine("Disassemble before register allocation:");
//			Disassembler.DisassembleUnconditionalToConsole(this);

			new LinearRegisterAllocator().Allocate(SpuBasicBlocks, GetNewSpillQuadOffset, _innerEpilog);

//			Console.WriteLine("Disassemble after register allocation:");
//			Disassembler.DisassembleUnconditionalToConsole(this);

//			SimpleRegAlloc.Alloc(SpuBasicBlocks, GetNewSpillQuadOffset);

//			List<SpuInstruction> asm = _instructions.GetAsList();
//			regalloc.alloc(asm, 16);

			State = MethodCompileState.S5RegisterAllocationDone;
			
		}

		private void PerformRemoveRedundantMoves()
		{
			AssertState(MethodCompileState.S7RemoveRedundantMoves - 1);

			RegAllocGraphColloring.RemoveRedundantMoves(_prolog.BasicBlocks);
			RegAllocGraphColloring.RemoveRedundantMoves(SpuBasicBlocks);
			RegAllocGraphColloring.RemoveRedundantMoves(_epilog.BasicBlocks);

			State = MethodCompileState.S7RemoveRedundantMoves;
		}

		private int _nextSpillOffset = 2; // Start by pointing to start of Local Variable Space.

		/// <summary>
		/// For escaping variables and for the register allocator to use to get SP offsets for spilling.
		/// </summary>
		/// <returns></returns>
		public int GetNewSpillQuadOffset()
		{
			return _nextSpillOffset++;
		}

		public int GetNewSpillQuadOffset(int count)
		{
			int offset = _nextSpillOffset;
			_nextSpillOffset += count;
			return offset;
		}

		#endregion

		#region Prolog/epilog
		SpuInstructionWriter _prolog;
		SpuInstructionWriter _epilog;

		private void PerformPrologAndEpilogGeneration()
		{
			AssertState(MethodCompileState.S6PrologAndEpilogDone - 1);

			_prolog = new SpuInstructionWriter();
			_prolog.BeginNewBasicBlock();
			WriteProlog(_prolog);

			_epilog = new SpuInstructionWriter();
			_epilog.BeginNewBasicBlock();
			SpuAbiUtilities.WriteEpilog(_epilog);

			_state = MethodCompileState.S6PrologAndEpilogDone;
		}

		/// <summary>
		/// Writes outer prolog.
		/// </summary>
		/// <param name="prolog"></param>
		private void WriteProlog(SpuInstructionWriter prolog)
		{
			// TODO: Store caller-saves registers that this method uses, based on negative offsts
			// from the caller's SP. Set GRSA_slots.


			// Number of 16 byte slots in the frame.
			int RASA_slots = 0; // Register argument save area. (vararg)
			int GRSA_slots = 0; // General register save area. (non-volatile registers)
			int PLA_slots = 0; // Parameter list area. (more than 72 argument registers)
			int LVS_slots = _nextSpillOffset - 2 - PLA_slots; // Local variable space. (escapes and spills)
			int frameSlots = RASA_slots + GRSA_slots + LVS_slots + PLA_slots + 2;

			// First/topmost caller-saves register caller SP slot offset.
//			int first_GRSA_slot_offset = -(RASA_slots + 1);

			SpuAbiUtilities.WriteProlog(frameSlots, prolog, _specialSpeObjects != null ? _specialSpeObjects.StackOverflow : null);
		}

		/// <summary>
		/// For unit testing.
		/// </summary>
		/// <returns></returns>
		public SpuInstructionWriter GetPrologWriter()
		{
			return _prolog;
		}

		/// <summary>
		/// For unit testing.
		/// </summary>
		/// <returns></returns>
		public SpuInstructionWriter GetEpilogWriter()
		{
			return _epilog;
		}


		#endregion

		/// <summary>
		/// Inserts offsets for local branches and call. That is, for instrukctions containing 
		/// <see cref="SpuBasicBlock"/> and <see cref="ObjectWithAddress"/> objects.
		/// </summary>
		public override void PerformAddressPatching()
		{
			AssertState(MethodCompileState.S8AddressPatchingDone - 1);

			// Iterate bbs, instructions to determine bb offsets and collect branch instructions,
			// so that the branch instructions afterwards can be patched with the bb addresses.

			Utilities.Assert(_prolog.BasicBlocks.Count > 0, "_prolog.BasicBlocks.Count == 0");
			Utilities.Assert(_epilog.BasicBlocks.Count > 0, "_epilog.BasicBlocks.Count == 0");

			List<SpuBasicBlock> bblist = new List<SpuBasicBlock>();
			bblist.Add(_prolog.BasicBlocks[0]);
			bblist.AddRange(_instructions.BasicBlocks);
			bblist.Add(_epilog.BasicBlocks[0]);


			// The 2nd last basicblock contains the inner epilog(restor callee saves register), 
			// which is generated in the register allocator.
			PerformAddressPatching(bblist, bblist[bblist.Count - 2]);


			State = MethodCompileState.S8AddressPatchingDone;
		}

		private SpuInstructionWriter _instructions;
		private StackTypeDescription _returnType;
		private PartialEvaluator _partialEvaluator;

		public override int[] Emit()
		{
			int[] prologbin;
			int[] bodybin;
			int[] epilogbin;

			try
			{
				prologbin = SpuInstruction.Emit(GetPrologWriter().GetAsList());
				bodybin = SpuInstruction.Emit(GetBodyWriter().GetAsList());
				epilogbin = SpuInstruction.Emit(GetEpilogWriter().GetAsList());
			}
			catch (BadSpuInstructionException e)
			{
				throw new BadSpuInstructionException(
					string.Format("An error occurred during Emit for method {0}.", _methodBase.Name), e);
			}

			int[] combined = new int[prologbin.Length + bodybin.Length + epilogbin.Length];
			Utilities.CopyCode(prologbin, 0, combined, 0, prologbin.Length);
			Utilities.CopyCode(bodybin, 0, combined, prologbin.Length, bodybin.Length);
			Utilities.CopyCode(epilogbin, 0, combined, prologbin.Length + bodybin.Length, epilogbin.Length);

			return combined;
		}

		public override IEnumerable<SpuInstruction> GetFinalInstructions()
		{
			if (State < MethodCompileState.S8AddressPatchingDone)
				throw new InvalidOperationException();

			List<SpuInstruction> list = new List<SpuInstruction>();
			list.AddRange(GetPrologWriter().GetAsList());
			list.AddRange(GetBodyWriter().GetAsList());
			list.AddRange(GetEpilogWriter().GetAsList());

			return list;
		}

		public override IEnumerable<SpuInstruction> GetInstructions()
		{
			List<SpuInstruction> list = new List<SpuInstruction>();

//			list.AddRange(GetPrologWriter().GetAsList());
			list.AddRange(GetBodyWriter().GetAsList());
//			list.AddRange(GetEpilogWriter().GetAsList());

			return list;
		}

	}
}