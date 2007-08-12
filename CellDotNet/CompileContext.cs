using System;
using System.Collections.Generic;
using System.Reflection;

namespace CellDotNet
{
	enum CompileContextState
	{
		S0None,
		S1Initial,
		S2TreeConstructionDone,
		S3InstructionSelectionDone,
		S4RegisterAllocationDone,
		S5MethodAddressesDetermined,
		S6AddressPatchingDone,
		S7CodeEmitted,
		S8Complete
	}

	/// <summary>
	/// This class acts as a driver and store for the compilation process.
	/// </summary>
	class CompileContext
	{
		private MethodCompiler _entryPoint;
		private Dictionary<string, MethodCompiler> _methods = new Dictionary<string, MethodCompiler>();

		/// <summary>
		/// The first method that is run.
		/// </summary>
		public MethodCompiler EntryPoint
		{
			get { return _entryPoint; }
		}

		private MethodBase _entryMethod;

		public Dictionary<string, MethodCompiler> Methods
		{
			get { return _methods; }
		}

		public CompileContext(MethodBase entryPoint)
		{
			State = CompileContextState.S1Initial;

			_entryMethod = entryPoint;
		}

		private CompileContextState _state;

		/// <summary>
		/// The least common state for all referenced methods.
		/// </summary>
		public CompileContextState State
		{
			get { return _state; }
			private set { _state = value; }
		}

		public void PerformProcessing(CompileContextState targetState)
		{
			if (State >= targetState)
				return;

			if (targetState <= CompileContextState.S1Initial)
				throw new ArgumentException("Invalid target state: " + targetState, "targetState");

			if (targetState >= CompileContextState.S2TreeConstructionDone && State < CompileContextState.S2TreeConstructionDone)
				PerformRecursiveMethodTreesConstruction();

			if (targetState >= CompileContextState.S3InstructionSelectionDone && State < CompileContextState.S3InstructionSelectionDone)
				PerformInstructionSelection();

			if (targetState >= CompileContextState.S4RegisterAllocationDone && State < CompileContextState.S4RegisterAllocationDone)
				PerformRegisterAllocation();

			if (targetState >= CompileContextState.S5MethodAddressesDetermined && State < CompileContextState.S5MethodAddressesDetermined)
				PerformMethodAddressDetermination();

			if (targetState >= CompileContextState.S6AddressPatchingDone && State < CompileContextState.S6AddressPatchingDone)
				PerformAddressPatching();

			if (targetState >= CompileContextState.S7CodeEmitted && State < CompileContextState.S7CodeEmitted)
				PerformCodeEmission();

			if (targetState > CompileContextState.S8Complete)
				throw new ArgumentException("Invalid target state: " + targetState, "targetState");
		}

		private void PerformRegisterAllocation()
		{
			AssertState(CompileContextState.S3InstructionSelectionDone);

			foreach (MethodCompiler mc in Methods.Values)
				mc.PerformProcessing(MethodCompileState.S5RegisterAllocationDone);

			State = CompileContextState.S4RegisterAllocationDone;
		}

		private int _totalCodeSize = -1;

		/// <summary>
		/// Determines local storage addresses for the methods.
		/// </summary>
		private void PerformMethodAddressDetermination()
		{
			AssertState(CompileContextState.S4RegisterAllocationDone);

			// Start from the beginning and lay them out sequentially.
			int lsOffset = 0;
			foreach (ObjectWithAddress o in GetAllObjects())
			{
				if (o is MethodCompiler)
				{
					((MethodCompiler) o).PerformProcessing(MethodCompileState.S6PrologAndEpilogDone);
				}

				o.Offset = lsOffset;
				lsOffset = Utilities.Align16(lsOffset + o.Size);
			}
			_totalCodeSize = lsOffset;

			State = CompileContextState.S5MethodAddressesDetermined;
		}

		/// <summary>
		/// Substitute label and method addresses for calls.
		/// </summary>
		private void PerformAddressPatching()
		{
			AssertState(CompileContextState.S5MethodAddressesDetermined);

			foreach (ObjectWithAddress owa in GetAllObjects())
			{
				if (owa is MethodCompiler)
					((MethodCompiler) owa).PerformProcessing(MethodCompileState.S7AddressPatchingDone);
				else if (owa is SpuRoutine)
					((SpuRoutine) owa).PerformAddressPatching();
			}
			

			State = CompileContextState.S6AddressPatchingDone;
		}

		private List<SpuRoutine> _spuRoutines;
		private RegisterSizedObject _returnValueLocation;
		private int[] _emittedCode;

		public int[] GetEmittedCode()
		{
			if (State < CompileContextState.S7CodeEmitted)
				throw new InvalidOperationException("State: " + State);

			Utilities.Assert(_emittedCode != null, "_emittedCode != null");

			return _emittedCode;
		}

		/// <summary>
		/// Returns a list of infrastructure SPU routines, including the initalization code.
		/// </summary>
		/// <returns></returns>
		private List<SpuRoutine> GetSpuRoutines()
		{
			Utilities.Assert(_spuRoutines != null, "_spuRoutines != null");
			return _spuRoutines;
		}

		private void GenerateSpuRoutines()
		{
			Utilities.Assert(_spuRoutines == null, "_spuRoutines == null");

			// Need address patching.
			if (State >= CompileContextState.S6AddressPatchingDone)
				throw new InvalidOperationException();

			// This one is not a routine, but it's convenient to initialize it here.
			_returnValueLocation = new RegisterSizedObject();

			_spuRoutines = new List<SpuRoutine>();
			SpuInitializer init = new SpuInitializer(EntryPoint, _returnValueLocation);

			// It's important that the initialization routine is the first one, since execution
			// will start at address 0.
			_spuRoutines.Add(init);
		}

		/// <summary>
		/// Enumerates all <see cref="ObjectWithAddress"/> objects that require storage and 
		/// optionally patching, including initialization and <see cref="RegisterSizedObject"/> objects.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<ObjectWithAddress> GetAllObjects()
		{
			List<ObjectWithAddress> all = new List<ObjectWithAddress>();
			// SPU routines go first, since we start execution at address 0.
			foreach (SpuRoutine routine in GetSpuRoutines())
				all.Add(routine);
			foreach (MethodCompiler mc in Methods.Values)
				all.Add(mc);

			// Data at the end.
			all.Add(_returnValueLocation);

			return all;
		}

		private void PerformCodeEmission()
		{
			AssertState(CompileContextState.S6AddressPatchingDone);

			_emittedCode = new int[_totalCodeSize];
			foreach (ObjectWithAddress owa in GetAllObjects())
			{
				/// Non-routine objects don't need to be consulted, since they get an address
				/// and currently can't be initialized.
				SpuRoutine routine = owa as SpuRoutine;
				if (routine == null)
					continue;

				int[] code = routine.Emit();
				Buffer.BlockCopy(code, 0, _emittedCode, routine.Offset, code.Length);
			}

			// TODO: Do something with the code.

			State = CompileContextState.S7CodeEmitted;
		}

		private void AssertState(CompileContextState requiredState)
		{
			if (State != requiredState)
				throw new InvalidOperationException(string.Format("Operation is invalid for the current state. " +
					"Current state: {0}; required state: {1}.", State, requiredState));
		}

		/// <summary>
		/// Creates a key that can be used to identify the type.
		/// </summary>
		/// <param name="typeref"></param>
		/// <returns></returns>
		private string CreateTypeRefKey(Type typeref)
		{
			return typeref.AssemblyQualifiedName;
		}

		/// <summary>
		/// Creates a key that can be used to identify an instantiated/complete method.
		/// </summary>
		/// <param name="methodRef"></param>
		/// <returns></returns>
		private string CreateMethodRefKey(MethodBase methodRef)
		{
			string key = CreateTypeRefKey(methodRef.DeclaringType) + "::";
			key += methodRef.Name;
			foreach (ParameterInfo param in methodRef.GetParameters())
			{
				key += "," + CreateTypeRefKey(param.ParameterType);
			}

			return key;
		}

		/// <summary>
		/// Finds and build MethodCompilers for the methods that are transitively referenced from the entry method.
		/// </summary>
		private void PerformRecursiveMethodTreesConstruction()
		{
			AssertState(CompileContextState.S1Initial);

			// Compile entry point and all any called methods.
			Dictionary<string, MethodBase> methodsToCompile = new Dictionary<string, MethodBase>();
			Dictionary<string, MethodBase> allMethods = new Dictionary<string, MethodBase>();
			Dictionary<string, List<TreeInstruction>> instructionsToPatch = new Dictionary<string, List<TreeInstruction>>();
			methodsToCompile.Add(CreateMethodRefKey(_entryMethod), _entryMethod);

			bool isfirst = true;

			while (methodsToCompile.Count > 0)
			{
				// Find next method.
				string nextmethodkey = Utilities.GetFirst(methodsToCompile.Keys);
				MethodBase method = methodsToCompile[nextmethodkey];
				methodsToCompile.Remove(nextmethodkey);
				allMethods.Add(nextmethodkey, method);

				// Compile.
				MethodCompiler mc = new MethodCompiler(method);
				mc.PerformProcessing(MethodCompileState.S2TreeConstructionDone);
				Methods.Add(nextmethodkey, mc);

				if (isfirst)
				{
					_entryPoint = mc;
					isfirst = false;
				}

				// Find referenced methods.
				mc.VisitTreeInstructions(
					delegate(TreeInstruction inst)
         			{
						MethodBase mr = inst.Operand as MethodBase;
						if (mr == null)
							return;

						string methodkey = CreateMethodRefKey(mr);
         				MethodCompiler calledMethod;
						if (Methods.TryGetValue(methodkey, out calledMethod))
						{
							// We encountered the method before, so just use it.
							inst.Operand = calledMethod;
							return;
						}
						else
						{
							// We haven't seen this method referenced before, so 
							// make a note that we need to compile it and remember
							// that this instruction must be patched with a MethodCompiler
							// once it is created.
							methodsToCompile[methodkey] = mr;
							List<TreeInstruction> patchlist;
							if (!instructionsToPatch.TryGetValue(methodkey, out patchlist))
							{
								patchlist = new List<TreeInstruction>();
								instructionsToPatch.Add(methodkey, patchlist);
							}
							inst.Operand = methodkey;
							patchlist.Add(inst);
						}
					});

				{
					// Patch the instructions that we've encountered earlier and that referenced this method.
					List<TreeInstruction> patchlist;
					string thismethodkey = CreateMethodRefKey(method);
					if (instructionsToPatch.TryGetValue(thismethodkey, out patchlist))
					{
						foreach (TreeInstruction inst in patchlist)
							inst.Operand = mc;
					}
				}
			}
//			_entryPoint = new MethodCompiler(_entryMethod);
//			_entryPoint.PerformProcessing(MethodCompileState.S2TreeConstructionDone);


			State = CompileContextState.S2TreeConstructionDone;
		}

		/// <summary>
		/// Performs instruction selected on all the methods.
		/// </summary>
		private void PerformInstructionSelection()
		{
			AssertState(CompileContextState.S2TreeConstructionDone);

			foreach (MethodCompiler mc in Methods.Values)
				mc.PerformProcessing(MethodCompileState.S4InstructionSelectionDone);

			GenerateSpuRoutines();

			State = CompileContextState.S3InstructionSelectionDone;
		}

	}
}
