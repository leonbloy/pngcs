using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using ICSharpCode.SharpZipLib.Zip.Compression;

namespace TestDeflater
{
    class TestDeflate
    {

      	public static void run() {
            int complevel = 6; 
            int t = TestDeflate.test("C:/temp/testdeflatecs.bin", complevel, ICSharpCode.SharpZipLib.Zip.Compression.DeflateStrategy.Default);
            Console.Out.WriteLine(t + " msecs");
	    }

        public static int test(String filename, int complevel, DeflateStrategy strat)
        {
            bool varsize = false;
            int buflen = 96;
            int buflen2 = buflen / 2;
            byte[] buf1 = new byte[buflen];
            byte[] buf2 = new byte[buflen];
            byte[] buf3 = new byte[buflen];
            for (int i = 0; i < buflen; i++)
            {
                buf1[i] = i == buflen - 1 ? (byte)0x0a : (byte)(i + 32);
                buf2[i] = i == buflen - 1 ? (byte)0x0a : (byte)(((i * 7) % 96) + 32);
                buf3[i] = i == buflen - 1 ? (byte)0x0a : (byte)(((i * 31) % 96) + 32);
            }
            FileStream fs = File.Create(filename);
            Deflater defl = new Deflater(complevel);
            defl.SetStrategy(strat);
            DeflaterOutputStream fs2 = new DeflaterOutputStream(fs, defl);
            Stream os = fs2;
            int times = 500000;
            long t0 = Environment.TickCount;
            for (int t = 0; t < times; t++)
            {
                os.Write(buf1, 0, varsize ? (t % buflen2) + buflen2 : buflen * 2 / 3);
                os.Write(buf2, 0, varsize ? ((t + buflen2) % buflen2) + buflen2 : buflen * 2 / 3);
                os.Write(buf3, 0, varsize ? ((t + buflen) % buflen2) + buflen2 : buflen * 2 / 3);
            }
            long t1 = Environment.TickCount;
            os.Close();
            return (int)(t1 - t0);

        }

    }


}
