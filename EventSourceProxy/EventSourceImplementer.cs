using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace EventSourceProxy
{
	/// <summary>
	/// Completes the implementation of an EventSource by generating a class from an interface or from an abstract class.
	/// </summary>
	public static class EventSourceImplementer
	{
		#region Private Members
		/// <summary>
		/// The cache of constructors.
		/// </summary>
		private static ConcurrentDictionary<Type, object> _eventSources = new ConcurrentDictionary<Type, object>();
		#endregion

		#region Public Members
		/// <summary>
		/// Implements an EventSource that matches the virtual or abstract methods of a type.
		/// If the type is an interface, this creates a type derived from EventSource that implements the interface.
		/// If the type is a class derived from EventSource, this derives from the type and implements any abstract methods.
		/// If the type is a class not derived from EventSource, this method fails while casting to T. Use GetEventSource instead.
		/// </summary>
		/// <typeparam name="T">An type to implement as an EventSource.</typeparam>
		/// <returns>An EventSource that is compatible with the given type.</returns>
		public static T GetEventSourceAs<T>() where T : class
		{
			// if it's not an interface or a subclass of EventSource
			// then we have to make an entirely new class that isn't derived from T
			// and the cast below will fail. Give the programmer a hint.
			if (!typeof(T).IsInterface && !typeof(T).IsSubclassOf(typeof(EventSource)))
				throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, "{0} must be derived from EventSource. Use GetEventSource instead.", typeof(T).FullName));

			return (T)(object)GetEventSource(typeof(T));
		}

		/// <summary>
		/// Implements an EventSource that matches the virtual or abstract methods of a type.
		/// If the type is an interface, this creates a type derived from EventSource that implements the interface.
		/// If the type is a class derived from EventSource, this derives from the type and implements any abstract methods.
		/// If the type is a class not derived from EventSource, this creates a type derived from EventSource that implements
		/// method that match the virtual methods of the target type.
		/// </summary>
		/// <typeparam name="T">An type to implement as an EventSource.</typeparam>
		/// <returns>An EventSource that is compatible with the given type.</returns>
		public static EventSource GetEventSource<T>() where T : class
		{
			return GetEventSource(typeof(T));
		}

		/// <summary>
		/// Implements an EventSource that matches the virtual or abstract methods of a type.
		/// If the type is an interface, this creates a type derived from EventSource that implements the interface.
		/// If the type is a class derived from EventSource, this derives from the type and implements any abstract methods.
		/// If the type is a class not derived from EventSource, this creates a type derived from EventSource that implements
		/// method that match the virtual methods of the target type.
		/// </summary>
		/// <param name="type">An type to implement as an EventSource.</param>
		/// <returns>An EventSource that is compatible with the given type.</returns>
		public static EventSource GetEventSource(Type type)
		{
			lock (_eventSources)
			{
				return (EventSource)_eventSources.GetOrAdd(
					type,
					t => new TypeImplementer(
						t,
						ProviderManager.GetProvider<ITraceContextProvider>(type),
						ProviderManager.GetProvider<ITraceSerializationProvider>(type) ?? new JsonObjectSerializer()).Create());
			}
		}

		/// <summary>
		/// Registers a Context Provider for a given event source.
		/// </summary>
		/// <typeparam name="TLog">The type of event source to register with.</typeparam>
		/// <param name="provider">The provider to register.</param>
		public static void RegisterProvider<TLog>(ITraceContextProvider provider)
		{
			RegisterProvider(typeof(TLog), typeof(ITraceContextProvider), provider);
		}

		/// <summary>
		/// Registers a Serialization Provider for a given event source.
		/// </summary>
		/// <typeparam name="TLog">The type of event source to register with.</typeparam>
		/// <param name="provider">The provider to register.</param>
		public static void RegisterProvider<TLog>(ITraceSerializationProvider provider)
		{
			RegisterProvider(typeof(TLog), typeof(ITraceSerializationProvider), provider);
		}
		#endregion

		#region Internal Members
		/// <summary>
		/// Registers a Provider for a given event source.
		/// </summary>
		/// <param name="logType">The type of event source to register with.</param>
		/// <param name="providerType">The type of provider being provided.</param>
		/// <param name="provider">The provider to register.</param>
		private static void RegisterProvider(Type logType, Type providerType, object provider)
		{
			lock (_eventSources)
			{
				// if the eventsource already exists, then fail
				if (_eventSources.ContainsKey(logType))
					throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, "Cannot register {0} after creating a log for type {1}", providerType.Name, logType.Name));

				ProviderManager.RegisterProvider(logType, providerType, provider);
			}
		}
		#endregion
	}
}