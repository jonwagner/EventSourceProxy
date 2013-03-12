using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EventSourceProxy
{
	/// <summary>
	/// Serializes an object to a string when an ETW log needs to serialize a non-native object.
	/// </summary>
	public interface ITraceSerializationProvider
	{
		/// <summary>
		/// Serializes an object to a string.
		/// </summary>
		/// <param name="value">The object to serialize.</param>
		/// <param name="context">The context of the serialization.</param>
		/// <returns>The serialized representation of the object.</returns>
		string SerializeObject(object value, TraceSerializationContext context);

		/// <summary>
		/// Returns if the should the given parameter be serialized.
		/// </summary>
		/// <param name="context">The context of the serialization.</param>
		/// <returns>True if the value should be serialized, false otherwise.</returns>
		bool ShouldSerialize(TraceSerializationContext context);
	}
}
