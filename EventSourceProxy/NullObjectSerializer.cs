using System;
using System.Collections.Generic;
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
	public class NullObjectSerializer : ObjectSerializationProvider
	{
		/// <summary>
		/// Serializes an object to a string.
		/// </summary>
		/// <param name="value">The object to serialize.</param>
		/// <param name="methodHandle">A RuntimeMethodHandle to the log method being called.</param>
		/// <param name="parameterIndex">The index of the current parameter being logged.</param>
		/// <returns>The serialized representation of the object.</returns>
		public override string SerializeObject(object value, RuntimeMethodHandle methodHandle, int parameterIndex)
		{
			return null;
		}

		/// <summary>
		/// Returns if the should the given parameter be serialized.
		/// </summary>
		/// <param name="method">The method being called.</param>
		/// <param name="parameterIndex">The index of the parameter to analyze.</param>
		/// <returns>True if the value should be serialized, false otherwise.</returns>
		public override bool ShouldSerialize(MethodInfo method, int parameterIndex)
		{
			return false;
		}
	}
}
