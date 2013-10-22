using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace EventSourceProxy.Tests
{
    [TestFixture]
    public class EventDataProviderTests
    {
        [Test]
        public void ShouldReturnCorrectSchemaInformation()
        {
            EventDataProvider<IEmail> emailDataProvider = new EventDataProvider<IEmail>(
                EventDataProvider<IEmail>.Inspect(email => email.From),
                EventDataProvider<IEmail>.Inspect(email => email.To),
                EventDataProvider<IEmail>.Inspect(email => email.Subject),
                EventDataProvider<IEmail>.Inspect(email => email.Body)
            );

            IEnumerable<Tuple<string, Type>> expected = new []
            {
                Tuple.Create("From", typeof(string)),
                Tuple.Create("To", typeof(string)),
                Tuple.Create("Subject", typeof(string)),
                Tuple.Create("Body", typeof(string)),
            };

            IEnumerable<Tuple<string, Type>> actual = emailDataProvider.Schema.ToArray();

            CollectionAssert.AreEquivalent(expected, actual);
        }

        [Test]
        public void ShouldReturnCorrectPayloadInformation()
        {
            EventDataProvider<IEmail> emailDataProvider = new EventDataProvider<IEmail>(
                EventDataProvider<IEmail>.Inspect(source => source.From),
                EventDataProvider<IEmail>.Inspect(source => source.To),
                EventDataProvider<IEmail>.Inspect(source => source.Subject),
                EventDataProvider<IEmail>.Inspect(source => source.Body)
            );

            IEmail email = new EMail("me@where.i.am", "you@where.you.are", "testing", "test");

            IEnumerable<Tuple<string, object>> expected = new[]
            {
                Tuple.Create<string, object>("From", "me@where.i.am"),
                Tuple.Create<string, object>("To", "you@where.you.are"),
                Tuple.Create<string, object>("Subject", "testing"),
                Tuple.Create<string, object>("Body", "test"),
            };

            IEnumerable<Tuple<string, object>> actual = emailDataProvider.GetPayloadFrom(email);

            CollectionAssert.AreEquivalent(expected, actual);
        }
    }
}
