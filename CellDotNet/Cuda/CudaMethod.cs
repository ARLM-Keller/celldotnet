﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using CellDotNet.Intermediate;
using CellDotNet.Spe;

namespace CellDotNet.Cuda
{
	internal enum CudaMethodCompileState
	{
		None,
		TreeConstructionDone,
		ListContructionDone,
		ConditionalBranchHandlingDone,
		InstructionSelectionDone
	}

	/// <summary>
	/// Equivalent of <see cref="MethodCompiler"/>. Maybe this one should also be used for cell one day.
	/// </summary>
	class CudaMethod
	{
		public CudaMethodCompileState State { get; private set; }

		private readonly MethodBase _method;
		public string PtxName { get; private set; }

		public List<BasicBlock> Blocks { get; private set; }
		public List<GlobalVReg> Parameters { get; private set; }

		public CudaMethod(MethodBase method)
		{
			Utilities.AssertArgumentNotNull(method, "method");

			// Protect lambdas.
			PtxName = method.Name.Replace('<', '$').Replace('>', '$');

			_method = method;
		}

		public void PerformProcessing(CudaMethodCompileState targetstate)
		{
			List<IRBasicBlock> treeblocks = null;
			List<MethodVariable> variables = null;
			List<MethodParameter> parameters = null;

			if (targetstate > State && State == CudaMethodCompileState.None)
			{
				PerformTreeConstruction(_method, out treeblocks, out parameters, out variables);
				// We're not storing the tree...
//				_state = CompileState.TreeConstructionDone;
			}

			if (targetstate > State && State <= CudaMethodCompileState.ListContructionDone - 1)
			{
				List<BasicBlock> newblocks;
				List<GlobalVReg> newparams;
				new TreeConverter().PerformListConstruction(treeblocks, parameters, variables, PtxName, out newblocks, out newparams);
				Blocks = newblocks;
				Parameters = newparams;
				State = CudaMethodCompileState.ListContructionDone;
			}
			if (targetstate > State && State <= CudaMethodCompileState.ConditionalBranchHandlingDone - 1)
			{
				PerformConditionalBranchDecomposition();
				State = CudaMethodCompileState.ConditionalBranchHandlingDone;
			}
			if (targetstate > State && State == CudaMethodCompileState.InstructionSelectionDone - 1)
			{
				PerformInstructionSelection();
				State = CudaMethodCompileState.InstructionSelectionDone;
			}
		}

		private void PerformConditionalBranchDecomposition()
		{
			foreach (var block in Blocks)
			{
				ListInstruction inst = block.Head;
				while (inst != null)
				{
					IRCode cmpopcode = 0;
					GlobalVReg pred = null;
					bool predicatenegation = false;

					var isFP = !(inst is MethodCallListInstruction) && inst.Source1 != null &&
					           (inst.Source1.StackType == StackType.R4 || inst.Source1.StackType == StackType.R8);

					switch (inst.IRCode)
					{
						case IRCode.Beq:
							cmpopcode = IRCode.Ceq;
							goto case IRCode.Brtrue;
                        case IRCode.Bge:
							cmpopcode = isFP ? IRCode.Clt_Un : IRCode.Clt;
							goto case IRCode.Brfalse;
						case IRCode.Bge_Un:
							cmpopcode = IRCode.Clt_Un;
							goto case IRCode.Brfalse;
						case IRCode.Bgt:
							cmpopcode = IRCode.Cgt;
							goto case IRCode.Brtrue;
						case IRCode.Bgt_Un:
							cmpopcode = IRCode.Cgt_Un;
							goto case IRCode.Brtrue;
						case IRCode.Ble:
							cmpopcode = isFP ? IRCode.Cgt_Un : IRCode.Cgt;
							goto case IRCode.Brfalse;
						case IRCode.Ble_Un:
							cmpopcode = isFP ? IRCode.Cgt : IRCode.Cgt_Un;
							goto case IRCode.Brfalse;
						case IRCode.Blt:
							cmpopcode = IRCode.Clt;
							goto case IRCode.Brtrue;
						case IRCode.Blt_Un:
							cmpopcode = IRCode.Clt_Un;
							goto case IRCode.Brtrue;
						case IRCode.Bne_Un:
							cmpopcode = IRCode.Ceq;
							goto case IRCode.Brfalse;
						case IRCode.Brfalse:
							predicatenegation = true;
							goto case IRCode.Brtrue;
						case IRCode.Brtrue:
							Utilities.DebugAssert(inst.Predicate == null, "Can't handle existing predicate.");
							BasicBlock target = (BasicBlock) inst.Operand;
							if (cmpopcode != 0)
							{
								pred = GlobalVReg.FromType(StackType.ValueType, VRegType.Register, CudaStateSpace.Register, typeof(PredicateValue));
								var newcmp = new ListInstruction(cmpopcode) { Source1 = inst.Source1, Source2 = inst.Source2, Destination = pred };
								block.Replace(inst, newcmp);
								inst = newcmp;
							}
							else if (inst.Source1.ReflectionType != typeof(PredicateValue))
							{
								// We don't handle the case where the condition is a native int, because that would mean
								// using ptx setp here, and it's probably best to avoid ptx opcodes before instruction selection.
								// Alternatively, an intrinsic method call could be used.
								throw new NotImplementedException();
							}

							var newbranch = new ListInstruction(IRCode.Br, target) { Predicate = pred, PredicateNegation = predicatenegation };

							block.InsertAfter(inst, newbranch);

							/// remember to forward when adding.
							break;
					}

					inst = inst.Next;
				}
			}
		}

		static void PerformTreeConstruction(MethodBase method, out List<IRBasicBlock> treeBlocks,
									out List<MethodParameter> parameters, out List<MethodVariable> variables)
		{
			var typederiver = new TypeDeriver();

			// Build Parameters.
			parameters = new List<MethodParameter>();
			int i = 0;

			if ((method.CallingConvention & CallingConventions.HasThis) != 0)
			{
				StackTypeDescription type = new StackTypeDescription(new TypeDescription(method.DeclaringType));

				StackTypeDescription thistype;
				if (!type.IsPointerType && type.CliType == CliType.ValueType)
					thistype = type.GetManagedPointer();
				else
					thistype = type;

				parameters.Add(new MethodParameter(thistype));
				i++;
			}

			foreach (ParameterInfo pi in method.GetParameters())
			{
				//Not true in instance methods.
				//				Utilities.Assert(pi.Position == i, "pi.Index == i");

				Utilities.Assert(pi.Position == i - ((method.CallingConvention & CallingConventions.HasThis) != 0 ? 1 : 0), "pi.Index == i");
				i++;

				parameters.Add(new MethodParameter(pi, typederiver.GetStackTypeDescription(pi.ParameterType)));
			}


			// Build Variables.
			variables = new List<MethodVariable>();
			i = 0;
			if (!(method is DynamicMethod))
			{
				MethodBody body = method.GetMethodBody();
				if (body == null)
					throw new MethodResolveException("Method " + method.Name + " has no body.");

				foreach (LocalVariableInfo lv in body.LocalVariables)
				{
					Utilities.Assert(lv.LocalIndex == i, "lv.LocalIndex == i");
					i++;

					variables.Add(new MethodVariable(lv, typederiver.GetStackTypeDescription(lv.LocalType)));
				}
			}

			if (method is MethodInfo)
			{
				MethodInfo mi = (MethodInfo)method;
				//				var _returnType = typederiver.GetStackTypeDescription(mi.ReturnType);
			}


			ILReader reader = new ILReader(method);
			try
			{
				treeBlocks = new IRTreeBuilder().BuildBasicBlocks(method, reader, variables,
																  new ReadOnlyCollection<MethodParameter>(parameters));
			}
			catch (NotImplementedException e)
			{
				throw new ILParseException(string.Format("An error occurred while parsing method '{0}.{1}'.",
					method.DeclaringType.Name, method.Name), e);
			}
			catch (ILParseException e)
			{
				throw new ILParseException(string.Format("An error occurred while parsing method '{0}.{1}'.",
														 method.DeclaringType.Name, method.Name), e);
			}
		}


		#region TreeConverter

		class TreeConverter
		{
			private Dictionary<MethodVariable, GlobalVReg> _variableMap;

			/// <summary>
			/// Constructs list IR from tree IR.
			/// </summary>
			public void PerformListConstruction(List<IRBasicBlock> treeblocks, List<MethodParameter> oldparameters, List<MethodVariable> oldvariables, string ptxname, out List<BasicBlock> newblocks, out List<GlobalVReg> newparams)
			{
				_variableMap = new Dictionary<MethodVariable, GlobalVReg>();
				newparams = new List<GlobalVReg>(oldparameters.Count);
				foreach (MethodParameter oldp in oldparameters)
				{
					var newp = GlobalVReg.FromStackTypeDescription(oldp.StackType, VRegType.Address, CudaStateSpace.Parameter);
					// TODO: Prefix the name to avoid clashes with other symbols.
					newp.Name = oldp.Name;
					newparams.Add(newp);
					_variableMap.Add(oldp, newp);
				}
				foreach (MethodVariable variable in oldvariables)
					_variableMap.Add(variable, GlobalVReg.FromStackTypeDescription(variable.StackType, VRegType.Register, CudaStateSpace.Register));

				// construct all output blocks up front, so we can reference them for branches.
				var blocknum = 0;
				var blockmap = treeblocks.ToDictionary(tb => tb, tb => new BasicBlock("LL" + ptxname + blocknum++));

				foreach (IRBasicBlock irblock in treeblocks)
				{
					var block = blockmap[irblock];

					foreach (var treeroot in irblock.Roots)
					{
						ConvertTreeNode(treeroot, block, blockmap, false);
					}
				}

				newblocks = treeblocks.Select(tb => blockmap[tb]).ToList();
			}

			/// <summary>
			/// Recursively convert <paramref name="treenode"/> to a <see cref="ListInstruction"/>
			/// and returns the produced variable/register.
			/// </summary>
			/// <param name="treenode"></param>
			/// <param name="block"></param>
			/// <param name="returnsValue"></param>
			/// <returns></returns>
			private GlobalVReg ConvertTreeNode(TreeInstruction treenode, BasicBlock block, Dictionary<IRBasicBlock, BasicBlock> blockmap, bool returnsValue)
			{
				ListInstruction newinst;

				if (treenode is MethodCallInstruction)
				{
					var mci = (MethodCallInstruction)treenode;
					var callinst = new MethodCallListInstruction(treenode.Opcode.IRCode, mci.Operand);

					foreach (TreeInstruction parameter in mci.Parameters)
					{
						callinst.Parameters.Add(ConvertTreeNode(parameter, block, blockmap, true));
					}
					newinst = callinst;
				}
				else
				{
					object operand;
					if (treenode.Operand is IRBasicBlock)
						operand = blockmap[treenode.OperandAsBasicBlock];
					else if (treenode.Operand is MethodVariable)
						operand = _variableMap[treenode.OperandAsVariable];
					else if (treenode.Operand is StackTypeDescription)
					{
						operand = GlobalVReg.FromStackTypeDescription(((StackTypeDescription) treenode.Operand), VRegType.None, CudaStateSpace.None);
//						operand = GlobalVReg.FromStackTypeDescription(StackTypeDescription.Float32, VRegType.None);
					}
					else
						operand = treenode.Operand;

					newinst = new ListInstruction(treenode.Opcode.IRCode, operand);
					if (treenode.Left != null)
						newinst.Source1 = ConvertTreeNode(treenode.Left, block, blockmap, true);
					if (treenode.Right != null)
						newinst.Source2 = ConvertTreeNode(treenode.Right, block, blockmap, true);
				}

				block.Append(newinst);
				if (returnsValue)
				{
					newinst.Destination = GlobalVReg.FromStackTypeDescription(treenode.StackType, VRegType.Register, CudaStateSpace.Register);
					return newinst.Destination;
				}
				return null;
			}
		}

		#endregion

		private void PerformInstructionSelection()
		{
			var selector = new PtxInstructionSelector();
			Blocks = selector.Select(Blocks);

//			throw new NotImplementedException();
		}
	}
}