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
		/// Serializes an object to a string when an ETW log needs to serialize a non-native object.
		/// </summary>
		/// <param name="value">The object to serialize.</param>
		/// <param name="methodHandle">A RuntimeMethodHandle to the log method being called.</param>
		/// <param name="parameterIndex">The index of the current parameter being logged.</param>
		/// <returns>The string representation of the object. This value can be null.</returns>
		public abstract string SerializeObject(object value, RuntimeMethodHandle methodHandle, int parameterIndex);

		/// <summary>
		/// Returns if the should the given parameter be serialized.
		/// </summary>
		/// <param name="method">The method being called.</param>
		/// <param name="parameterIndex">The index of the parameter to analyze.</param>
		/// <returns>True if the value should be serialized, false otherwise.</returns>
		public virtual bool ShouldSerialize(MethodInfo method, int parameterIndex)
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
