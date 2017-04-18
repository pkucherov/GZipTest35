using System;
using System.IO.Compression;
using System.IO;
using System.Threading;


namespace GZipTest
{
    class Compressor : GZipProcessor
    {
        protected override void Read(FileStream originalFileStream, CommonData data)
        {
            int nBufferOrder = 0;
            byte[] buffer = new byte[nBufferSize];
            int numRead;
            while ((numRead = originalFileStream.Read(buffer, 0, nBufferSize)) != 0)
            {
                Buffer bd = new Buffer(nBufferOrder++, buffer, numRead);                                             
                data.dataQueue.Enqueue(bd);
            }
        }
        protected override void Process(CommonData data, Buffer bd)
        {
            using (MemoryStream memStream = new MemoryStream())
            {
                using (GZipStream gzipStream = new GZipStream(memStream, data.CompressionMode))
                {
                    gzipStream.Write(bd.Data, 0, bd.Length);
                }
                byte[] outBuffer = memStream.ToArray();
                Buffer outData = new Buffer(bd.Order, outBuffer, outBuffer.Length);

                data.processedDataQueue.Add(bd.Order, outData);                
            }
        }
        protected override void Write(FileStream compressedFileStream, Buffer bd)
        {
            byte[] abMagic = BitConverter.GetBytes(Magic);
            compressedFileStream.Write(abMagic, 0, abMagic.Length);

            byte[] abLength = BitConverter.GetBytes(bd.Data.Length);
            compressedFileStream.Write(abLength, 0, abLength.Length);

            compressedFileStream.Write(bd.Data, 0, bd.Data.Length);
        }
    }
}
