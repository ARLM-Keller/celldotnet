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
		}

		private CompileState _state;

		private MethodBase _method;

		public List<BasicBlock> Blocks { get; private set; }

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
		}

		/// <summary>
		/// Constructs list IR from tree IR.
		/// </summary>
		static List<BasicBlock> PerformListConstruction(List<IRBasicBlock> treeblocks, List<MethodParameter> parameters, List<MethodVariable> variables)
		{
			var blocks = new List<BasicBlock>();
			foreach (IRBasicBlock irblock in treeblocks)
			{
				var block = new BasicBlock();
				blocks.Add(block);

				foreach (var treeroot in irblock.Roots)
				{
					ConvertTreeNode(treeroot, block, false);
				}
			}

			return blocks;
		}

		/// <summary>
		/// Recursively convert <paramref name="treenode"/> to a <see cref="ListInstruction"/>
		/// and returns the produced variable/register.
		/// </summary>
		/// <param name="treenode"></param>
		/// <param name="block"></param>
		/// <param name="returnsValue"></param>
		/// <returns></returns>
		private static MethodVariable ConvertTreeNode(TreeInstruction treenode, BasicBlock block, bool returnsValue)
		{
			ListInstruction newinst;

			if (treenode is MethodCallInstruction)
			{
				var mci = (MethodCallInstruction) treenode;
				var callinst = new MethodCallListInstruction(treenode.Opcode, mci.Operand);

				// TODO parameters.
				callinst.Parameters = new List<MethodVariable>(mci.Parameters.Count);
				foreach (TreeInstruction parameter in mci.Parameters)
				{
					callinst.Parameters.Add(ConvertTreeNode(parameter, block, true));
				}
				newinst = callinst;
			}
			else
			{
				newinst = new ListInstruction(treenode.Opcode, treenode.Operand);
				if (treenode.Left != null)
					newinst.Source1 = ConvertTreeNode(treenode.Left, block, true);
				if (treenode.Right != null)
					newinst.Source2 = ConvertTreeNode(treenode.Right, block, true);
			}

			block.Append(newinst);
			if (returnsValue)
			{
				newinst.Destination = new MethodVariable(5000, treenode.StackType);
				return newinst.Destination;
			}
			return null;
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
				MethodInfo mi = (MethodInfo) method;
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
	}
}