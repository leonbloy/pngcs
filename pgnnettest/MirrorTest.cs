namespace pgncstest
{
	
	using Ar.Com.Hjg.Pngcs;
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.IO;
	using System.Runtime.CompilerServices;
	
	/// <summary>
	/// To test all images in PNG test suite (except interlaced) doing a horizontal
	/// mirror on all them
	/// </summary>
	///
	public class MirrorTest {
		private static bool showInfo = false;

        public static void Reencode(String orig, String dest)
        {
			PngReader pngr = FileHelper.CreatePngReader(orig);
			if (showInfo)
				System.Console.Out.WriteLine(pngr.ToString());
			// at this point we have loaded al chucks before IDAT
			PngWriter pngw = FileHelper.CreatePngWriter(dest, pngr.imgInfo, true);
			pngw.CopyChunksFirst(pngr, Ar.Com.Hjg.Pngcs.Chunks.ChunksToWrite.COPY_ALL_SAFE | Ar.Com.Hjg.Pngcs.Chunks.ChunksToWrite.COPY_PALETTE
					| Ar.Com.Hjg.Pngcs.Chunks.ChunksToWrite.COPY_TRANSPARENCY);
			ImageLine lout = new ImageLine(pngw.imgInfo);
			int[] line = null;
			int cols = pngr.imgInfo.cols;
			int channels = pngr.imgInfo.channels;
			int aux;
			for (int row = 0; row < pngr.imgInfo.rows; row++) {
				ImageLine l1 = pngr.ReadRow(row);
				line = l1.Tf_unpack(line, false);
				for (int c1 = 0, c2 = cols - 1; c1 < c2; c1++, c2--) {
					for (int i = 0; i < channels; i++) {
						aux = line[c1 * channels + i];
						line[c1 * channels + i] = line[c2 * channels + i];
						line[c2 * channels + i] = aux;
					}
				}
				lout.Tf_pack(line, false);
				lout.SetRown(l1.GetRown());
				pngw.WriteRow(lout);
			}
			pngr.End();
			pngw.CopyChunksLast(pngr, Ar.Com.Hjg.Pngcs.Chunks.ChunksToWrite.COPY_ALL_SAFE
					| Ar.Com.Hjg.Pngcs.Chunks.ChunksToWrite.COPY_TRANSPARENCY);
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
                    Reencode(name, name2);
					if (name.StartsWith("x")) {
						System.Console.Error.WriteLine("this should have failed! " + name);
						conterr++;
					}
				} catch (Exception e) {
					if (name.StartsWith("x")) { // suppposed to fail
						System.Console.Out.WriteLine("ok error with " + name + " " + e.Message);
					} else { // real error
						System.Console.Error.WriteLine("error with " + name + " " + e.Message);
						conterr++;
					}
				}
			}

            System.Console.Out.WriteLine("Errors: " + conterr + "/" + cont);
		}
	
		public static void Test1() {
			// reencode("resources/testsuite1/basn0g01.png", "C:/temp/x.png");
			// reencode(new File("resources/testsuite1/basn0g02.png"), new
			// File("C:/temp/x2.png"));
            Reencode("C:/temp/pngsuite/ori/pp0n2c16.png", "C:/temp/x3.png");
		}
	
        /* notice: in the standard test suite, the files strarting with x are supposed to fail! */
		public static void Run() {
            TestAll("C:/temp/pngsuite/ori/", "C:/temp/pngsuite/pngcs/");	
			// test1();
		}
	}
}
