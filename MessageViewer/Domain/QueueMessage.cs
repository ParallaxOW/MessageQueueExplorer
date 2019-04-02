using Microsoft.Azure.ServiceBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageViewer.Domain
{
    public class QueueMessage
    {
        public Message Message { get; set; }
        public string MessageString { get { return Encoding.UTF8.GetString(Message.Body); } }
        public string LockToken { get { return Message.SystemProperties.LockToken; } }
    }
}
