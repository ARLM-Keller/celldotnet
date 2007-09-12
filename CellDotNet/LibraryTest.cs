using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using NUnit.Framework;

namespace CellDotNet
{
	[TestFixture]
	public class LibraryTest : UnitTest
	{
		[DllImport("ExternalTestLibrary1")]
		private static extern int ExternalTestMethod1(int arg);

		class FakeLibrary : Library
		{
			private LibraryMethod _method;

			public FakeLibrary() : base(new byte[30])
			{
			}

			public FakeLibrary(byte[] contents) : base(contents)
			{
				
			}

			public override LibraryMethod ResolveMethod(MethodInfo reflectionMethod)
			{
				return _method;
			}

			public void SetSingleMethod(LibraryMethod method)
			{
				_method = method;
			}
		}

		class FakeLibraryResolver : LibraryResolver
		{
			private FakeLibrary _library;

			public FakeLibraryResolver(FakeLibrary library)
			{
				_library = library;
			}

			public override Library ResolveLibrary(string dllImportName)
			{
				return _library;
			}
		}

		[Test]
		public void TestParseAndResolveFakeMethod()
		{
			Converter<int, int> del = ExternalTestMethod1;

			FakeLibrary lib = new FakeLibrary();
			LibraryMethod method = new LibraryMethod("TestMethod", lib, 200, del.Method);
			lib.SetSingleMethod(method);
			FakeLibraryResolver resolver = new FakeLibraryResolver(lib);

			CompileContext cc = new CompileContext(del.Method);
			cc.SetLibraryResolver(resolver);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(0, cc.Methods.Count);
			Assert.AreSame(method, cc.EntryPoint);
		}

		[DllImport("NonExistingLibrary")]
		private static extern void MethodInNonExistingLibrary(int i);

		[Test, ExpectedException(typeof(DllNotFoundException))]
		public void TestNonExistingLibrary()
		{
			Action<int> del = MethodInNonExistingLibrary;
			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);
		}

		[DllImport("ManualRoutineLibrary")]
		private static extern int HandMadeExternalMethod(int arg);

		[Test]
		public void TestHandMadeExternalMethod()
		{
			Converter<int, int> del = HandMadeExternalMethod;

			// The routine adds 50 to the argument.
			ManualRoutine routine = new ManualRoutine(true, "ManualExternalRoutine");
			VirtualRegister fiftyRegister = HardwareRegister.GetHardwareRegister(10);
			routine.Writer.BeginNewBasicBlock();
			routine.Writer.WriteLoadI4(fiftyRegister, 50);
			routine.Writer.WriteA(HardwareRegister.GetHardwareArgumentRegister(0), fiftyRegister, HardwareRegister.HardwareReturnValueRegister);
			routine.Writer.WriteBi(HardwareRegister.LR);

			// Create library with the routine.
			int[] code = routine.Emit();
			AreEqual(3, code.Length);
			byte[] libcontents = Utilities.WriteBigEndianBytes(code);
			FakeLibrary lib = new FakeLibrary(libcontents);
			LibraryMethod libmethod = new LibraryMethod(routine.Name, lib, 0, del.Method);
			lib.SetSingleMethod(libmethod);
			FakeLibraryResolver resolver = new FakeLibraryResolver(lib);

			CompileContext cc = new CompileContext(del.Method);
			cc.SetLibraryResolver(resolver);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(0, cc.Methods.Count);
			Assert.AreSame(libmethod, cc.EntryPoint);

			cc.WriteAssemblyToFile("externalmethod.s", 5);

			if (!SpeContext.HasSpeHardware)
				return;

			object rv = SpeContext.UnitTestRunProgram(cc, 5);
			AreEqual(55, (int) rv);
		}
	}
}
