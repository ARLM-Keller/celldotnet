using System;
using System.Collections.Generic;
using System.Text;

namespace CellDotNet
{
	static class Utilities
	{
		static public void AssertArgumentNotNull(object arg, string paramName)
		{
			if (arg == null)
				throw new ArgumentNullException(paramName);
		}

		static public void AssertArgument(bool condition, string message)
		{
			if (!condition)
				throw new ArgumentException(message);
		}

		static public void AssertNotNull(object arg, string expressionOrMessage)
		{
			if (arg == null)
				throw new Exception("An expression is null: " + expressionOrMessage);
		}

		static public void Assert(bool condition, string message)
		{
			if (!condition)
				throw new Exception(message); // Should we use another exception?
		}
	}
}
