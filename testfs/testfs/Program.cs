using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Permissions;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Collections.Concurrent;

using testfs.Model;
using System.Text;

using Blackbird.Core.ConsoleIO;

namespace testfs
{
    public class testfs
    {
        //private static TfsContext context = null;
        //public static HashAlgorithm hashManager = null;

        private static bool verbose = false;
        private static ConcurrentQueue<TfsQueueTask> queue = null;
        private static TfsDbManager manager = null;

        private static CommandLineArgs ca = null;
        private static bool noQueue = false;
        private static bool noScan = false;
        private static object lockable = new object();

        public static void Main(string[] args)
        {
            ca = new CommandLineArgs(args);
            verbose = ca.IsOptionEnabled("verbose");

            noScan = ca.Options.IsOptionEnabled("noscan");
            noQueue = ca.Options.IsOptionEnabled("noqueue");

            // If a directory is not specified, exit program.
            if (ca.Arguments.Count < 1)
            {
                // Display the proper way to call the program.
                Console.WriteLine("Usage: testfs.exe (folder)");
                return;
            }

            manager = new TfsDbManager(ca.Arguments[0]);

            if (ca.IsOptionEnabled("reset"))
                manager.Reset();

            Run();

            manager.Dispose();
        }

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public static void Run()
        {
            if (!noScan)
                ScanManager(); // esegui in async

            if (!noQueue)
                QueueManager(); // esegui in async

            // Create a new FileSystemWatcher and set its properties.
            FileSystemWatcher watcher = new FileSystemWatcher();
            watcher.Path = ca.Arguments[0];
            // Watch for changes in LastAccess and LastWrite times, and the renaming of files or directories
            watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.Size | NotifyFilters.CreationTime;
            watcher.Filter = "*.*";
            watcher.IncludeSubdirectories = true;
            watcher.InternalBufferSize = 1024 * 1024;

            // Add event handlers.
            watcher.Error += watcher_Error;
            watcher.Changed += delegate(object sender, FileSystemEventArgs e) { DoEnqueue(TfsOperationType.Update, e.FullPath); };
            watcher.Created += delegate(object sender, FileSystemEventArgs e) { DoEnqueue(TfsOperationType.Create, e.FullPath); };
            watcher.Deleted += delegate(object sender, FileSystemEventArgs e) { DoEnqueue(TfsOperationType.Delete, e.FullPath); };
            watcher.Renamed += delegate(object sender, RenamedEventArgs e) { DoEnqueue(TfsOperationType.Rename, e.FullPath, e.OldFullPath); };

            // Begin watching.
            watcher.EnableRaisingEvents = true;
            Console.WriteLine("Enabled watcher");

            // Wait for the user to quit the program.
            Console.WriteLine("Keys: \'q\'=quit, \'s\'=stats, \'v\'=toggle verbose");
            
            ConsoleKeyInfo? c = null;
            while ( ((ConsoleKeyInfo)(c = Console.ReadKey(true))).Key != ConsoleKey.Q)
            {
                if (((ConsoleKeyInfo)c).Key == ConsoleKey.S)
                {
                    lock (lockable)
                    {
                        TfsStats stats = manager.Stats;
                        Console.WriteLine("Run:{0} Scan:{1} -- Queue:{2} waiting - Scan:{3} to go",
                            stats.RunId,
                            stats.ScanId,
                            queue.Count,
                            stats.Todo);
                    }
                }

                if (((ConsoleKeyInfo)c).Key == ConsoleKey.V)
                {
                    if (verbose == true)
                        verbose = false;
                    else
                        verbose = true;

                    Console.WriteLine("verbose is {0}", (verbose) ? "on" : "off");
                }
            }
        }

        private static void DoEnqueue(TfsOperationType operation, string arg1, string arg2 = null)
        {
            if (noQueue)
                return;

            bool? check = arg1.PathIsDirectory();
            if (check == null)
                return;

            if (queue == null)
                queue = new ConcurrentQueue<TfsQueueTask>();

            if (check == true)
            {
                // se il path e'una directory, dobbiamo inserire in coda un task per ogni file.
                foreach (string s in Directory.EnumerateFiles(arg1, "*.*", SearchOption.AllDirectories))
                {
                    // qui la cosa e' piu' complicata : recupera il path originale da arg2
                    string a2 = (operation != TfsOperationType.Rename) ? null : (Path.Combine(arg2, s.Substring(arg1.Length + 1)));

                    queue.Enqueue(new TfsQueueTask() { Operation = operation, Arg1 = s, Arg2 = a2 });

                    //if (!suppressInfo)
                    //    Console.WriteLine("Added to queue: {0}", s);
                }
            }
            else
            {
                queue.Enqueue(new TfsQueueTask() { Operation = operation, Arg1 = arg1, Arg2 = arg2 });

                //if (!suppressInfo)
               //     Console.WriteLine("Added to queue: {0}", arg1);
            }
        }

        static void watcher_Error(object sender, ErrorEventArgs e)
        {
            Console.WriteLine("Error: {0}", e.GetException().Message);
        }

        #region Managers
        private static async void QueueManager()
        {
            lock (lockable)
            {
                manager.Initialize();
            }

            await HandleQueueAsync();
        }

        private static async void ScanManager()
        {
            lock (lockable)
            {
                manager.Initialize();
            }

            GC.GetTotalMemory(true);
            DateTime start = DateTime.Now;
            Console.WriteLine("Scanning folder {0} [{1}]", manager.Scanpath.Path, GC.GetTotalMemory(false));
            await GetFilesAsync();
            DateTime end = DateTime.Now;
            //Console.WriteLine("Scan complete! {0} files found in {1}sec [{2}]", scancount.Count, (int)((end - start).TotalSeconds), GC.GetTotalMemory(false));
        }
        #endregion

        #region Async delegates for managers
        private static async Task HandleQueueAsync()
        {
            await Task.Run(() =>
            {
                while (true)
                {
                    if (queue == null)
                    {
                        queue = new ConcurrentQueue<TfsQueueTask>();
                        System.Threading.Thread.Sleep(100);
                        continue;
                    }

                    if (queue.Count < 1)
                    {
                        System.Threading.Thread.Sleep(100);
                        continue;
                    }

                    TfsQueueTask queueTask = null;
                    if (queue.TryDequeue(out queueTask) == false)
                    {
                        System.Threading.Thread.Sleep(100);
                        continue;
                    }

                    switch (queueTask.Operation)
                    {
                        case TfsOperationType.Create:
                        case TfsOperationType.Update:
                            {
                                lock (lockable)
                                {
                                    manager.AddUpdateFile(queueTask.Arg1);
                                }
                                break;
                            }

                        case TfsOperationType.Delete:
                            {
                                lock (lockable)
                                {
                                    manager.DeleteFile(queueTask.Arg1);
                                }
                                break;
                            }

                        case TfsOperationType.Rename:
                            {
                                lock (lockable)
                                {
                                    manager.Rename(queueTask.Arg1, queueTask.Arg2);
                                }
                                break;
                            }

                        default:
                            {
                                // non deve succedere MAI
                                break;
                            }
                    }

                    if (verbose)
                        Console.WriteLine("Handled request for {0} {1}", queueTask.Operation, queueTask.Arg1);

                    System.Threading.Thread.Sleep(100);
                }
            });
        }

        private static async Task GetFilesAsync()
        {
            await Task.Run(() =>
            {
                // STEP1 : use EnumerateFiles instead of GetFiles so we can early access the collection without loading TONS of files in memory
                foreach (string s in Directory.EnumerateFiles(manager.Scanpath.Path, "*.*", SearchOption.AllDirectories))
                {
                    lock (lockable)
                    {
                        manager.AddUpdateFile(s);
                        System.Threading.Thread.Sleep(100);
                    }
                }

                // STEP 2 : cleanup : remove all records which do NOT belong to current run (=> it means they had been deleted or broken)
                lock (lockable)
                {
                    manager.CleanUp();
                }
            });
        }
        #endregion
    }
}
