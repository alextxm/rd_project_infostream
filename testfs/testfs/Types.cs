using System;

namespace testfs
{
    internal enum TfsOperationType
    {
        Create,
        Delete,
        Update,
        Rename
    }

    internal class TfsPath
    {
        public string Path { get; set; }
        public string PathHash { get; set; }
    }

    internal class TfsQueueTask
    {
        public TfsOperationType Operation { get; set; }
        public string Arg1 { get; set; }
        public string Arg2 { get; set; }
    }

    public class TfsStats
    {
        public long RunId { get; set; }
        public long ScanId { get; set; }
        public long Todo { get; set; }
    }
}