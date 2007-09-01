using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

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
		private Dictionary<string, MethodCompiler> _methodDict = new Dictionary<string, MethodCompiler>();
		private SpecialSpeObjects _specialSpeObjects;

		/// <summary>
		/// The first method that is run.
		/// </summary>
		public MethodCompiler EntryPoint
		{
			get { return _entryPoint; }
		}

		private MethodBase _entryMethod;

		public ICollection<MethodCompiler> Methods
		{
			get { return _methodDict.Values; }
		}

		public CompileContext(MethodBase entryPoint)
		{
			State = CompileContextState.S1Initial;
			if (!entryPoint.IsStatic)
				throw new ArgumentException("Only static methods are supported.");

			_entryMethod = entryPoint;
		}

		private CompileContextState _state;

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

			if (targetState >= CompileContextState.S5MethodAddressesDetermined && State < CompileContextState.S5MethodAddressesDetermined)
				PerformMethodAddressDetermination();

			if (targetState >= CompileContextState.S6AddressPatchingDone && State < CompileContextState.S6AddressPatchingDone)
				PerformAddressPatching();

			if (targetState >= CompileContextState.S7CodeEmitted && State < CompileContextState.S7CodeEmitted)
				PerformCodeEmission();

			State = CompileContextState.S8Complete;

			if (targetState > CompileContextState.S8Complete)
				throw new ArgumentException("Invalid target state: " + targetState, "targetState");
		}

		private void PerformRegisterAllocation()
		{
			AssertState(CompileContextState.S4RegisterAllocationDone - 1);

			foreach (MethodCompiler mc in Methods)
				mc.PerformProcessing(MethodCompileState.S7RemoveRedundantMoves);

			State = CompileContextState.S4RegisterAllocationDone;
		}

		private int _totalCodeSize = -1;

		/// <summary>
		/// Determines local storage addresses for the methods.
		/// </summary>
		private void PerformMethodAddressDetermination()
		{
			AssertState(CompileContextState.S5MethodAddressesDetermined - 1);

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
			AssertState(CompileContextState.S6AddressPatchingDone - 1);

			foreach (ObjectWithAddress owa in GetAllObjects())
			{
				if (owa is MethodCompiler)
					((MethodCompiler) owa).PerformProcessing(MethodCompileState.S8AddressPatchingDone);
				else if (owa is SpuRoutine)
					((SpuRoutine) owa).PerformAddressPatching();
			}
			

			State = CompileContextState.S6AddressPatchingDone;
		}

		private List<SpuRoutine> _spuRoutines;
		private RegisterSizedObject _returnValueLocation;
		private int[] _emittedCode;
		private DataObject _argumentArea;

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
			if (EntryPoint.ReturnType != StackTypeDescription.None)
				_returnValueLocation = new RegisterSizedObject("ReturnValueLocation");

			_argumentArea = DataObject.FromQuadWords(EntryPoint.Parameters.Count, "ArgumentArea");

			_spuRoutines = new List<SpuRoutine>();
			SpuInitializer init = new SpuInitializer(EntryPoint, _returnValueLocation, _specialSpeObjects.StackSizeObject);

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
			foreach (SpuRoutine routine in GetSpuRoutines())
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
			if (State < CompileContextState.S6AddressPatchingDone)
				throw new InvalidOperationException("Address patching has not yet been performed.");

			return GetAllObjects();
		}

		private void PerformCodeEmission()
		{
			AssertState(CompileContextState.S7CodeEmitted - 1);

			List<ObjectWithAddress> objects = GetAllObjects();
			List<SpuRoutine> routines = new List<SpuRoutine>();
			foreach (ObjectWithAddress o in objects)
			{
				SpuRoutine routine = o as SpuRoutine;
				if (routine != null)
					routines.Add(routine);
			}
			_emittedCode = new int[Utilities.Align16(_totalCodeSize) / 4];
			CopyCode(_emittedCode, routines);

			// Initialize memory allocation.
			{
				const int TotalSpeMem = 256 * 1024;
				const int StackSize = 8*1024;
				int codeByteSize = _emittedCode.Length*4;
				_specialSpeObjects.SetMemorySettings(StackSize, codeByteSize, TotalSpeMem - codeByteSize - StackSize);

				// We only need to write to the preferred slot.
				_emittedCode[_specialSpeObjects.AllocatableByteCountObject.Offset/4] = _specialSpeObjects.AllocatableByteCount;
				_emittedCode[_specialSpeObjects.AllocatableByteCountObject.Offset/4+1] = _specialSpeObjects.AllocatableByteCount;
				_emittedCode[_specialSpeObjects.AllocatableByteCountObject.Offset/4+2] = _specialSpeObjects.AllocatableByteCount;
				_emittedCode[_specialSpeObjects.AllocatableByteCountObject.Offset/4+3] = _specialSpeObjects.AllocatableByteCount;
				_emittedCode[_specialSpeObjects.NextAllocationStartObject.Offset/4] = _specialSpeObjects.NextAllocationStart;
				_emittedCode[_specialSpeObjects.NextAllocationStartObject.Offset/4+1] = _specialSpeObjects.NextAllocationStart;
				_emittedCode[_specialSpeObjects.NextAllocationStartObject.Offset/4+2] = _specialSpeObjects.NextAllocationStart;
				_emittedCode[_specialSpeObjects.NextAllocationStartObject.Offset/4+3] = _specialSpeObjects.NextAllocationStart;
				_emittedCode[_specialSpeObjects.StackSizeObject.Offset/4] = _specialSpeObjects.StackSize;
			}

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
			AssertState(CompileContextState.S2TreeConstructionDone - 1);

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
				_methodDict.Add(nextmethodkey, mc);

				if (isfirst)
				{
					_entryPoint = mc;
					isfirst = false;
				}

				// Find referenced methods.
				mc.ForeachTreeInstruction(
					delegate(TreeInstruction inst)
         			{
						MethodBase mr = inst.Operand as MethodBase;
						if (mr == null)
							return;

						string methodkey = CreateMethodRefKey(mr);
         				MethodCompiler calledMethod;
						if (_methodDict.TryGetValue(methodkey, out calledMethod))
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

		static internal void CopyCode(int[] targetBuffer, ICollection<SpuRoutine> objects)
		{
			Set<int> usedOffsets = new Set<int>();

			foreach (SpuRoutine routine in objects)
			{
				int[] code = routine.Emit();

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

		/// <summary>
		/// Writes the code to the file with instructions on how to compile it.
		/// The executable should be ready for debugging with spu-gdb.
		/// </summary>
		/// <param name="filename"></param>
		public void WriteAssemblyToFile(string filename)
		{
			int[] code = GetEmittedCode();

			using (StreamWriter writer = new StreamWriter(filename, false, Encoding.UTF8))
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
main:
", Path.GetFileName(filename), Path.GetFileNameWithoutExtension(filename), DateTime.Now);

				List<ObjectWithAddress> symbols = new List<ObjectWithAddress>(GetAllObjectsForDisassembly());
				symbols.Sort(delegate(ObjectWithAddress x, ObjectWithAddress y) { return x.Offset - y.Offset; });
				

				// We don't want duplicate names.
				Set<string> usedNames = new Set<string>(symbols.Count);
				List<KeyValuePair<ObjectWithAddress, string>> symbolsWithNames = new List<KeyValuePair<ObjectWithAddress, string>>();
				foreach (ObjectWithAddress symbol in symbols)
				{
					// Anonymous methods contains '<' and '>' in their name.
					string encodedname = symbol.Name;
					encodedname = encodedname.Replace("<", "").Replace('>', '$');

					if (usedNames.Contains(encodedname))
					{
						int suffix = 0;
						string newname;
						do
						{
							suffix++;
							newname = encodedname + "$" + suffix;
						} while (usedNames.Contains(encodedname));
						encodedname = newname;
					}

					symbolsWithNames.Add(new KeyValuePair<ObjectWithAddress, string>(symbol, encodedname));
				}



				int wordOffset = 0;
				int byteOffset = 0;
				int nextSymIndex = 0;
				const int wordsPerLine = 4;
				const int bytesPerLine = wordsPerLine * 4;
				while (wordOffset < code.Length)
				{
					// Comment where symbols starts.
					bool firstSymbolBlockLine = true;
					while (nextSymIndex < symbols.Count && symbols[nextSymIndex].Offset >= byteOffset
						&& symbols[nextSymIndex].Offset < byteOffset + bytesPerLine)
					{
						ObjectWithAddress symbol = symbols[nextSymIndex];

						if (firstSymbolBlockLine)
						{
							writer.WriteLine();
							firstSymbolBlockLine = false;
						}

						writer.Write("# {0} (0x{1:x})", symbol.Name, symbol.Offset);
						if (symbol.Offset > byteOffset)
							writer.Write(" (line offset 0x" + (symbol.Offset - byteOffset).ToString("x") + ")");

						writer.WriteLine();
						nextSymIndex++;
					}

					const bool useKnownToWorkByteSyntax = false;
					if (useKnownToWorkByteSyntax)
						writer.Write("  .byte ");
					else
						writer.Write("  .int ");
					for (int i = 0; i < wordsPerLine && wordOffset + i < code.Length; i++)
					{
						if (i > 0)
							writer.Write(",  ");
						uint inst = (uint) code[wordOffset + i];

						if (useKnownToWorkByteSyntax)
						{
							writer.Write("0x" + ((byte)(inst >> 24)).ToString("x2"));
							writer.Write(", 0x" + ((byte)(inst >> 16)).ToString("x2"));
							writer.Write(", 0x" + ((byte)(inst >> 8)).ToString("x2"));
							writer.Write(", 0x" + ((byte)(inst >> 0)).ToString("x2"));
						}
						else 
							writer.Write("0x" + inst.ToString("x8"));
					}
					writer.WriteLine();

					wordOffset += wordsPerLine;
					byteOffset = wordOffset * 4;
				}
				
				writer.WriteLine();
				writer.WriteLine();

				// Write the symbol values.
				foreach (KeyValuePair<ObjectWithAddress, string> item in symbolsWithNames)
				{
					ObjectWithAddress obj = item.Key;
					string name = item.Value;

					if (obj.Offset == 0)
						continue;


					writer.Write(@"
  .globl {0}
  .type {0}, @object
  .set {0}, main + 0x{1:x}
", name, obj.Offset);
				}
			}
		}
	}
}
