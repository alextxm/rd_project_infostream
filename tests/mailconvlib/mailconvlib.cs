using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;

using MailBee;
using MailBee.Mime;
using MailBee.Outlook;

using datainterfaces;

public static class mailconvlib
{
    public delegate bool ConverterDelegate(IEmailMessage message);

    public static void Convert<T>(string pstFile, ConverterDelegate convDelegate) where T : IEmailMessage
    {
        MailBee.Outlook.PstReader.LicenseKey = "MN600-7CB48BDAB49DB45FB40FB583BBC7-8279";

        // If Outlook is installed and running, the .PST file may be blocked for read access.
        PstReader pstRead = new PstReader(pstFile);
        PstFolderCollection folders = pstRead.GetPstRootFolders(true);

        int limit = 20;
        long sz = (long)(GC.GetTotalMemory(false) / (1024 * 1024)); // MB
        long szN = sz;

        foreach (PstFolder folder in folders)
        {
            Console.WriteLine("{0}", folder.ShortName);
            foreach (PstFolder f in folder.GetPstSubFolders(true))
            {
                //Console.WriteLine("\t{0} : {1}", f.ShortName, f.Items.Count);

                int i=0;
                foreach (PstItem item in f.Items)
                {
                    if(item.PstType != PstItemType.Message)
                        continue;

                    using (MailMessage inboxMsg = item.GetAsMailMessage())
                    {
                        convDelegate(inboxMsg.ToIEmailMessage<T>(f.Name));
                    }
                    //messages.Add(inboxMsg.ToEmailMessage(f.Name));
                    long szz = (long)(GC.GetTotalMemory(false) / (1024 * 1024)); // MB
                    int th = (szz > szN) ? (int)(((double)szz / (double)szN) * 100) : 0;
                    if (th > 100 && (th - 100) > limit)
                    {
                        Console.WriteLine("Pressure raised to {0}% : forcing cleanup", th);
                        szN = GC.GetTotalMemory(true);
                    }

                    i++;
                }
            }

            //if (folder.ShortName.ToLower() == "inbox")
            //{
            //    // For all inbox e-mails, display subject.
            //    MailMessage inboxMsg = null;
            //    foreach (PstItem item in folder.Items)
            //    {
            //        inboxMsg = item.GetAsMailMessage();
            //        Console.WriteLine(inboxMsg.Subject);
            //    }

            //    // And save the last e-mail to disk as .EML file.
            //    if (inboxMsg != null)
            //    {
            //        inboxMsg.SaveMessage(@"C:\Temp\inbox.eml");
            //    }
            //}

            //// Save last e-mail in Drafts folder to disk as .EML file.
            //// Here, we do not use "foreach" and access the last item directly.
            //if (folder.ShortName.ToLower() == "drafts")
            //{
            //    if (folder.Items.Count > 0)
            //    {
            //        // Note that indices are zero-based (in Outlook interop, they are 1-based).
            //        MailMessage draftsMsg = folder.Items[folder.Items.Count - 1].GetAsMailMessage();
            //        draftsMsg.SaveMessage(@"C:\Temp\drafts.eml");
            //    }
            //}
        }

        pstRead.Close();
    }

    private static IEmailMessage ToIEmailMessage<T>(this MailMessage inboxMsg, string folder) where T : IEmailMessage
    {
        IEmailMessage msg = (Activator.CreateInstance<T>() as IEmailMessage);
        if (msg == null)
            return null;

        msg.Filename = (inboxMsg.Filename == null) ? String.Empty : inboxMsg.Filename;
        msg.FullPathName = folder;
        msg.MessageID = inboxMsg.MessageID;
        msg.ServerUID = (inboxMsg.UidOnServer == null) ? String.Empty : inboxMsg.UidOnServer.ToString();
        msg.Importance = (int)inboxMsg.Importance;
        msg.Priority = (int)inboxMsg.Priority;
        msg.Size = inboxMsg.Size;
        msg.Received = inboxMsg.DateReceived;
        msg.Sent = inboxMsg.DateSent;
        msg.Sender = inboxMsg.From.Email;
        msg.Recipients = inboxMsg.To.Cast<EmailAddress>().Select(p => p.Email).ToList();
        msg.RecipientsCC = inboxMsg.Cc.Cast<EmailAddress>().Select(p => p.Email).ToList();
        msg.RecipientsBCC = inboxMsg.Bcc.Cast<EmailAddress>().Select(p => p.Email).ToList();
        msg.Subject = inboxMsg.Subject;
        msg.Message = inboxMsg.BodyPlainText;

        return msg;
    }
}

//public class mailconvlibV2 : IDisposable
//{
//    public mailconvlibV2()
//    {

//    }

//    private bool disposed = false;

//    public void Dispose()
//    {
//        Dispose(false);
//        GC.SuppressFinalize();
//    }

//    private void Dispose(bool disposing)
//    {

//    }
//}
