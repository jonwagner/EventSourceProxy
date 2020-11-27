using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace EventSourceProxy
{
	/// <summary>
	/// Serializes objects by calling ToString on them.
	/// </summary>
	public class ToStringObjectSerializer : TraceSerializationProvider
	{
		#region Constructors
		/// <summary>
		/// Initializes a new instance of the ToStringObjectSerializer class.
		/// The default is to allow serialization whenever tracing occurs.
		/// </summary>
		public ToStringObjectSerializer()
		{
		}

		/// <summary>
		/// Initializes a new instance of the ToStringObjectSerializer class.
		/// </summary>
		/// <param name="defaultEventLevel">
		/// The default EventLevel to allow object serialization.
		/// The default is to serialize objects whenever tracing occurs, but this can be used to allow serialization
		/// only when logging is at a particular level of verbosity.
		/// </param>
		public ToStringObjectSerializer(EventLevel defaultEventLevel) : base(defaultEventLevel)
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
			if (value == null)
				return null;

			return value.ToString();
		}
	}
}
