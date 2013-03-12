using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EventSourceProxy
{
	/// <summary>
	/// Defines a provider that serializes objects to the ETW log.
	/// </summary>
	public abstract class ObjectSerializationProvider : ITraceSerializationProvider
	{
		/// <summary>
		/// Serializes an object to a string.
		/// </summary>
		/// <param name="value">The object to serialize.</param>
		/// <param name="context">The context of the serialization.</param>
		/// <returns>The serialized representation of the object.</returns>
		public abstract string SerializeObject(object value, TraceSerializationContext context);

		/// <summary>
		/// Returns if the should the given parameter be serialized.
		/// </summary>
		/// <param name="context">The context of the serialization.</param>
		/// <returns>True if the value should be serialized, false otherwise.</returns>
		public virtual bool ShouldSerialize(TraceSerializationContext context)
		{
			return true;
		}

		/// <summary>
		/// Gets the serialization provider for a given type.
		/// </summary>
		/// <param name="type">The type to serialize.</param>
		/// <returns>The serialization provider or the default JSON provider.</returns>
		internal static ITraceSerializationProvider GetSerializationProvider(Type type)
		{
			return ProviderManager.GetProvider<ITraceSerializationProvider>(type, () => new ToStringObjectSerializer());
		}
	}
}
