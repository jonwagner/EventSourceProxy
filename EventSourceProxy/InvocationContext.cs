using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EventSourceProxy
{
	/// <summary>
	/// Specifies the invocation of a method and the type of the invocation (MethodCall, MethodCompletion, or MethodFaulted).
	/// </summary>
	public class InvocationContext
	{
		/// <summary>
		/// The handle of the method being invoked.
		/// </summary>
		/// <remarks>This will be null when the MethodInfo is provided in the constructor.</remarks>
		private RuntimeMethodHandle _methodHandle;

		/// <summary>
		/// The method being invoked.
		/// </summary>
		private MethodInfo _methodInfo;

		/// <summary>
		/// Initializes a new instance of the InvocationContext class.
		/// </summary>
		/// <param name="methodHandle">The handle of the method being invoked.</param>
		/// <param name="contextType">The context type for this invocation.</param>
		public InvocationContext(RuntimeMethodHandle methodHandle, InvocationContextType contextType)
		{
			_methodHandle = methodHandle;
			ContextType = contextType;
		}

		/// <summary>
		/// Initializes a new instance of the InvocationContext class.
		/// </summary>
		/// <param name="methodInfo">The handle of the method being invoked.</param>
		/// <param name="contextType">The context type for this invocation.</param>
		public InvocationContext(MethodInfo methodInfo, InvocationContextType contextType)
		{
			_methodInfo = methodInfo;
			ContextType = contextType;
		}

		/// <summary>
		/// Gets the method being invoked.
		/// </summary>
		public MethodInfo MethodInfo
		{
			get
			{
				return _methodInfo = _methodInfo ?? (MethodInfo)MethodBase.GetMethodFromHandle(_methodHandle);
			}
		}

		/// <summary>
		/// Gets the type of the invocation.
		/// </summary>
		public InvocationContextType ContextType { get; private set; }

		/// <summary>
		/// Creates a clone of this InvocationContext, changing the type of the context.
		/// </summary>
		/// <param name="contextType">The new InvocationContextType.</param>
		/// <returns>A clone of this InvocationContext with a new context type.</returns>
		internal InvocationContext SpecifyType(InvocationContextType contextType)
		{
			InvocationContext context = (InvocationContext)this.MemberwiseClone();
			context.ContextType = contextType;
			return context;
		}
	}
}