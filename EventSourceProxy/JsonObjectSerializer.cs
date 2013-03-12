using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace EventSourceProxy
{
	/// <summary>
	/// Used internally to serialize a string. By default, it uses Newtonsoft.Json to JSON serialize the object.
	/// </summary>
	public class JsonObjectSerializer : ObjectSerializationProvider
	{
		/// <summary>
		/// Serializes an object to a string.
		/// </summary>
		/// <param name="value">The object to serialize.</param>
		/// <param name="context">The context of the serialization.</param>
		/// <returns>The serialized representation of the object.</returns>
		public override string SerializeObject(object value, TraceSerializationContext context)
		{
			// if we have a task, don't attempt to serialize the task if it's not completed
			Task t = value as Task;
			if (t != null && !t.IsCompleted)
			{
				return JsonConvert.SerializeObject(new { TaskId = t.Id });
			}

			return JsonConvert.SerializeObject(value);
		}
	}
}
