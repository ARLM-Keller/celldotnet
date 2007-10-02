using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace CellDotNet
{
	public enum CompileContextState
	{
		S0None,
		S1Initial,
		S2TreeConstructionDone,
		S3InstructionSelectionDone,
		S4RegisterAllocationDone,
		S5AddressesDetermined,
		S6AddressPatchingDone,
		S7CodeEmitted,
		S8Complete
	}

	/// <summary>
	/// This class acts as a driver and store for the compilation process.
	/// </summary>
	public class CompileContext
	{
		private SpuRoutine _entryPoint;
		private SpecialSpeObjects _specialSpeObjects;

		private int _totalCodeSize = -1;

		private List<SpuDynamicRoutine> _spuRoutines;
		private RegisterSizedObject _returnValueLocation;
		private int[] _emittedCode;
		private DataObject _argumentArea;

		private CompileContextState _state;

		private MethodBase _entryPointMethod;

		LibraryManager _librarymanager;

		internal RegisterSizedObject DebugValueObject
		{
			get { return _specialSpeObjects.DebugValueObject; }
			set { throw new Exception("");}
		}

		/// <summary>
		/// The first method that is run.
		/// </summary>
		internal SpuRoutine EntryPoint
		{
			get { return _entryPoint; }
		}

		internal MethodCompiler EntryPointAsMetodCompiler
		{
			get { return EntryPoint as MethodCompiler; }
		}

		internal ICollection<MethodCompiler> Methods
		{
			get { return _methods.GetMethodCompilers(); }
		}

		public CompileContext(MethodBase entryPoint)
		{
			State = CompileContextState.S1Initial;
			if (!entryPoint.IsStatic)
				throw new ArgumentException("Only static methods are supported.");

			_entryPointMethod = entryPoint;
		}

		/// <summary>
		/// The least common state for all referenced methods.
		/// </summary>
		internal CompileContextState State
		{
			get { return _state; }
			private set { _state = value; }
		}

		/// <summary>
		/// The LS address where the return value from <see cref="EntryPoint"/> (if any)
		/// will be placed after the method returns.
		/// </summary>
		internal LocalStorageAddress ReturnValueAddress
		{
			get { return (LocalStorageAddress) _returnValueLocation.Offset; }
		}

		/// <summary>
		/// The area where arguments for the entry point is to be put.
		/// </summary>
		internal DataObject ArgumentArea
		{
			get
			{
				AssertState(CompileContextState.S8Complete);
				return _argumentArea;
			}
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

			if (targetState >= CompileContextState.S5AddressesDetermined && State < CompileContextState.S5AddressesDetermined)
				PerformAddressDetermination();

			if (targetState >= CompileContextState.S6AddressPatchingDone && State < CompileContextState.S6AddressPatchingDone)
				PerformAddressPatching();

			if (targetState >= CompileContextState.S7CodeEmitted && State < CompileContextState.S7CodeEmitted)
				PerformCodeEmission();

			if (targetState == CompileContextState.S8Complete)
				State = CompileContextState.S8Complete;
			else if (targetState > CompileContextState.S8Complete)
				throw new ArgumentException("Invalid target state: " + targetState, "targetState");
		}

		private void PerformRegisterAllocation()
		{
			AssertState(CompileContextState.S4RegisterAllocationDone - 1);

			foreach (MethodCompiler mc in Methods)
				mc.PerformProcessing(MethodCompileState.S7RemoveRedundantMoves);

			State = CompileContextState.S4RegisterAllocationDone;
		}

		/// <summary>
		/// Determines local storage addresses for the methods.
		/// </summary>
		private void PerformAddressDetermination()
		{
			AssertState(CompileContextState.S5AddressesDetermined - 1);

			// Start from the beginning and lay them out sequentially.
			int lsOffset = 0;
			List<ObjectWithAddress> all = GetAllObjects();

			// This sort is only to make windows and mono assign the same addresses.
			all.Sort(delegate(ObjectWithAddress x, ObjectWithAddress y)
			         	{
							// Make sure that the initializer goes first.
							if (x is SpuInitializer && y is SpuInitializer)
								return 0;
							if (x is SpuInitializer)
								return -1;
							else if (y is SpuInitializer)
								return 1;
			         		return String.CompareOrdinal(x.Name, y.Name);
			         	});

			foreach (ObjectWithAddress o in all)
			{
				if (o is MethodCompiler)
				{
					((MethodCompiler) o).PerformProcessing(MethodCompileState.S6PrologAndEpilogDone);
				}

				o.Offset = lsOffset;
				lsOffset = Utilities.Align16(lsOffset + o.Size);
			}


			// Determine positions for libraries.
			foreach (Library lib in LibMan.Libraries)
			{
				Utilities.Assert(lib.Size > 0, "lib.Size > 0");

				lib.Offset = lsOffset;
				lsOffset += Utilities.Align16(lib.Size);
			}

			_totalCodeSize = lsOffset;

			State = CompileContextState.S5AddressesDetermined;
		}

		// Used in ILOpCodeExecutionTest.
		internal static int LayoutObjects(IEnumerable<ObjectWithAddress> objects)
		{
			// Start from the beginning and lay them out sequentially.
			int lsOffset = 0;
			foreach (ObjectWithAddress o in objects)
			{
				o.Offset = lsOffset;
				lsOffset = Utilities.Align16(lsOffset + o.Size);
			}
			return lsOffset;
		}

		/// <summary>
		/// Substitute label and method addresses for calls.
		/// </summary>
		private void PerformAddressPatching()
		{
			AssertState(CompileContextState.S6AddressPatchingDone - 1);

			foreach (ObjectWithAddress owa in GetAllObjects())
			{
				if (owa is MethodCompiler)
					((MethodCompiler) owa).PerformProcessing(MethodCompileState.S8AddressPatchingDone);
				else if (owa is SpuDynamicRoutine)
					((SpuDynamicRoutine) owa).PerformAddressPatching();
			}
			

			State = CompileContextState.S6AddressPatchingDone;
		}

		/// <summary>
		/// Returns a list of infrastructure SPU routines, including the initalization code.
		/// </summary>
		/// <returns></returns>
		private List<SpuDynamicRoutine> GetSpuRoutines()
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
			if (EntryPoint.ReturnType != StackTypeDescription.None)
				_returnValueLocation = new RegisterSizedObject("ReturnValueLocation");

			_argumentArea = DataObject.FromQuadWords(EntryPoint.Parameters.Count, "ArgumentArea");

			_spuRoutines = new List<SpuDynamicRoutine>();
			SpuInitializer init = new SpuInitializer(EntryPoint, _returnValueLocation,
				_argumentArea, EntryPoint.Parameters.Count,
				_specialSpeObjects.StackPointerObject,
				_specialSpeObjects.NextAllocationStartObject, _specialSpeObjects.AllocatableByteCountObject);

			// It's important that the initialization routine is the first one, since execution
			// will start at address 0.
			_spuRoutines.Add(init);

		}

		/// <summary>
		/// Enumerates all <see cref="ObjectWithAddress"/> objects that require storage and 
		/// optionally patching, including initialization and <see cref="RegisterSizedObject"/> objects.
		/// </summary>
		/// <returns></returns>
		private List<ObjectWithAddress> GetAllObjects()
		{
			List<ObjectWithAddress> all = new List<ObjectWithAddress>();
			// SPU routines go first, since we start execution at address 0.
			foreach (SpuDynamicRoutine routine in GetSpuRoutines())
				all.Add(routine);
			all.AddRange(_specialSpeObjects.GetAll());
			foreach (MethodCompiler mc in Methods)
				all.Add(mc);

			// Data at the end.
			if (_returnValueLocation != null)
				all.Add(_returnValueLocation);
			all.Add(_argumentArea);

			return all;
		}

		internal ICollection<ObjectWithAddress> GetAllObjectsForDisassembly()
		{
			return GetAllObjects();
		}

		private void PerformCodeEmission()
		{
			AssertState(CompileContextState.S7CodeEmitted - 1);

			List<ObjectWithAddress> objects = GetAllObjects();
			List<SpuDynamicRoutine> routines = new List<SpuDynamicRoutine>();
			foreach (ObjectWithAddress o in objects)
			{
				SpuDynamicRoutine dynamicRoutine = o as SpuDynamicRoutine;
				if (dynamicRoutine != null)
					routines.Add(dynamicRoutine);
			}
			_emittedCode = new int[Utilities.Align16(_totalCodeSize) / 4];
			CopyCode(_emittedCode, routines);

			// Initialize memory allocation.
			{
				const int TotalSpeMem = 256 * 1024;
				const int StackPointer = 256*1024-32;
				const int StackSize = 8*1024;
				int codeByteSize = _emittedCode.Length*4;
				_specialSpeObjects.SetMemorySettings(StackPointer, StackSize, codeByteSize, TotalSpeMem - codeByteSize - StackSize);

				// We only need to write to the preferred slot.
				_emittedCode[_specialSpeObjects.AllocatableByteCountObject.Offset/4] = _specialSpeObjects.AllocatableByteCount;
				_emittedCode[_specialSpeObjects.NextAllocationStartObject.Offset/4] = _specialSpeObjects.NextAllocationStart;
				_emittedCode[_specialSpeObjects.StackPointerObject.Offset/4] = _specialSpeObjects.InitialStackPointer;
				_emittedCode[_specialSpeObjects.StackPointerObject.Offset / 4 + 1] = _specialSpeObjects.StackSize;
				// NOTE: SpuAbiUtilities.WriteProlog() is dependending on that the two larst words is >= stackSize.
				_emittedCode[_specialSpeObjects.StackPointerObject.Offset / 4 + 2] = _specialSpeObjects.StackSize;
				_emittedCode[_specialSpeObjects.StackPointerObject.Offset / 4 + 3] = _specialSpeObjects.StackSize;
			}

			// Get external libraries.
			foreach (Library lib in LibMan.Libraries)
			{
				Utilities.Assert(lib.Offset > 0, "lib.Offset > 0");
				Utilities.Assert(lib.Offset + lib.Size <= _totalCodeSize, "lib.Offset + lib.Size <= _totalCodeSize");

				byte[] contents = lib.GetContents();
				Buffer.BlockCopy(contents, 0, _emittedCode, lib.Offset, contents.Length);
			}

			State = CompileContextState.S7CodeEmitted;
		}

		private void AssertState(CompileContextState requiredState)
		{
			if (State != requiredState)
				throw new InvalidOperationException(string.Format("Operation is invalid for the current state. " +
					"Current state: {0}; required state: {1}.", State, requiredState));
		}

		#region Tree construction / resolving

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

		internal void SetLibraryResolver(LibraryResolver resolver)
		{
			Utilities.AssertArgumentNotNull(resolver, "resolver");

			if (_librarymanager != null)
				throw new InvalidOperationException("Library resolver cannot be changed after first use.");
			_librarymanager = new LibraryManager(resolver);
		}

		private LibraryManager LibMan
		{
			get
			{
				if (_librarymanager == null)
					_librarymanager = new LibraryManager(new StaticFileLibraryResolver());

				return _librarymanager;
			}
		}

		class LibraryManager
		{
			internal class LibraryUsage
			{
				private Library _library;
				private Dictionary<string, LibraryMethod> _usedMethods = new Dictionary<string, LibraryMethod>(StringComparer.Ordinal);

				public LibraryUsage(Library library)
				{
					_library = library;
				}

				public Library Library
				{
					get { return _library; }
				}

				public void RecordUsage(LibraryMethod method)
				{
					LibraryMethod oldRecord;
					if (_usedMethods.TryGetValue(method.Name, out oldRecord) && !ReferenceEquals(oldRecord, method))
						throw new ArgumentException("A method named '" + method.Name +
						                            "' has previously been seen, but it was another object.");
					else
						_usedMethods[method.Name] = method;
				}

				public ICollection<LibraryMethod> UsedMethods
				{
					get { return _usedMethods.Values; }
				}
			}

			Dictionary<string, LibraryUsage> _libraries = new Dictionary<string, LibraryUsage>(StringComparer.OrdinalIgnoreCase);
			private LibraryResolver _resolver;

			public LibraryManager(LibraryResolver libraryResolver)
			{
				Utilities.AssertArgumentNotNull(libraryResolver, "libraryResolver");
				_resolver = libraryResolver;
			}

			public LibraryMethod GetMethod(MethodInfo method)
			{
				DllImportAttribute att = (DllImportAttribute)method.GetCustomAttributes(typeof(DllImportAttribute), false)[0];

				string libraryName = att.Value;
				LibraryUsage libusage;
				if (!_libraries.TryGetValue(libraryName, out libusage))
				{
					Library lib = _resolver.ResolveLibrary(libraryName);
					libusage = new LibraryUsage(lib);
					_libraries.Add(libraryName, libusage);
				}

				LibraryMethod extmethod = libusage.Library.ResolveMethod(method);
				libusage.RecordUsage(extmethod);

				return extmethod;
			}

			public ICollection<Library> Libraries
			{
				get
				{
					List<Library> libs = new List<Library>(_libraries.Count);
					foreach (KeyValuePair<string, LibraryUsage> pair in _libraries)
						libs.Add(pair.Value.Library);

					return libs;
				}
			}

			public ICollection<LibraryUsage> Usage
			{
				get { return _libraries.Values; }
			}
		}

		/// <summary>
		/// Tracks the methods which need to be compiled and also external methods like <see cref="LibraryMethod"/> and
		/// <see cref="PpeMethod"/>.
		/// </summary>
		class MethodSet
		{
			private Dictionary<string, MethodCompiler> _methodcompilers;


			public MethodSet()
			{
				_methodcompilers = new Dictionary<string, MethodCompiler>();
			}

			public void Add(string key, MethodCompiler mc)
			{
				_methodcompilers.Add(key, mc);
			}

			public bool TryGetValue(string methodkey, out SpuRoutine routine)
			{
				MethodCompiler mc;
				if (_methodcompilers.TryGetValue(methodkey, out mc))
				{
					routine = mc;
					return true;
				}
				routine = null;
				return false;
			}

			public ICollection<MethodCompiler> GetMethodCompilers()
			{
				return _methodcompilers.Values;
			}
		}

		/// <summary>
		/// Finds and build MethodCompilers for the methods that are transitively referenced from the entry method.
		/// </summary>
		private void PerformRecursiveMethodTreesConstruction()
		{
			AssertState(CompileContextState.S2TreeConstructionDone - 1);

			// The types whose methods should only be executed on the ppe.
			Set<Type> _ppeTypes = new Set<Type>();
			foreach (ParameterInfo param in _entryPointMethod.GetParameters())
			{
				Type t = param.ParameterType;
				if (t.IsPrimitive || t.IsValueType)
					continue;

				_ppeTypes.Add(t);
			}

			// Compile entry point and any called methods, except PPE class methods.
			Dictionary<string, MethodBase> methodsToCompile = new Dictionary<string, MethodBase>();
			Dictionary<string, List<TreeInstruction>> instructionsToPatch = new Dictionary<string, List<TreeInstruction>>();
			methodsToCompile.Add(CreateMethodRefKey(_entryPointMethod), _entryPointMethod);

			bool isfirst = true;

			while (methodsToCompile.Count > 0)
			{
				// Find next method.
				string methodkey = Utilities.GetFirst(methodsToCompile.Keys);
				MethodBase method = methodsToCompile[methodkey];
				methodsToCompile.Remove(methodkey);

				SpuRoutine routine;
				if (DetectPpeType(_ppeTypes, method.DeclaringType))
				{
					routine = new PpeMethod((MethodInfo) method);
				}
				else if (method.IsDefined(typeof(DllImportAttribute), false))
				{
					routine = LibMan.GetMethod((MethodInfo) method);
				}
				else
				{
					routine = CreateMethodCompiler(method, methodkey, instructionsToPatch, methodsToCompile);
				}

				// Patch the instructions that we've encountered earlier and that referenced this method.
				List<TreeInstruction> patchlist;
				string thismethodkey = CreateMethodRefKey(method);
				if (instructionsToPatch.TryGetValue(thismethodkey, out patchlist))
				{
					foreach (TreeInstruction inst in patchlist)
					{
						SetCalledRoutine((MethodCallInstruction) inst, routine);
					}
				}

				if (isfirst)
				{
					_entryPoint = routine;
					isfirst = false;
				}
			}

			State = CompileContextState.S2TreeConstructionDone;
		}

		/// <summary>
		/// Replaces the operand method of <paramref name="inst"/> with <paramref name="calledRoutine"/>
		/// and possible changes the call opcode according to the type of routine (<see cref="IROpCodes.PpeCall"/>).
		/// </summary>
		/// <param name="inst"></param>
		/// <param name="calledRoutine"></param>
		private void SetCalledRoutine(MethodCallInstruction inst, SpuRoutine calledRoutine)
		{
			IROpCode callopcode;
			if (calledRoutine is PpeMethod)
				callopcode = IROpCodes.PpeCall;
			else
				callopcode = inst.Opcode;

			inst.SetCalledMethod(calledRoutine, callopcode);
		}

		/// <summary>
		/// Determines if any base type (except Object) is a known ppe type.
		/// </summary>
		/// <param name="currentlyKnownPpeTypes"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		private bool DetectPpeType(Set<Type> currentlyKnownPpeTypes, Type type)
		{
			if (type.IsValueType)
				return false;

			Type tester = type;
			bool isppe = false;
			while (tester != null && tester != typeof(object))
			{
				if (currentlyKnownPpeTypes.Contains(tester))
				{
					isppe = true;
					break;
				}

				tester = tester.BaseType;
			}

			if (isppe)
			{
				Type tmp = type;
				while (tmp != null && tmp != typeof(object))
				{
					currentlyKnownPpeTypes.Add(tmp);
					tmp = tester.BaseType;
				}
				return true;
			}

			return false;
		}

		MethodSet _methods = new MethodSet();

		/// <summary>
		/// Creates a <see cref="MethodCompiler"/> from <paramref name="method"/> while 
		/// updating <paramref name="instructionsToPatch"/> and <paramref name="methodsToCompile"/>.
		/// <paramref name="instructionsToPatch"/> is at the same time used to patch
		/// previously encountered instructions that referenced this method.
		/// </summary>
		/// <param name="instructionsToPatch"></param>
		/// <param name="method"></param>
		/// <param name="methodKey"></param>
		/// <param name="methodsToCompile"></param>
		/// <returns></returns>
		private MethodCompiler CreateMethodCompiler(MethodBase method, string methodKey,
		                                            Dictionary<string, List<TreeInstruction>> instructionsToPatch,
		                                            Dictionary<string, MethodBase> methodsToCompile)
		{
			MethodCompiler mc = new MethodCompiler(method);
			mc.PerformProcessing(MethodCompileState.S2TreeConstructionDone);

			_methods.Add(methodKey, mc);

			// Find referenced methods.
			mc.ForeachTreeInstruction(
				delegate(TreeInstruction inst)
					{
						MethodCallInstruction mci = inst as MethodCallInstruction;
						if (mci == null)
							return;

						MethodBase mb = SystemLibMap.GetUseableMethodBase(inst.OperandAsMethod);
						if (mb == null)
							return;

						string methodkey = CreateMethodRefKey(mb);
						SpuRoutine calledRoutine;
						if (_methods.TryGetValue(methodkey, out calledRoutine))
						{
							// We encountered the method before, so just use it.
							SetCalledRoutine(mci, calledRoutine);
						}
						else
						{
							// We haven't seen this method referenced before, so 
							// make a note that we need to compile it and remember
							// that this instruction must be patched with a MethodCompiler
							// once it is created.
							methodsToCompile[methodkey] = mb;
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

			return mc;
		}

		#endregion

		/// <summary>
		/// Performs instruction selected on all the methods.
		/// </summary>
		private void PerformInstructionSelection()
		{
			AssertState(CompileContextState.S3InstructionSelectionDone - 1);

			_specialSpeObjects = new SpecialSpeObjects();

			foreach (MethodCompiler mc in Methods)
			{
				mc.SetRuntimeSettings(_specialSpeObjects);
				mc.PerformProcessing(MethodCompileState.S4InstructionSelectionDone);
			}

			GenerateSpuRoutines();

			State = CompileContextState.S3InstructionSelectionDone;
		}

		public static void AssertAllValueTypeFields(Type t)
		{
			if (!t.IsValueType)
				throw new ArgumentException("Type " + t.FullName + " is not a value type.");

			foreach (FieldInfo field in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
			{
				Type ft = field.FieldType;

				if (!ft.IsValueType)
					throw new ArgumentException("Field " + field.Name + " of type " + t.FullName + " is not a value type.");

				// Check recursively if it's a struct.
				if (Type.GetTypeCode(ft) == TypeCode.Object)
					AssertAllValueTypeFields(ft);
			}
		}

		static internal void CopyCode(int[] targetBuffer, ICollection<SpuDynamicRoutine> objects)
		{
			Set<int> usedOffsets = new Set<int>();

			foreach (SpuDynamicRoutine routine in objects)
			{
				int[] code;
				try
				{
					code = routine.Emit();
				}
				catch (InvalidOperationException e)
				{
					throw new InvalidOperationException("An error occurred during Emit for the routine '" + routine.Name + "'.", e);
				}

				try
				{
					Utilities.Assert(routine.Offset >= 0, "routine.Offset >= 0");
					Utilities.Assert(code.Length == routine.Size / 4, string.Format("code.Length == routine.Size * 4. code.Length: {0:x4}, routine.Size: {1:x4}", code.Length, routine.Size));
					Utilities.Assert(!usedOffsets.Contains(routine.Offset), "!usedOffsets.Contains(routine.Offset). Offset: " + routine.Offset.ToString("x6"));
					usedOffsets.Add(routine.Offset);

					Buffer.BlockCopy(code, 0, targetBuffer, routine.Offset, routine.Size);
				}
				catch (Exception e)
				{
					throw new BadCodeLayoutException(e.Message, e);
				}

			}
		}

		#region Assembly

		/// <summary>
		/// Writes the code to the file with instructions on how to compile it.
		/// The executable should be ready for debugging with spu-gdb.
		/// </summary>
		/// <param name="filename"></param>
		/// <param name="arguments">Optional.</param>
		public void WriteAssemblyToFile(string filename, params ValueType[] arguments)
		{
			if (State < CompileContextState.S6AddressPatchingDone)
				throw new InvalidOperationException("Address patching has not yet been performed.");

			int[] code;
			if (arguments != null && arguments.Length != 0)
				code = GetEmittedCode(arguments, null);
			else
				code = GetEmittedCode();

			List<ObjectWithAddress> symbols = new List<ObjectWithAddress>(GetAllObjectsForDisassembly());
			WriteAssemblyToFile(filename, code, symbols, LibMan);
		}

		internal static void WriteAssemblyToFile(string filename, int[] code, List<ObjectWithAddress> symbols)
		{
			WriteAssemblyToFile(filename, code, symbols, null);
		}

		/// <summary>
		/// Writes the code to the file with instructions on how to compile it.
		/// The executable should be ready for debugging with spu-gdb.
		/// </summary>
		/// <param name="filename"></param>
		/// <param name="code"></param>
		/// <param name="symbols"></param>
		/// <param name="libman">Can be null.</param>
		private static void WriteAssemblyToFile(string filename, int[] code, List<ObjectWithAddress> symbols, LibraryManager libman)
		{
			using (StreamWriter writer = new StreamWriter(filename, false, Encoding.ASCII))
			{
				// Write the bytes and define main (which really is the initializer).
				writer.Write(@"
# {0}
# Generated on {2:G}
# Compile this file with the following command:
# spu-as {0} -o {1}.o && spu-gcc {1}.o -o {1}

  .section "".text""
  .globl main
  .type main, @function
  .align 4
main:
", Path.GetFileName(filename), Path.GetFileNameWithoutExtension(filename), DateTime.Now);

				symbols.Sort(delegate(ObjectWithAddress x, ObjectWithAddress y) { return x.Offset - y.Offset; });

				// Problably wont format correctly if the first symbol doesn't start at offset 0.
				Utilities.Assert(symbols.Count == 0 || symbols[0].Offset == 0, "symbols.Count == 0 || symbols[0].Offset == 0");

				// We don't want duplicate names.
				Set<string> usedNames = new Set<string>(symbols.Count);
				List<KeyValuePair<ObjectWithAddress, string>> sortedSymbolsWithNames = new List<KeyValuePair<ObjectWithAddress, string>>();
				foreach (ObjectWithAddress symbol in symbols)
				{
					if (string.IsNullOrEmpty(symbol.Name))
						throw new ArgumentException("Object with offset " + symbol.Offset.ToString("x") + " has no name.");

					// Anonymous methods contains '<' and '>' in their name.
					string encodedname = symbol.Name;
					encodedname = encodedname.Replace("<", "").Replace('>', '$');


					if (usedNames.Contains(encodedname))
					{
						int suffix = 1;
						string newname;
						do
						{
							suffix++;
							newname = encodedname + "$" + suffix;
						} while (usedNames.Contains(newname));
						encodedname = newname;
					}

					usedNames.Add(encodedname);
					sortedSymbolsWithNames.Add(new KeyValuePair<ObjectWithAddress, string>(symbol, encodedname));
				}

				// Write routines/objects.
				for (int i = 0; i < sortedSymbolsWithNames.Count; i++)
				{
					KeyValuePair<ObjectWithAddress, string> pair = sortedSymbolsWithNames[i];
					ObjectWithAddress symbol = pair.Key;

					Utilities.Assert(Utilities.IsWordAligned(symbol.Offset), "Not word-aligned.");

					writer.WriteLine();
					writer.WriteLine();
					writer.Write("  # {0} (0x{1:x} - 0x{2:x} bytes)", symbol.Name, symbol.Offset, symbol.Size);

					int roundedSize = Utilities.Align4(symbol.Size);
					WriteAssemblyData(writer, code, symbol.Offset / 4, roundedSize / 4);

					// Write padding.
					if (i < sortedSymbolsWithNames.Count - 1)
					{
						ObjectWithAddress nextSymbol = sortedSymbolsWithNames[i + 1].Key;
						if (symbol.Offset + roundedSize != nextSymbol.Offset)
						{
							writer.WriteLine();
							writer.WriteLine();
							int paddingOffset = symbol.Offset + roundedSize;
							int paddingSize = nextSymbol.Offset - paddingOffset;
							WriteAssemblyData(writer, code, paddingOffset / 4, paddingSize / 4);
						}
					}
				}

				// Write libraries.
				if (sortedSymbolsWithNames.Count > 0)
				{
					ObjectWithAddress lastSymbol = sortedSymbolsWithNames[sortedSymbolsWithNames.Count - 1].Key;
					int afterLastWrittenCodeIndex = (lastSymbol.Offset + Utilities.Align4(lastSymbol.Size)) / 4;
					writer.WriteLine();
					writer.WriteLine();
					writer.WriteLine("  # Other code/data begins here.");
					WriteAssemblyData(writer, code, afterLastWrittenCodeIndex, code.Length - afterLastWrittenCodeIndex);
				}

				
				writer.WriteLine();
				writer.WriteLine();

				// Write the symbol values.
				foreach (KeyValuePair<ObjectWithAddress, string> item in sortedSymbolsWithNames)
				{
					ObjectWithAddress obj = item.Key;
					string name = item.Value;

					if (obj.Offset == 0)
						continue;

					WriteAssemblySymbolDefinition(writer, name, obj.Offset);
				}

				// Write used library symbols.
				if (libman != null)
				{
					foreach (LibraryManager.LibraryUsage usage in libman.Usage)
					{
						foreach (LibraryMethod lm in usage.UsedMethods)
						{
							WriteAssemblySymbolDefinition(writer, lm.Name, lm.Offset);
						}
					}
				}
			}
		}

		private static void WriteAssemblySymbolDefinition(StreamWriter writer, string name, int offset)
		{
			writer.Write(@"
  .globl {0}
  .type {0}, @object
  .set {0}, main + 0x{1:x}
", name, offset);
		}

		private static void WriteAssemblyData(TextWriter writer, int[] arr, int startindex, int wordcount)
		{
			for (int i = 0; i < wordcount; i++)
			{
				if (i % 4 == 0)
				{
					writer.WriteLine();
					writer.Write("  .int ");
				}
				else writer.Write(", ");

				writer.Write("0x" + arr[startindex + i].ToString("x8"));
			}
		}

		#endregion


		public int[] GetEmittedCode()
		{
			if (State < CompileContextState.S7CodeEmitted)
				throw new InvalidOperationException("State: " + State);

			Utilities.Assert(_emittedCode != null, "_emittedCode != null");

			int[] copy = new int[_emittedCode.Length];
			Utilities.CopyCode(_emittedCode, 0, copy, 0, _emittedCode.Length);

			return copy;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="arguments"></param>
		/// <param name="marshaler">The marshaler which is to be used. Null is ok.</param>
		/// <returns></returns>
		internal int[] GetEmittedCode(object[] arguments, Marshaler marshaler)
		{
			if (EntryPoint.Parameters.Count != arguments.Length)
				throw new ArgumentException(string.Format("Invalid number of arguments in array; expected {0}, got {1}.",
														  EntryPoint.Parameters.Count, arguments.Length));
			Utilities.Assert(ArgumentArea.Size == arguments.Length * 16, "cc.ArgumentArea.Size == arguments.Length * 16");

			if (marshaler == null)
				marshaler = new Marshaler();
			byte[] argmem = marshaler.GetArgumentsImage(arguments);
			if (argmem.Length != ArgumentArea.Size)
				throw new NotSupportedException("Argument buffer is not the same size as argument area. Entry point arguments can only take up one quadword each.");

			int[] code = GetEmittedCode();
			Buffer.BlockCopy(argmem, 0, code, ArgumentArea.Offset, argmem.Length);

			return code;
		}
	}
}
