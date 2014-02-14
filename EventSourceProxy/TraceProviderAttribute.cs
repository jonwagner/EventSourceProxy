using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if NUGET
namespace EventSourceProxy.NuGet
#else
namespace EventSourceProxy
#endif
{
	/// <summary>
	/// Specifies a TraceProvider for a class or interface.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
	[SuppressMessage("Microsoft.Performance", "CA1813:AvoidUnsealedAtttributes", Justification = "Other attributes derive from this class")]
	public class TraceProviderAttribute : Attribute
	{
		/// <summary>
		/// Initializes a new instance of the TraceProviderAttribute class.
		/// </summary>
		/// <param name="providerType">The type of the provider to assign to this class or interface.</param>
		public TraceProviderAttribute(Type providerType)
		{
			ProviderType = providerType;
		}

		/// <summary>
		/// Gets the type of the provider to assign to the class or interface.
		/// </summary>
		public Type ProviderType { get; private set; }
	}
}
