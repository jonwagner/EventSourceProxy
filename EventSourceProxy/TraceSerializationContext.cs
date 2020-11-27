using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
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
		internal TraceSerializationContext(InvocationContext invocationContext, int parameterIndex) :
			base(invocationContext.MethodInfo, invocationContext.ContextType)
		{
			ParameterIndex = parameterIndex;
		}

		/// <summary>
		/// Gets the index of the parameter being serialized.
		/// </summary>
		public int ParameterIndex { get; private set; }

		/// <summary>
		/// Gets the EventLevel required to serialize this object.
		/// </summary>
		public EventLevel? EventLevel { get; internal set; }
	}
}
