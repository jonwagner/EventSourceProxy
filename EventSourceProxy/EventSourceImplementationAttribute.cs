using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventSourceProxy
{
	/// <summary>
	/// Specifies the classes to use for the Keywords, Tasks, and Opcodes enumerations for an EventSource.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
	public sealed class EventSourceImplementationAttribute : Attribute
	{
		/// <summary>
		/// Gets or sets the name of the EventSource. This overrides any EventSource attribute.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Gets or sets the guid of the EventSource. This overrides any EventSource attribute.
		/// </summary>
		public string Guid { get; set; }

		/// <summary>
		/// Gets or sets the LocalizationResources of the EventSource. This overrides any EventSource attribute.
		/// </summary>
		public string LocalizationResources { get; set; }

		/// <summary>
		/// Gets or sets the type that contains the Keywords enumeration for the EventSource.
		/// </summary>
		public Type Keywords { get; set; }

		/// <summary>
		/// Gets or sets the type that contains the Tasks enumeration for the EventSource.
		/// </summary>
		public Type Tasks { get; set; }

		/// <summary>
		/// Gets or sets the type that contains the Opcodes enumeration for the EventSource.
		/// </summary>
		public Type OpCodes { get; set; }

		/// <summary>
		/// Gets or sets the default event level for the EventSource.
		/// </summary>
		public EventLevel? Level { get; set; }
	}
}
