﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventSourceProxy
{
	/// <summary>
	/// Allows the TraceSerialization level to be adjusted at the class, method, interface, or parameter level.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method | AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = false)]
	public sealed class TraceSerializationAttribute : Attribute
	{
		/// <summary>
		/// Initializes a new instance of the TraceSerializationAttribute class.
		/// </summary>
		/// <param name="level">The minimum EventLevel required to enable serialization.</param>
		public TraceSerializationAttribute(EventLevel level)
		{
			Level = level;
		}

		/// <summary>
		/// Gets the minimum EventLevel required to enable serialization.
		/// </summary>
		public EventLevel Level { get; private set; }
	}
}
