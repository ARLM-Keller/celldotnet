// 
// Copyright (C) 2007 Klaus Hansen and Rasmus Halland
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

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
		private readonly CompileContext _compileContext;
		private int[] _spuCode;
		private readonly Delegate _typedWrapperDelegate;
		private readonly Delegate _typedOriginalDelegate;


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
			Utilities.AssertArgumentNotNull(delegateToWrap, "delegateToWrap");
			Delegate del = delegateToWrap as Delegate;
			if (delegateToWrap == null)
				throw new ArgumentException("Argument must be of delegate type.");

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
			_compileContext = new CompileContext(method);
			_compileContext.PerformProcessing(CompileContextState.S8Complete);
			_spuCode = _compileContext.GetEmittedCode();

			_typedWrapperDelegate = CreateWrapperDelegate(delegateToWrap, method);
			_typedOriginalDelegate = delegateToWrap;
		}

		private Delegate CreateWrapperDelegate(Delegate del, MethodInfo method)
		{
			// The parameters are the same as those of the original delegate,
			// but the "this" parameters is also mentioned explicitly here.
			var paramtypes = new List<Type> {typeof (SpeDelegateRunner)};
			foreach (ParameterInfo parameter in method.GetParameters())
				paramtypes.Add(parameter.ParameterType);

			var dm = new DynamicMethod(method.Name + "-sperunner",
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
			/// 7a: If delegate has non-void return type: ret.
			/// 7b: If delegate has void return type: pop, ret.

			ILGenerator ilgen = dm.GetILGenerator();

			// Create object[].
			LocalBuilder arr = ilgen.DeclareLocal(typeof(object[]));
			ilgen.Emit(OpCodes.Ldc_I4, paramtypes.Count - 1);
			ilgen.Emit(OpCodes.Newarr, typeof(object));
			// Save array in local.
			ilgen.Emit(OpCodes.Stloc, arr);

			// Save delegate args in array.
			for (int i = 0; i < paramtypes.Count - 1; i++)
			{
				ilgen.Emit(OpCodes.Ldloc, arr);

				ilgen.Emit(OpCodes.Ldc_I4, i);
				ilgen.Emit(OpCodes.Conv_I);

				ilgen.Emit(OpCodes.Ldarg, i); // arg 0 is instance.
				ilgen.Emit(OpCodes.Box, paramtypes[i+1]);

				ilgen.Emit(OpCodes.Stelem, typeof(object));
			}

			// Call work method.
			ilgen.Emit(OpCodes.Ldarg_0);
			ilgen.Emit(OpCodes.Ldloc, arr);
			ilgen.EmitCall(OpCodes.Callvirt, new Func<object[], object>(SpeDelegateWrapperExecute).Method, null);

			// Handle return value.
			if (method.ReturnType != typeof(void))
			{
				ilgen.Emit(OpCodes.Unbox_Any, method.ReturnType);
			}
			ilgen.Emit(OpCodes.Ret);

			return dm.CreateDelegate(del.GetType(), this);
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
				return sc.RunProgram(_compileContext, args);

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
