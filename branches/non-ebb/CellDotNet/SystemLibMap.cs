using System;
using System.Collections.Generic;
using System.Reflection;

namespace CellDotNet
{
	static class SystemLibMap
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

			map.Add(new Converter<float, float>(Math.Abs).Method, new Converter<float, float>(SpuMath.Abs).Method);
			map.Add(new Func<float, float, float>(Math.Min).Method, new Func<float, float, float>(SpuMath.Min).Method);
			map.Add(new Func<float, float, float>(Math.Max).Method, new Func<float, float, float>(SpuMath.Max).Method);

			map.Add(new Converter<int, int>(Math.Abs).Method, new Converter<int, int>(SpuMath.Abs).Method);
			map.Add(new Func<int, int, int>(Math.Min).Method, new Func<int, int, int>(SpuMath.Min).Method);
			map.Add(new Func<int, int, int>(Math.Max).Method, new Func<int, int, int>(SpuMath.Max).Method);

			return map;
		}
	}
}
