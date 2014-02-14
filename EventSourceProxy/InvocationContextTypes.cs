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
	/// Specifies the type of invocation of the method.
	/// </summary>
	[Flags]
	public enum InvocationContextTypes
	{
		/// <summary>
		/// The invocation is the method call.
		/// </summary>
		MethodCall = 1 << 0,

		/// <summary>
		/// The invocation is the completion of the method.
		/// The parameter is the return value, if any.
		/// </summary>
		MethodCompletion = 1 << 1,

		/// <summary>
		/// The invocation is the exception event.
		/// The parameter is the exception.
		/// </summary>
		MethodFaulted = 1 << 2,

		/// <summary>
		/// No types of method invocations.
		/// </summary>
		None = 0,

		/// <summary>
		/// The invocation is to bundle a set of parameters into a single parameter.
		/// The parameter is a string-to-string map.
		/// </summary>
		BundleParameters = 1 << 4,

		/// <summary>
		/// All types of method invocations.
		/// </summary>
		All = MethodCall | MethodCompletion | MethodFaulted | BundleParameters
	}
}
