using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace CellDotNet
{
	static class DelegateWrapper
	{
		/// <summary>
		/// <paramref name="delegateToBeWrapped"/> instance is only here to provide implicit specification of the delegate type. 
		/// null can be passed.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="delegateToBeWrapped"></param>
		/// <param name="interceptor">
		/// The delegate which is supposed to function as interceptor for the <typeparamref name="T"/> delegate type.
		/// </param>
		/// <returns></returns>
		public static T CreateWrapper<T>(T delegateToBeWrapped, Action<object[]> interceptor) where T : class
		{
			Utilities.AssertArgument(typeof(Delegate).IsAssignableFrom(typeof(T)), "T is not a delegate type.");

			return new DelegateWrapperImpl<T>(interceptor).Wrapper;
		}

		class DelegateWrapperImpl<T> where T : class
		{
			private readonly Action<object[]> _interceptor;
			public T Wrapper;

			public DelegateWrapperImpl(Action<object[]> interceptor)
			{
				_interceptor = interceptor;

				Delegate w = CreateWrapperDelegate(typeof (T).GetMethod("Invoke"));
				Wrapper = w as T;
				Utilities.DebugAssert(Wrapper != null, "Wrapper != null");
			}


			private Delegate CreateWrapperDelegate(MethodInfo method)
//			private Delegate CreateWrapperDelegate(Delegate del, MethodInfo method)
			{
				// The parameters are the same as those of the original delegate,
				// but the "this" parameters is also mentioned explicitly here.
				var paramtypes = new List<Type> { typeof(Action<object[]>) };
				foreach (ParameterInfo parameter in method.GetParameters())
					paramtypes.Add(parameter.ParameterType);

				var dm = new DynamicMethod(method.Name + "-sperunner",
										   method.ReturnType, paramtypes.ToArray(), GetType(), true);

				/// body:
				/// 1: create object[] for args.
				/// 2: save array in local.
				/// 3: for each arg (excluding the this-arg):
				///    load array fra local
				///    load param index
				///    save in array.
				/// -  At this point the stack is empty.	
				/// 4: Load this-arg onto stack.
				/// 5: Load array onto stack.
				/// 6: Call interceptor method.
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
					ilgen.Emit(OpCodes.Box, paramtypes[i + 1]);

					ilgen.Emit(OpCodes.Stelem, typeof(object));
				}

				// Call work method.
				ilgen.Emit(OpCodes.Ldarg_0);
				ilgen.Emit(OpCodes.Ldloc, arr);
				ilgen.EmitCall(OpCodes.Callvirt, _interceptor.Method, null);
//				ilgen.EmitCall(OpCodes.Callvirt, new Func<object[], object>(SpeDelegateWrapperExecute).Method, null);

				// Handle return value.
				if (method.ReturnType != typeof(void))
				{
					ilgen.Emit(OpCodes.Unbox_Any, method.ReturnType);
				}
				ilgen.Emit(OpCodes.Ret);

				return dm.CreateDelegate(typeof(T), this);
//				return dm.CreateDelegate(del.GetType(), this);
			}
		}
	}
}
