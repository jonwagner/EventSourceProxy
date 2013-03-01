using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace EventSourceProxy
{
	/// <summary>
	/// Serializes objects by calling ToString on them.
	/// </summary>
	public class ToStringObjectSerializer : ObjectSerializationProvider
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
			if (value == null)
				return null;

			return value.ToString();
		}
	}
}
