namespace TestPngSuite
{
	
	using Ar.Com.Hjg.Pngcs;
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.IO;
	using System.Runtime.CompilerServices;
    using Ar.Com.Hjg.Pngcs.Chunks;
	
	/// <summary>
	/// To test all images in PNG test suite (except interlaced) doing a horizontal
	/// mirror on all them
	/// </summary>
	///
    public class TestPngSuite
    {
		
        public static void mirror(String orig, String dest)
        {
            if (orig.Equals(dest)) throw new PngjException("input and output file cannot coincide");
            PngReader pngr = FileHelper.CreatePngReader(orig);
            PngWriter pngw = FileHelper.CreatePngWriter(dest, pngr.ImgInfo, true);
            pngw.SetFilterType(FilterType.FILTER_ALTERNATE); // just to test all filters
            int copyPolicy = ChunkCopyBehaviour.COPY_ALL;
            pngw.CopyChunksFirst(pngr, copyPolicy);
            ImageLine lout = new ImageLine(pngw.ImgInfo);
            int[] line = null;
            int cols = pngr.ImgInfo.Cols;
            int channels = pngr.ImgInfo.Channels;
            int aux;
            for (int row = 0; row < pngr.ImgInfo.Rows; row++)
            {
                ImageLine l1 = pngr.ReadRow(row);
                line = l1.Tf_unpack(line, false);
                for (int c1 = 0, c2 = cols - 1; c1 < c2; c1++, c2--)
                {
                    for (int i = 0; i < channels; i++)
                    {
                        aux = line[c1 * channels + i];
                        line[c1 * channels + i] = line[c2 * channels + i];
                        line[c2 * channels + i] = aux;
                    }
                }
                lout.Tf_pack(line, false);
                pngw.WriteRow(lout, row);
            }
            pngw.CopyChunksLast(pngr, copyPolicy);
            pngr.End();
            pngw.End();
        }
	
	

		public static void TestAll(String dirsrc, String dirdest) {
			int cont = 0;
			int conterr = 0;
			/* foreach file in the suite */
            foreach (String name in System.IO.Directory.EnumerateFiles(dirsrc))
            {
                FileInfo fi = new FileInfo(name);
				if (!name.EndsWith(".png"))
					continue;
				try {
					cont++;
                    String name2 = dirdest + fi.Name.Replace(".png", "z.png");
                    mirror(name, name2);
                    if (fi.Name.StartsWith("x"))
                    {
						System.Console.Error.WriteLine("this should have failed! " + name);
						conterr++;
					}
				} catch (Exception e) {
                    if (fi.Name.StartsWith("x"))
                    { // suppposed to fail
						System.Console.Out.WriteLine("ok error with " + name + " " + e.Message);
					} else { // real error
						System.Console.Error.WriteLine("error with " + name + " " + e.Message);
						conterr++;
					}
				}
			}

            System.Console.Out.WriteLine("Errors: " + conterr + "/" + cont);
		}
	
	}
}
