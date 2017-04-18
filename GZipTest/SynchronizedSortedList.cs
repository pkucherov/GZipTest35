using System.Collections.Generic;
using System.Threading;

namespace GZipTest
{
    class SynchronizedSortedList<N, T>
    {
        private ReaderWriterLockSlim locker = new ReaderWriterLockSlim();
        private SortedList<N, T> list = new SortedList<N, T>();

        private ManualResetEvent empty = new ManualResetEvent(false);
        private ManualResetEvent exit = new ManualResetEvent(false);
        private WaitHandle[] emptyAndExit;
        private object retrieveLocker = new object();

        public SynchronizedSortedList()
        {
            emptyAndExit = new WaitHandle[] { empty, exit };
        }

        public int Count
        {
            get
            {
                locker.EnterReadLock();
                try
                {
                    return list.Count;
                }
                finally
                {
                    locker.ExitReadLock();
                }
            }
        }

        public void Exit()
        {
            exit.Set();
        }

        public void Add(N key, T value)
        {
            locker.EnterWriteLock();
            try
            {
                list.Add(key, value);
                empty.Set();
            }
            finally
            {
                locker.ExitWriteLock();
            }
        }

        public bool TryRetrieveValue(N key, out T value)
        {
            lock(retrieveLocker)
            {
                value = default(T);
                if (exit.WaitOne(0) && !empty.WaitOne(0))
                {                    
                    return false;
                }
                WaitHandle.WaitAny(emptyAndExit);
                locker.EnterWriteLock();
                try
                {
                    bool bRet = list.TryGetValue(key, out value);
                    if (bRet)
                    {
                        list.Remove(key);
                        if (list.Count == 0)
                        {
                            empty.Reset();
                        }
                    }

                    return bRet;
                }
                finally
                {
                    locker.ExitWriteLock();
                }
            }
        }
    }
}
