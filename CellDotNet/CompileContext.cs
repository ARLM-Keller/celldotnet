using System;
using System.Collections.Generic;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace CellDotNet
{
	enum CompileContextState
	{
		None,
		Initial,
		TreeConstructionDone,
		InstructionSelectionDone,
		RegisterAllocationDone,
		MethodAddressesDetermined,
		Complete
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
			State = CompileContextState.Initial;

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
			if (targetState == State)
				return;

			switch (targetState)
			{
				case CompileContextState.InstructionSelectionDone:
					PerformRecursiveMethodTreesConstruction();
					break;
				case CompileContextState.TreeConstructionDone:
					PerformInstructionSelection();
					break;
				case CompileContextState.RegisterAllocationDone:
				case CompileContextState.MethodAddressesDetermined:
				case CompileContextState.Complete:
					throw new NotImplementedException("State: " + targetState);
				case CompileContextState.None:
				case CompileContextState.Initial:
				default:
					throw new ArgumentException("Invalid target state: " + targetState, "targetState");
			}
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
			AssertState(CompileContextState.Initial);

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
				ci.PerformProcessing(MethodCompileState.TreeConstructionDone);
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
			_entryPoint.PerformProcessing(MethodCompileState.TreeConstructionDone);


			State = CompileContextState.TreeConstructionDone;
		}

		/// <summary>
		/// Performs instruction selected on all the methods.
		/// </summary>
		private void PerformInstructionSelection()
		{
			AssertState(CompileContextState.TreeConstructionDone);

			foreach (MethodCompiler mc in Methods.Values)
			{
				mc.PerformProcessing(MethodCompileState.InstructionSelectionDone);
			}


			State = CompileContextState.InstructionSelectionDone;
		}
	}
}
