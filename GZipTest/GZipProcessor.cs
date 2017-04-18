using System;
using System.IO.Compression;
using System.IO;
using System.Threading;

namespace GZipTest
{
    class Buffer
    {
        private byte[] data;
        public byte[] Data
        {
            get
            {
                return data;
            }
        }
        private int order;
        public int Order
        {
            get
            {
                return order;
            }
        }
        private int length;

        public int Length
        {
            get
            {
                return length;
            }
        }

        public Buffer(int nOrder, byte[] d, int l)
        {
            order = nOrder;
            data = new byte[l];
            Array.Copy(d, data, l);
            length = l;
        }
    }
    class CommonData
    {
        public const int ThreadCount = 4;
        public const int MaxQueueCount = 10;
        public SynchronizedQueue<Buffer> dataQueue = new SynchronizedQueue<Buffer>(MaxQueueCount);
        public SynchronizedSortedList<int, Buffer> processedDataQueue = new SynchronizedSortedList<int, Buffer>();
        public ManualResetEvent FinishReadingEvent = new ManualResetEvent(false);
        public ManualResetEvent FinishWritingEvent = new ManualResetEvent(false);
        public ManualResetEvent[] FinishProcessingEvent = new ManualResetEvent[ThreadCount];
        public CompressionMode CompressionMode;
        public FileInfo SourceFile;
        public FileInfo TargetFile;

        public bool IsProcessingFinished()
        {
            bool ret = true;
            for (int i = 0; i < FinishProcessingEvent.Length; i++)
            {
                ret &= FinishProcessingEvent[i].WaitOne(0);
            }
            return ret;
        }
        public void WaitProcessingFinished()
        {
            WaitHandle[] finishEvents = new WaitHandle[FinishProcessingEvent.Length];
            for (int i = 0; i < FinishProcessingEvent.Length; i++)
            {
                finishEvents[i] = FinishProcessingEvent[i];
            }
            WaitHandle.WaitAll(finishEvents);
        }
    }

    class ThreadStartInfo
    {
        public int ID;
        public CommonData CompressData;
        public ThreadStartInfo(int id, CommonData data)
        {
            ID = id;
            CompressData = data;
        }
    }

    interface IGZipProcessor
    {
        void Process(string sourceFileName, string targetFileName, CompressionMode CompressionMode);
    }

    abstract class GZipProcessor : IGZipProcessor
    {
        protected int nBufferSize = 1024 * 1024;
        protected int nMaxQueueCount = 10;
        protected const uint Magic = 0xFAFABCCD;
        public void Process(string sourceFileName, string targetFileName, CompressionMode CompressionMode)
        {
            FileInfo fiSource = new FileInfo(sourceFileName);
            FileInfo fiTarget = new FileInfo(targetFileName);

            CommonData data = new CommonData();
            data.CompressionMode = CompressionMode;
            data.SourceFile = fiSource;
            data.TargetFile = fiTarget;

            for (int i = 0; i < CommonData.ThreadCount; i++)
            {
                data.FinishProcessingEvent[i] = new ManualResetEvent(false);
                Thread thread = new Thread(processDataThread);
                thread.Start(new ThreadStartInfo(i, data));
            }
            Thread threadWrite = new Thread(writeProcessedDataThread);
            threadWrite.Start(data);

            try
            {
                using (FileStream originalFileStream = fiSource.OpenRead())
                {
                    Read(originalFileStream, data);
                }
            }
            finally
            {
                data.FinishReadingEvent.Set();
                data.dataQueue.Exit();
                data.WaitProcessingFinished();
                data.processedDataQueue.Exit();
                data.FinishWritingEvent.WaitOne();
            }
        }

        private void processDataThread(object oData)
        {
            ThreadStartInfo tsi = (ThreadStartInfo)oData;
            CommonData data = tsi.CompressData;
            try
            {
                while (!data.FinishReadingEvent.WaitOne(0) || data.dataQueue.Count > 0)
                {
                    Buffer bd = data.dataQueue.Dequeue();

                    if (bd != null && bd.Length > 0)
                    {
                        Process(data, bd);
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in (De)Compression: {0}", ex.Message);
            }
            finally
            {
                data.FinishProcessingEvent[tsi.ID].Set();
            }
        }

        private void writeProcessedDataThread(object oData)
        {
            CommonData data = (CommonData)oData;
            try
            {
                int nBufferOrder = 0;

                using (FileStream compressedFileStream = File.Create(data.TargetFile.FullName))
                {
                    while (!data.IsProcessingFinished() || data.processedDataQueue.Count > 0)
                    {
                        Buffer bd = null;

                        if (!data.processedDataQueue.TryRetrieveValue(nBufferOrder, out bd))
                        {
                            Thread.Sleep(0);
                            continue;
                        }

                        if (bd != null && bd.Data.Length > 0)
                        {
                            Write(compressedFileStream, bd);
                        }
                        nBufferOrder++;                        
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in writing: {0}", ex.Message);
            }
            finally
            {
                data.FinishWritingEvent.Set();
            }
        }

        protected abstract void Read(FileStream stream, CommonData data);
        protected abstract void Process(CommonData data, Buffer bd);
        protected abstract void Write(FileStream stream, Buffer bd);
    }
}