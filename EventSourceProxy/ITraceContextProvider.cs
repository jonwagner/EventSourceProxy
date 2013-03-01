using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventSourceProxy
{
	/// <summary>
	/// Provides context information, such as security context, during a trace.
	/// The ProvideContext method is called when the provider is attached to an EventSource.
	/// </summary>
	public interface ITraceContextProvider
	{
		/// <summary>
		/// Provides context information, such as security context, for a trace session.
		/// </summary>
		/// <returns>A string representing the current context.</returns>
		string ProvideContext();
	}
}
