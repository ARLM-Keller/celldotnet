using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using NUnit.Framework;

namespace CellDotNet
{
	[TestFixture]
	public class SpeContextTest : UnitTest
	{
		[Test]
		public void SpeSimpleDMATest()
		{
			if (!SpeContext.HasSpeHardware)
				return;
			using (SpeContext ctxt = new SpeContext())
			{
				ctxt.LoadProgram(new int[] { 13 });

				int[] lsa = ctxt.GetCopyOffLocalStorage();

				if (lsa[0] != 13)
					Assert.Fail("DMA error.");
			}
		}

		private delegate void BasicTestDelegate();

		[Test]
		unsafe public void TestFirstCellProgram()
		{
			BasicTestDelegate del = delegate
										{
											int* i;
											i = (int*)0x40;
											*i = 34;
										};


			MethodBase method = del.Method;
			MethodCompiler mc = new MethodCompiler(method);
			mc.PerformProcessing(MethodCompileState.S2TreeConstructionDone);

			new TreeDrawer().DrawMethod(mc);


			mc.PerformProcessing(MethodCompileState.S4InstructionSelectionDone);
			mc.GetBodyWriter().WriteStop();

			Console.WriteLine();
			Console.WriteLine("Disassembly: ");
			Console.WriteLine(mc.GetBodyWriter().Disassemble());

			mc.PerformProcessing(MethodCompileState.S5RegisterAllocationDone);

			Console.WriteLine();
			Console.WriteLine("Disassembly after regalloc: ");
			Console.WriteLine(mc.GetBodyWriter().Disassemble());

			mc.PerformProcessing(MethodCompileState.S7RemoveRedundantMoves);

			Console.WriteLine();
			Console.WriteLine("Disassembly after remove of redundant moves: ");
			Console.WriteLine(mc.GetBodyWriter().Disassemble());

			int[] bincode = SpuInstruction.emit(mc.GetBodyWriter().GetAsList());

			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext ctx = new SpeContext())
			{
				ctx.LoadProgram(bincode);

				ctx.Run();
				int[] ls = ctx.GetCopyOffLocalStorage();

				if (ls[0x40 / 4] != 34)
				{
					Console.WriteLine("øv");
					Console.WriteLine("Value: {0}", ls[0x40/4]);
				}
			}
		}

		[Test]
		public void TestPutGetInt32()
		{
			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext ctx = new SpeContext())
			{
				ctx.DmaPutValue((LocalStorageAddress) 32, 33000);
				ctx.DmaPutValue((LocalStorageAddress) 64, 34000);

				int readvalue = ctx.DmaGetValue<int>((LocalStorageAddress) 32);
				AreEqual(33000, readvalue);
			}
		}

		[Test]
		public void TestPutGetFloat()
		{
			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext ctx = new SpeContext())
			{
				ctx.DmaPutValue((LocalStorageAddress)32, 33000f);
				ctx.DmaPutValue((LocalStorageAddress)64, 34000f);

				float readvalue = ctx.DmaGetValue<float>((LocalStorageAddress)32);
				AreEqual(33000f, readvalue);
			}
		}


		[Test]
		public void TestHasSpe()
		{
			// For this test we assume that a windows machine does not have spe hw,
			// and that anything else has spe hw.
			if (Path.DirectorySeparatorChar == '\\')
				IsFalse(SpeContext.HasSpeHardware);
			else
				IsTrue(SpeContext.HasSpeHardware);
		}

		[Test, Explicit]
		public void TestGetSpeControlArea()
		{
			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext spe = new SpeContext())
			{
				SpeControlArea area = spe.GetControlArea();
				AreEqual((uint)0, area.SPU_NPC);
			}
		}

		[Test]
		public void TestRunProgram_ReturnInt32_Manual()
		{
			const int magicNumber = 40;
			IntReturnDelegate del = delegate { return magicNumber; };
			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);
			Disassembler.DisassembleToConsole(cc);

			int[] code = cc.GetEmittedCode();

			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext ctx = new SpeContext())
			{
				ctx.LoadProgram(code);

				int rc = ctx.Run();
				AreEqual(0, rc);

				int retval = ctx.DmaGetValue<int>(cc.ReturnValueAddress);
				AreEqual(magicNumber, retval);
			}
		}

		[Test]
		public void TestRunProgram_ReturnInt32()
		{
			const int magicNumber = 40;
			IntReturnDelegate del = delegate { return magicNumber; };

			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext ctx = new SpeContext())
			{
				object retval = ctx.RunProgram(del);
				AreEqual(typeof(int), retval.GetType());
				AreEqual(magicNumber, (int)retval);
			}
		}

		[Test]
		public void TestRunProgram_ReturnSingle()
		{
			const float magicNumber = float.NaN;
			SingleReturnDelegate del = delegate { return magicNumber; };

			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext ctx = new SpeContext())
			{
				object retval = ctx.RunProgram(del);
				AreEqual(typeof(float), retval.GetType());
				AreEqual(magicNumber, (float)retval);
			}
		}

		private delegate int IntReturnDelegate();
		private delegate float SingleReturnDelegate();

		[Test]
		public void TestDelegateRun_IntReturn()
		{
			IntReturnDelegate del = delegate { return 40; };
			IntReturnDelegate del2 = CreateSpeDelegate(del);

			int retval = del2();
			AreEqual(40, retval);
		}


		/// <summary>
		/// Wraps the specified delegate in a new delegate of the same type;
		/// the returned delegate will execute the delegate on an SPE.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="delegateToWrap"></param>
		/// <returns></returns>
		private static T CreateSpeDelegate<T>(T delegateToWrap) where T : class
		{
			Utilities.AssertArgumentNotNull(delegateToWrap, "delegateToWrap");
			if (!(delegateToWrap is Delegate))
				throw new ArgumentException("Argument is not a delegate.");

			Delegate del = delegateToWrap as Delegate;
			MethodInfo method = del.Method;

			// This should problably only be done here during unit test,
			// since it will also be done when the wrapper delegate is returned.
			CompileContext cc = new CompileContext(method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			List<Type> paramtypes = new List<Type>();
			foreach (ParameterInfo parameter in method.GetParameters())
				paramtypes.Add(parameter.ParameterType);


			DynamicMethod dm = new DynamicMethod(method.Name + "-spewrapper", 
method.ReturnType, paramtypes.ToArray(), typeof(SpeContextTest));
			ILGenerator ilgen = dm.GetILGenerator();

			/// body:
			/// 1: create object[] til args.
			/// 2: gem array i local.
			/// 3: for hver arg:
			///    load array fra local
			///    load param indeks
			///    gem i array.
			/// -  Der er på nuværende tidspunkt intet på stakken.
			/// 4: load entry method token as token on stack.
			/// 5: Load array onto stack.
			/// 6: Call static work method.
			/// 7a: Hvis delegate har ikke-void-returtype: ret.
			/// 7b: Hvis delegate har void-returtype: pop, ret.

			// Create object[].
			LocalBuilder arr = ilgen.DeclareLocal(typeof (object[]));
			ilgen.Emit(OpCodes.Ldc_I4, paramtypes.Count);
			ilgen.Emit(OpCodes.Newarr, typeof(object));
			// Save array in local.
			ilgen.Emit(OpCodes.Stloc, arr);

			// Save delegate args in array.
			for (int i = 0; i < paramtypes.Count; i++)
			{
				ilgen.Emit(OpCodes.Ldloc, arr);

				ilgen.Emit(OpCodes.Ldc_I4, i);
				ilgen.Emit(OpCodes.Conv_I);

				ilgen.Emit(OpCodes.Ldarg, i);
				ilgen.Emit(OpCodes.Box, paramtypes[i]);

				ilgen.Emit(OpCodes.Stelem, typeof (object));
			}

			// Load entry method as token on stack.
			ilgen.Emit(OpCodes.Ldc_I4, method);

			// Call work method.
			ilgen.Emit(OpCodes.Ldloc, arr);
			MethodInfo workmethod = typeof(SpeContextTest).GetMethod("SpeDelegateWrapperExecute", BindingFlags.NonPublic | BindingFlags.Static);
			ilgen.EmitCall(OpCodes.Call, workmethod, null);
			if (method.ReturnType == typeof(void))
				ilgen.Emit(OpCodes.Pop);
			ilgen.Emit(OpCodes.Ret);

			Delegate wrapperDel = dm.CreateDelegate(del.GetType());
			T typedWrapperDel = wrapperDel as T;
			Utilities.AssertNotNull(typedWrapperDel, "typedWrapperDel");

			return typedWrapperDel;
		}

		private static object SpeDelegateWrapperExecute(int methodtoken, params object[] args)
		{
			MethodBase targetMethod = typeof (SpeContextTest).Module.ResolveMethod(methodtoken);
			throw new Exception("the delegate made it!!");
			
		}
	}
}
