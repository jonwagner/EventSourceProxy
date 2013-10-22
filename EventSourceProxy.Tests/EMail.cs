using System;
using System.Collections.Generic;

namespace EventSourceProxy.Tests
{
    public interface IEmail
    {
        string From { get; }
        string To { get; }
        string Subject { get; }
        string Body { get; }

        IEnumerable<Byte[]> Attachments { get; }
    }

    public class EMail : IEmail
    {
        public EMail(string from, string to, string subject, string body, IEnumerable<Byte[]> attachements = null)
        {
            From = from;
            To = to;
            Subject = subject;
            Body = body;
            Attachments = attachements;
        }

        public string From { get; private set; }
        public string To { get; private set; }
        public string Subject { get; private set; }
        public string Body { get; private set; }
        public IEnumerable<Byte[]> Attachments { get; private set; }
    }
}
