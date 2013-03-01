using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventSourceProxy
{
	/// <summary>
	/// Manages the providers for the Event Sources.
	/// </summary>
	class ProviderManager
	{
		/// <summary>
		/// The list of registered providers.
		/// </summary>
		private static ConcurrentDictionary<Tuple<Type, Type>, object> _providers = new ConcurrentDictionary<Tuple<Type, Type>, object>();

		/// <summary>
		/// Registers a Provider for a given event source.
		/// </summary>
		/// <param name="logType">The type of event source to register with.</param>
		/// <param name="providerType">The type of provider being provided.</param>
		/// <param name="provider">The provider to register.</param>
		internal static void RegisterProvider(Type logType, Type providerType, object provider)
		{
			if (!providerType.IsInstanceOfType(provider))
				throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, "Provider must be an instance of {0}", providerType.Name));

			// save the provider for future construction
			// if the provider already exists, then fail
			var key = Tuple.Create(logType, providerType);
			if (!_providers.TryAdd(key, provider))
				throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, "{0} already exists for type {1}", providerType.Name, logType.Name));
		}

		/// <summary>
		/// Gets the given type of provider for the given type of log.
		/// </summary>
		/// <typeparam name="T">The type of provider being provided.</typeparam>
		/// <param name="logType">The type of event source to register with.</param>
		/// <returns>The provider for a given type, or null if there is no provider.</returns>
		internal static T GetProvider<T>(Type logType)
		{
			return (T)GetProvider(logType, typeof(T));
		}

		/// <summary>
		/// Gets the given type of provider for the given type of log.
		/// </summary>
		/// <param name="logType">The type of event source to register with.</param>
		/// <param name="providerType">The type of provider being provided.</param>
		/// <returns>The provider for a given type, or null if there is no provider.</returns>
		private static object GetProvider(Type logType, Type providerType)
		{
			object provider = null;

			var key = Tuple.Create(logType, providerType);
			_providers.TryGetValue(key, out provider);

			if (_providers.Count > 0)
			{
				var p = _providers.Keys.First();
				bool b1 = key.Item1 == p.Item1;
				bool b2 = key.Item2 == p.Item2;
			}

			return provider;
		}
	}
}
