using System;
using System.Collections.Generic;
#if NUGET
using Microsoft.Diagnostics.Tracing;
#else
using System.Diagnostics.Tracing;
#endif
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

#if NUGET
namespace EventSourceProxy.NuGet
#else
namespace EventSourceProxy
#endif
{
	/// <summary>
	/// Serializes an object by returning null. This is effectively a NoOp.
	/// </summary>
	public class NullObjectSerializer : TraceSerializationProvider
	{
		#region Constructors
		/// <summary>
		/// Initializes a new instance of the NullObjectSerializer class.
		/// The Null serializer serializes everything as null.
		/// </summary>
		public NullObjectSerializer()
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
