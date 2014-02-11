using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace EventSourceProxy.Tests
{
	[TestFixture]
	public class EventActivityScopeTests
	{
		[Test]
		public void GetActivityIdShouldReturnEmptyGuid()
		{
			using (EventActivityScope scope1 = new EventActivityScope())
			{
				Assert.AreEqual(Guid.Empty, scope1.PreviousActivityId);
			}
		}

		[Test]
		public void NewScopeShouldGenerateNewActivity()
		{
			using (EventActivityScope scope1 = new EventActivityScope())
			{
				// make sure we don't have an outer activity, but do have an inner activity
				Assert.AreEqual(Guid.Empty, scope1.PreviousActivityId);
				Assert.AreNotEqual(Guid.Empty, scope1.ActivityId);

				using (EventActivityScope scope2 = new EventActivityScope())
				{
					Assert.AreEqual(scope1.ActivityId, scope2.PreviousActivityId);
					Assert.AreNotEqual(scope1.ActivityId, scope2.ActivityId);
				}
			}
		}

		[Test]
		public void ReuseScopeShouldReuseActivityId()
		{
			using (EventActivityScope scope1 = new EventActivityScope())
			{
				// make sure we don't have an outer activity, but do have an inner activity
				Assert.AreEqual(Guid.Empty, scope1.PreviousActivityId);
				Assert.AreNotEqual(Guid.Empty, scope1.ActivityId);

				using (EventActivityScope scope2 = new EventActivityScope(true))
				{
					Assert.AreEqual(scope1.ActivityId, scope2.PreviousActivityId);
					Assert.AreEqual(scope1.ActivityId, scope2.ActivityId);
				}
			}
		}

		[Test]
		public void NewScopeWithExistingActivityIdShouldUseCorrectActivityId()
		{
			Guid externalGuid = Guid.Parse("c2cbe3e9-53ee-440c-b16b-2dec89df7202");
			using (EventActivityScope scope1 = new EventActivityScope(externalGuid))
			{
				Assert.AreEqual(scope1.ActivityId, externalGuid);
			}
		}

		[Test]
		public void ReuseScopeShouldReuseExternalActivityId()
		{
			Guid externalGuid = Guid.Parse("b423a74f-f5c7-4707-8555-552567ec446a");
			using (EventActivityScope scope1 = new EventActivityScope(externalGuid))
			{
				Assert.AreEqual(scope1.ActivityId, externalGuid);
				// make sure we don't have an outer activity, but do have an inner activity
				Assert.AreEqual(Guid.Empty, scope1.PreviousActivityId);
				Assert.AreNotEqual(Guid.Empty, scope1.ActivityId);

				using (EventActivityScope scope2 = new EventActivityScope(true))
				{
					Assert.AreEqual(scope1.ActivityId, scope2.PreviousActivityId);
					Assert.AreEqual(scope1.ActivityId, scope2.ActivityId);
				}
			}
		}

		[Test]
		public void NewScopeShouldGenerateNewActivityIdWhenParentHasExternalActivityId()
		{
			Guid externalGuid = Guid.Parse("adc174e5-6f7b-4280-a4a4-8d8550ab4f89");
			using (EventActivityScope scope1 = new EventActivityScope(externalGuid))
			{
				Assert.AreEqual(scope1.ActivityId, externalGuid);
				// make sure we don't have an outer activity, but do have an inner activity
				Assert.AreEqual(Guid.Empty, scope1.PreviousActivityId);
				Assert.AreNotEqual(Guid.Empty, scope1.ActivityId);

				using (EventActivityScope scope2 = new EventActivityScope())
				{
					Assert.AreEqual(scope1.ActivityId, scope2.PreviousActivityId);
					Assert.AreNotEqual(scope1.ActivityId, scope2.ActivityId);
				}
			}
		}

		[Test]
		public void NewScopeWithExternalActivityIdShouldUseCorrectActivityId()
		{
			Guid externalGuid = Guid.Parse("64a49a09-c775-4a91-b4ec-91a9a6e3caeb");
			using (EventActivityScope scope1 = new EventActivityScope())
			{
				// make sure we don't have an outer activity, but do have an inner activity
				Assert.AreEqual(Guid.Empty, scope1.PreviousActivityId);
				Assert.AreNotEqual(Guid.Empty, scope1.ActivityId);

				using (EventActivityScope scope2 = new EventActivityScope(externalGuid))
				{
					Assert.AreEqual(scope2.ActivityId, externalGuid);
					Assert.AreEqual(scope1.ActivityId, scope2.PreviousActivityId);
					Assert.AreNotEqual(scope1.ActivityId, scope2.ActivityId);
				}
			}
		}

		#region Async Tests
		public interface ILogForAsync
		{
			void Log();
		}

		public async Task TestActivityIDAsync()
		{
			var log = EventSourceImplementer.GetEventSourceAs<ILogForAsync>();

			using (var scope = new EventSourceProxy.EventActivityScope())
			{
				var before = EventActivityScope.CurrentActivityId;

				log.Log();

				await Task.Factory.StartNew(() => {});

				Assert.AreEqual(before, EventActivityScope.CurrentActivityId);
				Assert.AreEqual(before, scope.ActivityId);
			}
		}

		[Test]
		public void ActivityIDShouldRestoreAfterAwait()
		{
			TestActivityIDAsync().Wait();
		}
		#endregion
	}
}
