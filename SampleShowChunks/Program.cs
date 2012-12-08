using System;
using System.Collections.Generic;
using System.Text;

namespace PngShowChunks
{
    class Program
    {
        static void Main(string[] args)
        {

            String file1 = args[0];
            Console.Out.WriteLine("processing " + file1);
            SampleShowChunks.showChunks(file1);
            Console.In.ReadLine();

        }
    }
}
