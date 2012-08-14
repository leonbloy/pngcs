namespace Hjg.Pngcs {

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
    public class PngWriter {
        /// <summary>
        /// Basic image info, inmutable
        /// </summary>
        public readonly ImageInfo ImgInfo;

        /// <summary>
        /// filename, or description - merely informative, can be empty
        /// </summary>
        protected readonly String filename;

        private FilterWriteStrategy filterStrat;

        /// <summary>
        /// Strategy for deflater
        /// </summary>
        /// <remarks>
        /// We define our own enums so as our public interface doesnt depend on implementation
        /// </remarks>
        public enum ECompressionStrategy {
            Filtered,
            HuffmanOnly,
            Default
        }
        /**
         * Deflate algortithm compression strategy
         */
        public ECompressionStrategy CompressionStrategy { get; set; }

        /// <summary>
        /// zip compression level (0 - 9)
        /// </summary>
        /// <remarks>
        /// default:6
        /// 
        /// 9 is the maximum compression
        /// </remarks>
        public int CompLevel { get; set; }
        /// <summary>
        /// true: closes stream after ending write
        /// </summary>
        public bool ShouldCloseStream { get; set; }
        /// <summary>
        /// Maximum size of IDAT chunks
        /// </summary>
        /// <remarks>
        /// 0=use default (PngIDatChunkOutputStream 32768)
        /// </remarks>
        public int IdatMaxSize { get; set; } // 

        /// <summary>
        /// A high level wrapper of a ChunksList : list of written/queued chunks
        /// </summary>
        private readonly PngMetadata metadata;
        /// <summary>
        /// written/queued chunks
        /// </summary>
        private readonly ChunksListForWrite chunksList;

        /// <summary>
        /// raw current row, as array of bytes,counting from 1 (index 0 is reserved for filter type)
        /// </summary>
        protected byte[] rowb;
        /// <summary>
        /// previuos raw row
        /// </summary>
        protected byte[] rowbprev; // rowb previous
        /// <summary>
        /// raw current row, after filtered
        /// </summary>
        protected byte[] rowbfilter;
        /// <summary>
        /// current line, one (packed) sample per element (layout differnt from rowb!)
        /// </summary>
        private int[] scanline;

        /// <summary>
        /// number of chunk group (0-6) last writen, or currently writing
        /// </summary>
        /// <remarks>see ChunksList.CHUNK_GROUP_NNN</remarks>
        public int CurrentChunkGroup { get; private set; }

        private int rowNum = -1; // current line number
        private readonly Stream outputStream;

        private PngIDatChunkOutputStream datStream;
        private Stream datStreamDeflated;

        private int[] histox = new int[256]; // auxiliar buffer, histogram, only used by reportResultsForFilter

        /// <summary>
        /// Constructs a PngWriter from a outputStream, with no filename information
        /// </summary>
        /// <param name="outputStream"></param>
        /// <param name="imgInfo"></param>
        public PngWriter(Stream outputStream, ImageInfo imgInfo)
            : this(outputStream, imgInfo, "[NO FILENAME AVAILABLE]") {
        }

        /// <summary>
        /// Constructs a PngWriter from a outputStream, with optional filename or description
        /// </summary>
        /// <remarks>
        /// After construction nothing is writen yet. You still can set some
        /// parameters (compression, filters) and queue chunks before start writing the pixels.
        /// 
        /// See also <c>FileHelper.createPngWriter()</c>
        /// </remarks>
        /// <param name="outputStream">Opened stream for binary writing</param>
        /// <param name="imgInfo">Basic image parameters</param>
        /// <param name="filename">Optional, can be the filename or a description.</param>
        public PngWriter(Stream outputStream, ImageInfo imgInfo,
                String filename) {
            this.filename = (filename == null) ? "" : filename;
            this.outputStream = outputStream;
            this.ImgInfo = imgInfo;
            // defaults settings
            this.CompLevel = 6;
            this.ShouldCloseStream = true;
            this.IdatMaxSize = 0; // use default
            this.CompressionStrategy = ECompressionStrategy.Default;
            // prealloc
            scanline = new int[imgInfo.SamplesPerRowP];
            rowb = new byte[imgInfo.BytesPerRow + 1];
            rowbprev = new byte[rowb.Length];
            rowbfilter = new byte[rowb.Length];
            chunksList = new ChunksListForWrite(ImgInfo);
            metadata = new PngMetadata(chunksList);
            filterStrat = new FilterWriteStrategy(ImgInfo, FilterType.FILTER_DEFAULT);
        }

        /// <summary>
        /// init: is called automatically before writing the first row
        /// </summary>
        private void init() {
            datStream = new PngIDatChunkOutputStream(this.outputStream, this.IdatMaxSize);
            datStreamDeflated = CreateCompressedStream(datStream, this.CompLevel, CompressionStrategy);
            WriteSignatureAndIHDR();
            WriteFirstChunks();
        }

        /// <summary>
        /// Creates the compressed stream with deflate (zlib) which decorates the IDAT stream.
        /// </summary>
        /// <param name="raw"></param>
        /// <param name="compLevel"></param>
        /// <param name="strat"></param>
        /// <returns></returns>
        protected virtual Stream CreateCompressedStream(Stream raw, int compLevel, ECompressionStrategy strat) {
            //return CreateCompressedStreamIonic(raw, compLevel, strat);
            return CreateCompressedStreamCSsharpLib(raw, compLevel, strat);
        }

        private Stream CreateCompressedStreamCSsharpLib(Stream raw, int compLevel, ECompressionStrategy strat) {
            Deflater defl = new Deflater(compLevel);
            switch (strat) {
                case ECompressionStrategy.Filtered: defl.SetStrategy(DeflateStrategy.Filtered); break;
                case ECompressionStrategy.HuffmanOnly: defl.SetStrategy(DeflateStrategy.HuffmanOnly); break;
                default: defl.SetStrategy(DeflateStrategy.Default); break;
            }
            DeflaterOutputStream datStream = new DeflaterOutputStream(raw, defl);
            datStream.IsStreamOwner = false;
            return datStream;
        }

        private void WriteEndChunk() {
            PngChunkIEND c = new PngChunkIEND(ImgInfo);
            c.CreateRawChunk().WriteChunk(outputStream);
        }

        private void WriteFirstChunks() {
            int nw = 0;
            CurrentChunkGroup = ChunksList.CHUNK_GROUP_1_AFTERIDHR;
            nw = chunksList.writeChunks(outputStream, CurrentChunkGroup);
            CurrentChunkGroup = ChunksList.CHUNK_GROUP_2_PLTE;
            nw = chunksList.writeChunks(outputStream, CurrentChunkGroup);
            if (nw > 0 && ImgInfo.Greyscale)
                throw new PngjOutputException("cannot write palette for this format");
            if (nw == 0 && ImgInfo.Indexed)
                throw new PngjOutputException("missing palette");
            CurrentChunkGroup = ChunksList.CHUNK_GROUP_3_AFTERPLTE;
            nw = chunksList.writeChunks(outputStream, CurrentChunkGroup);
            CurrentChunkGroup = ChunksList.CHUNK_GROUP_4_IDAT;
        }

        private void WriteLastChunks() { // not including end
            CurrentChunkGroup = ChunksList.CHUNK_GROUP_5_AFTERIDAT;
            chunksList.writeChunks(outputStream, CurrentChunkGroup);
            // should not be unwriten chunks
            List<PngChunk> pending = chunksList.GetQueuedChunks();
            if (pending.Count > 0)
                throw new PngjOutputException(pending.Count + " chunks were not written! Eg: " + pending[0].ToString());
            CurrentChunkGroup = ChunksList.CHUNK_GROUP_6_END;
        }



        /// <summary>
        /// Write id signature and also "IHDR" chunk
        /// </summary>
        ///
        private void WriteSignatureAndIHDR() {
            CurrentChunkGroup = ChunksList.CHUNK_GROUP_0_IDHR;
            PngHelperInternal.WriteBytes(outputStream, Hjg.Pngcs.PngHelperInternal.pngIdBytes); // signature
            PngChunkIHDR ihdr = new PngChunkIHDR(ImgInfo);
            // http://www.libpng.org/pub/png/spec/1.2/PNG-Chunks.html
            ihdr.Cols = ImgInfo.Cols;
            ihdr.Rows = ImgInfo.Rows;
            ihdr.Bitspc = ImgInfo.BitDepth;
            int colormodel = 0;
            if (ImgInfo.Alpha)
                colormodel += 0x04;
            if (ImgInfo.Indexed)
                colormodel += 0x01;
            if (!ImgInfo.Greyscale)
                colormodel += 0x02;
            ihdr.Colormodel = colormodel;
            ihdr.Compmeth = 0; // compression method 0=deflate
            ihdr.Filmeth = 0; // filter method (0)
            ihdr.Interlaced = 0; // never interlace
            ihdr.CreateRawChunk().WriteChunk(outputStream);
        }

        private void ConvertRowToBytes() {
            // http://www.libpng.org/pub/png/spec/1.2/PNG-DataRep.html
            int i, j, x;
            if (ImgInfo.BitDepth <= 8) { // !!! optimizar con foreach
                for (i = 0, j = 1; i < ImgInfo.SamplesPerRowP; i++) {
                    rowb[j++] = (byte)(((int)scanline[i]) & 0xFF);
                }
            } else { // 16 bitspc
                for (i = 0, j = 1; i < ImgInfo.SamplesPerRowP; i++) {
                    x = (int)(scanline[i]) & 0xFFFF;
                    rowb[j++] = (byte)((x & 0xFF00) >> 8);
                    rowb[j++] = (byte)(x & 0xFF);
                }
            }
        }


        private void FilterRow(int rown) {
            // warning: filters operation rely on: "previos row" (rowbprev) is
            // initialized to 0 the first time
            if (filterStrat.shouldTestAll(rown)) {
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
            switch (filterType) {
                case Hjg.Pngcs.FilterType.FILTER_NONE:
                    FilterRowNone();
                    break;
                case Hjg.Pngcs.FilterType.FILTER_SUB:
                    FilterRowSub();
                    break;
                case Hjg.Pngcs.FilterType.FILTER_UP:
                    FilterRowUp();
                    break;
                case Hjg.Pngcs.FilterType.FILTER_AVERAGE:
                    FilterRowAverage();
                    break;
                case Hjg.Pngcs.FilterType.FILTER_PAETH:
                    FilterRowPaeth();
                    break;
                default:
                    throw new PngjOutputException("Filter type " + filterType + " not implemented");
            }
            reportResultsForFilter(rown, filterType, false);
        }


        private void FilterRowAverage() {
            int i, j, imax;
            imax = ImgInfo.BytesPerRow;
            for (j = 1 - ImgInfo.BytesPixel, i = 1; i <= imax; i++, j++) {
                rowbfilter[i] = (byte)(rowb[i] - ((rowbprev[i]) + (j > 0 ? rowb[j] : 0)) / 2);
            }
        }

        private void FilterRowNone() {
            for (int i = 1; i <= ImgInfo.BytesPerRow; i++) {
                rowbfilter[i] = (byte)rowb[i];
            }
        }


        private void FilterRowPaeth() {
            int i, j, imax;
            imax = ImgInfo.BytesPerRow;
            for (j = 1 - ImgInfo.BytesPixel, i = 1; i <= imax; i++, j++) {
                rowbfilter[i] = (byte)(rowb[i] - PngHelperInternal.FilterPaethPredictor(j > 0 ? rowb[j] : 0,
                        rowbprev[i], j > 0 ? rowbprev[j] : 0));
            }
        }

        private void FilterRowSub() {
            int i, j;
            for (i = 1; i <= ImgInfo.BytesPixel; i++) {
                rowbfilter[i] = (byte)rowb[i];
            }
            for (j = 1, i = ImgInfo.BytesPixel + 1; i <= ImgInfo.BytesPerRow; i++, j++) {
                rowbfilter[i] = (byte)(rowb[i] - rowb[j]);
            }
        }

        private void FilterRowUp() {
            for (int i = 1; i <= ImgInfo.BytesPerRow; i++) {
                rowbfilter[i] = (byte)(rowb[i] - rowbprev[i]);
            }
        }


        private long SumRowbfilter() { // sums absolute value 
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
        private void CopyChunks(PngReader reader, int copy_mask, bool onlyAfterIdat) {
            bool idatDone = CurrentChunkGroup >= ChunksList.CHUNK_GROUP_4_IDAT;
            if (onlyAfterIdat && reader.CurrentChunkGroup < ChunksList.CHUNK_GROUP_6_END) throw new PngjException("tried to copy last chunks but reader has not ended");
            foreach (PngChunk chunk in reader.GetChunksList().GetChunks()) {
                int group = chunk.ChunkGroup;
                if (group < ChunksList.CHUNK_GROUP_4_IDAT && idatDone)
                    continue;
                bool copy = false;
                if (chunk.Crit) {
                    if (chunk.Id.Equals(ChunkHelper.PLTE)) {
                        if (ImgInfo.Indexed && ChunkHelper.maskMatch(copy_mask, ChunkCopyBehaviour.COPY_PALETTE))
                            copy = true;
                        if (!ImgInfo.Greyscale && ChunkHelper.maskMatch(copy_mask, ChunkCopyBehaviour.COPY_ALL))
                            copy = true;
                    }
                } else { // ancillary
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
                    if (chunk is PngChunkSkipped)
                        copy = false;
                }
                if (copy) {
                    chunksList.Queue(PngChunk.CloneChunk(chunk, ImgInfo));
                }
            }
        }

        public void CopyChunksFirst(PngReader reader, int copy_mask) {
            CopyChunks(reader, copy_mask, false);
        }

        public void CopyChunksLast(PngReader reader, int copy_mask) {
            CopyChunks(reader, copy_mask, true);
        }


        /// <summary>
        /// Finalizes the image creation and closes the file stream. </summary>
        ///   <remarks>
        ///   This MUST be called after writing the lines.
        ///   </remarks>      
        ///
        public void End() {
            if (rowNum != ImgInfo.Rows - 1)
                throw new PngjOutputException("all rows have not been written");
            try {
                datStreamDeflated.Close();
                datStream.Close();
                WriteLastChunks();
                WriteEndChunk();
                if (this.ShouldCloseStream)
                    outputStream.Close();
            } catch (IOException e) {
                throw new PngjOutputException(e);
            }
        }

        /// <summary>
        ///  Filename or description, from the optional constructor argument.
        /// </summary>
        /// <returns></returns>
        public String GetFilename() {
            return filename;
        }

        /// <summary>
        /// Writes a full image row.
        /// </summary>
        /// <remarks>
        /// This must be called sequentially from n=0 to
        /// n=rows-1 One integer per sample , in the natural order: R G B R G B ... (or
        /// R G B A R G B A... if has alpha) The values should be between 0 and 255 for
        /// 8 bitspc images, and between 0- 65535 form 16 bitspc images (this applies
        /// also to the alpha channel if present) The array can be reused.
        /// </remarks>
        /// <param name="newrow">Array of pixel values</param>
        /// <param name="rown">Number of row, from 0 (top) to rows-1 (bottom)</param>
        public void WriteRow(int[] newrow, int rown) {
            if (rown == 0) {
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
        public void WriteRow(ImageLine imgline, int rownumber) {
            WriteRow(imgline.Scanline, rownumber);
        }

        public void WriteRow(int[] newrow) {
            WriteRow(newrow, -1);
        }

        public PngMetadata GetMetadata() {
            return metadata;
        }

        public ChunksList GetChunksList() {
            return chunksList;
        }

        /// <summary>
        /// Sets internal prediction filter type, or strategy to choose it.
        /// </summary>
        /// <remarks>
        /// This must be called just after constructor, before starting writing.
        /// 
        /// Recommended values: DEFAULT (default) or AGGRESIVE
        /// </remarks>
        /// <param name="filterType">One of the five prediction types or strategy to choose it</param>
        public void SetFilterType(FilterType filterType) {
            filterStrat = new FilterWriteStrategy(ImgInfo, filterType);
        }

        /// <summary>
        /// Computes compressed size/raw size, approximate
        /// </summary>
        /// <remarks>Actually: compressed size = total size of IDAT data , raw size = uncompressed pixel bytes = rows * (bytesPerRow + 1)
        /// </remarks>
        /// <returns></returns>
        public double ComputeCompressionRatio() {
            if (CurrentChunkGroup < ChunksList.CHUNK_GROUP_6_END)
                throw new PngjException("must be called after End()");
            double compressed = (double)datStream.GetCountFlushed();
            double raw = (ImgInfo.BytesPerRow + 1) * ImgInfo.Rows;
            return compressed / raw;
        }

        private void reportResultsForFilter(int rown, FilterType type, bool tentative) {
            for (int i = 0; i < histox.Length; i++)
                histox[i] = 0;
            int s = 0, v;
            for (int i = 1; i <= ImgInfo.BytesPerRow; i++) {
                v = rowbfilter[i];
                if (v < 0)
                    s -= (int)v;
                else
                    s += (int)v;
                histox[v & 0xFF]++;
            }
            filterStrat.fillResultsForFilter(rown, type, s, histox, tentative);
        }

    }
}
