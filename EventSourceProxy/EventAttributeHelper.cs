using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace EventSourceProxy
{
	/// <summary>
	/// Helps emit attribute data to IL.
	/// </summary>
	static class EventAttributeHelper
	{
		#region EventAttribute Members
		/// <summary>
		/// The constructor for EventAttribute.
		/// </summary>
		private static ConstructorInfo _eventAttributeConstructor = typeof(EventAttribute).GetConstructor(new[] { typeof(int) });

		/// <summary>
		/// The array of properties used to serialize the custom attribute values.
		/// </summary>
		private static PropertyInfo[] _eventAttributePropertyInfo = new PropertyInfo[]
		{
			typeof(EventAttribute).GetProperty("Keywords"),
			typeof(EventAttribute).GetProperty("Level"),
			typeof(EventAttribute).GetProperty("Message"),
			typeof(EventAttribute).GetProperty("Opcode"),
			typeof(EventAttribute).GetProperty("Task"),
			typeof(EventAttribute).GetProperty("Version"),
		};
		#endregion

		/// <summary>
		/// Converts an EventAttribute to a CustomAttributeBuilder so it can be assigned to a method.
		/// </summary>
		/// <param name="attribute">The attribute to copy.</param>
		/// <returns>A CustomAttributeBuilder that can be assigned to a method.</returns>
		internal static CustomAttributeBuilder ConvertEventAttributeToAttributeBuilder(EventAttribute attribute)
		{
			var propertyValues = new object[]
			{
				attribute.Keywords,
				attribute.Level,
				attribute.Message,
				attribute.Opcode,
				attribute.Task,
				attribute.Version
			};

			CustomAttributeBuilder attributeBuilder = new CustomAttributeBuilder(
				_eventAttributeConstructor,
				new object[] { attribute.EventId },
				_eventAttributePropertyInfo,
				propertyValues);

			return attributeBuilder;
		}
	}
}
