using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using datainterfaces;

namespace datatypes
{
    public class EmailMessage : IEmailMessage
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

        public EmailMessage() { }

        public EmailMessage(IEmailMessage inboxMsg)
        {
            this.Filename = inboxMsg.Filename;
            this.FullPathName = inboxMsg.FullPathName;
            this.MessageID = inboxMsg.MessageID;
            this.ServerUID = inboxMsg.ServerUID;
            this.Importance = (int)inboxMsg.Importance;
            this.Priority = (int)inboxMsg.Priority;
            this.Size = inboxMsg.Size;
            this.Received = inboxMsg.Received;
            this.Sent = inboxMsg.Sent;
            this.Sender = inboxMsg.Sender;
            this.Recipients = inboxMsg.Recipients;
            this.RecipientsCC = inboxMsg.RecipientsCC;
            this.RecipientsBCC = inboxMsg.RecipientsBCC;
            this.Subject = inboxMsg.Subject;
            this.Message = inboxMsg.Message;
        }
    }
}
