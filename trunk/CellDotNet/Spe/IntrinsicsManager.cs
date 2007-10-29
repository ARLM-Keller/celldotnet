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

namespace CellDotNet.Spe
{
	/// <summary>
	/// Used to determine information about intrinsics.
	/// 
	/// <para>
	/// NB: Currently (20070906) this class isn't used. Maybe it should be deleted...
	/// </para>
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
		static private Dictionary<MethodKey, SpuIntrinsicMethod> s_map;


		public IntrinsicsManager()
		{
			lock (s_lock)
			{
				if (s_map == null)
					ConstructIntrinsicsMap();
			}
		}

		public bool TryGetIntrinsic(MethodInfo method, out SpuIntrinsicMethod intrinsic)
		{
			return s_map.TryGetValue(new MethodKey(method), out intrinsic);
		}

		static private void ConstructIntrinsicsMap()
		{
			Dictionary<MethodKey, SpuIntrinsicMethod> map = new Dictionary<MethodKey, SpuIntrinsicMethod>();

			Type[] typesWithIntrinsics = new Type[] { typeof(Mfc), typeof(SpuRuntime) };
			foreach (Type type in typesWithIntrinsics)
			{
				MethodInfo[] methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
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
