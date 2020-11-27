using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventSourceProxy
{
	/// <summary>
	/// Specifies the TraceSerializationProvider to use for a class or interface.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false)]
	public sealed class EventAttributeProviderAttribute : TraceProviderAttribute
	{
		/// <summary>
		/// Initializes a new instance of the EventAttributeProviderAttribute class.
		/// </summary>
		/// <param name="providerType">The type of the provider to assign to this class or interface.</param>
		public EventAttributeProviderAttribute(Type providerType)
			: base(providerType)
		{
		}
	}
}
