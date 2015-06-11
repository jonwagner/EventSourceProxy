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
	/// Allows a method to be executed against a value before tracing.
	/// </summary>
	/// <remarks>Consumers can derive from this base to create their own methods to run against values being traced.
	/// This is particularly useful as a means of masking or filtering data.
	/// </remarks>
	[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = true)]
	public abstract class TraceMethodAttribute : TraceAsAttribute
	{
		/// <summary>
		/// Method called to retrieve the MethodInfo for a static method to apply to the trace value.
		/// </summary>
		/// <remarks>The MethodInfo should correspond to a static method which can handle the supplied input Type.
		/// The method's response will be used as the trace value.</remarks>
		/// <returns>MethodInfo for the method to apply.</returns>
		public abstract MethodInfo GetMethod(Type inputType);
	}	
}
