using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EventSourceProxy
{
	/// <summary>
	/// Describes the context in which an object is being serialized.
	/// </summary>
	public class TraceSerializationContext : InvocationContext
	{
		/// <summary>
		/// Initializes a new instance of the TraceSerializationContext class.
		/// </summary>
		/// <param name="invocationContext">The InvocationContext this is based on.</param>
		/// <param name="parameterIndex">The index of the parameter being serialized.</param>
		public TraceSerializationContext(InvocationContext invocationContext, int parameterIndex) :
			base(invocationContext.MethodInfo, invocationContext.ContextType)
		{
			ParameterIndex = parameterIndex;
		}

		/// <summary>
		/// Initializes a new instance of the TraceSerializationContext class.
		/// </summary>
		/// <param name="methodHandle">The handle of the method being invoked.</param>
		/// <param name="contextType">The type of the invocation.</param>
		/// <param name="parameterIndex">The index of the parameter being serialized.</param>
		public TraceSerializationContext(RuntimeMethodHandle methodHandle, InvocationContextType contextType, int parameterIndex) :
			base(methodHandle, contextType)
		{
			ParameterIndex = parameterIndex;
		}

		/// <summary>
		/// Initializes a new instance of the TraceSerializationContext class.
		/// </summary>
		/// <param name="methodInfo">The handle of the method being invoked.</param>
		/// <param name="contextType">The type of the invocation.</param>
		/// <param name="parameterIndex">The index of the parameter being serialized.</param>
		public TraceSerializationContext(MethodInfo methodInfo, InvocationContextType contextType, int parameterIndex)
			: base(methodInfo, contextType)
		{
			ParameterIndex = parameterIndex;
		}

		/// <summary>
		/// Gets the index of the parameter being serialized.
		/// </summary>
		public int ParameterIndex { get; private set; }
	}
}