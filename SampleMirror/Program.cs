using System;
using System.Collections.Generic;
using System.Text;

namespace PngSampleMirror
{
    class Program
    {
        static void Main(string[] args)
        {

            if (args.Length < 2) {
                Console.Error.WriteLine("Sintax: PngSampleMirror [inputfile] [outputfile]");
                return;
            }
            String file1 = args[0];
            String file2 = args[1];
            SampleMirrorImage.mirror(file1, file2);
            Console.Out.WriteLine("done " + file1 + " -> " +file2);

        }
    }
}
