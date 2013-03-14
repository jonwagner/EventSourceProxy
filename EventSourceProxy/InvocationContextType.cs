using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventSourceProxy
{
	/// <summary>
	/// Specifies the type of invocation of the method.
	/// </summary>
	public enum InvocationContextType
	{
		/// <summary>
		/// The invocation is the method call.
		/// </summary>
		MethodCall,

		/// <summary>
		/// The invocation is the completion of the method.
		/// The parameter is the return value, if any.
		/// </summary>
		MethodCompletion,

		/// <summary>
		/// The invocation is the exception event.
		/// The parameter is the exception.
		/// </summary>
		MethodFaulted
	}
}
