using System;
using System.Collections.Generic;
#if NUGET
using Microsoft.Diagnostics.Tracing;
#else
using System.Diagnostics.Tracing;
#endif
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

#if NUGET
namespace EventSourceProxy.NuGet
#else
namespace EventSourceProxy
#endif
{
	/// <summary>
	/// Specifies the invocation of a method and the type of the invocation (MethodCall, MethodCompletion, or MethodFaulted).
	/// </summary>
	public class InvocationContext
	{
		/// <summary>
		/// Initializes a new instance of the InvocationContext class.
		/// </summary>
		/// <param name="methodInfo">The handle of the method being invoked.</param>
		/// <param name="contextType">The context type for this invocation.</param>
		internal InvocationContext(MethodInfo methodInfo, InvocationContextTypes contextType)
		{
			MethodInfo = methodInfo;
			ContextType = contextType;
		}

		/// <summary>
		/// Gets the EventSource associated with the InvocationContext.
		/// </summary>
		public EventSource EventSource { get; internal set; }

		/// <summary>
		/// Gets the method being invoked.
		/// </summary>
		public MethodInfo MethodInfo { get; private set; }

		/// <summary>
		/// Gets the type of the invocation.
		/// </summary>
		public InvocationContextTypes ContextType { get; private set; }

		/// <summary>
		/// Creates a clone of this InvocationContext, changing the type of the context.
		/// </summary>
		/// <param name="contextType">The new InvocationContextType.</param>
		/// <returns>A clone of this InvocationContext with a new context type.</returns>
		internal InvocationContext SpecifyType(InvocationContextTypes contextType)
		{
			InvocationContext context = (InvocationContext)this.MemberwiseClone();
			context.ContextType = contextType;
			return context;
		}
	}
}