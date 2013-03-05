using System;
using System.Collections.Concurrent;
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
	/// Constructs a proxy out of a logger and an object to give you automatic logging of an interface.
	/// </summary>
	public static class TracingProxy
	{
		#region Private Members
		/// <summary>
		/// A cache of the constructors for the proxies.
		/// </summary>
		private static ConcurrentDictionary<Tuple<Type, Type>, Func<object, object, object, object>> _constructors =
			new ConcurrentDictionary<Tuple<Type, Type>, Func<object, object, object, object>>();
		#endregion

		#region Public Members
		/// <summary>
		/// Creates a tracing proxy around the T interface or class of the given object.
		/// Events will log to the EventSource defined for type T.
		/// The proxy will trace any virtual or interface methods of type T.
		/// </summary>
		/// <typeparam name="T">The interface or class to proxy and log.</typeparam>
		/// <param name="instance">The instance of the object to log.</param>
		/// <returns>A proxy object of type T that traces calls.</returns>
		public static T Create<T>(object instance)
			where T : class
		{
			var logger = EventSourceImplementer.GetEventSource<T>();

			return (T)CreateInternal(instance, typeof(T), logger, logger.GetType());
		}

		/// <summary>
		/// Creates a tracing proxy around the T interface or class of the given object
		/// and attempts to log to an alternate EventSource defined by TEventSource.
		/// Events will log to the EventSource defined for type TEventSource.
		/// The proxy will trace any methods that match the signatures of methods on TEventSource.
		/// </summary>
		/// <typeparam name="T">The interface or class to proxy and log.</typeparam>
		/// <typeparam name="TEventSource">The matching interface to log to.</typeparam>
		/// <param name="instance">The instance of the object to log.</param>
		/// <returns>A proxy object of type T that traces calls.</returns>
		public static T Create<T, TEventSource>(T instance)
			where T : class
			where TEventSource : class
		{
			var logger = EventSourceImplementer.GetEventSourceAs<TEventSource>();
			return (T)CreateInternal(instance, typeof(T), logger, logger.GetType());
		}

		/// <summary>
		/// Creates a tracing proxy around the T interface or class of the given object.
		/// Events will log to the EventSource defined for type T.
		/// The proxy will trace any virtual or interface methods of type T.
		/// </summary>
		/// <param name="instance">The instance of the object to log.</param>
		/// <param name="interfaceType">The type of interface to log on.</param>
		/// <returns>A proxy object of type interfaceType that traces calls.</returns>
		public static object Create(object instance, Type interfaceType)
		{
			var logger = EventSourceImplementer.GetEventSource(interfaceType);

			return CreateInternal(instance, interfaceType, logger, logger.GetType());
		}
		#endregion

		#region Implementation
		/// <summary>
		/// Creates a proxy out of a logger and an object to give you automatic logging of an instance.
		/// The logger and object must implement the same interface.
		/// </summary>
		/// <typeparam name="T">The interface that is shared.</typeparam>
		/// <param name="execute">The instance of the object that executes the interface.</param>
		/// <param name="executeType">The type of the execute object.</param>
		/// <param name="log">The instance of the logging interface.</param>
		/// <param name="logType">The type on the log object that should be mapped to the execute object.</param>
		/// <returns>A proxy object of type T that logs to the log object and executes on the execute object.</returns>
		private static object CreateInternal(object execute, Type executeType, object log, Type logType)
		{
			// cache constructors based on tuple of types, including logoverride
			var tuple = Tuple.Create(executeType, logType);

			// get the serialization provider
			var serializer = ObjectSerializationProvider.GetSerializationProvider(logType);

			// get the constructor
			var creator = _constructors.GetOrAdd(
				tuple,
				t => (Func<object, object, object, object>)new TracingProxyImplementer(t.Item1, t.Item2).CreateMethod.CreateDelegate(typeof(Func<object, object, object, object>)));

			return creator(execute, log, serializer);
		}
		#endregion
	}
}