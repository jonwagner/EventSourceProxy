using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
#if NUGET
using Microsoft.Diagnostics.Tracing;
#else
using System.Diagnostics.Tracing;
#endif
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

#if NUGET
namespace EventSourceProxy.NuGet
#else
namespace EventSourceProxy
#endif
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

	    private static PropertyInfo _isDisposedProperty = typeof (EventSource).GetProperty("IsDisposed", BindingFlags.Instance | BindingFlags.NonPublic);
		#endregion

		#region Public Members
		/// <summary>
		/// Gets or sets a value indicating whether EventSources should always have auto-keywords.
		/// Set this to true if you were using ESP before v3.0 and need auto-keywords to be on.
		/// </summary>
		public static bool ForceAutoKeywords { get; set; }

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
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter")]
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
				var source = (EventSource)_eventSources.GetOrAdd(type, t => new TypeImplementer(t).EventSource);

                if (_isDisposedProperty != null)
			    {
                    //HACK: using reflection on the private IsDisposed field to determine if the cached event source is disposed
			        var isDisposed = (bool)_isDisposedProperty.GetValue(source);
			        if (isDisposed)
			        {
                        // we don't want to return a disposed event source. Remove it from the cache and create a new source
			            object removed;
			            _eventSources.TryRemove(type, out removed);
                        source = (EventSource)_eventSources.GetOrAdd(type, t => new TypeImplementer(t).EventSource);
                    }
			    }

			    return source;
			}
		}

		/// <summary>
		/// Registers a Context Provider for a given event source.
		/// </summary>
		/// <typeparam name="TLog">The type of event source to register with.</typeparam>
		/// <param name="provider">The provider to register.</param>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter")]
		public static void RegisterProvider<TLog>(TraceContextProvider provider)
		{
			RegisterProvider(typeof(TLog), typeof(TraceContextProvider), provider);
		}

		/// <summary>
		/// Registers a Context Provider for a given event source.
		/// </summary>
		/// <param name="type">The type of event source to register with.</param>
		/// <param name="provider">The provider to register.</param>
		public static void RegisterProvider(Type type, TraceContextProvider provider)
		{
			RegisterProvider(type, typeof(TraceContextProvider), provider);
		}

		/// <summary>
		/// Registers a default TraceContextProvider for all event sources.
		/// </summary>
		/// <param name="provider">The provider to register.</param>
		public static void RegisterDefaultProvider(TraceContextProvider provider)
		{
			RegisterProvider(null, provider);
		}

		/// <summary>
		/// Registers a Serialization Provider for a given event source.
		/// </summary>
		/// <typeparam name="TLog">The type of event source to register with.</typeparam>
		/// <param name="provider">The provider to register.</param>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter")]
		public static void RegisterProvider<TLog>(TraceSerializationProvider provider)
		{
			RegisterProvider(typeof(TLog), typeof(TraceSerializationProvider), provider);
		}

		/// <summary>
		/// Registers a Serialization Provider for a given event source.
		/// </summary>
		/// <param name="type">The type of event source to register with.</param>
		/// <param name="provider">The provider to register.</param>
		public static void RegisterProvider(Type type, TraceSerializationProvider provider)
		{
			RegisterProvider(type, typeof(TraceSerializationProvider), provider);
		}

		/// <summary>
		/// Registers a default TraceSerializationProvider for all event sources.
		/// </summary>
		/// <param name="provider">The provider to register.</param>
		public static void RegisterDefaultProvider(TraceSerializationProvider provider)
		{
			RegisterProvider(null, provider);
		}

		/// <summary>
		/// Registers an EventAttributeProvider for a given event source.
		/// </summary>
		/// <typeparam name="TLog">The type of event source to register with.</typeparam>
		/// <param name="provider">The provider to register.</param>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter")]
		public static void RegisterProvider<TLog>(EventAttributeProvider provider)
		{
			RegisterProvider(typeof(TLog), typeof(EventAttributeProvider), provider);
		}

		/// <summary>
		/// Registers a EventAttributeProvider for a given event source.
		/// </summary>
		/// <param name="type">The type of event source to register with.</param>
		/// <param name="provider">The provider to register.</param>
		public static void RegisterProvider(Type type, EventAttributeProvider provider)
		{
			RegisterProvider(type, typeof(EventAttributeProvider), provider);
		}

		/// <summary>
		/// Registers a default EventAttributeProvider for all event sources.
		/// </summary>
		/// <param name="provider">The provider to register.</param>
		public static void RegisterDefaultProvider(EventAttributeProvider provider)
		{
			RegisterProvider(null, provider);
		}

		/// <summary>
		/// Registers an TraceParameterProvider for a given event source.
		/// </summary>
		/// <typeparam name="TLog">The type of event source to register with.</typeparam>
		/// <param name="provider">The provider to register.</param>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter")]
		public static void RegisterProvider<TLog>(TraceParameterProvider provider)
		{
			RegisterProvider(typeof(TLog), typeof(TraceParameterProvider), provider);
		}

		/// <summary>
		/// Registers a TraceParameterProvider for a given event source.
		/// </summary>
		/// <param name="type">The type of event source to register with.</param>
		/// <param name="provider">The provider to register.</param>
		public static void RegisterProvider(Type type, TraceParameterProvider provider)
		{
			RegisterProvider(type, typeof(TraceParameterProvider), provider);
		}

		/// <summary>
		/// Registers a default TraceParameterProvider for all event sources.
		/// </summary>
		/// <param name="provider">The provider to register.</param>
		public static void RegisterDefaultProvider(TraceParameterProvider provider)
		{
			RegisterProvider(null, provider);
		}

		/// <summary>
		/// Gets the keyword value for a method on a type.
		/// </summary>
		/// <typeparam name="T">The type of the EventSource.</typeparam>
		/// <param name="methodName">The name of the method.</param>
		/// <returns>The keyword value.</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter")]
		public static EventKeywords GetKeywordValue<T>(string methodName) where T : class
		{
			if (methodName == null) throw new ArgumentNullException("methodName");

			var logType = GetEventSourceAs<T>().GetType();

			var keywordType = logType.GetNestedType("Keywords");
			if (keywordType == null)
				throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, "Keywords have not been defined for {0}", typeof(T)));

			var keyword = keywordType.GetField(methodName);
			if (keyword == null)
				throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, "Keyword has not been defined for {1} on {0}", typeof(T), methodName));
			
			return (EventKeywords)keyword.GetValue(null);
		}
		#endregion

		#region Internal Members
		/// <summary>
		/// Registers a Provider for a given event source.
		/// </summary>
		/// <param name="logType">The type of event source to register with. If null, then the default provider is overridden.</param>
		/// <param name="providerType">The type of provider being provided.</param>
		/// <param name="provider">The provider to register.</param>
		private static void RegisterProvider(Type logType, Type providerType, object provider)
		{
			if (providerType == null) throw new ArgumentNullException("providerType");

			lock (_eventSources)
			{
				// if the eventsource already exists, then fail
				if (logType != null && _eventSources.ContainsKey(logType))
					throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, "Cannot register {0} after creating a log for type {1}", providerType.Name, logType.Name));

				ProviderManager.RegisterProvider(logType, providerType, provider);
			}
		}
		#endregion
	}
}