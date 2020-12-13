using System;

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Reflection;

namespace EventSourceProxy
{
	/// <summary>
	/// Specifies the classes to use for the Keywords, Tasks, and Opcodes enumerations for an EventSource.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
	public class EventSourceImplementationAttribute : Attribute
	{
		#region Static Fields
		/// <summary>
		/// The overridden attributes
		/// </summary>
		private static ConcurrentDictionary<Type, EventSourceImplementationAttribute> _attributes = new ConcurrentDictionary<Type, EventSourceImplementationAttribute>();
		#endregion

		/// <summary>
		/// Specifies whether complement methods should be emitted.
		/// </summary>
		private bool _implementComplementMethods = true;

		/// <summary>
		/// Gets or sets the name of the EventSource. This overrides any EventSource attribute.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether event write errors should throw. Used when constructing the EventSource.  This specifies whether to throw an exception when an error occurs in the underlying Windows code.  Default is false.
		/// </summary>
		public bool ThrowOnEventWriteErrors { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the guid of the EventSource. This overrides any EventSource attribute.
		/// </summary>
		public string Guid { get; set; }

		/// <summary>
		/// Gets or sets the LocalizationResources of the EventSource. This overrides any EventSource attribute.
		/// </summary>
		public string LocalizationResources { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the EventSource should auto-generate keywords.
		/// </summary>
		public bool AutoKeywords { get; set; }

		/// <summary>
		/// Gets or sets the type that contains the Keywords enumeration for the EventSource.
		/// </summary>
		public Type Keywords { get; set; }

		/// <summary>
		/// Gets or sets the type that contains the Tasks enumeration for the EventSource.
		/// </summary>
		public Type Tasks { get; set; }

		/// <summary>
		/// Gets or sets the type that contains the Opcodes enumeration for the EventSource.
		/// </summary>
		public Type OpCodes { get; set; }

		/// <summary>
		/// Gets or sets the default event level for the EventSource.
		/// </summary>
		public EventLevel? Level { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the _Completed and _Faulted methods should be implemented
		/// on the EventSource. The default (null) indicates that complement methods are implemented on all classes
		/// that do not derive from EventSource.
		/// </summary>
		public bool ImplementComplementMethods
		{
			get { return _implementComplementMethods; }
			set { _implementComplementMethods = value; }
		}

		/// <summary>
		/// Overrides the EventSourceImplementationAttribute for a type. Allows you to define logging for other people's interfaces.
		/// </summary>
		/// <typeparam name="T">The type of interface we are overriding.</typeparam>
		/// <param name="attribute">The new EventSourceImplementationAttribute for the type.</param>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter")]
		public static void For<T>(EventSourceImplementationAttribute attribute)
		{
			if (attribute == null) throw new ArgumentNullException("attribute");

			_attributes.AddOrUpdate(typeof(T), attribute, (t, a) => a);
		}

		/// <summary>
		/// Get the EventSourceImplementationAttribute for a type.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <returns>The attribute.</returns>
		internal static EventSourceImplementationAttribute GetAttributeFor(Type type)
		{
			EventSourceImplementationAttribute attribute;
			if (_attributes.TryGetValue(type, out attribute))
				return attribute;

			return type.GetCustomAttribute<EventSourceImplementationAttribute>() ?? new EventSourceImplementationAttribute();
		}
	}
}
