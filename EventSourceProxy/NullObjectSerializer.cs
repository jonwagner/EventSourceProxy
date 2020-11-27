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
		/// The Null serializer serializes everything as null.
		/// </summary>
		public NullObjectSerializer()
		{
		}
		#endregion

		/// <summary>
		/// Serializes an object to a string.
		/// </summary>
		/// <param name="value">The object to serialize.</param>
		/// <param name="context">The context of the serialization.</param>
		/// <returns>The serialized representation of the object.</returns>
		public override string SerializeObject(object value, TraceSerializationContext context)
		{
			return String.Empty;
		}

		/// <summary>
		/// Returns if the should the given parameter be serialized.
		/// </summary>
		/// <param name="context">The context of the serialization.</param>
		/// <returns>True if the value should be serialized, false otherwise.</returns>
		public override bool ShouldSerialize(TraceSerializationContext context)
		{
			return true;
		}
	}
}
