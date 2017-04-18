using System.Collections.Generic;
using System.Threading;

namespace GZipTest
{
    class SynchronizedQueue<T>
    {
        private ReaderWriterLockSlim locker = new ReaderWriterLockSlim();
        private Queue<T> queue = new Queue<T>();

        private ManualResetEvent empty = new ManualResetEvent(false);
        private ManualResetEvent full = new ManualResetEvent(true);
        private ManualResetEvent exit = new ManualResetEvent(false);
        private WaitHandle[] emptyAndExit;
        private object dequeueLocker = new object();
        private object enqueueLocker = new object();
        private readonly int size;
        
        public SynchronizedQueue(int nSize)
        {
            size = nSize;
            emptyAndExit = new WaitHandle[] { empty, exit };            
        }
        public int Count
        {
            get
            {
                locker.EnterReadLock();
                try
                {                     
                    return queue.Count;                    
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

        public T Dequeue()
        {
            lock(dequeueLocker)
            {                
                if (exit.WaitOne(0) && !empty.WaitOne(0))
                {                    
                    return default(T);
                }                
                WaitHandle.WaitAny(emptyAndExit);                            
                locker.EnterWriteLock();                
                try
                {
                    T ret = default(T);
                    if (queue.Count > 0)
                    {
                        full.Set();
                        ret = queue.Dequeue();
                        if (queue.Count == 0)
                        {                            
                            empty.Reset();                            
                        }
                    }

                    return ret;
                }
                finally
                {                    
                    locker.ExitWriteLock();                    
                }
            }         
        }

        public void Enqueue(T item)
        {
            lock(enqueueLocker)
            {
                locker.EnterWriteLock();
                try
                {                    
                    if (queue.Count > size)
                    {                        
                        full.Reset();                        
                        locker.ExitWriteLock();
                        full.WaitOne();
                        locker.EnterWriteLock();                        
                    }
                    queue.Enqueue(item);
                }
                finally
                {
                    locker.ExitWriteLock();
                }
                empty.Set();
            }
        }
    }
}
