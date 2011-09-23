 namespace Ar.Com.Hjg.Pngcs {
	
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.IO;
	using System.Text;
	
	/// <summary>
	/// some utility static methods. see also FileHelper (if not sandboxed)
	/// </summary>
	///
     public class FileHelper
     {

         public static Stream OpenFileForReading(String file)
         {
             Stream isx = null;
             if (file == null || !File.Exists(file))
                 throw new PngjInputException("Cannot open file for reading (" + file + ")");
             isx = new FileStream(file, FileMode.Open);
             return isx;
         }

         public static Stream OpenFileForWriting(String file, bool allowOverwrite)
         {
             Stream osx = null;
             if (File.Exists(file) && !allowOverwrite)
                 throw new PngjOutputException("File already exists (" + file + ") and overwrite=false");
             osx = new FileStream(file, FileMode.Create);
             return osx;
         }


         public static PngWriter CreatePngWriter(String file, ImageInfo imgInfo, bool allowOverwrite)
         {
             return new PngWriter(OpenFileForWriting(file, allowOverwrite), imgInfo,
                     file);
         }

         public static PngReader CreatePngReader(String file)
         {
             return new PngReader(OpenFileForReading(file), file);
         }
     }
 }
