using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Ar.Com.Hjg.Pngcs;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using ICSharpCode.SharpZipLib.Zip.Compression;

namespace pgncstest
{
    class TestDeflate
    {

        private static void makeTest(String filename)
        {
            byte[] buf = new byte[40];
            using (FileStream fs = File.Create(filename))
            {
                using (DeflaterOutputStream fs2 = new DeflaterOutputStream(fs,new Deflater(3)))
                {
                    for (int i = 0; i < 50; i++)
                    {
                        for (int j = 0; j < 40; j++)
                        {
                            buf[j] = (byte)(i + 0x30);
                        }
                        fs2.Write(buf, 0, buf.Length);
                    }
                }
            }

        }

        public static void Run()
        {
            makeTest("C:\\temp\\testdeflate.bin");
        }
    }


}
