using System;
using System.Collections.Generic;
using System.Text;

namespace SampleCreateOrangeGradient {
    class Program {
        static void Main(string[] args) {
            String file = "C:/temp/orangegradient.png";
            CreateOrange.Create(file, 500, 200);
            Console.WriteLine("Done : " + file);
        }
    }
}
