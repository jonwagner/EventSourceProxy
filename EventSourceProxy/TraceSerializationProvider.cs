using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EventSourceProxy
{
	/// <summary>
	/// Defines a provider that serializes objects to the ETW log when the log cannot handle the type natively.
	/// </summary>
	public abstract class TraceSerializationProvider
	{
		#region Private Fields
		/// <summary>
		/// The default EventLevel at which to allow serialization.
		/// </summary>
		private EventLevel _defaultEventLevel;
		#endregion

		#region Constructors
		/// <summary>
		/// Initializes a new instance of the TraceSerializationProvider class.
		/// The default is to allow serialization whenever tracing occurs.
		/// </summary>
		protected TraceSerializationProvider() : this(EventLevel.LogAlways)
		{
		}

		/// <summary>
		/// Initializes a new instance of the TraceSerializationProvider class.
		/// </summary>
		/// <param name="defaultEventLevel">
		/// The default EventLevel to allow object serialization.
		/// The default is to serialize objects whenever tracing occurs, but this can be used to allow serialization
		/// only when logging is at a particular level of verbosity.
		/// </param>
		protected TraceSerializationProvider(EventLevel defaultEventLevel)
		{
			_defaultEventLevel = defaultEventLevel;
		}
		#endregion

		/// <summary>
		/// Returns the EventLevel at which to enable serialization for the given context.
		/// This method looks at the TraceSerializationAttributes on the parameter, method, or class.
		/// </summary>
		/// <param name="context">The serialization context to evaluate.</param>
		/// <returns>The EventLevel at which to enable serialization for the given context.</returns>
		public virtual EventLevel? GetEventLevelForContext(TraceSerializationContext context)
		{
			TraceSerializationAttribute attribute = null;

			// look on the parameter first
			ParameterInfo parameterInfo = null;
			switch (context.ContextType)
			{
				case InvocationContextType.MethodCall:
					parameterInfo = context.MethodInfo.GetParameters()[context.ParameterIndex];
					break;
				case InvocationContextType.MethodCompletion:
					parameterInfo = context.MethodInfo.ReturnParameter;
					break;
			}

			if (parameterInfo != null)
			{
				// look at the attribute on the parameter
				attribute = parameterInfo.GetCustomAttribute<TraceSerializationAttribute>();
				if (attribute != null)
					return attribute.EventLevel;

				// look at the attribute on the parameter's type
				attribute = parameterInfo.ParameterType.GetCustomAttribute<TraceSerializationAttribute>();
				if (attribute != null)
					return attribute.EventLevel;
			}

			// now look on the method
			attribute = context.MethodInfo.GetCustomAttribute<TraceSerializationAttribute>();
			if (attribute != null)
				return attribute.EventLevel;

			// now look at the type
			attribute = context.MethodInfo.DeclaringType.GetCustomAttribute<TraceSerializationAttribute>();
			if (attribute != null)
				return attribute.EventLevel;

			return _defaultEventLevel;
		}

		/// <summary>
		/// Called by EventSourceProxy to serialize an object. This method should call ShouldSerialize
		/// then SerializeObject if serialization is enabled.
		/// </summary>
		/// <param name="value">The value to serialize.</param>
		/// <param name="context">The serialization context.</param>
		/// <returns>The serialized value.</returns>
		public virtual string ProvideSerialization(object value, TraceSerializationContext context)
		{
			if (!ShouldSerialize(context))
				return null;

			return SerializeObject(value, context);
		}

		/// <summary>
		/// Returns if the should the given parameter be serialized.
		/// </summary>
		/// <param name="context">The context of the serialization.</param>
		/// <returns>True if the value should be serialized, false otherwise.</returns>
		public virtual bool ShouldSerialize(TraceSerializationContext context)
		{
			if (context.EventLevel == null)
				return false;

			var eventLevel = context.EventLevel.Value;
			if (eventLevel == EventLevel.LogAlways)
				return true;

			return context.EventSource.IsEnabled(eventLevel, (EventKeywords)(-1));
		}

		/// <summary>
		/// Serializes an object to a string.
		/// </summary>
		/// <param name="value">The object to serialize.</param>
		/// <param name="context">The context of the serialization.</param>
		/// <returns>The serialized representation of the object.</returns>
		public abstract string SerializeObject(object value, TraceSerializationContext context);

		/// <summary>
		/// Gets the serialization provider for a given type.
		/// </summary>
		/// <param name="type">The type to serialize.</param>
		/// <returns>The serialization provider or the default JSON provider.</returns>
		internal static TraceSerializationProvider GetSerializationProvider(Type type)
		{
			return ProviderManager.GetProvider<TraceSerializationProvider>(type, typeof(TraceSerializationProviderAttribute), () => new JsonObjectSerializer());
		}
	}
}