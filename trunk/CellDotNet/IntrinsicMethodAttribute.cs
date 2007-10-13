using System;
using System.Collections.Generic;
using System.Text;

namespace CellDotNet
{
	/// <summary>
	/// Applying this attribute to a method is an indication to the runtime.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, AllowMultiple = false)]
	sealed class IntrinsicMethodAttribute : Attribute
	{
		public readonly SpuIntrinsicMethod Intrinsic;

		public IntrinsicMethodAttribute(SpuIntrinsicMethod intrinsic)
		{
			Intrinsic = intrinsic;
		}
	}
}
