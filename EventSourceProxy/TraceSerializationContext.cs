using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EventSourceProxy
{
	/// <summary>
	/// Describes the context in which an object is being serialized.
	/// </summary>
	public class TraceSerializationContext
	{
		/// <summary>
		/// The handle of the method being invoked.
		/// </summary>
		/// <remarks>This will be null when the MethodInfo is provided in the constructor.</remarks>
		private RuntimeMethodHandle _methodHandle;

		/// <summary>
		/// The method being invoked.
		/// </summary>
		private MethodInfo _methodInfo;

		/// <summary>
		/// Initializes a new instance of the TraceSerializationContext class.
		/// </summary>
		/// <param name="methodHandle">The handle of the method being invoked.</param>
		/// <param name="parameterIndex">The index of the parameter being serialized.</param>
		public TraceSerializationContext(RuntimeMethodHandle methodHandle, int parameterIndex)
		{
			_methodHandle = methodHandle;
			ParameterIndex = parameterIndex;
		}

		/// <summary>
		/// Initializes a new instance of the TraceSerializationContext class.
		/// </summary>
		/// <param name="methodInfo">The handle of the method being invoked.</param>
		/// <param name="parameterIndex">The index of the parameter being serialized.</param>
		public TraceSerializationContext(MethodInfo methodInfo, int parameterIndex)
		{
			_methodInfo = methodInfo;
			ParameterIndex = parameterIndex;
		}

		/// <summary>
		/// Gets the method being invoked during serialization.
		/// </summary>
		public MethodInfo MethodInfo
		{
			get
			{
				return _methodInfo = _methodInfo ?? (MethodInfo)MethodBase.GetMethodFromHandle(_methodHandle);
			}
		}

		/// <summary>
		/// Gets the index of the parameter being serialized.
		/// </summary>
		public int ParameterIndex { get; private set; }
	}
}