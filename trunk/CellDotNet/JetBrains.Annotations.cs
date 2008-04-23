using System;

namespace JetBrains.Annotations
{
	[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
	public class AssertionConditionAttribute : Attribute
	{
		// Fields
		private readonly AssertionConditionType myConditionType;

		// Methods
		public AssertionConditionAttribute(AssertionConditionType conditionType)
		{
			myConditionType = conditionType;
		}

		// Properties
		public AssertionConditionType ConditionType
		{
			get { return myConditionType; }
		}
	}

	public enum AssertionConditionType
	{
		IS_TRUE,
		IS_FALSE,
		IS_NULL,
		IS_NOT_NULL
	}


	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
	public class AssertionMethodAttribute : Attribute
	{
	}


	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
	public class BaseTypeRequiredAttribute : Attribute
	{
		// Fields
		private readonly Type[] myBaseTypes;

		// Methods
		public BaseTypeRequiredAttribute(Type baseType)
		{
			myBaseTypes = new[] {baseType};
		}

		public BaseTypeRequiredAttribute(params Type[] baseTypes)
		{
			myBaseTypes = baseTypes;
		}

		// Properties
		public Type[] BaseTypes
		{
			get { return myBaseTypes; }
		}
	}


	[AttributeUsage(
		AttributeTargets.Delegate | AttributeTargets.Parameter | AttributeTargets.Field | AttributeTargets.Property |
		AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
	public class CanBeNullAttribute : Attribute
	{
	}


	[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Struct | AttributeTargets.Class, AllowMultiple = false,
		Inherited = true)]
	public class CannotApplyEqualityOperatorAttribute : Attribute
	{
	}


	[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
	public class InvokerParameterNameAttribute : Attribute
	{
	}


	[AttributeUsage(
		AttributeTargets.Delegate | AttributeTargets.Parameter | AttributeTargets.Field | AttributeTargets.Property |
		AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
	public class NotNullAttribute : Attribute
	{
	}


	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, AllowMultiple = false, Inherited = true)]
	public class StringFormatMethodAttribute : Attribute
	{
		// Fields
		private readonly string myFormatParameterName;

		// Methods
		public StringFormatMethodAttribute(string formatParameterName)
		{
			myFormatParameterName = formatParameterName;
		}

		// Properties
		public string FormatParameterName
		{
			get { return myFormatParameterName; }
		}
	}


	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
	public class TerminatesProgramAttribute : Attribute
	{
	}
}
