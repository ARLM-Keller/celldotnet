using System;
using System.Collections.Generic;
using System.Reflection;

namespace CellDotNet
{
	class SystemLibMap
	{
		private static Dictionary<MethodBase, MethodBase> _libmap = InitializeMap();

		public static MethodBase GetUseableMethodBase(MethodBase method)
		{
			if (method == null)
				return null;

			MethodBase mb;
			if (_libmap.TryGetValue(method, out mb))
			{
				return mb;
			}
			else
			{
				return method;
			}
		}

		private static Dictionary<MethodBase, MethodBase> InitializeMap()
		{
			Dictionary<MethodBase, MethodBase> map = new Dictionary<MethodBase, MethodBase>();
			map.Add(typeof(System.Math).GetMethod("Abs", new Type[] { typeof(Single) }), typeof(CellDotNet.SpuMath).GetMethod("Abs", new Type[] { typeof(Single) }));
			map.Add(typeof(System.Math).GetMethod("Min", new Type[] { typeof(Single), typeof(Single) }), typeof(CellDotNet.SpuMath).GetMethod("Min", new Type[] { typeof(Single), typeof(Single) }));
			map.Add(typeof(System.Math).GetMethod("Max", new Type[] { typeof(Single), typeof(Single) }), typeof(CellDotNet.SpuMath).GetMethod("Max", new Type[] { typeof(Single), typeof(Single) }));

			map.Add(typeof(System.Math).GetMethod("Abs", new Type[] { typeof(int)}), typeof(CellDotNet.SpuMath).GetMethod("Abs", new Type[] { typeof(int)}));
			map.Add(typeof(System.Math).GetMethod("Min", new Type[] { typeof(int), typeof(int) }), typeof(CellDotNet.SpuMath).GetMethod("Min", new Type[] { typeof(int), typeof(int) }));
			map.Add(typeof(System.Math).GetMethod("Max", new Type[] { typeof(int), typeof(int) }), typeof(CellDotNet.SpuMath).GetMethod("Max", new Type[] { typeof(int), typeof(int) }));

			return map;
		}
	}
}
