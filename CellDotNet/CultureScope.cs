using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CellDotNet
{
	/// <summary>
	/// Handy for locally changing to a specific culture.
	/// </summary>
	internal class CultureScope : IDisposable
	{
		private readonly CultureInfo _oldCulture;
		public CultureScope(string culture)
		{
			_oldCulture = CultureInfo.CurrentCulture;
			System.Threading.Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo(culture);
		}

		public void Dispose()
		{
			System.Threading.Thread.CurrentThread.CurrentCulture = _oldCulture;
		}
	}
}
