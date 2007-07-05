using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Metadata;
using NUnit.Framework;

namespace CellDotNet
{
	[TestFixture]
	public class MethodCompilerTest
	{
		private delegate void BasicTestDelegate();

		public static MethodDefinition GetMethod(Delegate a)
		{
			MethodInfo m = a.Method;
			return GetMethod(m);
		}

		private static MethodDefinition GetMethod(MethodInfo m)
		{
			AssemblyDefinition ass = AssemblyFactory.GetAssembly(m.DeclaringType.Assembly.Location);
			return (MethodDefinition)ass.MainModule.LookupByToken(new MetadataToken(m.MetadataToken));
		}

		[Test]
		public void TestBuildTree()
		{
			Converter<int, long> del =
				delegate(int i)
				{
					int j;

					char c1;
					char c2;

					j = 8 + (i * 5);
					c1 = (char)j;
					c2 = (char)(j + 1);
					j += c1 + c2;
					//						DateTime[] arr= new DateTime[0];
					if (i > 5)
						j++;
					while (j < 0)
					{
						j--;
					}
					//						int[] arr = new int[4];
					//						arr[1] = 9;

					return j * 2;
				};
			MethodDefinition method = GetMethod(del);
			MethodCompiler ci = new MethodCompiler(method);
			ci.PerformProcessing(MethodCompileState.TreeConstructionDone);


			TreeDrawer td = new TreeDrawer();
			td.DrawMethod(ci, method);
		}


		[Test]
		public void TestParseFunctionCall()
		{
			BasicTestDelegate del = delegate
										{
											int rem;
											Math.DivRem(9, 13, out rem);
											Math.Max(Math.Min(3, 1), 5L);
										};
			MethodDefinition method = GetMethod(del);
			MethodCompiler ci = new MethodCompiler(method);
			ci.PerformProcessing(MethodCompileState.TreeConstructionDone);
			new TreeDrawer().DrawMethod(ci, method);
		}

		[Test]
		public void TestParseObjectInstantiationAndInstanceMethodCall()
		{
			BasicTestDelegate del = delegate
										{
											ArrayList list = new ArrayList(34);
											list.Clear();
										};
			MethodDefinition method = GetMethod(del);
			MethodCompiler ci = new MethodCompiler(method);
			ci.PerformProcessing(MethodCompileState.TreeConstructionDone);
			new TreeDrawer().DrawMethod(ci, method);
		}

		[Test]
		public void TestParseArrayInstantiation()
		{
			BasicTestDelegate del = delegate
										{
											int[] arr = new int[5];
											int j = arr.Length;
										};
			MethodDefinition method = GetMethod(del);
			MethodCompiler ci = new MethodCompiler(method);
			ci.PerformProcessing(MethodCompileState.TreeConstructionDone);
			new TreeDrawer().DrawMethod(ci, method);
		}

		[Test]
		public void Test()
		{
#if !DEBUG 
			Assert.Fail("No debug mode.");
#endif
		}
	}
}
