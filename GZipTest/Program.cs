using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Compression;
using System.IO;

namespace GZipTest
{  
    class Program
    {
        private const string compress = "compress";
        private const string decompress = "decompress";
        static void Main(string[] args)
        {
            if (args.Length != 3)
            {
                printUsage();
                return;
            }

            CompressionMode mode = CompressionMode.Compress;
            if (string.Compare(args[0], decompress, true) == 0)
            {
                mode = CompressionMode.Decompress;
            }
            else if (string.Compare(args[0], compress, true) != 0)
            {
                printUsage();
                return;
            }


            IGZipProcessor proc = null; 

            if (mode == CompressionMode.Compress)
            {
                proc = new Compressor();
            }
            else
            {
                proc = new Decompressor();
            }

            try
            {
                proc.Process(args[1], args[2], mode);
            }
            catch(Exception ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
            }
        }
        static void printUsage()
        {
            Console.WriteLine("Usage: GZipTest.exe <compress/decompress> <source file> <target file> ");
        }
        
    }
}
