using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Runtime.InteropServices;

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
	internal class MethodCompiler : SpuRoutine
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
		public ReadOnlyCollection<MethodParameter> Parameters
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

		public StackTypeDescription ReturnType
		{
			get { return _returnType; }
		}

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
			foreach (ParameterInfo pi in _methodBase.GetParameters())
			{
				Utilities.Assert(pi.Position == i, "pi.Index == i");
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

			_partialEvaluator.Evaluate(this);

			State = MethodCompileState.S2TreeConstructionDone;
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

		#endregion

		#region Instruction selection preparations

		private void PerformInstructionSelectionPreparations()
		{
			if (State != MethodCompileState.S2TreeConstructionDone)
				throw new InvalidOperationException("State != MethodCompileState.S2TreeConstructionDone");

			DetermineEscapes();

			State = MethodCompileState.S3InstructionSelectionPreparationsDone;
		}

		/// <summary>
		/// Determines escapes in the tree and allocates virtual registers to them if they haven't
		/// already got them.
		/// </summary>
		private void DetermineEscapes()
		{
			foreach (MethodVariable var in Variables)
			{
				var.Escapes = false;
				if (var.VirtualRegister == null)
					var.VirtualRegister = NextRegister();
			}
			foreach (MethodParameter p in Parameters)
			{
				p.Escapes = false;
				if (p.VirtualRegister == null)
					p.VirtualRegister = NextRegister();
			}

			Action<TreeInstruction> action =
				delegate(TreeInstruction obj)
					{
						if (obj.Opcode.IRCode == IRCode.Ldarga)
						{
							((MethodParameter) obj.Operand).Escapes = true;
							((MethodParameter) obj.Operand).StackLocation = GetNewSpillOffset();
						}
						else if (obj.Opcode.IRCode == IRCode.Ldloca)
						{
							((MethodVariable) obj.Operand).Escapes = true;
							((MethodVariable) obj.Operand).StackLocation = GetNewSpillOffset();
						}
					};
			ForeachTreeInstruction(action);
		}

		#endregion

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
			_instructions.BeginNewBasicBlock();
			List<VirtualRegister> calleTemps = new List<VirtualRegister>(48);
			for (int regnum = 80; regnum <= 127; regnum++)
			{
				VirtualRegister temp = NextRegister();
				calleTemps.Add(temp);
				_instructions.WriteMove(HardwareRegister.GetHardwareRegister(regnum), temp);
			}

			// Generate the body.
			RecursiveInstructionSelector selector;
			if (_specialSpeObjects != null)
				selector = new RecursiveInstructionSelector(_specialSpeObjects);
			else
				selector = new RecursiveInstructionSelector();

			selector.GenerateCode(Blocks, Parameters, _instructions);

			// Move callee saves temps back to physical regs.
			_instructions.BeginNewBasicBlock();
			for (int regnum = 80; regnum <= 127; regnum++)
			{
				_instructions.WriteMove(calleTemps[regnum - 80], HardwareRegister.GetHardwareRegister(regnum));
			}

			State = MethodCompileState.S4InstructionSelectionDone;
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

		private void PerformRegisterAllocation()
		{
			
			AssertState(MethodCompileState.S5RegisterAllocationDone - 1);

			RegAllocGraphColloring regalloc = new RegAllocGraphColloring();
			regalloc.Alloc(SpuBasicBlocks, GetNewSpillOffset);

//			SimpleRegAlloc regalloc = new SimpleRegAlloc();
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
		/// For the register allocator to use to get SP offsets for spilling.
		/// </summary>
		/// <returns></returns>
		public int GetNewSpillOffset()
		{
			return _nextSpillOffset++;
		}

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


			PerformAddressPatching(bblist, _epilog.BasicBlocks[0]);


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
				prologbin = SpuInstruction.emit(GetPrologWriter().GetAsList());
				bodybin = SpuInstruction.emit(GetBodyWriter().GetAsList());
				epilogbin = SpuInstruction.emit(GetEpilogWriter().GetAsList());
			}
			catch (BadSpuInstructionException e)
			{
				throw new BadSpuInstructionException(
					string.Format("An error occurred during emit for method {0}.", _methodBase.Name), e);
			}

			int[] combined = new int[prologbin.Length + bodybin.Length + epilogbin.Length];
			Utilities.CopyCode(prologbin, 0, combined, 0, prologbin.Length);
			Utilities.CopyCode(bodybin, 0, combined, prologbin.Length, bodybin.Length);
			Utilities.CopyCode(epilogbin, 0, combined, prologbin.Length + bodybin.Length, epilogbin.Length);

			return combined;
		}

		public override IEnumerable<SpuInstruction> GetInstructions()
		{
			if (State < MethodCompileState.S8AddressPatchingDone)
				throw new InvalidOperationException();

			List<SpuInstruction> list = new List<SpuInstruction>();
			list.AddRange(GetPrologWriter().GetAsList());
			list.AddRange(GetBodyWriter().GetAsList());
			list.AddRange(GetEpilogWriter().GetAsList());

			return list;
		}
	}
}