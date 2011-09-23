namespace pgncstest {
	
	using Ar.Com.Hjg.Pngcs;
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.IO;
	using System.Runtime.CompilerServices;
	
	/// <summary>
	/// reencodes a png image with a given filter and compression level
	/// </summary>
	///
	public class PngReencode {
		public static void Reencode(String orig, String dest, PngFilterType filterType,
				int cLevel) {
			PngReader pngr = FileHelper.CreatePngReader(orig);
            PngWriter pngw = FileHelper.CreatePngWriter(dest, pngr.imgInfo, true);
			System.Console.Out.WriteLine(pngr.ToString());
			pngw.SetFilterType(filterType);
			pngw.SetCompLevel(cLevel);
			pngw.CopyChunksFirst(pngr, Ar.Com.Hjg.Pngcs.Chunks.ChunksToWrite.COPY_ALL);
			System.Console.Out.WriteLine(String.Format("Creating Image {0}  filter={1} compLevel={2}", 
                pngw.GetFilename(),	filterType.ToString(), cLevel));
			for (int row = 0; row < pngr.imgInfo.rows; row++) {
				ImageLine l1 = pngr.ReadRow(row);
				// pngw.writeRow(l1.vals, row);
				pngw.WriteRow(l1);
			}
			pngr.End();
			pngw.CopyChunksLast(pngr, Ar.Com.Hjg.Pngcs.Chunks.ChunksToWrite.COPY_ALL);
			pngw.End();
			System.Console.Out.WriteLine("Done");
		}
	
		public static void Run() {
            String file = MainProgram.getTestDir() + "\\orig.png";
            String file2 = MainProgram.getTestDir() + "\\reencode.png";
            //Reencode(file, file2, PngFilterType.FILTER_AGRESSIVE, 6);
            Reencode(file, file2, PngFilterType.FILTER_PAETH, 6);
        }
	}
}
