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
		/// <param name="attributeType">The type of the ProviderAttribute that can specify the provider.</param>
		/// <param name="defaultConstructor">The constructor to use to create the provider if it does not exist.</param>
		/// <returns>The provider for a given type, or null if there is no provider.</returns>
		internal static T GetProvider<T>(Type logType, Type attributeType, Func<T> defaultConstructor)
		{
			if (defaultConstructor == null)
				defaultConstructor = () => default(T);

			return (T)GetProvider(logType, typeof(T), attributeType, () => defaultConstructor());
		}

		/// <summary>
		/// Gets the given type of provider for the given type of log.
		/// </summary>
		/// <param name="logType">The type of event source to register with.</param>
		/// <param name="providerType">The type of provider being provided.</param>
		/// <param name="attributeType">The type of the ProviderAttribute that can specify the provider.</param>
		/// <param name="defaultConstructor">The constructor to use to create the provider if it does not exist.</param>
		/// <returns>The provider for a given type, or null if there is no provider.</returns>
		private static object GetProvider(Type logType, Type providerType, Type attributeType, Func<object> defaultConstructor)
		{
			var key = Tuple.Create(logType, providerType);

			return _providers.GetOrAdd(
				key,
				_ =>
				{
					// if there is a provider attribute on the class or interface,
					// then instantiate the given type
					if (attributeType != null)
					{
						var providerAttribute = (TraceProviderAttribute)logType.GetCustomAttributes(attributeType, true).FirstOrDefault();
						if (providerAttribute != null)
							return providerAttribute.ProviderType.GetConstructor(Type.EmptyTypes).Invoke(null);
					}

					return defaultConstructor();
				});
		}
	}
}
