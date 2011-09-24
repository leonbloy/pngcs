using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace pgncstest
{
    class MainProgram
    {
        public static String getTestDir() {
            return "C:\\temp";
        }

        static void Main(string[] args)
        {
            int t0 = Environment.TickCount;
            //TestWrite1.Run();
           //TestCrcs.Run();
           // TestDeflate.Run();
           // PngReencode.Run();
            MirrorTest.Run();
            int t1 = Environment.TickCount;
            Console.WriteLine("{0} msecs - press key to end",t1-t0);
            Console.ReadKey();
        }
    }
}
