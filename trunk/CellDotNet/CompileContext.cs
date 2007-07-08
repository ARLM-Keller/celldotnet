using System;
using System.Collections.Generic;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;

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
		S6AddressSubstitutionDone,
		S7Complete
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

		private MethodDefinition _entryMethod;

		public Dictionary<string, MethodCompiler> Methods
		{
			get { return _methods; }
		}

		public CompileContext(MethodDefinition entryPoint)
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

			if (targetState >= CompileContextState.S6AddressSubstitutionDone && State < CompileContextState.S6AddressSubstitutionDone)
				PerformAddressSubstitution();

			if (targetState >= CompileContextState.S7Complete && State < CompileContextState.S7Complete)
				throw new NotImplementedException("State: " + targetState);

			if (targetState > CompileContextState.S7Complete)
				throw new ArgumentException("Invalid target state: " + targetState, "targetState");
		}

		/// <summary>
		/// Substitute label and method addresses for calls.
		/// </summary>
		private void PerformAddressSubstitution()
		{
			AssertState(CompileContextState.S5MethodAddressesDetermined);

			foreach (MethodCompiler mc in Methods.Values)
				mc.PerformProcessing(MethodCompileState.S6AdressSubstitutionDone);

			State = CompileContextState.S7Complete;
		}

		/// <summary>
		/// Determines local storage addresses for the methods.
		/// </summary>
		private void PerformMethodAddressDetermination()
		{
			AssertState(CompileContextState.S4RegisterAllocationDone);

			// Start from the beginning and lay them out sequentially.
			const int lsMethodStart = 0;

			int lsAddress = lsMethodStart;
			Dictionary<MethodCompiler, int> methodAddresses = new Dictionary<MethodCompiler, int>();
			foreach (MethodCompiler mc in Methods.Values)
			{
				methodAddresses.Add(mc, lsAddress);
				lsAddress += mc.GetSpuInstructionCount();
			}

			State = CompileContextState.S5MethodAddressesDetermined;
		}

		private void PerformRegisterAllocation()
		{
			AssertState(CompileContextState.S2TreeConstructionDone);

			foreach (MethodCompiler mc in Methods.Values)
				mc.PerformProcessing(MethodCompileState.S4RegisterAllocationDone);

			State = CompileContextState.S4RegisterAllocationDone;
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
		private string CreateTypeRefKey(TypeReference typeref)
		{
			return typeref.Scope + "," + typeref.FullName;
		}

		/// <summary>
		/// Creates a key that can be used to identify an instantiated/complete method.
		/// </summary>
		/// <param name="methodRef"></param>
		/// <returns></returns>
		private string CreateMethodRefKey(MethodReference methodRef)
		{
			string key = CreateTypeRefKey(methodRef.DeclaringType) + "::";
			key += methodRef.Name;
			foreach (ParameterDefinition param in methodRef.Parameters)
			{
				key += "," + CreateTypeRefKey(param.ParameterType);
			}

			return key;
		}

		private T GetFirstElement<T>(IEnumerable<T> set)
		{
			IEnumerator<T> enumerator = set.GetEnumerator();
			if (!enumerator.MoveNext())
				throw new ArgumentException("Empty set.");
			return enumerator.Current;
		}

		/// <summary>
		/// Finds and build MethodCompilers for the methods that are transitively referenced from the entry method.
		/// </summary>
		private void PerformRecursiveMethodTreesConstruction()
		{
			AssertState(CompileContextState.S1Initial);

			// Compile entry point and all any called methods.
			Dictionary<string, MethodReference> methodsToCompile = new Dictionary<string, MethodReference>();
			methodsToCompile.Add(CreateMethodRefKey(_entryMethod), _entryMethod);

			while (methodsToCompile.Count > 0)
			{
				// Find next method.
				string nextmethodkey = GetFirstElement(methodsToCompile.Keys);
				MethodReference mref = methodsToCompile[nextmethodkey];
				methodsToCompile.Remove(nextmethodkey);

				// Compile.
				MethodDefinition mdef;
				if (mref is MethodDefinition)
				{
					mdef = (MethodDefinition)mref;
				}
				else
				{
					AssemblyNameReference scope = (AssemblyNameReference) mref.DeclaringType.Scope;
					AssemblyDefinition assdef = null;
					foreach (Assembly ass in AppDomain.CurrentDomain.GetAssemblies())
					{
						if (ass.FullName == scope.FullName)
							assdef = AssemblyFactory.GetAssembly(ass.Location);
					}
					if (assdef == null)
						throw new Exception("Can't find assembly.");

					TypeDefinition typedef = assdef.MainModule.Types[mref.DeclaringType.FullName];
					mdef = typedef.Methods.GetMethod(mref.Name, mref.Parameters);
				}

				MethodCompiler ci = new MethodCompiler(mdef);
				ci.PerformProcessing(MethodCompileState.S2TreeConstructionDone);
				Methods.Add(nextmethodkey, ci);

				// Find referenced methods.
				foreach (Instruction inst in mdef.Body.Instructions)
				{
					MethodReference mr = inst.Operand as MethodReference;
					if (mr == null)
						continue;

					string methodkey = CreateMethodRefKey(mr);
					if (Methods.ContainsKey(methodkey))
						continue;

					methodsToCompile[methodkey] = mr;
				}
			}
			_entryPoint = new MethodCompiler(_entryMethod);
			_entryPoint.PerformProcessing(MethodCompileState.S2TreeConstructionDone);


			State = CompileContextState.S2TreeConstructionDone;
		}

		/// <summary>
		/// Performs instruction selected on all the methods.
		/// </summary>
		private void PerformInstructionSelection()
		{
			AssertState(CompileContextState.S2TreeConstructionDone);

			foreach (MethodCompiler mc in Methods.Values)
				mc.PerformProcessing(MethodCompileState.S3InstructionSelectionDone);

			State = CompileContextState.S3InstructionSelectionDone;
		}

	}
}
