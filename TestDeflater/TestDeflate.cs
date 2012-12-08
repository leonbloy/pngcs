using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using ICSharpCode.SharpZipLib.Zip.Compression;
/**
 *  deflater/default
 *             2 23.83%   655 msecs
 *             6 19.46%  2199 msecs
 *             9 18.63% 12917 msecs
 *  deflater/filtered
 *             2 23.83%   655 msecs
 *             6 19.10%  2309 msecs
 *             9 18.23% 12901 msecs
 * */
namespace TestDeflater
{
    class TestDeflate
    {

      	public static void run() {
            int complevel = 9; 
            DeflateStrategy strat= DeflateStrategy.Default;
            int t = TestDeflate.test2("C:/temp/pragad.bmp", complevel, strat);
            Console.Out.WriteLine(t + " msecs");
            Console.In.ReadLine();

	    }


        public static int test2(String filename, int complevel, DeflateStrategy strat)
        {
            FileStream fs = File.OpenRead(filename);
            NullStream sink = new NullStream();
            Deflater defl = new Deflater(complevel);
            defl.SetStrategy(strat);
            DeflaterOutputStream fs2 = new DeflaterOutputStream(sink, defl);     //fs2.Strategy = strat;
            byte[] buf = new byte[16000];
            Stream os = fs2;
            long t0 = Environment.TickCount;
            int c = 0;
            int total = 0;
            for (int k = 0; k < 4; k++)
            {
                while ((c = fs.Read(buf, 0, 16000)) > 0)
                {
                    os.Write(buf, 0, c);
                    total += c;
                }
                fs.Seek(0, SeekOrigin.Begin);
            }
            os.Close();
            long t1 = Environment.TickCount;
            os.Close();
            Console.WriteLine(String.Format("{0:F2}%  {1} msecs", (sink.Length * 100.0) / total, t1 - t0));
            return (int)(t1 - t0);
        }


    }


}
