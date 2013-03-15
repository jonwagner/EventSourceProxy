using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace EventSourceProxy
{
	/// <summary>
	/// Serializes an object by returning null. This is effectively a NoOp.
	/// </summary>
	public class NullObjectSerializer : TraceSerializationProvider
	{
		#region Constructors
		/// <summary>
		/// Initializes a new instance of the NullObjectSerializer class.
		/// The default is to allow serialization whenever tracing occurs.
		/// </summary>
		public NullObjectSerializer()
		{
		}

		/// <summary>
		/// Initializes a new instance of the NullObjectSerializer class.
		/// </summary>
		/// <param name="defaultEventLevel">
		/// The default EventLevel to allow object serialization.
		/// The default is to serialize objects whenever tracing occurs, but this can be used to allow serialization
		/// only when logging is at a particular level of verbosity.
		/// </param>
		public NullObjectSerializer(EventLevel defaultEventLevel) : base(defaultEventLevel)
		{
		}
		#endregion

		/// <summary>
		/// Returns the EventLevel at which to enable serialization for the given context.
		/// This method looks at the TraceSerializationAttributes on the parameter, method, or class.
		/// </summary>
		/// <param name="context">The serialization context to evaluate.</param>
		/// <returns>The EventLevel at which to enable serialization for the given context.</returns>
		public override EventLevel? GetEventLevelForContext(TraceSerializationContext context)
		{
			return null;
		}

		/// <summary>
		/// Serializes an object to a string.
		/// </summary>
		/// <param name="value">The object to serialize.</param>
		/// <param name="context">The context of the serialization.</param>
		/// <returns>The serialized representation of the object.</returns>
		public override string SerializeObject(object value, TraceSerializationContext context)
		{
			return null;
		}

		/// <summary>
		/// Returns if the should the given parameter be serialized.
		/// </summary>
		/// <param name="context">The context of the serialization.</param>
		/// <returns>True if the value should be serialized, false otherwise.</returns>
		public override bool ShouldSerialize(TraceSerializationContext context)
		{
			return false;
		}
	}
}
