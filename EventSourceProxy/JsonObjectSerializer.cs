using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
#if NUGET
using Microsoft.Diagnostics.Tracing;
#else
using System.Diagnostics.Tracing;
#endif
using System.Globalization;
using System.Linq;
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
	/// Used internally to serialize a string. By default, it uses Newtonsoft.Json to JSON serialize the object.
	/// </summary>
	public class JsonObjectSerializer : TraceSerializationProvider
	{
		#region Constructors
		/// <summary>
		/// Initializes a new instance of the JsonObjectSerializer class.
		/// The default is to serialize objects only in Verbose tracing.
		/// </summary>
		public JsonObjectSerializer() : base(EventLevel.Verbose)
		{
		}

		/// <summary>
		/// Initializes a new instance of the JsonObjectSerializer class.
		/// </summary>
		/// <param name="defaultEventLevel">
		/// The default EventLevel to allow object serialization.
		/// The default is to serialize objects whenever tracing occurs, but this can be used to allow serialization
		/// only when logging is at a particular level of verbosity.
		/// </param>
		public JsonObjectSerializer(EventLevel defaultEventLevel) : base(defaultEventLevel)
		{
		}
		#endregion

		/// <summary>
		/// Serializes an object to a string.
		/// </summary>
		/// <param name="value">The object to serialize.</param>
		/// <param name="context">The context of the serialization.</param>
		/// <returns>The serialized representation of the object.</returns>
		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Logging should not affect program behavior.")]
		public override string SerializeObject(object value, TraceSerializationContext context)
		{
			try
			{
				// if we have a task, don't attempt to serialize the task if it's not completed
				Task t = value as Task;
				if (t != null && !t.IsCompleted)
				{
					return JsonConvert.SerializeObject(new { TaskId = t.Id });
				}

				return JsonConvert.SerializeObject(value);
			}
			catch (Exception e)
			{
				// don't let serialization exceptions blow up processing
				return String.Format(CultureInfo.InvariantCulture, "{{ Exception: '{0}' }}", e.Message.Replace("'", "\\'"));
			}
		}
	}
}
