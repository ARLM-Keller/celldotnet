using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace CellDotNet
{
	/// <summary>
	/// Use this class to wrap a delegate with a new delegate of the same type; the
	/// new delegate will, when called, execute the code on an SPE.
	/// </summary>
	internal class SpeDelegateRunner
	{
		private CompileContext _compileContext;
		private int[] _spuCode;
		private Delegate _typedWrapperDelegate;
		private Delegate _typedOriginalDelegate;


		public CompileContext CompileContext
		{
			get { return _compileContext; }
		}

		protected Delegate WrapperDelegate
		{
			get { return _typedWrapperDelegate; }
		}

		protected Delegate OriginalDelegate
		{
			get { return _typedOriginalDelegate; }
		}

		public static T CreateSpeDelegate<T>(T delegateToWrap) where T : class
		{
			Delegate del = delegateToWrap as Delegate;
			SpeDelegateRunner runner = new SpeDelegateRunner(del);
			return runner.WrapperDelegate as T;
		}

		/// <summary>
		/// Wraps the specified delegate in a new delegate of the same type;
		/// the returned delegate will execute the delegate on an SPE.
		/// </summary>
		/// <param name="delegateToWrap"></param>
		/// <returns></returns>
		protected SpeDelegateRunner(Delegate delegateToWrap)
		{
			Utilities.AssertArgumentNotNull(delegateToWrap, "delegateToWrap");

			MethodInfo method = delegateToWrap.Method;
			Compile(method);

			CreateWrapperDelegate(delegateToWrap, method);
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
			paramtypes.Add(typeof(SpeDelegateRunner));
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
			LocalBuilder arr = ilgen.DeclareLocal(typeof(object[]));
			ilgen.Emit(OpCodes.Ldc_I4, paramtypes.Count - 1);
			ilgen.Emit(OpCodes.Newarr, typeof(object));
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

				ilgen.Emit(OpCodes.Stelem, typeof(object));
			}

			// Call work method.
			ilgen.Emit(OpCodes.Ldarg_0);
			ilgen.Emit(OpCodes.Ldloc, arr);
			ilgen.EmitCall(OpCodes.Callvirt, new Converter<object[], object>(SpeDelegateWrapperExecute).Method, null);

			// Handle return value.
			if (method.ReturnType != typeof(void))
			{
				ilgen.Emit(OpCodes.Unbox_Any, method.ReturnType);
			}
			ilgen.Emit(OpCodes.Ret);

			Delegate wrapperDel = dm.CreateDelegate(del.GetType(), this);
			_typedWrapperDelegate = wrapperDel;
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
}
