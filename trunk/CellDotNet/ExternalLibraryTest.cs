using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NUnit.Framework;

namespace CellDotNet
{
	[TestFixture]
	public class ExternalLibraryTest
	{
		[DllImport("ExternalTestLibrary1")]
		private static extern int ExternalTestMethod1(int arg);

		class FakeLibrary : ExternalLibrary
		{
			private ExternalMethod _method;


			public FakeLibrary(ExternalMethod method)
			{
				_method = method;
			}

			public override ExternalMethod ResolveMethod(string name)
			{
				return _method;
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
		public void TestMethod1()
		{
			Converter<int, int> del = ExternalTestMethod1;

			CompileContext cc = new CompileContext(del.Method);

			cc.PerformProcessing(CompileContextState.S8Complete);

//			cc.EntryPoint

			// Tjek:
			// At den fundne externalmethod er det samme objekt som vi laver her.


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
