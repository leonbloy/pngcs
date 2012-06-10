using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PngShowChunks
{
    class Program
    {
        static void Main(string[] args)
        {

            String file1 = args[0];
            SampleShowChunks.showChunks(file1);
            Console.Out.WriteLine("processing " + file1);

        }
    }
}
