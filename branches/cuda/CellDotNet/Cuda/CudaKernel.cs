using System;
using System.Collections.Generic;
using System.Reflection;

namespace CellDotNet.Cuda
{
	enum CudaKernelCompileState
	{
		None,
		IRConstructionDone,
		InstructionSelectionDone,
		PtxEmissionComplete,
		PtxCompilationComplete,
		Complete,
	}

	public class CudaKernel<T> : CudaKernel where T : class
	{
		private readonly T _kernelWrapperDelegate;

		internal CudaKernel(MethodInfo kernelMethod) : base(kernelMethod)
		{
			throw new NotImplementedException();
		}
//
//		public CudaKernel(T kerneldelegate)
//		{
//			if (!(kerneldelegate is Delegate))
//				throw new ArgumentException("Type argument must be a delegate type.");
//
//			// TODO Generate LCG delegate wrapper.
//			this._kernelWrapperDelegate = kerneldelegate;
//
//			_kernelMethod = (kerneldelegate as Delegate).Method;
//			Utilities.AssertArgument(_kernelMethod.IsStatic, "Kernel method must be static.");
//		}

		public T Execute
		{
			get { return _kernelWrapperDelegate; }
		}
	}

	public class CudaKernel : IDisposable
	{
		private CudaKernelCompileState _state;

		private List<CudaMethod> _methods;
		private readonly MethodInfo _kernelMethod;
		private CudaContext _context;
		private PtxEmitter _emitter;
		private string _cubin;
		private CudaFunction _function;

		public CudaKernel(MethodInfo kernelMethod)
		{
			Utilities.AssertArgumentNotNull(kernelMethod, "kernelMethod");

			_kernelMethod = kernelMethod;
			Utilities.AssertArgument(_kernelMethod.IsStatic, "Kernel method must be static.");
		}

		public static CudaKernel<T> Create<T>(MethodInfo method) where T : class
		{
			return new CudaKernel<T>(method);
		}

		public static CudaKernel Create(MethodInfo method)
		{
			return new CudaKernel(method);
		}

		public static CudaKernel Create(Delegate del)
		{
			return new CudaKernel(del.Method);
		}

		internal void PerformProcessing(CudaKernelCompileState targetstate)
		{
			if (targetstate > _state && _state == CudaKernelCompileState.IRConstructionDone - 1)
			{
				_methods = PerformIRConstruction(_kernelMethod);
				_state = CudaKernelCompileState.IRConstructionDone;
			}
			if (targetstate > _state && _state == CudaKernelCompileState.InstructionSelectionDone - 1)
			{
				PerformInstructionSelection(_methods);
				_state = CudaKernelCompileState.InstructionSelectionDone;
			}			
			if (targetstate > _state && _state == CudaKernelCompileState.PtxEmissionComplete - 1)
			{
				_emitter = PerformPtxEmission(_methods);
				_state = CudaKernelCompileState.PtxEmissionComplete;
			}			
			if (targetstate > _state && _state == CudaKernelCompileState.PtxCompilationComplete - 1)
			{
				_cubin = PerformPtxCompilation(_emitter);
				_state = CudaKernelCompileState.PtxCompilationComplete;
			}
		}

		public void ExecuteUntyped(params object[] arguments)
		{
			EnsurePrepared();

			_function.Launch(arguments);
		}

		private string PerformPtxCompilation(PtxEmitter emitter)
		{
			AssertState(CudaKernelCompileState.PtxCompilationComplete - 1);

			string cubin = new PtxCompiler().CompileToCubin(emitter.GetEmittedPtx());
			return cubin;
		}

		private PtxEmitter PerformPtxEmission(List<CudaMethod> methods)
		{
			AssertState(CudaKernelCompileState.PtxEmissionComplete - 1);

			var emitter = new PtxEmitter();
			foreach (CudaMethod method in methods)
			{
				emitter.Emit(method);
			}

			return emitter;
		}

		private void AssertState(CudaKernelCompileState requiredState)
		{
			if (_state != requiredState)
				throw new InvalidOperationException(string.Format("Operation is invalid for the current state. " +
					"Current state: {0}; required state: {1}.", _state, requiredState));
		}

		private void PerformInstructionSelection(List<CudaMethod> methods)
		{
			AssertState(CudaKernelCompileState.InstructionSelectionDone - 1);

			foreach (CudaMethod method in methods)
			{
				method.PerformProcessing(CudaMethodCompileState.InstructionSelectionDone);
			}
		}

		private List<CudaMethod> PerformIRConstruction(MethodInfo kernelMethod)
		{
			AssertState(CudaKernelCompileState.IRConstructionDone - 1);

			var methodmap = new Dictionary<MethodBase, CudaMethod>();
			var methodWorkList = new Stack<MethodBase>();
			var instructionsNeedingPatching = new List<ListInstruction>();

			// Construct CudaMethods by traversing the call graph.
			methodWorkList.Push(kernelMethod);
			while (methodWorkList.Count != 0)
			{
				MethodBase methodBase = methodWorkList.Pop();
				var cm = new CudaMethod(methodBase);
				cm.PerformProcessing(CudaMethodCompileState.ListContructionDone);
				methodmap.Add(methodBase, cm);

				foreach (BasicBlock block in cm.Blocks)
				{
					foreach (ListInstruction inst in block.Instructions)
					{
						if (!(inst.Operand is MethodBase)) 
							continue;

						CudaMethod calledmethod;
						if (methodmap.TryGetValue((MethodBase) inst.Operand, out calledmethod))
						{
							// The encountered MethodBase has been encountered before.
							inst.Operand = calledmethod;
						}
						else
						{
							// The encountered MethodBase has not been encountered before, so it's pushed onto 
							// a work list, and make a note that the current instruction needs to be patched later.
							methodWorkList.Push((MethodBase)inst.Operand);
							instructionsNeedingPatching.Add(inst);
						}
					}
				}
			}

			foreach (ListInstruction inst in instructionsNeedingPatching)
			{
				inst.Operand = methodmap[(MethodBase) inst.Operand];
			}

			return new List<CudaMethod>(methodmap.Values);
		}

		internal ICollection<CudaMethod> Methods
		{
			get { return _methods; }
		}

		public CudaContext Context
		{
			get
			{
				if (_context == null)
					_context = CudaContext.GetCurrentOrNew();

				return _context;
			}
		}

		public void SetBlockShape(int x, int y)
		{
			SetBlockShape(x, y, 1);
		}

		public void SetBlockShape(int x, int y, int z)
		{
			GetFunction().SetBlockSize(x, y, z);
		}

		public void SetGridSize(int x, int y)
		{
			GetFunction().SetGridSize(x, y);
		}

		private CudaFunction GetFunction()
		{
			EnsurePrepared();
			return _function;
		}

		/// <summary>
		/// Lock'n load.
		/// </summary>
		private void Prepare()
		{
			PerformProcessing(CudaKernelCompileState.Complete);

			Utilities.Assert(!string.IsNullOrEmpty(_cubin), "No cubin?");
			CudaMethod kernelMethod = _methods[0];

			var module = CudaModule.LoadData(_cubin, Context.Device);
			_function = module.GetFunction(kernelMethod.PtxName);
		}

		public void EnsurePrepared()
		{
			if (_function != null)
				return;

			Prepare();

			Utilities.AssertNotNull(_function, "_function");
		}

		public void Dispose()
		{
			if (_context != null)
				_context.Dispose();
			_context = null;
		}
	}
}