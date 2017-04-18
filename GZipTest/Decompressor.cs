using System;
using System.IO.Compression;
using System.IO;
using System.Threading;


namespace GZipTest
{
    class Decompressor : GZipProcessor
    {
        private int nReadBufferSize = 4096;
        protected override void Read(FileStream originalFileStream, CommonData data)
        {
            int nBufferOrder = 0;
            for (;;)
            {
                int numRead;
                byte[] abMagic = new byte[4];
                numRead = originalFileStream.Read(abMagic, 0, 4);
                if (numRead == 0)
                    break;

                uint nMagic = BitConverter.ToUInt32(abMagic, 0);

                if (nMagic != Magic)
                    throw new InvalidOperationException("Invalid magic block");

                byte[] abLength = new byte[4];
                numRead = originalFileStream.Read(abLength, 0, 4);
                if (numRead == 0)
                    break;
                int nLength = BitConverter.ToInt32(abLength, 0);
                
                byte[] buffer = new byte[nLength];

                numRead = originalFileStream.Read(buffer, 0, nLength);
                if (numRead == 0)
                    break;
                                
                Buffer bd = new Buffer(nBufferOrder++, buffer, numRead);
                                               
                data.dataQueue.Enqueue(bd);
                
                Thread.Sleep(0);
            }
        }

        protected override void Process(CommonData data, Buffer bd)
        {
            using (MemoryStream outStream = new MemoryStream())
            {
                using (MemoryStream memStream = new MemoryStream(bd.Data))
                {
                    using (GZipStream gzipStream = new GZipStream(memStream, data.CompressionMode))
                    {
                        byte[] buffer = new byte[nReadBufferSize];
                        int numRead;
                        while ((numRead = gzipStream.Read(buffer, 0, buffer.Length)) != 0)
                        {
                            outStream.Write(buffer, 0, numRead);
                        }
                    }
                }
                byte[] outBuffer = outStream.ToArray();
                Buffer outData = new Buffer(bd.Order, outBuffer, outBuffer.Length);
                                
                data.processedDataQueue.Add(bd.Order, outData);                
            }
        }

        protected override void Write(FileStream compressedFileStream, Buffer bd)
        {
            compressedFileStream.Write(bd.Data, 0, bd.Data.Length);
        }
    }
}
