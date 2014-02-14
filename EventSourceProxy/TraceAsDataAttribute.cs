using System;
using System.Collections.Generic;
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
	/// Specifies that the parameter should be traced in an element called "data".
	/// </summary>
	/// <remarks>
	/// If multiple parameters are traced into the same name, then they are traced as a string-to-string map,
	/// and serialized into a string by the TraceSerializationProvider.
	/// If TraceAsDataAttribute is applied to a method, then all parameters of the method are traced into the specified name
	/// unless other TraceAsAttributes are applied.
	/// </remarks>
	[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Method, AllowMultiple = false)]
	public sealed class TraceAsDataAttribute : TraceAsAttribute
	{
		/// <summary>
		/// Initializes a new instance of the TraceAsDataAttribute class, providing a name for the given parameter.
		/// </summary>
		public TraceAsDataAttribute() : base("data")
		{
		}
	}
}
