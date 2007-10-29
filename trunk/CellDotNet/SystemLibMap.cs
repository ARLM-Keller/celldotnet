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
