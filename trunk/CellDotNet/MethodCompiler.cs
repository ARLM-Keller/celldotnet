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
	/// Returns an offset (measured in quadwords) from the stack pointer of unused space.
	/// </summary>
	/// <param name="quadWordCount"></param>
	/// <returns></returns>
	internal delegate int StackSpaceAllocator(int quadWordCount);

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


			PerformIRTreeConstruction();
		}

		public void ForeachTreeInstruction(Action<TreeInstruction> action)
		{
			IRBasicBlock.ForeachTreeInstruction(Blocks, action);
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

				StackTypeDescription thistype;
				if (type.IndirectionLevel == 0 && type.CliType == CliType.ValueType)
					thistype = type.GetManagedPointer();
				else
					thistype = type;

				parlist.Add(new MethodParameter(thistype));
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
			catch (NotImplementedException e)
			{
				throw new ILParseException(string.Format("An error occurred while parsing method '{0}.{1}'.", 
					_methodBase.DeclaringType.Name, _methodBase.Name), e);
			}
			catch (ILParseException e)
			{
				throw new ILParseException(string.Format("An error occurred while parsing method '{0}.{1}'.", 
					_methodBase.DeclaringType.Name, _methodBase.Name), e);
			}

			PatchSystemLib();

//			PatchDivOperator();

			// This one should be before escape determination, since some of the address ops might be removed.
			RemoveAddressOperations();

			DetermineEscapesAndAllocateStackVariables();

			IRBasicBlock.ConvertTreeInstructions(Blocks, DivConverter);

			State = MethodCompileState.S2TreeConstructionDone;
		}

		/// <summary>
		/// Removes opcodes which take addresses of instances of well-known immutable structs which we would like
		/// to stay in registers: The vector types.
		/// </summary>
		private void RemoveAddressOperations()
		{
			// Replace vector types constructor "call"s (which use ldloca) with newobj and stloc.
			// The ctor call will be a root.
			foreach (IRBasicBlock bb in Blocks)
			{
				for (int i = 0; i < bb.Roots.Count; i++)
				{
					TreeInstruction root = bb.Roots[i];

					MethodCallInstruction mci = root as MethodCallInstruction;
					bool isConstructorCall = mci != null && root.Opcode == IROpCodes.IntrinsicNewObj;
					if (!isConstructorCall)
						continue;

					Utilities.Assert(mci.Parameters.Count >= 1, "mci.Parameters.Count >= 1");

					TreeInstruction ldthis = mci.Parameters[0];

					Utilities.Assert(ldthis.StackType.IndirectionLevel == 1 && 
						ldthis.StackType.Dereference().IsImmutableSingleRegisterType, 
						"ldthis.StackType.IndirectionLevel == 1");

					// Remove the address load and insert a store instead.

					mci.Parameters.RemoveAt(0);
					TreeInstruction newRootStoreInst;
					if (ldthis.Opcode == IROpCodes.Ldloca || ldthis.Opcode == IROpCodes.Ldarga)
					{
						if (ldthis.Opcode == IROpCodes.Ldloca)
							newRootStoreInst = new TreeInstruction(IROpCodes.Stloc);
						else
							newRootStoreInst = new TreeInstruction(IROpCodes.Starg);

						MethodVariable thisvar = ldthis.OperandAsVariable;
						newRootStoreInst.Operand = thisvar;
						newRootStoreInst.Left = mci;
					}
					else
					{
						newRootStoreInst = new TreeInstruction(IROpCodes.Stobj);
						newRootStoreInst.Left = ldthis;
						newRootStoreInst.Right = mci;
					}

					bb.Roots[i] = newRootStoreInst;
				}
			}

			ForeachTreeInstruction(RemoveAddressOperations);
		}

		private void RemoveAddressOperations(TreeInstruction obj)
		{
			MethodCallInstruction mci = obj as MethodCallInstruction;
			if (mci != null && (mci.Opcode == IROpCodes.IntrinsicCall || mci.Opcode == IROpCodes.SpuInstructionMethod) &&
			    !mci.IntrinsicMethod.IsStatic && !mci.IntrinsicMethod.IsConstructor)
			{
				// Replace instance method calls (ld(loc|arg)(a), call) on vector types with (ld(loc|arg), ldobj, call) or
				// (ldarg, ldobj, call) for ref arguments so that vector instance methods operate on values instead of pointers.

				MethodBase method = mci.IntrinsicMethod;
				TreeInstruction ldthis = mci.Parameters[0];
				StackTypeDescription thistype = ldthis.StackType;

//				bool isDefiningTypeInstanceMethodCall = method.DeclaringType == ldthis.OperandAsVariable.ReflectionType;
				bool canRemoveAddressOp = ldthis.OperandAsVariable != null && method.DeclaringType == ldthis.OperandAsVariable.ReflectionType &&
				                          thistype.IndirectionLevel == 1 && thistype.Dereference().IsImmutableSingleRegisterType;

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
		}

		//TODO nameing
		private void PatchSystemLib()
		{
			ForeachTreeInstruction(
				delegate(TreeInstruction obj)
					{
						MethodBase mb = obj.OperandAsMethod;
						if(mb != null)
						{
							obj.Operand = SystemLibMap.GetUseableMethodBase(mb);
						}

					});
		}

		private void PatchDivOperator()
		{
			foreach (IRBasicBlock block in Blocks)
			{
				for(int r = 0; r < block.Roots.Count; r++)
				{
					TreeInstruction root = block.Roots[r];
					TreeInstruction newRoot =  PatchDivOperator(root);
					if(newRoot != null)
					{
						block.Roots[r] = newRoot;
					}

				}
			}
		}

		private static TreeInstruction DivConverter(TreeInstruction inst)
		{
			MethodBase div_un_mb = new Func<uint, uint, uint>(SpuMath.Div_Un).Method;
			MethodBase div_mb = new Func<int, int, int>(SpuMath.Div).Method;

			MethodBase rem_un_mb = new Func<uint, uint, uint>(SpuMath.Rem_Un).Method;
			MethodBase rem_mb = new Func<int, int, int>(SpuMath.Rem).Method;
			
			MethodCallInstruction newInst = null;

			if (inst.Opcode == IROpCodes.Div)
			{
				if ((inst.Left.StackType == StackTypeDescription.Int32 || inst.Left.StackType == StackTypeDescription.NativeInt) &&
					(inst.Right.StackType == StackTypeDescription.Int32 || inst.Right.StackType == StackTypeDescription.NativeInt))
				{
					newInst = new MethodCallInstruction(div_mb, IROpCodes.Call);

					newInst.Parameters.AddRange(inst.GetChildInstructions());
					newInst.Offset = inst.Offset;
				}
				else if (inst.Left.StackType == StackTypeDescription.Float32 && inst.Right.StackType == StackTypeDescription.Float32)
				{
					// TODO
				}
			}
			else if (inst.Opcode == IROpCodes.Div_Un)
			{
				if ((inst.Left.StackType == StackTypeDescription.Int32 || inst.Left.StackType == StackTypeDescription.NativeInt) &&
					(inst.Right.StackType == StackTypeDescription.Int32 || inst.Right.StackType == StackTypeDescription.NativeInt))
				{
					newInst = new MethodCallInstruction(div_un_mb, IROpCodes.Call);

					newInst.Parameters.AddRange(inst.GetChildInstructions());
					newInst.Offset = inst.Offset;
				}
				else if (inst.Left.StackType == StackTypeDescription.Float32 && inst.Right.StackType == StackTypeDescription.Float32)
				{
					// TODO
				}
			}
			else if (inst.Opcode == IROpCodes.Rem)
			{
				if ((inst.Left.StackType == StackTypeDescription.Int32 || inst.Left.StackType == StackTypeDescription.NativeInt) &&
					(inst.Right.StackType == StackTypeDescription.Int32 || inst.Right.StackType == StackTypeDescription.NativeInt))
				{
					newInst = new MethodCallInstruction(rem_mb, IROpCodes.Call);

					newInst.Parameters.AddRange(inst.GetChildInstructions());
					newInst.Offset = inst.Offset;
				}
				else if (inst.Left.StackType == StackTypeDescription.Float32 && inst.Right.StackType == StackTypeDescription.Float32)
				{
					// TODO
				}
			}
			else if (inst.Opcode == IROpCodes.Rem)
			{
				if ((inst.Left.StackType == StackTypeDescription.Int32 || inst.Left.StackType == StackTypeDescription.NativeInt) &&
					(inst.Right.StackType == StackTypeDescription.Int32 || inst.Right.StackType == StackTypeDescription.NativeInt))
				{
					newInst = new MethodCallInstruction(rem_un_mb, IROpCodes.Call);

					newInst.Parameters.AddRange(inst.GetChildInstructions());
					newInst.Offset = inst.Offset;
				}
			}

			if (newInst != null)
				new TypeDeriver().DeriveType(newInst);

			return newInst;
		}

		private static TreeInstruction PatchDivOperator(TreeInstruction root)
		{
			if (root == null)
				return null;

			TreeInstruction newRoot = null;

			Stack<TreeInstruction> parrentlist = new Stack<TreeInstruction>();
			Stack<int> chieldIndexList = new Stack<int>();

			TreeInstruction parrent = null;
			int chieldIndex = 0;

			TreeInstruction inst = root;

			do
			{
				// DO som matching
				if(inst.Opcode == IROpCodes.Div)
				{
					if ((inst.Left.StackType == StackTypeDescription.Int32 || inst.Left.StackType == StackTypeDescription.NativeInt) &&
						(inst.Right.StackType == StackTypeDescription.Int32 || inst.Right.StackType == StackTypeDescription.NativeInt))
					{
						MethodBase mb = typeof(SpuMath).GetMethod("Div", new Type[] { typeof(int), typeof(int) });
						MethodCallInstruction newInst = new MethodCallInstruction(mb, IROpCodes.Call);

						newInst.Parameters.AddRange(inst.GetChildInstructions());

						if (parrent != null)
							parrent.ReplaceChild(chieldIndex, newInst);
						else
							newRoot = newInst;
					}
					else if (inst.Left.StackType == StackTypeDescription.Float32 && inst.Right.StackType == StackTypeDescription.Float32)
					{
						// TODO
					}
				}
				else if (inst.Opcode == IROpCodes.Div_Un)
				{
					if ((inst.Left.StackType == StackTypeDescription.Int32 || inst.Left.StackType == StackTypeDescription.NativeInt) &&
						(inst.Right.StackType == StackTypeDescription.Int32 || inst.Right.StackType == StackTypeDescription.NativeInt))
					{
						MethodBase mb = typeof(SpuMath).GetMethod("Div_Un", new Type[] { typeof(uint), typeof(uint) });
						MethodCallInstruction newInst = new MethodCallInstruction(mb, IROpCodes.Call);

						newInst.Parameters.AddRange(inst.GetChildInstructions());

						if (parrent != null)
							parrent.ReplaceChild(chieldIndex, newInst);
						else
							newRoot = newInst;
					}
					else if (inst.Left.StackType == StackTypeDescription.Float32 && inst.Right.StackType == StackTypeDescription.Float32)
					{
						// TODO
					}
				}

				// Go to the nest instruction.
				if (inst.GetChildInstructions().Length > 0)
				{
					parrentlist.Push(parrent);
					chieldIndexList.Push(chieldIndex);

					parrent = inst;
					chieldIndex = 0;

					inst = inst.GetChildInstructions()[0];
				}
				else if (parrent != null && ++chieldIndex < parrent.GetChildInstructions().Length)
				{
				}
				else if (parrent != null)
				{
					parrent = parrentlist.Pop();
					chieldIndex = chieldIndexList.Pop();
//					parrent = parrentlist.Peek();
//					chieldIndex = chieldIndexList.Peek();
				}
			} while (parrent != null);
			return newRoot;
		}

//		private void ForeachTreeInstruction(Converter<TreeInstruction, TreeInstruction> converter)
//		{
//			foreach (IRBasicBlock block in Blocks)
//			{
//				for (int r = 0; r < block.Roots.Count; r++)
//				{
//					TreeInstruction root = block.Roots[r];
//					TreeInstruction newRoot = ForeachTreeInstruction(root, converter);
//					if (newRoot != null)
//					{
//						block.Roots[r] = newRoot;
//					}
//
//				}
//			}
//		}

//		private static TreeInstruction ForeachTreeInstruction(TreeInstruction root, Converter<TreeInstruction, TreeInstruction> converter)
//		{
//			if (root == null)
//				return null;
//
//			TreeInstruction newRoot = null;
//
//			Stack<TreeInstruction> parrentlist = new Stack<TreeInstruction>();
//			Stack<int> chieldIndexList = new Stack<int>();
//
//			TreeInstruction parrent = null;
//			int chieldIndex = 0;
//
//			TreeInstruction inst = root;
//
//			do
//			{
//				TreeInstruction newInst = converter(inst);
//
//				if(newInst != null)
//				{
//					inst = newInst;
//					if (parrent != null)
//						parrent.ReplaceChild(chieldIndex, newInst);
//					else
//						newRoot = newInst;
//				}
//
//				// Go to the nest instruction.
//				if (inst.GetChildInstructions().Length > 0)
//				{
//					parrentlist.Push(parrent);
//					chieldIndexList.Push(chieldIndex);
//
//					parrent = inst;
//					chieldIndex = 0;
//
//					inst = inst.GetChildInstructions()[0];
//				}
//				else if (parrent != null && ++chieldIndex < parrent.GetChildInstructions().Length)
//				{
//				}
//				else if (parrent != null)
//				{
//					parrent = parrentlist.Pop();
//					chieldIndex = chieldIndexList.Pop();
//					//					parrent = parrentlist.Peek();
//					//					chieldIndex = chieldIndexList.Peek();
//				}
//			} while (parrent != null);
//			return newRoot;
//		}

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
		private void DetermineEscapesAndAllocateStackVariables()
		{
			foreach (MethodVariable var in Variables)
			{
				var.Escapes = false;
				if (var.VirtualRegister == null)
					var.VirtualRegister = NextRegister();

				if (var.StackType.IsStackValueType)
				{
					var.Escapes = true;
					var.StackLocation = GetNewSpillQuadOffset(var.StackType.ComplexType.QuadWordCount);
				}
			}

			foreach (MethodParameter p in Parameters)
			{
				p.Escapes = false;

				// The linear register allocator will move physical argument registers into these virtual registers.

				if (p.StackType.IsStackValueType)
				{
					p.Escapes = true;
					p.StackLocation = GetNewSpillQuadOffset(p.StackType.ComplexType.QuadWordCount);
				}
				else
					p.VirtualRegister = NextRegister();
			}

			Action<TreeInstruction> action =
				delegate(TreeInstruction obj)
					{
						if (obj.Opcode.IRCode == IRCode.Ldarga || obj.Opcode.IRCode == IRCode.Ldloca)
						{
							((MethodVariable) obj.Operand).Escapes = true;
							if (((MethodVariable)obj.Operand).StackLocation == 0)
								((MethodVariable) obj.Operand).StackLocation = GetNewSpillQuadOffset(1);
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
			}

			// Generate the body.
			RecursiveInstructionSelector selector;
			if (_specialSpeObjects != null)
				selector = new RecursiveInstructionSelector(_specialSpeObjects, GetNewSpillQuadOffset);
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
			}

			// Instruction scheduling.
//			ListInstructionScheduler scheduler = new ListInstructionScheduler();
//			foreach (SpuBasicBlock bb in _instructions.BasicBlocks)
//				scheduler.Schedule(bb);


			State = MethodCompileState.S4InstructionSelectionDone;
		}

		private void PerformInstructionOptimization()
		{
			List<SpuInstruction> jumpInsts = new List<SpuInstruction>();

			Dictionary<SpuBasicBlock, Set<SpuInstruction>> branchSourcesForConditionalBranch = new Dictionary<SpuBasicBlock, Set<SpuInstruction>>();
			Dictionary<SpuBasicBlock, List<SpuInstruction>> branchSources = new Dictionary<SpuBasicBlock, List<SpuInstruction>>();

			// Maps to basic block number.
			List<int> possibleRemoveableBranche = new List<int>();


			for (int i = 0; i < SpuBasicBlocks.Count; i++ )
			{
				SpuBasicBlock block = SpuBasicBlocks[i];

				SpuInstruction inst = block.Head;

				while (inst != null)
				{
					if (inst.JumpTarget == null)
						continue;

					int target = SpuBasicBlocks.IndexOf(inst.JumpTarget);
					// Looks for the firste non empty basic block
					while (target < SpuBasicBlocks.Count && SpuBasicBlocks[target].Head == null)
						target++;

					if (i >= SpuBasicBlocks.Count)
						continue;

					SpuInstruction targetInst = SpuBasicBlocks[target].Head;

					if (targetInst.OpCode != SpuOpCode.br && targetInst.OpCode != SpuOpCode.ret)
						continue;

					possibleRemoveableBranche.Add(target);

					if (targetInst.OpCode == SpuOpCode.ret)
						targetInst.JumpTarget = _innerEpilog;

					if(!branchSourcesForConditionalBranch.ContainsKey(SpuBasicBlocks[target]))
						branchSourcesForConditionalBranch[SpuBasicBlocks[target]] = new Set<SpuInstruction>();

					branchSourcesForConditionalBranch[SpuBasicBlocks[target]].Add(inst);
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

			possibleRemoveableBranche.Sort();
			possibleRemoveableBranche.Reverse();

			foreach (int i in possibleRemoveableBranche)
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

			new LinearRegisterAllocator().Allocate(SpuBasicBlocks, GetNewSpillQuadOffset, _innerEpilog);

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

		public int GetNewSpillQuadOffset(int count)
		{
			Utilities.AssertArgumentRange(count >= 1 && count <= 10, "count", count);

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