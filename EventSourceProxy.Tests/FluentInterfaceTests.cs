using System;
using System.Collections.Generic;
using System.Linq;
using EventSourceProxy.Fluent;
using NUnit.Framework;

namespace EventSourceProxy.Tests
{
    [TestFixture]
    public class FluentInterfaceTests
    {
        [Test]
        public void ForSourceWithParamTrace()
        {
            ITraceDescription traceDesciption = For<IEmailer>.With<IEmail>().Trace(email => email.From);

            Assert.IsNotNull(traceDesciption);
            Assert.That(traceDesciption, Is.InstanceOf<ITraceDescriptor>());

            ITraceDescriptor traceDescriptor = (ITraceDescriptor) traceDesciption;

            Assert.That(traceDescriptor.Source, Is.EqualTo(typeof(IEmailer)));
            Assert.That(traceDescriptor.Param, Is.EqualTo(typeof(IEmail)));
            Assert.That(traceDescriptor.Method, Is.Null);

            ITraceValue[] traceValues = traceDescriptor.Values.ToArray();

            Assert.That(traceValues.Length, Is.EqualTo(1));
            Assert.That(traceValues[0].Type, Is.EqualTo(typeof(string)));
            Assert.That(traceValues[0].Alias, Is.EqualTo("From"));
            Assert.That(traceValues[0].Expression, Is.Not.Null);
        }

        [Test]
        public void ForSourceWithParamTraceUsing()
        {
            ITraceDescription traceDesciption = For<IEmailer>.With<IEmail>().Trace(email => email.Attachments).Using(attachments => string.Join("/", attachments.Select(Convert.ToBase64String).ToArray()));

            Assert.IsNotNull(traceDesciption);
            Assert.That(traceDesciption, Is.InstanceOf<ITraceDescriptor>());

            ITraceDescriptor traceDescriptor = (ITraceDescriptor)traceDesciption;

            Assert.That(traceDescriptor.Source, Is.EqualTo(typeof(IEmailer)));
            Assert.That(traceDescriptor.Param, Is.EqualTo(typeof(IEmail)));
            Assert.That(traceDescriptor.Method, Is.Null);

            ITraceValue[] traceValues = traceDescriptor.Values.ToArray();

            Assert.That(traceValues.Length, Is.EqualTo(1));
            Assert.That(traceValues[0].Type, Is.EqualTo(typeof(IEnumerable<byte[]>)));
            Assert.That(traceValues[0].Alias, Is.EqualTo("Attachments"));
            Assert.That(traceValues[0].Expression, Is.Not.Null);
            Assert.That(traceValues[0].Serializer, Is.Not.Null);
        }

        [Test]
        public void ForSourceWithParamTraceAs()
        {
            ITraceDescription traceDesciption = For<IEmailer>.With<IEmail>().Trace(email => email.From).As("Sender");

            Assert.IsNotNull(traceDesciption);
            Assert.That(traceDesciption, Is.InstanceOf<ITraceDescriptor>());

            ITraceDescriptor traceDescriptor = (ITraceDescriptor)traceDesciption;

            Assert.That(traceDescriptor.Source, Is.EqualTo(typeof(IEmailer)));
            Assert.That(traceDescriptor.Param, Is.EqualTo(typeof(IEmail)));
            Assert.That(traceDescriptor.Method, Is.Null);

            ITraceValue[] traceValues = traceDescriptor.Values.ToArray();

            Assert.That(traceValues.Length, Is.EqualTo(1));
            Assert.That(traceValues[0].Type, Is.EqualTo(typeof(string)));
            Assert.That(traceValues[0].Alias, Is.EqualTo("Sender"));
            Assert.That(traceValues[0].Expression, Is.Not.Null);
        }

        [Test]
        public void ForSourceWithParamTraceTrace()
        {
            ITraceDescription traceDesciption = For<IEmailer>.With<IEmail>().Trace(email => email.From).Trace(email => email.To);

            Assert.IsNotNull(traceDesciption);
            Assert.That(traceDesciption, Is.InstanceOf<ITraceDescriptor>());

            ITraceDescriptor traceDescriptor = (ITraceDescriptor)traceDesciption;

            Assert.That(traceDescriptor.Source, Is.EqualTo(typeof(IEmailer)));
            Assert.That(traceDescriptor.Param, Is.EqualTo(typeof(IEmail)));
            Assert.That(traceDescriptor.Method, Is.Null);

            ITraceValue[] traceValues = traceDescriptor.Values.ToArray();

            Assert.That(traceValues.Length, Is.EqualTo(2));

            Assert.That(traceValues[0].Type, Is.EqualTo(typeof(string)));
            Assert.That(traceValues[0].Alias, Is.EqualTo("From"));
            Assert.That(traceValues[0].Expression, Is.Not.Null);

            Assert.That(traceValues[1].Type, Is.EqualTo(typeof(string)));
            Assert.That(traceValues[1].Alias, Is.EqualTo("To"));
            Assert.That(traceValues[1].Expression, Is.Not.Null);
        }

        [Test]
        public void ForSourceWithParamTraceAsTrace()
        {
            ITraceDescription traceDesciption = For<IEmailer>.With<IEmail>().Trace(email => email.From).As("Sender").Trace(email => email.To).As("Recipient");

            Assert.IsNotNull(traceDesciption);
            Assert.That(traceDesciption, Is.InstanceOf<ITraceDescriptor>());

            ITraceDescriptor traceDescriptor = (ITraceDescriptor)traceDesciption;

            Assert.That(traceDescriptor.Source, Is.EqualTo(typeof(IEmailer)));
            Assert.That(traceDescriptor.Param, Is.EqualTo(typeof(IEmail)));
            Assert.That(traceDescriptor.Method, Is.Null);

            ITraceValue[] traceValues = traceDescriptor.Values.ToArray();

            Assert.That(traceValues.Length, Is.EqualTo(2));

            Assert.That(traceValues[0].Type, Is.EqualTo(typeof(string)));
            Assert.That(traceValues[0].Alias, Is.EqualTo("Sender"));
            Assert.That(traceValues[0].Expression, Is.Not.Null);

            Assert.That(traceValues[1].Type, Is.EqualTo(typeof(string)));
            Assert.That(traceValues[1].Alias, Is.EqualTo("Recipient"));
            Assert.That(traceValues[1].Expression, Is.Not.Null);
        }

        [Test]
        public void ForSourceWithMethodParamTrace()
        {
            ITraceDescription traceDesciption = For<IEmailer>.Method(source => source.Send(Any<IEmail>.Ignore)).With<IEmail>().Trace(email => email.From);

            Assert.IsNotNull(traceDesciption);
            Assert.That(traceDesciption, Is.InstanceOf<ITraceDescriptor>());

            ITraceDescriptor traceDescriptor = (ITraceDescriptor)traceDesciption;

            Assert.That(traceDescriptor.Source, Is.EqualTo(typeof(IEmailer)));
            Assert.That(traceDescriptor.Param, Is.EqualTo(typeof(IEmail)));
            Assert.That(traceDescriptor.Method, Is.Not.Null);

            ITraceValue[] traceValues = traceDescriptor.Values.ToArray();

            Assert.That(traceValues.Length, Is.EqualTo(1));

            Assert.That(traceValues[0].Type, Is.EqualTo(typeof(string)));
            Assert.That(traceValues[0].Alias, Is.EqualTo("From"));
            Assert.That(traceValues[0].Expression, Is.Not.Null);
        }

        [Test]
        public void ForSourceWithMethodParamTraceAs()
        {
            ITraceDescription traceDesciption = For<IEmailer>.Method(source => source.Send(Any<IEmail>.Ignore)).With<IEmail>().Trace(email => email.From).As("Sender");

            Assert.IsNotNull(traceDesciption);
            Assert.That(traceDesciption, Is.InstanceOf<ITraceDescriptor>());

            ITraceDescriptor traceDescriptor = (ITraceDescriptor)traceDesciption;

            Assert.That(traceDescriptor.Source, Is.EqualTo(typeof(IEmailer)));
            Assert.That(traceDescriptor.Param, Is.EqualTo(typeof(IEmail)));
            Assert.That(traceDescriptor.Method, Is.Not.Null);

            ITraceValue[] traceValues = traceDescriptor.Values.ToArray();

            Assert.That(traceValues.Length, Is.EqualTo(1));

            Assert.That(traceValues[0].Type, Is.EqualTo(typeof(string)));
            Assert.That(traceValues[0].Alias, Is.EqualTo("Sender"));
            Assert.That(traceValues[0].Expression, Is.Not.Null);
        }

        [Test]
        public void AnythingWithParamTrace()
        {
            ITraceDescription traceDesciption = Anything.With<IEmail>().Trace(email => email.From);

            Assert.IsNotNull(traceDesciption);
            Assert.That(traceDesciption, Is.InstanceOf<ITraceDescriptor>());

            ITraceDescriptor traceDescriptor = (ITraceDescriptor)traceDesciption;

            Assert.That(traceDescriptor.Source, Is.Null);
            Assert.That(traceDescriptor.Param, Is.EqualTo(typeof(IEmail)));
            Assert.That(traceDescriptor.Method, Is.Null);

            ITraceValue[] traceValues = traceDescriptor.Values.ToArray();

            Assert.That(traceValues.Length, Is.EqualTo(1));

            Assert.That(traceValues[0].Type, Is.EqualTo(typeof(string)));
            Assert.That(traceValues[0].Alias, Is.EqualTo("From"));
            Assert.That(traceValues[0].Expression, Is.Not.Null);
        }

        [Test]
        public void ForParamTraceUsing()
        {
            ITraceDescription traceDesciption = Anything.With<IEmail>().Trace(email => email.Attachments).Using(attachments => string.Join("/", attachments.Select(Convert.ToBase64String).ToArray()));

            Assert.IsNotNull(traceDesciption);
            Assert.That(traceDesciption, Is.InstanceOf<ITraceDescriptor>());

            ITraceDescriptor traceDescriptor = (ITraceDescriptor)traceDesciption;

            Assert.That(traceDescriptor.Source, Is.Null);
            Assert.That(traceDescriptor.Param, Is.EqualTo(typeof(IEmail)));
            Assert.That(traceDescriptor.Method, Is.Null);

            ITraceValue[] traceValues = traceDescriptor.Values.ToArray();

            Assert.That(traceValues.Length, Is.EqualTo(1));
            Assert.That(traceValues[0].Type, Is.EqualTo(typeof(IEnumerable<byte[]>)));
            Assert.That(traceValues[0].Alias, Is.EqualTo("Attachments"));
            Assert.That(traceValues[0].Expression, Is.Not.Null);
            Assert.That(traceValues[0].Serializer, Is.Not.Null);
        }

        [Test]
        public void AnythingWithParamTraceAs()
        {
            ITraceDescription traceDesciption = Anything.With<IEmail>().Trace(email => email.From).As("Sender");

            Assert.IsNotNull(traceDesciption);
            Assert.That(traceDesciption, Is.InstanceOf<ITraceDescriptor>());

            ITraceDescriptor traceDescriptor = (ITraceDescriptor)traceDesciption;

            Assert.That(traceDescriptor.Source, Is.Null);
            Assert.That(traceDescriptor.Param, Is.EqualTo(typeof(IEmail)));
            Assert.That(traceDescriptor.Method, Is.Null);

            ITraceValue[] traceValues = traceDescriptor.Values.ToArray();

            Assert.That(traceValues.Length, Is.EqualTo(1));

            Assert.That(traceValues[0].Type, Is.EqualTo(typeof(string)));
            Assert.That(traceValues[0].Alias, Is.EqualTo("Sender"));
            Assert.That(traceValues[0].Expression, Is.Not.Null);
        }

        [Test]
        public void AnythingWithParamTraceTrace()
        {
            ITraceDescription traceDesciption = Anything.With<IEmail>().Trace(email => email.From).Trace(email => email.To);

            Assert.IsNotNull(traceDesciption);
            Assert.That(traceDesciption, Is.InstanceOf<ITraceDescriptor>());

            ITraceDescriptor traceDescriptor = (ITraceDescriptor)traceDesciption;

            Assert.That(traceDescriptor.Source, Is.Null);
            Assert.That(traceDescriptor.Param, Is.EqualTo(typeof(IEmail)));
            Assert.That(traceDescriptor.Method, Is.Null);

            ITraceValue[] traceValues = traceDescriptor.Values.ToArray();

            Assert.That(traceValues.Length, Is.EqualTo(2));

            Assert.That(traceValues[0].Type, Is.EqualTo(typeof(string)));
            Assert.That(traceValues[0].Alias, Is.EqualTo("From"));
            Assert.That(traceValues[0].Expression, Is.Not.Null);

            Assert.That(traceValues[1].Type, Is.EqualTo(typeof(string)));
            Assert.That(traceValues[1].Alias, Is.EqualTo("To"));
            Assert.That(traceValues[1].Expression, Is.Not.Null);
        }

        [Test]
        public void AnythingWithParamTraceAsTraceAs()
        {
            ITraceDescription traceDesciption = Anything.With<IEmail>().Trace(email => email.From).As("Sender").Trace(email => email.To).As("Recipient");

            Assert.IsNotNull(traceDesciption);
            Assert.That(traceDesciption, Is.InstanceOf<ITraceDescriptor>());

            ITraceDescriptor traceDescriptor = (ITraceDescriptor)traceDesciption;

            Assert.That(traceDescriptor.Source, Is.Null);
            Assert.That(traceDescriptor.Param, Is.EqualTo(typeof(IEmail)));
            Assert.That(traceDescriptor.Method, Is.Null);

            ITraceValue[] traceValues = traceDescriptor.Values.ToArray();

            Assert.That(traceValues.Length, Is.EqualTo(2));

            Assert.That(traceValues[0].Type, Is.EqualTo(typeof(string)));
            Assert.That(traceValues[0].Alias, Is.EqualTo("Sender"));
            Assert.That(traceValues[0].Expression, Is.Not.Null);

            Assert.That(traceValues[1].Type, Is.EqualTo(typeof(string)));
            Assert.That(traceValues[1].Alias, Is.EqualTo("Recipient"));
            Assert.That(traceValues[1].Expression, Is.Not.Null);
        }
    }
}
