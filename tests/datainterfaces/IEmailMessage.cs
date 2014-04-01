using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace datainterfaces
{
    public class IEmailMessage
    {
        public string Sender { get; set; }
        public List<string> Recipients { get; set; }
        public List<string> RecipientsCC { get; set; }
        public List<string> RecipientsBCC { get; set; }

        public string Subject { get; set; }
        public string Message { get; set; }

        public int Priority { get; set; }
        public int Importance { get; set; }
        public DateTime Sent { get; set; }
        public DateTime Received { get; set; }
        public int Size { get; set; }

        public string Filename { get; set; }
        public string FullPathName { get; set; }
        public string MessageID { get; set; }
        public string ServerUID { get; set; }
    }
}
