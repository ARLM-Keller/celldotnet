using System;
using System.Collections.Generic;
using System.Reflection;

namespace CellDotNet
{
	/// <summary>
	/// Used to determine information about intrinsics.
	/// </summary>
	class IntrinsicsManager
	{
		struct MethodKey
		{
			private readonly RuntimeMethodHandle MethodHandle;

			public MethodKey(MethodBase method)
			{
				MethodHandle = method.MethodHandle;
			}
		}

		static private object s_lock = new object();
		static private Dictionary<MethodKey, SpuIntrinsicFunction> s_map;


		public IntrinsicsManager()
		{
			lock (s_lock)
			{
				if (s_map == null)
					ConstructIntrinsicsMap();
			}
		}

		public bool TryGetIntrinsic(MethodInfo method, out SpuIntrinsicFunction intrinsic)
		{
			return s_map.TryGetValue(new MethodKey(method), out intrinsic);
		}

		static private void ConstructIntrinsicsMap()
		{
			Dictionary<MethodKey, SpuIntrinsicFunction> map = new Dictionary<MethodKey, SpuIntrinsicFunction>();

			Type[] typesWithIntrinsics = new Type[] { typeof(Dma), typeof(SpuRuntime) };
			foreach (Type type in typesWithIntrinsics)
			{
				MethodInfo[] methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public);
				foreach (MethodInfo mi in methods)
				{
					object[] arr = mi.GetCustomAttributes(typeof (IntrinsicMethodAttribute), false);
					if (arr.Length != 1)
						continue;

					IntrinsicMethodAttribute att = (IntrinsicMethodAttribute) arr[0];
					map.Add(new MethodKey(mi), att.Intrinsic);
				}
			}

			s_map = map;
		}
	}
}