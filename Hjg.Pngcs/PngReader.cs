namespace Hjg.Pngcs {

    using Hjg.Pngcs.Chunks;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System;
    using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
 
    /// <summary>
    /// Reads a PNG image, line by line
    /// </summary>
    /// <remarks>
    /// The reading sequence is as follows:
    /// 
    /// 1. At construction time, the header and IHDR chunk are read (basic image info)
    /// 
    /// 2. Optional: If you call GetMetadata() before reading the rows, the chunks before IDAT are automatically loaded
    /// 
    /// 3. The rows are read in sequence, from 0 to nrows-1 (you can skip rows by calling GetRow())
    /// 
    /// 4. The reading of the last row triggers the loading of trailing chunks, and ends the reader.
    /// 
    /// 5. End() forcibly finishes/aborts the reading and closes the stream
    /// </remarks>
    public class PngReader {
        /// <summary>
        /// Basic image info, inmutable
        /// </summary>
        public readonly ImageInfo ImgInfo;

        /// <summary>
        /// filename, or description - merely informative, can be empty
        /// </summary>
        protected readonly String filename;

        /// <summary>
        /// Strategy for chunk loading. Default: LOAD_CHUNK_ALWAYS
        /// </summary>
        public ChunkLoadBehaviour ChunkLoadBehaviour { get; set; }

        /// <summary>
        /// Should close the underlying Input Stream when ends?
        /// </summary>
        public bool ShouldCloseStream { get; set; }

        /// <summary>
        /// Maximum amount of bytes from ancillary chunks to load in memory 
        /// </summary>
        /// <remarks>
        ///  Default: 5MB. 0: unlimited. If exceeded, chunks will be skipped
        /// </remarks>
        public int MaxBytesMetadata { get; set; }

        /// <summary>
        /// Maximum total bytes to read from stream 
        /// </summary>
        /// <remarks>
        ///  Default: 200MB. 0: Unlimited. If exceeded, an exception will be thrown
        /// </remarks>
        public int MaxTotalBytesRead { get; set; }


        /// <summary>
        /// Maximum ancillary chunk size
        /// </summary>
        /// <remarks>
        ///  Default: 2MB, 0: unlimited. Chunks exceeding this size will be skipped (nor even CRC checked)
        /// </remarks>
        public int SkipChunkMaxSize { get; set; }

        /// <summary>
        /// Ancillary chunks to skip
        /// </summary>
        /// <remarks>
        ///  Default: { "fdAT" }. chunks with these ids will be skipped (nor even CRC checked)
        /// </remarks>
        public String[] SkipChunkIds { get; set; }

        private Dictionary<string, int> skipChunkIdsSet = null; // lazily created

        /// <summary>
        /// A high level wrapper of a ChunksList : list of read chunks
        /// </summary>
        private readonly PngMetadata metadata;
        /// <summary>
        /// Read chunks
        /// </summary>
        private readonly ChunksList chunksList;

        /// <summary>
        /// buffer: last read line
        /// </summary>
        protected ImageLine imgLine;


        /// <summary>
        /// raw current row, as array of bytes,counting from 1 (index 0 is reserved for filter type)
        /// </summary>
        protected byte[] rowb;
        /// <summary>
        /// previuos raw row
        /// </summary>
        protected byte[] rowbprev; // rowb previous
        /// <summary>
        /// raw current row, after unfiltered
        /// </summary>
        protected byte[] rowbfilter;

        /// <summary>
        /// number of chunk group (0-6) last read, or currently reading
        /// </summary>
        /// <remarks>see ChunksList.CHUNK_GROUP_NNN</remarks>
        public int CurrentChunkGroup { get; private set; }
        /// <summary>
        /// last read row number
        /// </summary>
        protected int rowNum = -1; // 
        private long offset = 0;  // offset in InputStream = bytes read
        private int bytesChunksLoaded = 0; // bytes loaded from anciallary chunks

        private readonly Stream inputStream;
        internal InflaterInputStream idatIstream;
        internal PngIDatChunkInputStream iIdatCstream;

        /// <summary>
        /// Constructs a PngReader from a Stream, with no filename information
        /// </summary>
        /// <param name="inputStream"></param>
        public PngReader(Stream inputStream)
            : this(inputStream, "[NO FILENAME AVAILABLE]") {
        }

        /// <summary>
        /// Constructs a PNGReader objet from a opened Stream
        /// </summary>
        /// <remarks>The constructor reads the signature and first chunk (IDHR)<seealso cref="FileHelper.CreatePngReader(string)"/>
        /// </remarks>
        /// 
        /// <param name="inputStream"></param>
        /// <param name="filename">Optional, can be the filename or a description.</param>
        public PngReader(Stream inputStream, String filename) {
            this.filename = (filename == null) ? "" : filename;
            this.inputStream = inputStream;
            this.chunksList = new ChunksList(null);
            this.metadata = new PngMetadata(chunksList);
            this.offset = 0;
            // set default options
            this.CurrentChunkGroup = -1;
            this.ShouldCloseStream = true;
            this.MaxBytesMetadata = 5 * 1024 * 1024;
            this.MaxTotalBytesRead = 200 * 1024 * 1024; // 200MB
            this.SkipChunkMaxSize = 2 * 1024 * 1024;
            this.SkipChunkIds = new string[] { "fdAT" };
            this.ChunkLoadBehaviour = Hjg.Pngcs.Chunks.ChunkLoadBehaviour.LOAD_CHUNK_ALWAYS;
            // starts reading: signature
            byte[] pngid = new byte[8];
            PngHelperInternal.ReadBytes(inputStream, pngid, 0, pngid.Length);
            offset += pngid.Length;
            if (!PngCsUtils.arraysEqual(pngid, PngHelperInternal.pngIdSignature))
                throw new PngjInputException("Bad PNG signature");
            CurrentChunkGroup = ChunksList.CHUNK_GROUP_0_IDHR;
            // reads first chunk IDHR
            int clen = PngHelperInternal.ReadInt4(inputStream);
            offset += 4;
            if (clen != 13)
                throw new Exception("IDHR chunk len != 13 ?? " + clen);
            byte[] chunkid = new byte[4];
            PngHelperInternal.ReadBytes(inputStream, chunkid, 0, 4);
            if (!PngCsUtils.arraysEqual4(chunkid, ChunkHelper.b_IHDR))
                throw new PngjInputException("IHDR not found as first chunk??? ["
                        + ChunkHelper.ToString(chunkid) + "]");
            offset += 4;
            PngChunkIHDR ihdr = (PngChunkIHDR)ReadChunk(chunkid, clen, false);
            bool alpha = (ihdr.Colormodel & 0x04) != 0;
            bool palette = (ihdr.Colormodel & 0x01) != 0;
            bool grayscale = (ihdr.Colormodel == 0 || ihdr.Colormodel == 4);
            // creates ImgInfo and imgLine, and allocates buffers
            ImgInfo = new ImageInfo(ihdr.Cols, ihdr.Rows, ihdr.Bitspc, alpha, grayscale, palette);
            imgLine = new ImageLine(ImgInfo);
            rowb = new byte[ImgInfo.BytesPerRow + 1];
            rowbprev = new byte[rowb.Length];
            rowbfilter = new byte[rowb.Length];
            // some checks
            if (ihdr.Interlaced != 0)
                throw new PngjUnsupportedException("PNG interlaced not supported by this library");
            if (ihdr.Filmeth != 0 || ihdr.Compmeth != 0)
                throw new PngjInputException("compmethod o filtermethod unrecognized");
            if (ihdr.Colormodel < 0 || ihdr.Colormodel > 6 || ihdr.Colormodel == 1
                    || ihdr.Colormodel == 5)
                throw new PngjInputException("Invalid colormodel " + ihdr.Colormodel);
            if (ihdr.Bitspc != 1 && ihdr.Bitspc != 2 && ihdr.Bitspc != 4 && ihdr.Bitspc != 8
                    && ihdr.Bitspc != 16)
                throw new PngjInputException("Invalid bit depth " + ihdr.Bitspc);
        }



        private void ConvertRowFromBytes(int[] buffer) {
            // see http://www.libpng.org/pub/png/spec/1.2/PNG-DataRep.html
            int i, j;
            if (ImgInfo.BitDepth <= 8) {
                for (i = 0, j = 1; i < ImgInfo.SamplesPerRowP; i++) {
                    buffer[i] = (rowb[j++]);
                }
            } else { // 16 bitspc
                for (i = 0, j = 1; i < ImgInfo.SamplesPerRowP; i++) {
                    buffer[i] = (rowb[j++] << 8) + rowb[j++];
                }
            }
        }

        private bool FirstChunksNotYetRead() {
            return CurrentChunkGroup < ChunksList.CHUNK_GROUP_1_AFTERIDHR;
        }

        /// <summary>
        /// Internally called after having read the last line. 
        /// It reads extra chunks after IDAT, if present.
        /// </summary>
        private void ReadLastAndClose() {
            idatIstream.Close();
            ReadLastChunks();
            Close();
        }

        private void Close() {
            if (CurrentChunkGroup < ChunksList.CHUNK_GROUP_6_END) { // this could only happen if forced close
                try {
                    idatIstream.Close();
                } catch (Exception e) {
                }
                CurrentChunkGroup = ChunksList.CHUNK_GROUP_6_END;
            }
            if (ShouldCloseStream)
                inputStream.Close();
        }




        private void UnfilterRow() {
            int ftn = rowbfilter[0];
            FilterType ft = (FilterType)ftn;
            switch (ft) {
                case Hjg.Pngcs.FilterType.FILTER_NONE:
                    UnfilterRowNone();
                    break;
                case Hjg.Pngcs.FilterType.FILTER_SUB:
                    UnfilterRowSub();
                    break;
                case Hjg.Pngcs.FilterType.FILTER_UP:
                    UnfilterRowUp();
                    break;
                case Hjg.Pngcs.FilterType.FILTER_AVERAGE:
                    UnfilterRowAverage();
                    break;
                case Hjg.Pngcs.FilterType.FILTER_PAETH:
                    UnfilterRowPaeth();
                    break;
                default:
                    throw new PngjInputException("Filter type " + ftn + " not implemented");
            }
        }


        private void UnfilterRowAverage() {
            int i, j, x;
            for (j = 1 - ImgInfo.BytesPixel, i = 1; i <= ImgInfo.BytesPerRow; i++, j++) {
                x = (j > 0) ? rowb[j] : 0;
                rowb[i] = (byte)(rowbfilter[i] + (x + (rowbprev[i] & 0xFF)) / 2);
            }
        }

        private void UnfilterRowNone() {
            for (int i = 1; i <= ImgInfo.BytesPerRow; i++) {
                rowb[i] = (byte)(rowbfilter[i]);
            }
        }

        private void UnfilterRowPaeth() {
            int i, j, x, y;
            for (j = 1 - ImgInfo.BytesPixel, i = 1; i <= ImgInfo.BytesPerRow; i++, j++) {
                x = (j > 0) ? rowb[j] : 0;
                y = (j > 0) ? rowbprev[j] : 0;
                rowb[i] = (byte)(rowbfilter[i] + PngHelperInternal.FilterPaethPredictor(x, rowbprev[i], y));
            }
        }

        private void UnfilterRowSub() {
            int i, j;
            for (i = 1; i <= ImgInfo.BytesPixel; i++) {
                rowb[i] = (byte)(rowbfilter[i]);
            }
            for (j = 1, i = ImgInfo.BytesPixel + 1; i <= ImgInfo.BytesPerRow; i++, j++) {
                rowb[i] = (byte)(rowbfilter[i] + rowb[j]);
            }
        }

        private void UnfilterRowUp() {
            for (int i = 1; i <= ImgInfo.BytesPerRow; i++) {
                rowb[i] = (byte)(rowbfilter[i] + rowbprev[i]);
            }
        }



        /// <summary>
        /// Reads chunks before first IDAT. Position before: after IDHR (crc included)
        /// Position after: just after the first IDAT chunk id Returns length of first
        /// IDAT chunk , -1 if not found
        /// </summary>
        ///
        private void ReadFirstChunks() {
            if (!FirstChunksNotYetRead())
                return;
            int clen = 0;
            bool found = false;
            byte[] chunkid = new byte[4]; // it's important to reallocate in each
            this.CurrentChunkGroup = ChunksList.CHUNK_GROUP_1_AFTERIDHR;
            while (!found) {
                clen = PngHelperInternal.ReadInt4(inputStream);
                offset += 4;
                if (clen < 0)
                    break;
                PngHelperInternal.ReadBytes(inputStream, chunkid, 0, 4);
                offset += 4;
                if (PngCsUtils.arraysEqual4(chunkid, Hjg.Pngcs.Chunks.ChunkHelper.b_IDAT)) {
                    found = true;
                    this.CurrentChunkGroup = ChunksList.CHUNK_GROUP_4_IDAT;
                    // add dummy idat chunk to list
                    chunksList.AppendReadChunk(new PngChunkIDAT(ImgInfo, clen, offset - 8), CurrentChunkGroup);
                    break;
                } else if (PngCsUtils.arraysEqual4(chunkid, Hjg.Pngcs.Chunks.ChunkHelper.b_IEND)) {
                    throw new PngjInputException("END chunk found before image data (IDAT) at offset=" + offset);
                }
                String chunkids = ChunkHelper.ToString(chunkid);
                if (chunkids.Equals(ChunkHelper.PLTE))
                    this.CurrentChunkGroup = ChunksList.CHUNK_GROUP_2_PLTE;
                ReadChunk(chunkid, clen, false);
                if (chunkids.Equals(ChunkHelper.PLTE))
                    this.CurrentChunkGroup = ChunksList.CHUNK_GROUP_3_AFTERPLTE;
            }
            int idatLen = found ? clen : -1;
            if (idatLen < 0)
                throw new PngjInputException("first idat chunk not found!");
            iIdatCstream = new PngIDatChunkInputStream(inputStream, idatLen, offset);
            idatIstream = new InflaterInputStream(iIdatCstream);
        }

        /// <summary>
        /// Reads (and processes ... up to a point) chunks after last IDAT.
        /// </summary>
        ///
        private void ReadLastChunks() {
            CurrentChunkGroup = ChunksList.CHUNK_GROUP_5_AFTERIDAT;
            // PngHelper.logdebug("idat ended? " + iIdatCstream.isEnded());
            if (!iIdatCstream.IsEnded())
                iIdatCstream.ForceChunkEnd();
            int clen = iIdatCstream.GetLenLastChunk();
            byte[] chunkid = iIdatCstream.GetIdLastChunk();
            bool endfound = false;
            bool first = true;
            bool skip = false;
            while (!endfound) {
                skip = false;
                if (!first) {
                    clen = PngHelperInternal.ReadInt4(inputStream);
                    offset += 4;
                    if (clen < 0)
                        throw new PngjInputException("bad len " + clen);
                    PngHelperInternal.ReadBytes(inputStream, chunkid, 0, 4);
                    offset += 4;
                }
                first = false;
                if (PngCsUtils.arraysEqual4(chunkid, ChunkHelper.b_IDAT)) {
                    skip = true; // extra dummy (empty?) idat chunk, it can happen, ignore it
                } else if (PngCsUtils.arraysEqual4(chunkid, ChunkHelper.b_IEND)) {
                    CurrentChunkGroup = ChunksList.CHUNK_GROUP_6_END;
                    endfound = true;
                }
                ReadChunk(chunkid, clen, skip);
            }
            if (!endfound)
                throw new PngjInputException("end chunk not found - offset=" + offset);
            // PngHelper.logdebug("end chunk found ok offset=" + offset);
        }

        /// <summary>
        /// Reads chunkd from input stream, adds to ChunksList, and returns it.
        /// If it's skipped, a PngChunkSkipped object is created
        /// </summary>
        /// <returns></returns>
        private PngChunk ReadChunk(byte[] chunkid, int clen, bool skipforced) {
            if (clen < 0) throw new PngjInputException("invalid chunk lenght: " + clen);
            // skipChunksByIdSet is created lazyly, if fist IHDR has already been read
            if (skipChunkIdsSet == null && CurrentChunkGroup > ChunksList.CHUNK_GROUP_0_IDHR) {
                skipChunkIdsSet = new Dictionary<string, int>();
                if(SkipChunkIds!=null)
                    foreach (string id in SkipChunkIds) skipChunkIdsSet.Add(id, 1);
            }

            String chunkidstr = ChunkHelper.ToString(chunkid);
            PngChunk pngChunk = null;
            bool skip = skipforced;
            if (MaxTotalBytesRead > 0 && clen + offset > MaxTotalBytesRead)
                throw new PngjInputException("Maximum total bytes to read exceeeded: " + MaxTotalBytesRead + " offset:"
                        + offset + " clen=" + clen);
            // an ancillary chunks can be skipped because of several reasons:
            if (CurrentChunkGroup > ChunksList.CHUNK_GROUP_0_IDHR && !ChunkHelper.IsCritical(chunkidstr))
                skip = skip || (SkipChunkMaxSize > 0 && clen >= SkipChunkMaxSize) || skipChunkIdsSet.ContainsKey(chunkidstr)
                        || (MaxBytesMetadata > 0 && clen > MaxBytesMetadata - bytesChunksLoaded)
                        || !ChunkHelper.ShouldLoad(chunkidstr, ChunkLoadBehaviour);

            if (skip) {
                PngHelperInternal.SkipBytes(inputStream, clen);
                PngHelperInternal.ReadInt4(inputStream); // skip - we dont call PngHelperInternal.skipBytes(inputStream, clen + 4) for risk of overflow 
                pngChunk = new PngChunkSkipped(chunkidstr, ImgInfo, clen);
            } else {
                ChunkRaw chunk = new ChunkRaw(clen, chunkid, true);
                chunk.ReadChunkData(inputStream);
                pngChunk = PngChunk.Factory(chunk, ImgInfo);
                if (!pngChunk.Crit) {
                    bytesChunksLoaded += chunk.Length;
                }

            }
            pngChunk.Offset = offset - 8L;
            chunksList.AppendReadChunk(pngChunk, CurrentChunkGroup);
            offset += clen + 4L;
            return pngChunk;
        }





        /// <summary>
        /// Logs/prints a warning.
        /// </summary>
        /// <remarks>
        /// The default behaviour is print to stderr, but it can be overriden.
        /// This happens rarely - most errors are fatal.
        /// </remarks>
        /// <param name="warn"></param>
        protected virtual void logWarn(String warn) {
            Console.Error.WriteLine(warn);
        }

        /// <summary>
        /// Returns the ancillary chunks available
        /// </summary>
        /// <remarks>
        /// If the rows have not yet still been read, this includes
        /// only the chunks placed before the pixels (IDAT)
        /// </remarks>
        /// <returns>ChunksList</returns>
        public ChunksList GetChunksList() {
            if (FirstChunksNotYetRead())
                ReadFirstChunks();
            return chunksList;
        }

        /// <summary>
        /// Returns the ancillary chunks available
        /// </summary>
        /// <remarks>
        /// see GetChunksList
        /// </remarks>
        /// <returns>PngMetadata</returns>
        public PngMetadata GetMetadata() {
            if (FirstChunksNotYetRead())
                ReadFirstChunks();
            return metadata;
        }

        /// <summary>
        /// reads the row using ImageLine as buffer
        /// </summary>
        ///<param name="nrow">row number - just as a check</param>
        /// <returns>the ImageLine that also is available inside this object</returns>
        public ImageLine ReadRow(int nrow) {
            ReadRow(imgLine.Scanline, nrow);
            imgLine.FilterUsed = (FilterType)rowbfilter[0];
            imgLine.SetRown(nrow);
            return imgLine;
        }

        /// <summary>
        /// Like readRow(int nrow) but this accepts non consecutive rows.
        /// </summary>
        /// <remarks>If it's the current row, it will just return it. Elsewhere, it will try to read it.
        /// This implementation only accepts  nrow greater or equal than current row, but
        /// an extended class could implement some partial or full cache of lines.
        /// 
        /// This should not  not be mixed with calls to readRow(int[] buffer, final int nrow)
        /// </remarks>
        /// <param name="nrow"></param>
        /// <returns></returns>
        public ImageLine GetRow(int nrow) {
            while (rowNum < nrow) ReadRow(rowNum + 1);
            // now it should be positioned in the desired row
            if (rowNum != nrow || imgLine.GetRown() != nrow)
                throw new PngjInputException("Invalid row: " + nrow);
            return imgLine;
        }

        /// <summary>
        /// Reads a line and returns it as a int array 
        /// </summary>
        /// <remarks>See also the other
        /// overloaded method</remarks>
        /// <param name="buffer">Buffer can be prealocated (in
        /// this case it must have enough len!) or can be null</param>
        /// <param name="nrow">number of row, as check</param>
        /// <returns>The same buffer if it was allocated, a newly allocated one
        /// otherwise</returns>
        public int[] ReadRow(int[] buffer, int nrow) {
            if (nrow < 0 || nrow >= ImgInfo.Rows)
                throw new PngjInputException("invalid line");
            if (nrow != rowNum + 1)
                throw new PngjInputException("invalid line (expected: " + (rowNum + 1));
            if (nrow == 0 && FirstChunksNotYetRead())
                ReadFirstChunks();
            rowNum++;
            if (buffer == null || buffer.Length < ImgInfo.SamplesPerRowP)
                buffer = new int[ImgInfo.SamplesPerRowP];
            byte[] tmp = rowb;
            rowb = rowbprev;
            rowbprev = tmp;
            // loads in rowbfilter "raw" bytes, with filter
            PngHelperInternal.ReadBytes(idatIstream, rowbfilter, 0, rowbfilter.Length);
            // updates and checks offset
            offset = iIdatCstream.GetOffset();
            if ((MaxTotalBytesRead >0 && offset >= MaxTotalBytesRead) || offset < 0)
                throw new PngjInputException("Reading IDAT: Maximum total bytes to read exceeeded: " + MaxTotalBytesRead
                        + " offset:" + offset);
            rowb[0] = 0;
            UnfilterRow();
            rowb[0] = rowbfilter[0];
            ConvertRowFromBytes(buffer);
            // new: if last row, automatically call ReadLastAndClose()
            if (nrow == ImgInfo.Rows - 1)
                ReadLastAndClose();
            return buffer;
        }


        public override String ToString() { // basic info
            return "filename=" + filename + " " + ImgInfo.ToString();
        }
        /// <summary>
        /// Normally this does nothing, but it can be used to force a premature closing
        /// </summary>
        /// <remarks></remarks>
        public void End() {
            if (CurrentChunkGroup < ChunksList.CHUNK_GROUP_6_END)
                Close();
        }

        /// <summary>
        /// for debug 
        /// </summary>
        ///
        public static void ShowLineInfo(ImageLine line) {
            System.Console.Out.WriteLine(line);
            Hjg.Pngcs.ImageLineHelper.ImageLineStats stats = new ImageLineHelper.ImageLineStats(line);
            System.Console.Out.WriteLine(stats);
            System.Console.Out.WriteLine(Hjg.Pngcs.ImageLineHelper.InfoFirstLastPixels(line));
        }

    }
}
