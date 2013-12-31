using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace EventSourceProxy
{
	/// <summary>
	/// Manages the lifetime of an ETW Activity ID.
	/// </summary>
	public sealed class EventActivityScope : IDisposable
	{
		#region Private Members
		/// <summary>
		/// The Activity ID outside of this scope. It is restored on the disposal of the scope.
		/// </summary>
		private Guid _previousActivityId;

		/// <summary>
		/// The Activity ID of this scope.
		/// </summary>
		private Guid _activityId;
		#endregion

		#region Constructors
		/// <summary>
		/// Initializes a new instance of the EventActivityScope class.
		/// A new Activity ID is generated and assigned to the current thread.
		/// </summary>
		public EventActivityScope()
			: this(false)
		{
		}

		/// <summary>
		/// Initializes a new instance of the EventActivityScope class.
		/// A new Activity ID is generated and assigned to the current thread.
		/// </summary>
		/// <param name="reuseExistingActivityId">
		/// True to reuse an existing Activity ID if one is already in use.
		/// Since EventSource currently does not support Activity ID transfer, you
		/// may want to call this constructor with 'true' so you are sure to have an Activity ID,
		/// but to keep the current activity if one has already been established.
		/// </param>
		public EventActivityScope(bool reuseExistingActivityId)
		{
			_previousActivityId = GetActivityId();

			if (!reuseExistingActivityId || _previousActivityId == Guid.Empty)
			{
				_activityId = Guid.NewGuid();
				SetActivityId(_activityId);
			}
			else
				_activityId = _previousActivityId;
		}
		#endregion

		#region Properties
		/// <summary>
		/// Gets the current Activity Id.
		/// </summary>
		public static Guid CurrentActivityId
		{
			get
			{
				return GetActivityId();
			}
		}

		/// <summary>
		/// Gets the Activity ID of the enclosing scope.
		/// </summary>
		public Guid PreviousActivityId { get { return _previousActivityId; } }

		/// <summary>
		/// Gets a value indicating whether the Activity ID of the enclosing scope.
		/// </summary>
		public bool IsNewScope { get { return _previousActivityId == Guid.Empty; } }

		/// <summary>
		/// Gets the Activity ID of this scope.
		/// </summary>
		public Guid ActivityId { get { return _activityId; } }
		#endregion

		/// <summary>
		/// Prepare for WriteEvent by setting the ETW activity ID.
		/// </summary>
		/// <remarks>
		/// The built-in .NET EventSource WriteEvent method does not set the ETW activity ID.
		/// This method allows EventSources to properly synchronize these values before logging.
		/// </remarks>
		[SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Justification = "SecurityCritical Attribute has been applied")]
		[SecurityCritical]
		public static void PrepareForWriteEvent()
		{
			UnsafeNativeMethods.SetActivityId(Trace.CorrelationManager.ActivityId);
		}

		/// <summary>
		/// Perform an action within an activity scope.
		/// This method ensures that an activity scope exists.
		/// If an activity scope exists, it is reused.
		/// </summary>
		/// <param name="action">The action to perform.</param>
		public static void DoInScope(Action action)
		{
			Do(action, newScope: false);
		}

		/// <summary>
		/// Perform an action within a new activity scope.
		/// </summary>
		/// <param name="action">The action to perform.</param>
		public static void DoInNewScope(Action action)
		{
			Do(action, newScope: true);
		}

		/// <summary>
		/// Disposes the current Activity Scope by restoring the previous scope.
		/// </summary>
		public void Dispose()
		{
			SetActivityId(_previousActivityId);
			_activityId = _previousActivityId;
			GC.SuppressFinalize(this);
		}

		#region Helper Methods
		/// <summary>
		/// Performs an action in an activity scope.
		/// </summary>
		/// <param name="action">The action to perform.</param>
		/// <param name="newScope">True to always create a new scope.</param>
		private static void Do(Action action, bool newScope)
		{
			Guid previousActivityId = GetActivityId();
			if (newScope || previousActivityId == Guid.Empty)
			{
				Guid activityID = Guid.NewGuid();
				try
				{
					SetActivityId(activityID);
					action();
				}
				finally
				{
					SetActivityId(previousActivityId);
				}
			}
		}

		/// <summary>
		/// Get the current Activity Id.
		/// </summary>
		/// <returns>The current activity Id.</returns>
		[SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Justification = "SecurityCritical Attribute has been applied")]
		[SecurityCritical]
		private static Guid GetActivityId()
		{
			return Trace.CorrelationManager.ActivityId;
		}

		/// <summary>
		/// Sets the current Activity Id.
		/// </summary>
		/// <param name="activityId">The activity Id to set.</param>
		[SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Justification = "SecurityCritical Attribute has been applied")]
		[SecurityCritical]
		private static void SetActivityId(Guid activityId)
		{
			Trace.CorrelationManager.ActivityId = activityId;
			UnsafeNativeMethods.SetActivityId(activityId);
		}
		#endregion
	}
}
