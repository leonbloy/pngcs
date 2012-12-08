using System;
using System.Collections.Generic;
using System.Text;
using Hjg.Pngcs;

namespace TestSpeed
{
    class TestSpeed
    {
        	/**
	 * return msecs
	 */
	public static int createHuge(String filename, int cols, int rows,FilterType filtertype,int compLevel)  {
		ImageInfo iminfo = new ImageInfo(cols, rows, 8, false);
		PngWriter png = FileHelper.CreatePngWriter(filename,iminfo,true);
		png.SetFilterType(filtertype);
		png.CompLevel= compLevel;
        png.CompressionStrategy = PngWriter.ECompressionStrategy.Filtered;
		ImageLine iline1 = new ImageLine(png.ImgInfo);
		ImageLine iline2 = new ImageLine(png.ImgInfo);
		ImageLine iline = iline1;
		for (int j = 0; j < cols; j++) {
			//ImageLineHelper.SetPixel(iline1, j, (j & 0xFF) , ((j * 3) & 0xFF) ,  (j * 2) );
			ImageLineHelper.setPixelFromARGB8(iline2, j, (j * 13) & 0xFFFFFF);
		}
		long t0 = Environment.TickCount;
		for (int row = 0; row < rows; row++) {
			iline = row % 4 == 0 ? iline2 : iline1;
			png.WriteRow(iline, row);
		}
		png.End();
		int dt = (int) (Environment.TickCount - t0);
		return dt;
	}

	public static int read(String filename, int ntimes) {
		long t0 = Environment.TickCount;
		for(int i=0;i<ntimes;i++) {
		PngReader pngr = FileHelper.CreatePngReader(filename);
		for (int row = 0; row < pngr.ImgInfo.Rows; row++)
			pngr.ReadRow(row);
		pngr.End();
		}
		int dt = (int) (Environment.TickCount - t0);
		return dt;
	}

    // con level=6: 
        
//               8050 (VA) 4352 4462 (A) 4758 4665 (C) read(x10) 15147
    //     IONIC 9189 (VA) 5007 5694 (A) 7816 7660 (C) read(x10) 14851
    // (size: 207089)
    
	public static void run(int cols, int rows)  {
		String fwrite="C:/temp/hugecs.png";
        String fread = fwrite; // "C:/temp/huge.png";
        Console.WriteLine("Please wait, this can take a minute or more...");
        int dt1 = createHuge(fwrite, cols, rows, FilterType.FILTER_VERYAGGRESSIVE, 6);
        Console.Write(".");
        int dt3 = createHuge(fwrite, cols, rows, FilterType.FILTER_AGGRESSIVE, 6);
        Console.Write(".");
        int dt4 = createHuge(fwrite, cols, rows, FilterType.FILTER_AGGRESSIVE, 6);
        Console.Write(".\n");
        int dt5 = createHuge(fwrite, cols, rows, FilterType.FILTER_CYCLIC, 6);
        Console.Write(".");
        int dt6 = createHuge(fwrite, cols, rows, FilterType.FILTER_CYCLIC, 6);
        Console.Write(".\n");
        int dtr = read(fread, 10);
        Console.Write(String.Format("write [{0} x {1}] {2} (VA) {3} {4} (A) {5} {6} (C) read(x10) {7} \n", cols, rows, dt1, dt3, dt4, dt5, dt6, dtr));
	}

    }
}
