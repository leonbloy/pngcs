using System;
using System.Collections.Generic;
using System.Text;
using System.IO;


namespace pgncstest
{
    class TestCrcs
    {
        static void Main(string[] args)
        {
            Run();
        }
    
    public static void Run()
    {
        ICSharpCode.SharpZipLib.Checksums.Crc32 c = new ICSharpCode.SharpZipLib.Checksums.Crc32();
		byte[] b = new byte[]{0,0,3,4,256-124};
		c.Update(b);
        int l1 = (int)c.Value;
		Console.WriteLine(l1);
		c.Reset();
		c.Update(b);
		c.Update(b);
		int l2= (int)c.Value;
        Console.WriteLine(l2);

		
    }
    }

    
}
