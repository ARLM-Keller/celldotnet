using System;
using System.Collections.Generic;

namespace CellDotNet
{
	internal delegate void Action<T1, T2>(T1 t1, T2 t2);
	internal delegate TReturn Func<TReturn>();
}
