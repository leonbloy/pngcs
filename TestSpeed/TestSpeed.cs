using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ar.Com.Hjg.Pngcs;

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
		png.SetCompLevel(compLevel);
		ImageLine iline1 = new ImageLine(png.ImgInfo);
		ImageLine iline2 = new ImageLine(png.ImgInfo);
		ImageLine iline = iline1;
		for (int j = 0; j < cols; j++) {
			ImageLineHelper.SetPixelRGB8(iline1, j, ((j & 0xFF) << 16) | (((j * 3) & 0xFF) << 8) | (j * 2) & 0xFF);
			ImageLineHelper.SetPixelRGB8(iline2, j, (j * 13) & 0xFFFFFF);
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

    // con level=6: [5000 x 5000] 8081 8377(VA) 4680 4727 (A) read(x10) 10530
	public static void run(int cols, int rows)  {
		String fwrite="C:/temp/hugecs.png";
        String fread = fwrite; // "C:/temp/huge.png";
        Console.WriteLine("Please wait, this can take a minute or more...");
        int dt1 = createHuge(fwrite, cols, rows, FilterType.FILTER_VERYAGGRESSIVE, 6);
        Console.Write(".");
        int dt2 = createHuge(fwrite, cols, rows, FilterType.FILTER_VERYAGGRESSIVE, 6);
        Console.Write(".");
        int dt3 = createHuge(fwrite, cols, rows, FilterType.FILTER_AGGRESSIVE, 6);
        Console.Write(".");
        int dt4 = createHuge(fwrite, cols, rows, FilterType.FILTER_AGGRESSIVE, 6);
        Console.Write(".\n");
        int dtr = read(fread, 10);
		Console.Write(String.Format("write [{0} x {1}] {2} {3}(VA) {4} {5} (A) read(x10) {6} \n",cols,rows,dt1,dt2,dt3,dt4,dtr));
	}

    }
}
