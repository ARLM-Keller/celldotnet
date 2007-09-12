using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using NUnit.Framework;

namespace CellDotNet
{
	[TestFixture]
	public class ExternalLibraryTest : UnitTest
	{
		[DllImport("ExternalTestLibrary1")]
		private static extern int ExternalTestMethod1(int arg);

		class FakeLibrary : ExternalLibrary
		{
			private ExternalMethod _method;

			public FakeLibrary()
			{
			}

			public override ExternalMethod ResolveMethod(MethodInfo reflectionMethod)
			{
				return _method;
			}

			public void SetSingleMethod(ExternalMethod method)
			{
				_method = method;
			}
		}

		class FakeLibraryResolver : ExternalLibraryResolver
		{
			private FakeLibrary _library;

			public FakeLibraryResolver(FakeLibrary library)
			{
				_library = library;
			}

			public override ExternalLibrary ResolveLibrary(string dllImportName)
			{
				return _library;
			}
		}

		[Test]
		public void TestParseAndResolveFakeMethod()
		{
			Converter<int, int> del = ExternalTestMethod1;

			FakeLibrary lib = new FakeLibrary();
			ExternalMethod method = new ExternalMethod("TestMethod", lib, 200, del.Method);
			lib.SetSingleMethod(method);
			FakeLibraryResolver resolver = new FakeLibraryResolver(lib);

			CompileContext cc = new CompileContext(del.Method);
			cc.SetLibraryResolver(resolver);
			cc.PerformProcessing(CompileContextState.S8Complete);

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
	}
}
