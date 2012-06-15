using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Ionic.Zlib;
/*
 * 
 * praga x 4
 * 
 *  deflate/default  
 *             2 23.83%  1373 msecs
 *             6 19.50%  4056 msecs
 *             9 18.63% 27160 msecs
 *             
 *  deflate/filtered
 *             2 23.83%  1404 msecs
 *             6 19.16%  4259 msecs
 *             9 18.25% 27035 msecs
 *  zlib
 *             2 23.83%  1451 msecs
 *             6 19.50%  4119 msecs
 *             9 18.63% 27347 msecs 
 * 
 * */

namespace TestDeflater2
{
    class TestDeflate
    {

      	public static void run() {
            int complevel = 2;
            CompressionStrategy strat = CompressionStrategy.Default;
            int t = TestDeflate.test2("C:/temp/pragad.bmp", complevel, strat);
            Console.Out.WriteLine(t + " msecs");
	    }

        internal static CompressionLevel GetCompressionLevel(int level) {
            switch(level) {
                case 0: return CompressionLevel.Level0;
                case 1: return CompressionLevel.Level1;
                case 2: return CompressionLevel.Level2;
                case 3: return CompressionLevel.Level3;
                case 4: return CompressionLevel.Level4;
                case 5: return CompressionLevel.Level5;
                case 6: return CompressionLevel.Level6;
                case 7: return CompressionLevel.Level7;
                case 8: return CompressionLevel.Level8;
                case 9: return CompressionLevel.Level9;
            }
            return CompressionLevel.Default;
        }

        public static int test2(String filename, int complevel, CompressionStrategy strat)
        {
            FileStream fs = File.OpenRead(filename);
            NullStream sink = new NullStream();
            DeflateStream fs2 = new DeflateStream(sink, CompressionMode.Compress, GetCompressionLevel(complevel), true);
            fs2.Strategy = strat;
            ZlibStream fs3 = new ZlibStream(sink, CompressionMode.Compress, GetCompressionLevel(complevel), true);
            //fs2.Strategy = strat;
            byte[] buf = new byte[16000];
            Stream os = fs2;
            long t0 = Environment.TickCount;
            int c=0;
            int total = 0;
            for(int k=0;k<4;k++){
            while( (c=fs.Read(buf,0,16000))>0) {
                os.Write(buf, 0, c);
                total += c;
            }
            fs.Seek(0,SeekOrigin.Begin);
            }
            os.Close();
            long t1 = Environment.TickCount;
            os.Close();
            Console.WriteLine(String.Format("{0:F2}%  {1} msecs", (sink.Length * 100.0) / total, t1 - t0));
            return (int)(t1 - t0);
        }

    }


}
