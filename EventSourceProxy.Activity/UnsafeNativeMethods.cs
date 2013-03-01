using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace EventSourceProxy
{
	/// <summary>
	/// Internal unsafe methods.
	/// </summary>
	static class UnsafeNativeMethods
	{
		/// <summary>
		/// Get the current Activity ID.
		/// </summary>
		private const int ActivityIdGet = 1;

		/// <summary>
		/// Set the current Activity ID.
		/// </summary>
		private const int ActivityIdSet = 2;

		/// <summary>
		/// Success code.
		/// </summary>
		private const ulong Success = 0;

		/// <summary>
		/// Returns the current Activity ID.
		/// </summary>
		/// <returns>The current Activity ID.</returns>
		internal static Guid GetActivityId()
		{
			Guid g = Guid.Empty;

			CallEventActivityIdControl(ActivityIdGet, ref g);

			return g;
		}

		/// <summary>
		/// Sets the current Activity ID.
		/// </summary>
		/// <param name="guid">The new Activity Id.</param>
		/// <returns>The previous Activity ID.</returns>
		internal static Guid SetActivityId(Guid guid)
		{
			CallEventActivityIdControl(ActivityIdSet, ref guid);

			return guid;
		}

		/// <summary>
		/// Calls EventActivityIdControl and throws an exception on failure.
		/// </summary>
		/// <param name="controlCode">The control code for the method.</param>
		/// <param name="activityId">The ActivityId to get or set.</param>
		private static void CallEventActivityIdControl(int controlCode, ref Guid activityId)
		{
			uint result = UnsafeNativeMethods.EventActivityIdControl(controlCode, ref activityId);
			if (result != Success)
				throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, "EventActivityIdControl {0} failed with result {1}", controlCode, result));
		}

		[SecurityCritical, DllImport("advapi32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
		private static extern uint EventActivityIdControl(int ControlCode, ref Guid activityId);
	}
}
