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
	/// Specifies that a member of the given parameter should be traced as a separate parameter.
	/// </summary>
	[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = true)]
	public sealed class TraceMemberAttribute : TraceAsAttribute
	{
		/// <summary>
		/// Initializes a new instance of the TraceMemberAttribute class.
		/// </summary>
		/// <param name="member">The name of the member to trace.</param>
		public TraceMemberAttribute(string member)
			: base(member)
		{
			Member = member;
		}

		/// <summary>
		/// Initializes a new instance of the TraceMemberAttribute class.
		/// </summary>
		/// <param name="member">The name of the member to trace.</param>
		/// <param name="name">The name to use to trace the member.</param>
		public TraceMemberAttribute(string member, string name)
			: base(name)
		{
			Member = member;
		}

		/// <summary>
		/// Gets the name of the member that will be traced.
		/// </summary>
		public string Member { get; private set; }
	}
}
