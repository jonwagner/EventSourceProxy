using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventSourceProxy.Tests
{
    public interface IEmail
    {
        string From { get; }
        string To { get; }
        string Subject { get; }
        string Body { get; }
    }

    public class EMail : IEmail
    {
        public EMail(string from, string to, string subject, string body)
        {
            From = from;
            To = to;
            Subject = subject;
            Body = body;
        }

        public string From { get; private set; }
        public string To { get; private set; }
        public string Subject { get; private set; }
        public string Body { get; private set; }
    }
}
