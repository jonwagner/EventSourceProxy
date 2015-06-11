using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

#if NUGET
namespace EventSourceProxy.NuGet
#else
namespace EventSourceProxy
#endif
{
	/// <summary>
	/// Provides a transformation method which will be used to modify the input value before tracing.
	/// </summary>
	/// <remarks>Consumers can derive from this base to create their own methods to run against values being traced.
	/// This is particularly useful as a means of masking or filtering data.
	/// </remarks>
	[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = true)]
	public abstract class TraceTransformAttribute : TraceAsAttribute
	{
		/// <summary>
		/// Retrieves the MethodInfo for a static method to use for transforming the trace value.
		/// </summary>
		/// <remarks>The MethodInfo should correspond to a static method which can handle the supplied input Type.
		/// The method's response will be used as the trace value.</remarks>
		/// <param name="inputType">The type of object to bind to.</param>
		/// <returns>MethodInfo for the method to use.</returns>
		public abstract MethodInfo GetTransformMethod(Type inputType);
	}	
}
