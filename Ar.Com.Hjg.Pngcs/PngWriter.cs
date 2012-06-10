namespace Ar.Com.Hjg.Pngcs
{

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
    public class PngWriter
    {
        public readonly ImageInfo ImgInfo;
        private readonly String filename; // optional, can be a description
        private int rowNum = -1; // current line number
        private readonly ChunksListForWrite chunkList;
        public ChunksListForWrite ChunkList { get { return chunkList; } }
        private readonly PngMetadata metadata; // high level wrapper over chunkList
        public PngMetadata Metadata { get {return metadata;}}
        
        protected int currentChunkGroup = -1;

        private FilterWriteStrategy filterStrat;
        private int compLevel = 6; // zip compression level 0 - 9
        private bool shouldCloseStream = true; // true: closes stream after ending write

        private readonly Stream os;

        private PngIDatChunkOutputStream datStream;
        private DeflaterOutputStream datStreamDeflated;
        /**
         * Deflate algortithm compression strategy
         */
        private DeflateStrategy deflaterStrategy = DeflateStrategy.Filtered;

        private int[] histox = new int[256]; // auxiliar buffer, only used by reportResultsForFilter

        private int idatMaxSize = 0; // 0=use default (PngIDatChunkOutputStream 32768)

        // current line, one (packed) sample per element (layout differnt from rowb!)
        private int[] scanline;
        private byte[] rowb; // element 0 is filter type! // !!! byte?
        private byte[] rowbprev; // rowb prev
        private byte[] rowbfilter; // current line with filter


        public PngWriter(Stream outputStream, ImageInfo imgInfo_0)
            : this(outputStream, imgInfo_0, "[NO FILENAME AVAILABLE]")
        {
        }

        public PngWriter(Stream outputStream, ImageInfo imgInfo_0,
                String filenameOrDescription)
        {
            this.compLevel = 6;
            this.rowNum = -1;
            this.scanline = null;
            this.rowb = null;
            this.rowbprev = null;
            this.rowbfilter = null;
            this.filename = (filenameOrDescription == null) ? "" : filenameOrDescription;
            this.os = outputStream;
            this.ImgInfo = imgInfo_0;
            // prealloc
            scanline = new int[imgInfo_0.SamplesPerRowP];
            rowb = new byte[imgInfo_0.BytesPerRow + 1];
            rowbprev = new byte[rowb.Length];
            rowbfilter = new byte[rowb.Length];
            datStream = new PngIDatChunkOutputStream(this.os);
            chunkList = new ChunksListForWrite(ImgInfo);
            metadata = new PngMetadata(chunkList);
            filterStrat = new FilterWriteStrategy(ImgInfo, FilterType.FILTER_DEFAULT);
        }

        private void init()
        {
            datStream = new PngIDatChunkOutputStream(this.os, idatMaxSize);
            WriteSignatureAndIHDR();
            WriteFirstChunks();
        }

        private void reportResultsForFilter(int rown, FilterType type, bool tentative)
        {
            for (int i = 0; i < histox.Length; i++)
                histox[i] = 0;
            int s = 0, v;
            for (int i = 1; i <= ImgInfo.BytesPerRow; i++)
            {
                v = rowbfilter[i];
                if (v < 0)
                    s -= (int)v;
                else
                    s += (int)v;
                histox[v & 0xFF]++;
            }
            filterStrat.fillResultsForFilter(rown, type, s, histox, tentative);
        }

        private void WriteEndChunk()
        {
            PngChunkIEND c = new PngChunkIEND(ImgInfo);
            c.CreateRawChunk().WriteChunk(os);
        }

        private void WriteFirstChunks()
        {
            int nw = 0;
            currentChunkGroup = ChunksList.CHUNK_GROUP_1_AFTERIDHR;
            nw = chunkList.writeChunks(os, currentChunkGroup);
            currentChunkGroup = ChunksList.CHUNK_GROUP_2_PLTE;
            nw = chunkList.writeChunks(os, currentChunkGroup);
            if (nw > 0 && ImgInfo.Greyscale)
                throw new PngjOutputException("cannot write palette for this format");
            if (nw == 0 && ImgInfo.Indexed)
                throw new PngjOutputException("missing palette");
            currentChunkGroup = ChunksList.CHUNK_GROUP_3_AFTERPLTE;
            nw = chunkList.writeChunks(os, currentChunkGroup);
            currentChunkGroup = ChunksList.CHUNK_GROUP_4_IDAT;
        }

        private void WriteLastChunks()
        { // not including end
            currentChunkGroup = ChunksList.CHUNK_GROUP_5_AFTERIDAT;
            chunkList.writeChunks(os, currentChunkGroup);
            // should not be unwriten chunks
            List<PngChunk> pending = chunkList.GetQueuedChunks();
            if (pending.Count > 0)
                throw new PngjOutputException(pending.Count + " chunks were not written! Eg: " + pending[0].ToString());
            currentChunkGroup = ChunksList.CHUNK_GROUP_6_END;
        }

        /// <summary>
        /// Write id signature and also "IHDR" chunk
        /// </summary>
        ///
        private void WriteSignatureAndIHDR()
        {
            currentChunkGroup = ChunksList.CHUNK_GROUP_0_IDHR;
            if (datStreamDeflated == null)
            {
                Deflater defl = new Deflater(compLevel);
                defl.SetStrategy(deflaterStrategy);
                datStreamDeflated = new DeflaterOutputStream(datStream, defl, 8192);
            }
            PngHelperInternal.WriteBytes(os, Ar.Com.Hjg.Pngcs.PngHelperInternal.pngIdBytes); // signature
            PngChunkIHDR ihdr = new PngChunkIHDR(ImgInfo);
            // http://www.libpng.org/pub/png/spec/1.2/PNG-Chunks.html
            ihdr.cols = ImgInfo.Cols;
            ihdr.rows = ImgInfo.Rows;
            ihdr.bitspc = ImgInfo.BitDepth;
            int colormodel = 0;
            if (ImgInfo.Alpha)
                colormodel += 0x04;
            if (ImgInfo.Indexed)
                colormodel += 0x01;
            if (!ImgInfo.Greyscale)
                colormodel += 0x02;
            ihdr.colormodel = colormodel;
            ihdr.compmeth = 0; // compression method 0=deflate
            ihdr.filmeth = 0; // filter method (0)
            ihdr.interlaced = 0; // never interlace
            ihdr.CreateRawChunk().WriteChunk(os);
        }

        private void ConvertRowToBytes()
        {
            // http://www.libpng.org/pub/png/spec/1.2/PNG-DataRep.html
            int i, j, x;
            if (ImgInfo.BitDepth <= 8)
            { // !!! optimizar con foreach
                for (i = 0, j = 1; i < ImgInfo.SamplesPerRowP; i++)
                {
                    rowb[j++] = (byte)(((int)scanline[i]) & 0xFF);
                }
            }
            else
            { // 16 bitspc
                for (i = 0, j = 1; i < ImgInfo.SamplesPerRowP; i++)
                {
                    x = (int)(scanline[i]) & 0xFFFF;
                    rowb[j++] = (byte)((x & 0xFF00) >> 8);
                    rowb[j++] = (byte)(x & 0xFF);
                }
            }
        }


        private void FilterRow(int rown)
        {
            // warning: filters operation rely on: "previos row" (rowbprev) is
            // initialized to 0 the first time
            if (filterStrat.shouldTestAll(rown))
            {
                FilterRowNone();
                reportResultsForFilter(rown, FilterType.FILTER_NONE, true);
                FilterRowSub();
                reportResultsForFilter(rown, FilterType.FILTER_SUB, true);
                FilterRowUp();
                reportResultsForFilter(rown, FilterType.FILTER_UP, true);
                FilterRowAverage();
                reportResultsForFilter(rown, FilterType.FILTER_AVERAGE, true);
                FilterRowPaeth();
                reportResultsForFilter(rown, FilterType.FILTER_PAETH, true);
            }
            FilterType filterType = filterStrat.gimmeFilterType(rown, true);
            rowbfilter[0] = (byte)(int)filterType;
            switch (filterType)
            {
                case Ar.Com.Hjg.Pngcs.FilterType.FILTER_NONE:
                    FilterRowNone();
                    break;
                case Ar.Com.Hjg.Pngcs.FilterType.FILTER_SUB:
                    FilterRowSub();
                    break;
                case Ar.Com.Hjg.Pngcs.FilterType.FILTER_UP:
                    FilterRowUp();
                    break;
                case Ar.Com.Hjg.Pngcs.FilterType.FILTER_AVERAGE:
                    FilterRowAverage();
                    break;
                case Ar.Com.Hjg.Pngcs.FilterType.FILTER_PAETH:
                    FilterRowPaeth();
                    break;
                default:
                    throw new PngjOutputException("Filter type " + filterType + " not implemented");
            }
            reportResultsForFilter(rown, filterType, false);
        }


        private void FilterRowAverage()
        {
            int i, j, imax;
            imax = ImgInfo.BytesPerRow;
            for (j = 1 - ImgInfo.BytesPixel, i = 1; i <= imax; i++, j++)
            {
                rowbfilter[i] = (byte)(rowb[i] - ((rowbprev[i]) + (j > 0 ? rowb[j] : 0)) / 2);
            }
        }

        private void FilterRowNone()
        {
            for (int i = 1; i <= ImgInfo.BytesPerRow; i++)
            {
                rowbfilter[i] = (byte)rowb[i];
            }
        }


        private void FilterRowPaeth()
        {
            int i, j, imax;
            imax = ImgInfo.BytesPerRow;
            for (j = 1 - ImgInfo.BytesPixel, i = 1; i <= imax; i++, j++)
            {
                rowbfilter[i] = (byte)(rowb[i] - PngHelperInternal.FilterPaethPredictor(j > 0 ? rowb[j] : 0,
                        rowbprev[i], j > 0 ? rowbprev[j] : 0));
            }
        }

        private void FilterRowSub()
        {
            int i, j;
            for (i = 1; i <= ImgInfo.BytesPixel; i++)
            {
                rowbfilter[i] = (byte)rowb[i];
            }
            for (j = 1, i = ImgInfo.BytesPixel + 1; i <= ImgInfo.BytesPerRow; i++, j++)
            {
                rowbfilter[i] = (byte)(rowb[i] - rowb[j]);
            }
        }

        private void FilterRowUp()
        {
            for (int i = 1; i <= ImgInfo.BytesPerRow; i++)
            {
                rowbfilter[i] = (byte)(rowb[i] - rowbprev[i]);
            }
        }


        private long SumRowbfilter()
        { // sums absolute value 
            long s = 0;
            for (int i = 1; i <= ImgInfo.BytesPerRow; i++)
                if (rowbfilter[i] < 0)
                    s -= (long)rowbfilter[i];
                else
                    s += (long)rowbfilter[i];
            return s;
        }

        /// <summary>
        /// copy chunks from reader - copy_mask : see ChunksToWrite.COPY_XXX
        /// If we are after idat, only considers those chunks after IDAT in PngReader
        /// TODO: this should be more customizable
        /// </summary>
        ///
        private void CopyChunks(PngReader reader, int copy_mask, bool onlyAfterIdat)
        {
            bool idatDone = currentChunkGroup >= ChunksList.CHUNK_GROUP_4_IDAT;
            if (onlyAfterIdat && reader.CurrentChunkGroup < ChunksList.CHUNK_GROUP_6_END) throw new PngjException("tried to copy last chunks but reader has not ended");
            foreach (PngChunk chunk in reader.ChunksList.GetChunks())
            {
                int group = chunk.ChunkGroup;
                if (group < ChunksList.CHUNK_GROUP_4_IDAT && idatDone)
                    continue;
                bool copy = false;
                if (chunk.Crit)
                {
                    if (chunk.Id.Equals(ChunkHelper.PLTE))
                    {
                        if (ImgInfo.Indexed && ChunkHelper.maskMatch(copy_mask, ChunkCopyBehaviour.COPY_PALETTE))
                            copy = true;
                        if (!ImgInfo.Greyscale && ChunkHelper.maskMatch(copy_mask, ChunkCopyBehaviour.COPY_ALL))
                            copy = true;
                    }
                }
                else
                { // ancillary
                    bool text = (chunk is PngChunkTextVar);
                    bool safe = chunk.Safe;
                    // notice that these if are not exclusive
                    if (ChunkHelper.maskMatch(copy_mask, ChunkCopyBehaviour.COPY_ALL))
                        copy = true;
                    if (safe && ChunkHelper.maskMatch(copy_mask, ChunkCopyBehaviour.COPY_ALL_SAFE))
                        copy = true;
                    if (chunk.Id.Equals(ChunkHelper.tRNS)
                            && ChunkHelper.maskMatch(copy_mask, ChunkCopyBehaviour.COPY_TRANSPARENCY))
                        copy = true;
                    if (chunk.Id.Equals(ChunkHelper.pHYs) && ChunkHelper.maskMatch(copy_mask, ChunkCopyBehaviour.COPY_PHYS))
                        copy = true;
                    if (text && ChunkHelper.maskMatch(copy_mask, ChunkCopyBehaviour.COPY_TEXTUAL))
                        copy = true;
                    if (ChunkHelper.maskMatch(copy_mask, ChunkCopyBehaviour.COPY_ALMOSTALL)
                            && !(ChunkHelper.IsUnknown(chunk) || text || chunk.Id.Equals(ChunkHelper.hIST) || chunk.Id
                                    .Equals(ChunkHelper.tIME)))
                        copy = true;
                }
                if (copy)
                {
                    chunkList.queue(PngChunk.CloneChunk(chunk, ImgInfo));
                }
            }
        }

        public void CopyChunksFirst(PngReader reader, int copy_mask)
        {
            CopyChunks(reader, copy_mask, false);
        }

        public void CopyChunksLast(PngReader reader, int copy_mask)
        {
            CopyChunks(reader, copy_mask, true);
        }


        /// <summary>
        /// Finalizes the image creation and closes the file stream. This MUST be
        /// called after writing the lines.
        /// </summary>
        ///
        public void End()
        {
            if (rowNum != ImgInfo.Rows - 1)
                throw new PngjOutputException("all rows have not been written");
            try
            {
                datStreamDeflated.Finish();
                datStream.Flush();
                WriteLastChunks();
                WriteEndChunk();
                if (shouldCloseStream)
                    os.Close();
            }
            catch (IOException e)
            {
                throw new PngjOutputException(e);
            }
        }

        
        /**
 * Filename or description, from the optional constructor argument.
 */
        public String GetFilename()
        {
            return filename;
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
        public void WriteRow(int[] newrow, int rown)
        {
            if (rown == 0)
            {
                init();
            }
            if (rown < -1 || rown > ImgInfo.Rows)
                throw new Exception("invalid value for row " + rown);
            rowNum++;
            if (rown >= 0 && rowNum != rown)
                throw new Exception("write order must be strict for rows " + rown
                        + " (expected=" + rowNum + ")");
            scanline = newrow;
            byte[] tmp = rowb;
            rowb = rowbprev;
            rowbprev = tmp;
            ConvertRowToBytes();
            FilterRow(rown);
            //datStream.Write(rowbfilter, 0, imgInfo.bytesPerRow + 1);
            datStreamDeflated.Write(rowbfilter, 0, ImgInfo.BytesPerRow + 1);
        }

        /// <summary>
        /// this uses the row number from the imageline!
        /// </summary>
        ///
        public void WriteRow(ImageLine imgline, int rownumber)
        {
            WriteRow(imgline.Scanline, rownumber);
        }

        public void WriteRow(int[] newrow)
        {
            WriteRow(newrow, -1);
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
        public void SetFilterType(FilterType filterType)
        {
            filterStrat = new FilterWriteStrategy(ImgInfo, filterType);
        }

        public void SetIdatMaxSize(int idatMaxSize)
        {
            this.idatMaxSize = idatMaxSize;
        }

        public void SetShouldCloseStream(bool shouldCloseStream)
        {
            this.shouldCloseStream = shouldCloseStream;
        }

        /// <summary>
        /// compression level: between 0 and 9 (default:6)
        /// </summary>
        ///
        public void SetCompLevel(int compLevel_0)
        {
            if (compLevel_0 < 0 || compLevel_0 > 9)
                throw new PngjException("Compression level invalid (" + compLevel_0
                        + ") Must be 0..9");
            this.compLevel = compLevel_0;
        }



    }
}
