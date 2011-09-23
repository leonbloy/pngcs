using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
//using Ar.Com.Hjg.Pngcs;
using ICSharpCode.SharpZipLib.Checksums;

namespace pgncstest
{
    class TestCrcs
    {
	
    
    public static void Run()
    {
        Crc32 c = new Crc32();
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
