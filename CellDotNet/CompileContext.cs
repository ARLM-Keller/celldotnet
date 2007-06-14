using System;
using System.Collections.Generic;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace CellDotNet
{
	/// <summary>
	/// This class acts as a driver and store for the compilation process.
	/// </summary>
	class CompileContext
	{
		private CompileInfo _entryPoint;
		private Dictionary<string, CompileInfo> _methods = new Dictionary<string, CompileInfo>();

		/// <summary>
		/// The first method that is run.
		/// </summary>
		public CompileInfo EntryPoint
		{
			get { return _entryPoint; }
		}

		public Dictionary<string, CompileInfo> Methods
		{
			get { return _methods; }
		}


		public CompileContext(MethodDefinition entryPoint)
		{
			CompileAll(entryPoint);
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

		private void CompileAll(MethodDefinition entryPoint)
		{
			// Compile entry point and all any called methods.
			Dictionary<string, MethodReference> methodsToCompile = new Dictionary<string, MethodReference>();
			methodsToCompile.Add(CreateMethodRefKey(entryPoint), entryPoint);

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

				CompileInfo ci = new CompileInfo(mdef);
				Methods.Add(nextmethodkey, ci);

				// Find references methods.
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
			_entryPoint = new CompileInfo(entryPoint);

		}
	}
}
