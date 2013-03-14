using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventSourceProxy
{
	/// <summary>
	/// Allows TraceContext to be enabled or disabled at a class or method level.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = false)]
	public sealed class TraceContextAttribute : Attribute
	{
		/// <summary>
		/// Initializes a new instance of the TraceContextAttribute class.
		/// Tracing will be enabled with this constructor.
		/// </summary>
		public TraceContextAttribute()
		{
			Enabled = true;
		}

		/// <summary>
		/// Initializes a new instance of the TraceContextAttribute class.
		/// </summary>
		/// <param name="enabled">True to enable tracing context.</param>
		public TraceContextAttribute(bool enabled)
		{
			Enabled = enabled;
		}

		/// <summary>
		/// Gets a value indicating whether the context provider should generate context for the method.
		/// </summary>
		public bool Enabled { get; private set; }
	}
}
