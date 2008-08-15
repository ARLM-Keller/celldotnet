using System;
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
	/// <summary>
	/// Equivalent of <see cref="MethodCompiler"/>. Maybe this one should also be used for cell one day.
	/// </summary>
	class CudaMethod
	{
		internal enum CompileState
		{
			None,
			TreeConstructionDone,
			ListContructionDone,
			InstructionSelectionDone
		}

		private CompileState _state;

		private readonly MethodBase _method;

		public List<BasicBlock> Blocks { get; private set; }
		private int _nextVarIdx = 1;

		public CudaMethod(MethodBase method)
		{
			Utilities.AssertArgumentNotNull(method, "method");
			_method = method;
//			throw new NotImplementedException();
		}

		public void PerformProcessing(CudaMethod.CompileState targetstate)
		{
			List<IRBasicBlock> treeblocks = null;
			List<MethodVariable> variables = null;
			List<MethodParameter> parameters = null;

			if (targetstate > _state && _state == CompileState.None)
			{
				PerformTreeConstruction(_method, out treeblocks, out parameters, out variables);
				// We're not storing the tree...
//				_state = CompileState.TreeConstructionDone;
			}

			if (targetstate > _state && _state <= CompileState.TreeConstructionDone)
			{
				Blocks = PerformListConstruction(treeblocks, parameters, variables);
				_state = CompileState.ListContructionDone;
			}
			if (targetstate > _state && _state == CompileState.InstructionSelectionDone - 1)
			{
				PerformInstructionSelection();
				_state = CompileState.InstructionSelectionDone;
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
			public int NextVariableIndex { get; set; }

			private Dictionary<MethodVariable, GlobalVReg> _variableMap;


			/// <summary>
			/// Constructs list IR from tree IR.
			/// </summary>
			public List<BasicBlock> PerformListConstruction(List<IRBasicBlock> treeblocks, List<MethodParameter> oldparameters, List<MethodVariable> oldvariables)
			{
				_variableMap = new Dictionary<MethodVariable, GlobalVReg>();
				foreach (MethodParameter parameter in oldparameters)
					_variableMap.Add(parameter, GlobalVReg.FromStackTypeDescription(parameter.StackType, NextVariableIndex++));
				foreach (MethodVariable variable in oldvariables)
					_variableMap.Add(variable, GlobalVReg.FromStackTypeDescription(variable.StackType, NextVariableIndex++));

				// construct all output blocks up front, so we can reference them for branches.
				var blockmap = treeblocks.ToDictionary(tb => tb, tb => new BasicBlock());

				foreach (IRBasicBlock irblock in treeblocks)
				{
					var block = blockmap[irblock];

					foreach (var treeroot in irblock.Roots)
					{
						ConvertTreeNode(treeroot, block, blockmap, false);
					}
				}

				return treeblocks.Select(tb => blockmap[tb]).ToList();
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
					newinst.Destination = GlobalVReg.FromStackTypeDescription(treenode.StackType, NextVariableIndex++);
					return newinst.Destination;
				}
				return null;
			}
		}

		#endregion

		#region  Tree construction

		/// <summary>
		/// Constructs list IR from tree IR.
		/// </summary>
		List<BasicBlock> PerformListConstruction(List<IRBasicBlock> treeblocks, List<MethodParameter> parameters, List<MethodVariable> variables)
		{
			var tc = new TreeConverter {NextVariableIndex = _nextVarIdx};
			List<BasicBlock> newblocklist = tc.PerformListConstruction(treeblocks, parameters, variables);
			_nextVarIdx = tc.NextVariableIndex;

			return newblocklist;
		}


		#endregion

		private void PerformInstructionSelection()
		{
			Blocks = new PtxInstructionSelector().Select(Blocks);

//			throw new NotImplementedException();
		}
	}
}