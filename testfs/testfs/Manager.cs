using System;
using System.Linq;
using System.IO;
using System.Security.Cryptography;
using System.Text;

using testfs.Model;

namespace testfs
{
    internal class TfsDbManager : IDisposable
    {
        private HashAlgorithm hashManager = null;
        private TfsContext context = null;
        private TfsPath scanpath = null;
        private bool initialized = false;

        private Scan scan = null;
        private Run run = null;

        public bool Initialized
        {
            get { return initialized; }
        }

        public TfsPath Scanpath
        {
            get { return scanpath; }
        }

        public HashAlgorithm HashManager
        {
            get { return hashManager; }
        }

        public TfsStats Stats
        {
            get { return GetStats(); }
        }

        public TfsDbManager(string path)
        {
            if (String.IsNullOrEmpty(path))
                throw new ArgumentNullException(path);

            hashManager = new SHA256Managed();
            string s = Path.GetFullPath(path);
            scanpath = new TfsPath() { Path = s, PathHash = hashManager.ComputeHash(s.ToHashSource()).ToHashString() };

            try
            {
                if (context == null)
                    context = new TfsContext();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
            }
        }

        public bool Initialize()
        {
            if (initialized)
                return true;

            try
            {
                scan = context.Scans.FirstOrDefault(p => p.ScanPathHash == scanpath.PathHash);
                if (scan == null)
                {
                    scan = new Scan() { ScanPath = scanpath.Path, ScanPathHash = scanpath.PathHash, HashAlg = hashManager.GetType().BaseType.Name };
                    context.Scans.Add(scan);
                    context.SaveChanges();
                }

                run = new Run() { RunDate = DateTime.Now, CheckId = Guid.NewGuid().ToString() };
                context.Runs.Add(run);
                context.SaveChanges();
                initialized = true;
            }
            catch
            {
                initialized = false;
                return false;
            }

            return true;
        }

        public bool AddUpdateFile(string s)
        {
            if (!initialized)
                return false;

            bool ret = false;
            TfsPath entryPath = new TfsPath() { Path = s.Substring(scanpath.Path.Length + 1), PathHash = hashManager.ComputeHash(s.ToHashSource()).ToHashString() };
            Model.File f = null;

            try
            {
                FileInfo fi = new FileInfo(s);
                f = context.Files.Include("Run").Include("Scan").SingleOrDefault(t => t.FilePathHash == entryPath.PathHash && t.ScanId == scan.ScanId);

                if (f == null)
                {
                    f = new Model.File()
                    {
                        FilePath = entryPath.Path,
                        FilePathHash = entryPath.PathHash,
                        Run = run,
                        Scan = scan,
                        LastWrite = fi.LastWriteTime
                    };

                    context.Files.Add(f);
                }
                else
                {
                    if (f.Run != run)
                    {
                        f.Run = run;
                        f.LastWrite = fi.LastWriteTime;
                    }
                }

                int k = context.SaveChanges();
                ret = true;
                //Console.WriteLine("Save:{0}", k);
            }
            catch
            {
                //Console.WriteLine("save failed");
            }
            finally
            {
                context.ToObjectContext().Detach(f); // detach to save memory
            }

            return ret;
        }

        public bool DeleteFile(string s)
        {
            if (!initialized)
                return false;

            bool ret = false;
            TfsPath entryPath = new TfsPath() { Path = s.Substring(scanpath.Path.Length + 1), PathHash = hashManager.ComputeHash(s.ToHashSource()).ToHashString() };

            try
            {
                Model.File f = context.Files.Include("Run").Include("Scan").SingleOrDefault(t => t.FilePathHash == entryPath.PathHash && t.ScanId == scan.ScanId);

                if (f != null)
                {
                    context.Files.Remove(f);
                    int k = context.SaveChanges();
                    ret = true;
                }
                else
                    ret = true; // se il file manca supponi siamo andati a buon fine
            }
            catch
            {
            }

            return ret;
        }

        public bool Rename(string s1, string s2)
        {
            if (!initialized)
                return false;

            bool ret = false;
            TfsPath entryPathS1 = new TfsPath() { Path = s1.Substring(scanpath.Path.Length + 1), PathHash = hashManager.ComputeHash(s1.ToHashSource()).ToHashString() };
            TfsPath entryPathS2 = new TfsPath() { Path = s2.Substring(scanpath.Path.Length + 1), PathHash = hashManager.ComputeHash(s2.ToHashSource()).ToHashString() };

            try
            {
                FileInfo fi = new FileInfo(s1);

                Model.File f2 = context.Files.Include("Run").Include("Scan").SingleOrDefault(t => t.FilePathHash == entryPathS2.PathHash && t.ScanId == scan.ScanId);
                Model.File f1 = context.Files.Include("Run").Include("Scan").SingleOrDefault(t => t.FilePathHash == entryPathS1.PathHash && t.ScanId == scan.ScanId);

                if (f2 != null)
                    context.Files.Remove(f2);


                if (f1 == null)
                {
                    f1 = new Model.File()
                    {
                        FilePath = entryPathS1.Path,
                        FilePathHash = entryPathS1.PathHash,
                        Run = run,
                        Scan = scan,
                        LastWrite = fi.LastWriteTime
                    };

                    context.Files.Add(f1);
                }
                else
                {
                    if (f1.Run != run)
                    {
                        f1.Run = run;
                        f1.LastWrite = fi.LastWriteTime;
                    }
                }

                int k = context.SaveChanges();
                ret = true;
                //Console.WriteLine("Save:{0}", k);
            }
            catch
            {
                //Console.WriteLine("save failed");
            }

            return ret;
        }

        public void CleanUp()
        {
            if (!initialized)
                return;

            try
            {
                context.Files.RemoveRange(context.Files.Include("Run").Include("Scan").Where(p => p.ScanId == scan.ScanId && p.Run.RunId != run.RunId));
            }
            catch
            {
            }
        }

        public void Reset()
        {
            if (initialized)
                return;

            try
            {
                if (context == null)
                    context = new TfsContext();

                context.Runs.RemoveRange(context.Runs);
                context.Scans.RemoveRange(context.Scans);
                context.SaveChanges();
            }
            catch
            {
            }
        }

        private TfsStats GetStats()
        {
            return new TfsStats()
                            {
                                RunId = run.RunId,
                                ScanId = scan.ScanId,
                                Todo = context.Files.Include("Run").Include("Scan").LongCount(p => p.ScanId == scan.ScanId && p.Run.RunId != run.RunId)
                            };
        }

        #region IDisposable
        protected bool disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (initialized)
                        initialized = false;

                    hashManager.Dispose();
                    hashManager = null;

                    context.Dispose();
                    context = null;

                    scan = null;
                    run = null;
                }

                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}