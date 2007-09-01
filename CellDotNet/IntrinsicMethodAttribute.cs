using System;
using System.Collections.Generic;
using System.Text;

namespace CellDotNet
{
	/// <summary>
	/// Applying this attribute to a method is an indication to the runtime.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
	class IntrinsicMethodAttribute : Attribute
	{
		public readonly SpuIntrinsicFunction Intrinsic;

		public IntrinsicMethodAttribute(SpuIntrinsicFunction intrinsic)
		{
			Intrinsic = intrinsic;
		}
	}
}
