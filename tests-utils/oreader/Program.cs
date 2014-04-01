using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;

using MailBee;
using MailBee.Mime;
using MailBee.Outlook;

public class oreader
{
    static void Main(string[] args)
    {
        if (args.Count() < 1 || !System.IO.File.Exists(args[0]))
        {
            Console.WriteLine("Unable to find file");
            Environment.Exit(1);
        }


        MailBee.Outlook.PstReader.LicenseKey = "MN600-7CB48BDAB49DB45FB40FB583BBC7-8279";

        // If Outlook is installed and running, the .PST file may be blocked for read access.
        PstReader pstRead = new PstReader(args[0]);
        PstFolderCollection folders = pstRead.GetPstRootFolders(true);

        foreach (PstFolder folder in folders)
        {
            Console.WriteLine("{0}", folder.ShortName);
            foreach (PstFolder f in folder.GetPstSubFolders(true))
            {
                Console.WriteLine("\t{0} : {1}", f.ShortName, f.Items.Count);
            }
        }

        pstRead.Close();
    }
}
