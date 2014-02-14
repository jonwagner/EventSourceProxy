using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if NUGET
namespace EventSourceProxy.NuGet
#else
namespace EventSourceProxy
#endif
{
	/// <summary>
	/// Allows TraceContext to be enabled or disabled at a class or method level.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = false)]
	[SuppressMessage("Microsoft.Design", "CA1019:DefineAccessorsForAttributeArguments", Justification = "This is a convenience method for enabling/disabling all invocations")]
	public sealed class TraceContextAttribute : Attribute
	{
		/// <summary>
		/// Initializes a new instance of the TraceContextAttribute class.
		/// Tracing will be enabled with this constructor.
		/// </summary>
		public TraceContextAttribute()
		{
			EnabledFor = InvocationContextTypes.All;
		}

		/// <summary>
		/// Initializes a new instance of the TraceContextAttribute class.
		/// </summary>
		/// <param name="enabled">True to enable context logging for all invocations, false to disable context logging for all invocations.</param>
		public TraceContextAttribute(bool enabled)
		{
			EnabledFor = enabled ? InvocationContextTypes.All : InvocationContextTypes.None;
		}

		/// <summary>
		/// Initializes a new instance of the TraceContextAttribute class.
		/// </summary>
		/// <param name="enabledFor">The types of invocations to enable context logging.</param>
		public TraceContextAttribute(InvocationContextTypes enabledFor)
		{
			EnabledFor = enabledFor;
		}

		/// <summary>
		/// Gets a value indicating whether the context provider should generate context for the method.
		/// </summary>
		public InvocationContextTypes EnabledFor { get; private set; }
	}
}
