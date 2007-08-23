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

		[Test]
		public void TestDelegateRun_SingleReturn()
		{
			SingleReturnDelegate del = delegate { return 40; };
			SingleReturnDelegate del2 = CreateSpeDelegate(del);

			float retval = del2();
			AreEqual(40f, retval);
		}

		private static T CreateSpeDelegate<T>(T delegateToWrap) where T : class
		{
			return SpeUnitTestDelegateWrapper<T>.CreateUnitTestSpeDelegate(delegateToWrap);
//			return SpeDelegateWrapper<T>.CreateSpeDelegate(delegateToWrap);
		}

		/// <summary>
		/// Use this class to wrap a delegate with a new delegate of the same type; the
		/// new delegate will, when called, execute the code on an SPE.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		private class SpeDelegateWrapper<T> where T : class
		{
			private CompileContext _compileContext;
			private int[] _spuCode;
			private T _typedWrapperDelegate;
			private T _typedOriginalDelegate;


			protected T TypedWrapperDelegate
			{
				get { return _typedWrapperDelegate; }
			}

			protected Delegate WrapperDelegate
			{
				get { return _typedWrapperDelegate as Delegate; }
			}

			protected Delegate OriginalDelegate
			{
				get { return _typedOriginalDelegate as Delegate; }
			}

			public static T CreateSpeDelegate(T delegateToWrap)
			{
				SpeDelegateWrapper<T> wrapper = new SpeDelegateWrapper<T>(delegateToWrap);
				return wrapper.TypedWrapperDelegate;
			}

			/// <summary>
			/// Wraps the specified delegate in a new delegate of the same type;
			/// the returned delegate will execute the delegate on an SPE.
			/// </summary>
			/// <typeparam name="T"></typeparam>
			/// <param name="delegateToWrap"></param>
			/// <returns></returns>
			protected SpeDelegateWrapper(T delegateToWrap) 
			{
				Utilities.AssertArgumentNotNull(delegateToWrap, "delegateToWrap");
				if (!(delegateToWrap is Delegate))
					throw new ArgumentException("Argument is not a delegate.");

				Delegate del = delegateToWrap as Delegate;
				MethodInfo method = del.Method;
				Compile(method);

				CreateWrapperDelegate(del, method);
				_typedOriginalDelegate = delegateToWrap;
			}

			private void Compile(MethodInfo method)
			{
				_compileContext = new CompileContext(method);
				_compileContext.PerformProcessing(CompileContextState.S8Complete);
				_spuCode = _compileContext.GetEmittedCode();
			}

			private void CreateWrapperDelegate(Delegate del, MethodInfo method)
			{
				// The parameters are the same as those of the original delegate,
				// but the "this" parameters is also mentioned explicitly here.
				List<Type> paramtypes = new List<Type>();
				paramtypes.Add(typeof(SpeDelegateWrapper<T>));
				foreach (ParameterInfo parameter in method.GetParameters())
					paramtypes.Add(parameter.ParameterType);

				DynamicMethod dm = new DynamicMethod(method.Name + "-spewrapper",
				                                     method.ReturnType, paramtypes.ToArray(), GetType(), true);
				/// body:
				/// 1: create object[] til args.
				/// 2: save array in local.
				/// 3: for each arg (excluding the this-arg):
				///    load array fra local
				///    load param index
				///    save in array.
				/// -  At this point the stack is empty.
				/// 4: Load this-arg onto stack.
				/// 5: Load array onto stack.
				/// 6: Call work method.
				/// 7a: Hvis delegate har ikke-void-returtype: ret.
				/// 7b: Hvis delegate har void-returtype: pop, ret.

				ILGenerator ilgen = dm.GetILGenerator();

				// Create object[].
				LocalBuilder arr = ilgen.DeclareLocal(typeof (object[]));
				ilgen.Emit(OpCodes.Ldc_I4, paramtypes.Count - 1);
				ilgen.Emit(OpCodes.Newarr, typeof (object));
				// Save array in local.
				ilgen.Emit(OpCodes.Stloc, arr);

				// Save delegate args in array.
				for (int i = 1; i < paramtypes.Count; i++)
				{
					ilgen.Emit(OpCodes.Ldloc, arr);

					ilgen.Emit(OpCodes.Ldc_I4, i);
					ilgen.Emit(OpCodes.Conv_I);

					ilgen.Emit(OpCodes.Ldarg, i); // arg 0 is instance.
					ilgen.Emit(OpCodes.Box, paramtypes[i]);

					ilgen.Emit(OpCodes.Stelem, typeof (object));
				}

				// Call work method.
				ilgen.Emit(OpCodes.Ldarg_0);
				ilgen.Emit(OpCodes.Ldloc, arr);
				ilgen.EmitCall(OpCodes.Callvirt, new Converter<object[], object>(SpeDelegateWrapperExecute).Method, null);

				// Handle return value.
				if (method.ReturnType != typeof (void))
				{
					ilgen.Emit(OpCodes.Unbox_Any, method.ReturnType);
				}
				ilgen.Emit(OpCodes.Ret);

				Delegate wrapperDel = dm.CreateDelegate(del.GetType(), this);
				T typedWrapperDel = wrapperDel as T;
				Utilities.AssertNotNull(typedWrapperDel, "typedWrapperDel");
				_typedWrapperDelegate = typedWrapperDel;
			}

			/// <summary>
			/// This one is called from the wrapper delegate with its arguments.
			/// </summary>
			/// <param name="args">The arguments that the user called the wrapper delegate with.</param>
			/// <returns></returns>
			protected virtual object SpeDelegateWrapperExecute(object[] args)
			{
				using (SpeContext sc = new SpeContext())
				{
					return sc.RunProgram(_compileContext, _spuCode, args);

//					sc.LoadProgram(_spuCode);
//					sc.LoadArguments(_compileContext, args);
//					sc.Run();
//
//					object retval = null;
//					if (_compileContext.EntryPoint.ReturnType != StackTypeDescription.None)
//						retval = sc.DmaGetValue(_compileContext.EntryPoint.ReturnType, _compileContext.ReturnValueAddress);
//
//					return retval;
				}
			}
		}


		/// <summary>
		/// This class is meant to be used in unit test: When SPE hardware is available
		/// it will behave as <see cref="SpeDelegateWrapper{T}"/>; and when not available,
		/// it will simply execute the original delegate.
		/// <para>
		/// This allows a unit test to execute totally indifferent whether it's running with
		/// or without SPE hardware.
		/// </para>
		/// </summary>
		/// <typeparam name="T"></typeparam>
		private class SpeUnitTestDelegateWrapper<T> : SpeDelegateWrapper<T> where T : class
		{
			public static T CreateUnitTestSpeDelegate(T delegateToWrap)
			{
				SpeUnitTestDelegateWrapper<T> wrapper = new SpeUnitTestDelegateWrapper<T>(delegateToWrap);
				return wrapper.TypedWrapperDelegate;
			}

			private SpeUnitTestDelegateWrapper(T delegateToWrap) : base(delegateToWrap)
			{
			}

			protected override object SpeDelegateWrapperExecute(object[] args)
			{
				object retval;

				if (SpeContext.HasSpeHardware)
					retval = base.SpeDelegateWrapperExecute(args);
				else
				{
					Delegate del = OriginalDelegate;
					retval = del.DynamicInvoke(args);
				}

				return retval;
			}
		}
	}
}
