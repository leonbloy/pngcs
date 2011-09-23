 namespace Ar.Com.Hjg.Pngcs {

    using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.IO;

     using System.Runtime.CompilerServices;
     using Chunks;
     using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
     using ICSharpCode.SharpZipLib.Zip.Compression;
	
	/// <summary>
     ///  Writes a PNG image, line by line.
	/// </summary>
	///
	public class PngWriter {
		public readonly ImageInfo imgInfo;
		private int compLevel ; // zip compression level 0 - 9
        private FilterWriteStrategy filterStrat;
		private int rowNum; // current line number
		// current line, one (packed) sample per element (layout differnt from rowb!)
		private int[] scanline;
		private short[] rowb; // element 0 is filter type!
        private short[] rowbprev; // rowb prev
		private byte[] rowbfilter; // current line with filter
		private readonly Stream os;
		private readonly String filename; // optional
		private PngIDatChunkOutputStream datStream;
		private DeflaterOutputStream datStreamDeflated;
		private ChunksToWrite chunks;
	
		public enum WriteStep {
			START, HEADER, HEADER_DONE, IDHR, IDHR_DONE, FIRST_CHUNKS, FIRST_CHUNKS_DONE, IDAT, IDAT_DONE, LAST_CHUNKS, LAST_CHUNKS_DONE, END
		}
	
		private PngWriter.WriteStep  step;
	
		public PngWriter(Stream outputStream, ImageInfo imgInfo_0) : this(outputStream, imgInfo_0, "[NO FILENAME AVAILABLE]") {
		}
	
		public PngWriter(Stream outputStream, ImageInfo imgInfo_0,
				String filenameOrDescription) {
			        this.compLevel = 6;
			this.rowNum = -1;
					this.scanline = null;
					this.rowb = null;
					this.rowbprev = null;
					this.rowbfilter = null;
			this.filename = (filenameOrDescription == null) ? "" : filenameOrDescription;
			this.os = outputStream;
			this.imgInfo = imgInfo_0;
			// prealloc
			scanline = new int[imgInfo_0.samplesPerRowP];
			rowb = new short[imgInfo_0.bytesPerRow + 1];
            rowbprev = new short[rowb.Length];
			rowbfilter = new byte[rowb.Length];
			datStream = new PngIDatChunkOutputStream(this.os);
            chunks = new ChunksToWrite(imgInfo_0);
			step = Ar.Com.Hjg.Pngcs.PngWriter.WriteStep.START;
            filterStrat = new FilterWriteStrategy(imgInfo, PngFilterType.FILTER_DEFAULT);
		}
	
		/// <summary>
		/// Write id signature and also "IHDR" chunk
		/// </summary>
		///
		private void WriteSignatureAndIHDR() {
            if (datStreamDeflated == null)
                datStreamDeflated = new DeflaterOutputStream(datStream, new Deflater(compLevel), 8192);
	
			Ar.Com.Hjg.Pngcs.PngHelper.WriteBytes(os, Ar.Com.Hjg.Pngcs.PngHelper.pngIdBytes); // signature
			step = Ar.Com.Hjg.Pngcs.PngWriter.WriteStep.IDHR;
			PngChunkIHDR ihdr = new PngChunkIHDR(imgInfo);
			// http://www.libpng.org/pub/png/spec/1.2/PNG-Chunks.html
			ihdr.cols = imgInfo.cols;
			ihdr.rows = imgInfo.rows;
			ihdr.bitspc = imgInfo.bitDepth;
			int colormodel = 0;
			if (imgInfo.alpha)
				colormodel += 0x04;
			if (imgInfo.indexed)
				colormodel += 0x01;
			if (!imgInfo.greyscale)
				colormodel += 0x02;
			ihdr.colormodel = colormodel;
			ihdr.compmeth = 0; // compression method 0=deflate
			ihdr.filmeth = 0; // filter method (0)
			ihdr.interlaced = 0; // never interlace
			ihdr.CreateChunk().WriteChunk(os);
			step = Ar.Com.Hjg.Pngcs.PngWriter.WriteStep.IDHR_DONE;
		}
	
		private void WriteFirstChunks() {
			step = Ar.Com.Hjg.Pngcs.PngWriter.WriteStep.FIRST_CHUNKS;
			PngChunkPLTE paletteChunk = null;
			/* foreach */
			// first pass: before palette (and saves palette chunk if found)
			foreach (PngChunk chunk  in  chunks.GetPending()) {
				if (chunk.beforePLTE)
					chunk.WriteAndMarkAsWrite(os);
				if (chunk  is  PngChunkPLTE)
					paletteChunk = (PngChunkPLTE) chunk;
			}
			// writes palette?
			if (paletteChunk != null) {
				if (imgInfo.greyscale)
					throw new PngjOutputException("cannot write palette for this format");
				paletteChunk.WriteAndMarkAsWrite(os);
			} else { // no palette
				if (imgInfo.indexed)
					throw new PngjOutputException("missing palette");
			}
			/* foreach */
			// second pass: after palette
			foreach (PngChunk chunk_0  in  chunks.GetPending()) {
				bool prio = chunk_0.GetWriteStatus() == 1;
				if (chunk_0.beforeIDAT || prio)
					chunk_0.WriteAndMarkAsWrite(os);
			}
			step = Ar.Com.Hjg.Pngcs.PngWriter.WriteStep.FIRST_CHUNKS_DONE;
		}
	
		private void WriteLastChunks() { // not including end
			step = Ar.Com.Hjg.Pngcs.PngWriter.WriteStep.LAST_CHUNKS;
			/* foreach */
			foreach (PngChunk chunk  in  chunks.GetPending()) {
				if (chunk.beforePLTE || chunk.beforeIDAT)
					throw new PngjOutputException("too late to write this chunk: " + chunk.id);
				chunk.WriteAndMarkAsWrite(os);
			}
			step = Ar.Com.Hjg.Pngcs.PngWriter.WriteStep.LAST_CHUNKS_DONE;
		}
	
		private void WriteEndChunk() {
			PngChunkIEND c = new PngChunkIEND(imgInfo);
			c.CreateChunk().WriteChunk(os);
			step = Ar.Com.Hjg.Pngcs.PngWriter.WriteStep.END;
		}
	
		private void WriteDataBeforeIDAT() {
			// notice that this if() are not exclusive
			if (step == Ar.Com.Hjg.Pngcs.PngWriter.WriteStep.START)
				WriteSignatureAndIHDR(); // now we are in IDHR_DONE
			if (step == Ar.Com.Hjg.Pngcs.PngWriter.WriteStep.IDHR_DONE)
				WriteFirstChunks(); // now we are in FIRST_CHUNKS_DONE
			if (step != Ar.Com.Hjg.Pngcs.PngWriter.WriteStep.FIRST_CHUNKS_DONE) // check
				throw new PngjOutputException("unexpected state before idat write: " + step);
		}
	
		/// <summary>
		/// Writes a full image row. This must be called sequentially from n=0 to
		/// n=rows-1 One integer per sample , in the natural order: R G B R G B ... (or
		/// R G B A R G B A... if has alpha) The values should be between 0 and 255 for
		/// 8 bitspc images, and between 0- 65535 form 16 bitspc images (this applies
		/// also to the alpha channel if present) The array can be reused.
		/// </summary>
		///
		/// <param name="newrow">Array of pixel values</param>
		/// <param name="n">Number of row, from 0 (top) to rows-1 (bottom)</param>
		public void WriteRow(int[] newrow, int n) {
			if (step != Ar.Com.Hjg.Pngcs.PngWriter.WriteStep.IDAT) {
				WriteDataBeforeIDAT();
				step = Ar.Com.Hjg.Pngcs.PngWriter.WriteStep.IDAT;
			}
			if (n < 0 || n > imgInfo.rows)
				throw new Exception("invalid value for row " + n);
			rowNum++;
			if (rowNum != n)
				throw new Exception("write order must be strict for rows " + n
						+ " (expected=" + rowNum + ")");
			scanline = newrow;
			// swap
            short[] tmp = rowb;
			rowb = rowbprev;
			rowbprev = tmp;
			ConvertRowToBytes();
			FilterRow(n);
			try {
				datStreamDeflated.Write(rowbfilter, 0, imgInfo.bytesPerRow + 1);
			} catch (IOException e) {
				throw new PngjOutputException(e);
			}
		}
	
		/// <summary>
		/// this uses the row number from the imageline!
		/// </summary>
		///
		public void WriteRow(ImageLine imgline) {
			WriteRow(imgline.scanline, imgline.GetRown());
		}
	
		/// <summary>
		/// Finalizes the image creation and closes the file stream. This MUST be
		/// called after writing the lines.
		/// </summary>
		///
		public void End() {
			if (rowNum != imgInfo.rows - 1)
				throw new PngjOutputException("all rows have not been written");
			try {
                datStreamDeflated.Finish();
                datStream.Flush();
                step = Ar.Com.Hjg.Pngcs.PngWriter.WriteStep.IDAT_DONE;
				WriteLastChunks();
				WriteEndChunk();
				os.Close();
			} catch (IOException e) {
				throw new PngjOutputException(e);
			}
		}

        private void FilterRow(int rown)
        {
            // warning: filters operation rely on: "previos row" (rowbprev) is
            // initialized to 0 the first time
            if (filterStrat.shouldTestAll(rown))
            {
                FilterRowNone();
                filterStrat.fillResultsForFilter(rown, PngFilterType.FILTER_NONE, SumRowbfilter());
                FilterRowSub();
                filterStrat.fillResultsForFilter(rown, PngFilterType.FILTER_SUB, SumRowbfilter());
                FilterRowUp();
                filterStrat.fillResultsForFilter(rown, PngFilterType.FILTER_UP, SumRowbfilter());
                FilterRowAverage();
                filterStrat.fillResultsForFilter(rown, PngFilterType.FILTER_AVERAGE,
                    SumRowbfilter());
                FilterRowPaeth();
                filterStrat.fillResultsForFilter(rown, PngFilterType.FILTER_PAETH, SumRowbfilter());
            }
            PngFilterType filterType = filterStrat.gimmeFilterType(rown);
            rowbfilter[0] = (byte)(int)filterType;
			switch (filterType) {
			case Ar.Com.Hjg.Pngcs.PngFilterType.FILTER_NONE:
				FilterRowNone();
				break;
			case Ar.Com.Hjg.Pngcs.PngFilterType.FILTER_SUB:
				FilterRowSub();
				break;
			case Ar.Com.Hjg.Pngcs.PngFilterType.FILTER_UP:
				FilterRowUp();
				break;
			case Ar.Com.Hjg.Pngcs.PngFilterType.FILTER_AVERAGE:
				FilterRowAverage();
				break;
			case Ar.Com.Hjg.Pngcs.PngFilterType.FILTER_PAETH:
				FilterRowPaeth();
				break;
			default:
				throw new PngjOutputException("Filter type " + filterType + " not implemented");
			}
		}

        private long SumRowbfilter()   { // sums absolute value 
            long s = 0;
            for (int i = 1; i <= imgInfo.bytesPerRow; i++)
                if (rowbfilter[i] < 0)
                    s -= (long)rowbfilter[i];
                else
                    s += (long)rowbfilter[i];
            return s;
        }

		private void FilterRowNone() {
			for (int i = 1; i <= imgInfo.bytesPerRow; i++) {
				rowbfilter[i] = (byte) rowb[i];
			}
		}
	
		private void FilterRowSub() {
			int i, j;
			for (i = 1; i <= imgInfo.bytesPixel; i++) {
				rowbfilter[i] = (byte) rowb[i];
			}
			for (j = 1, i = imgInfo.bytesPixel + 1; i <= imgInfo.bytesPerRow; i++, j++) {
				rowbfilter[i] = (byte) (rowb[i] - rowb[j]);
			}
		}
	
		private void FilterRowUp() {
			for (int i = 1; i <= imgInfo.bytesPerRow; i++) {
				rowbfilter[i] = (byte) (rowb[i] - rowbprev[i]);
			}
		}
	
		private void FilterRowAverage() {
			int i, j, x;
			for (i = 1; i <= imgInfo.bytesPerRow; i++) {
				if (rowb[i] < 0 || rowb[i] > 255)
					throw new PngjOutputException("??" + rowb[i]);
				if (rowbprev[i] < 0 || rowbprev[i] > 255)
					throw new PngjOutputException("??" + rowbprev[i]);
			}
			for (j = 1 - imgInfo.bytesPixel, i = 1; i <= imgInfo.bytesPerRow; i++, j++) {
				x = (j > 0) ? rowb[j] : 0;
				rowbfilter[i] = (byte) (rowb[i] - (rowbprev[i] + x) / 2);
			}
		}
	
		private void FilterRowPaeth() {
			int i, j, x, y;
			for (i = 1; i <= imgInfo.bytesPerRow; i++) {
				if (rowb[i] < 0 || rowb[i] > 255)
					throw new PngjOutputException("??" + rowb[i] + " i=" + i + " row=" + rowNum);
				if (rowbprev[i] < 0 || rowbprev[i] > 255)
					throw new PngjOutputException("??" + rowbprev[i]);
			}
			for (j = 1 - imgInfo.bytesPixel, i = 1; i <= imgInfo.bytesPerRow; i++, j++) {
				x = (j > 0) ? rowb[j] : 0;
				y = (j > 0) ? rowbprev[j] : 0;
				rowbfilter[i] = (byte) (rowb[i] - Ar.Com.Hjg.Pngcs.PngHelper.FilterPaethPredictor(x, rowbprev[i], y));
			}
		}
	
		private void ConvertRowToBytes() {
			// http://www.libpng.org/pub/png/spec/1.2/PNG-DataRep.html
			int i, j, x;
			if (imgInfo.bitDepth <= 8) {
				for (i = 0, j = 1; i < imgInfo.samplesPerRowP; i++) {
                    rowb[j++] = (short)(((int)scanline[i]) & 0xFF);
				}
			} else { // 16 bitspc
				for (i = 0, j = 1; i < imgInfo.samplesPerRowP; i++) {
					x = (int) (scanline[i]) & 0xFFFF;
                    rowb[j++] = (short)((x & 0xFF00) >> 8);
                    rowb[j++] = (short)(x & 0xFF);
				}
			}
		}
	
		// /// several getters / setters - all this setters are optional
		// ////////////
		// see also the txtInfo property ////////////////////////////////////////
		/// <summary>
		/// Set physical resolution, in DPI (dots per inch) optional, only informative
		/// </summary>
		///
		public void SetDpi(double dpi) {
			chunks.SetPHYSdpi(dpi);
		}
	
		/// <summary>
		/// Sets internal prediction filter type, or strategy to choose it.
	 ///
	 /// This must be called just after constructor, before starting writing.
	 ///
	 /// See also setCompLevel()
	 ///
	 /// @param filterType One of the five prediction types or strategy to choose it
        /// (see <code>PngFilterType</code>) Recommended values: DEFAULT (default) or AGGRESIV
        /// </summary>
        public void SetFilterType(PngFilterType filterType)
        {
            filterStrat = new FilterWriteStrategy(imgInfo, filterType);
        }
	
		
		/// <summary>
		/// compression level: between 0 and 9 (default:6)
		/// </summary>
		///
		public void SetCompLevel(int compLevel_0) {
			if (compLevel_0 < 0 || compLevel_0 > 9)
				throw new PngjException("Compression level invalid (" + compLevel_0
						+ ") Must be 0..9");
			this.compLevel = compLevel_0;
		}
	
		public int GetCols() {
			return imgInfo.cols;
		}
	
		public int GetRows() {
			return imgInfo.rows;
		}
	
		public String GetFilename() {
			return filename;
		}
	
		/// <summary>
		/// copy chunks from reader - copy_mask : see ChunksToWrite.COPY_XXX
		/// If we are after idat, only considers those chunks after IDAT in PngReader
		/// TODO: this should be more customizable
		/// </summary>
		///
		private void CopyChunks(PngReader reader, int copy_mask, bool onlyAfterIdat) {
			bool idatDone = step.CompareTo(Ar.Com.Hjg.Pngcs.PngWriter.WriteStep.IDAT) >= 0;
			int posidat = reader.chunks.PositionIDAT();
			if (onlyAfterIdat && posidat < 0)
				return; // nothing to do
			IList<PngChunk> chunksR = reader.chunks.GetChunks();
			for (int i = 0; i < chunksR.Count; i++) {
				if (i < posidat && onlyAfterIdat)
					continue;
				bool copy = false;
				PngChunk chunk = chunksR[i];
				if (chunk.crit) {
					if (chunk.id.Equals(Ar.Com.Hjg.Pngcs.Chunks.ChunkHelper.PLTE_TEXT)) {
						if (imgInfo.indexed
								&& Ar.Com.Hjg.Pngcs.Chunks.ChunksToWrite.MaskMatch(copy_mask, Ar.Com.Hjg.Pngcs.Chunks.ChunksToWrite.COPY_PALETTE))
							copy = true;
						if (!imgInfo.greyscale
								&& Ar.Com.Hjg.Pngcs.Chunks.ChunksToWrite.MaskMatch(copy_mask, Ar.Com.Hjg.Pngcs.Chunks.ChunksToWrite.COPY_ALL))
							copy = true;
					}
				} else { // ancillary
					bool text = (chunk  is  PngChunkTextVar);
					bool safe = chunk.safe;
					if (Ar.Com.Hjg.Pngcs.Chunks.ChunksToWrite.MaskMatch(copy_mask, Ar.Com.Hjg.Pngcs.Chunks.ChunksToWrite.COPY_ALL))
						copy = true;
					if (safe && Ar.Com.Hjg.Pngcs.Chunks.ChunksToWrite.MaskMatch(copy_mask, Ar.Com.Hjg.Pngcs.Chunks.ChunksToWrite.COPY_ALL_SAFE))
						copy = true;
					if (chunk.id.Equals(Ar.Com.Hjg.Pngcs.Chunks.ChunkHelper.tRNS_TEXT)
							&& Ar.Com.Hjg.Pngcs.Chunks.ChunksToWrite.MaskMatch(copy_mask, Ar.Com.Hjg.Pngcs.Chunks.ChunksToWrite.COPY_TRANSPARENCY))
						copy = true;
					if (chunk.id.Equals(Ar.Com.Hjg.Pngcs.Chunks.ChunkHelper.pHYs_TEXT)
							&& Ar.Com.Hjg.Pngcs.Chunks.ChunksToWrite.MaskMatch(copy_mask, Ar.Com.Hjg.Pngcs.Chunks.ChunksToWrite.COPY_PHYS))
						copy = true;
					if (text && Ar.Com.Hjg.Pngcs.Chunks.ChunksToWrite.MaskMatch(copy_mask, Ar.Com.Hjg.Pngcs.Chunks.ChunksToWrite.COPY_TEXTUAL))
						copy = true;
				}
				if (copy) {
					if (chunk.beforeIDAT && idatDone) {
						System.Console.Error.WriteLine("too late to add pre-idat chunk - " + chunk);
						continue;
					}
					chunks.CloneAndAdd(chunk, false);
				}
			}
		}
	
		public void CopyChunksFirst(PngReader reader, int copy_mask) {
			CopyChunks(reader, copy_mask, false);
		}
	
		public void CopyChunksLast(PngReader reader, int copy_mask) {
			CopyChunks(reader, copy_mask, true);
		}
	}
}
