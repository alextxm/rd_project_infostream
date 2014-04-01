using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace testfs
{
    internal static class TfsUtils
    {
        /// <summary>
        /// controlla se il path e' una directory
        /// </summary>
        /// <param name="path">path da controllare</param>
        /// <returns></returns>
        public static bool? PathIsDirectory(this string path)
        {
            try
            {
                FileAttributes attr = File.GetAttributes(path);
                return attr.HasFlag(FileAttributes.Directory);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// controlla se il path e' un file
        /// </summary>
        /// <param name="path">path da controllare</param>
        /// <returns></returns>
        public static bool? PathIsFile(this string path)
        {
            try
            {
                FileAttributes attr = File.GetAttributes(path);
                return !attr.HasFlag(FileAttributes.Directory);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Controlla se il file e' in uso
        /// </summary>
        /// <param name="file">file da controllare</param>
        /// <returns></returns>
        public static bool IsFileLocked(this FileInfo file)
        {
            FileStream stream = null;

            try
            {
                stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {
                // the file is unavailable because it is:
                // still being written to
                // or being processed by another thread
                // or does not exist (has already been processed)
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            // file is not locked
            return false;
        }

        /// <summary>
        /// Controlla se il file e' in uso
        /// </summary>
        /// <param name="fileName">file da controllare</param>
        /// <returns></returns>
        public static bool IsFileLocked(this string fileName)
        {
            return new FileInfo(Path.GetFullPath(fileName)).IsFileLocked();
        }

        public static System.Data.Entity.Core.Objects.ObjectContext ToObjectContext(this System.Data.Entity.DbContext context)
        {
            return ((System.Data.Entity.Infrastructure.IObjectContextAdapter)context).ObjectContext;
        }

        public static byte[] ToHashSource(this string source)
        {
            return System.Text.ASCIIEncoding.ASCII.GetBytes(source);
        }

        public static string ToHashString(this byte[] hash)
        {
            if (hash == null)
                return null;

            StringBuilder sb = new StringBuilder();

            foreach (byte b in hash)
                sb.Append(b.ToString("x2", System.Globalization.CultureInfo.InvariantCulture));

            return sb.ToString();
        }

        public static string ComputeHash(this string source, HashAlgorithm alg)
        {
            return alg.ComputeHash(source.ToHashSource()).ToHashString();
        }

        private static bool IsApplicationRunningOnMono(string processName)
        {
            int processFound = 0;

            //find all processes called 'mono', that's necessary because our app runs under the mono process!
            System.Diagnostics.Process[] monoProcesses = System.Diagnostics.Process.GetProcessesByName("mono");

            for (int i = 0; i <= monoProcesses.GetUpperBound(0); ++i)
            {
                System.Diagnostics.ProcessModuleCollection processModuleCollection = monoProcesses[i].Modules;

                for (int j = 0; j < processModuleCollection.Count; ++j)
                {
                    if (processModuleCollection[j].FileName.EndsWith(processName))
                        processFound++;
                }
            }

            // we don't find the current process, but if there is already another one running, return true!
            return (processFound == 1);
        }

    }
}