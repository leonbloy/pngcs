namespace Ar.Com.Hjg.Pngcs
{

    using Ar.Com.Hjg.Pngcs.Chunks;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System;
    using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

    /// <summary>
    /// Reads a PNG image, line by line
    /// </summary>
    ///
    public class PngReader
    {
        public readonly ImageInfo ImgInfo;
        private readonly String filename; // not necesarily a filename, can be a description - merely informative
        private int maxBytesChunksToLoad = 1024 * 1024; // for ancillary chunks
        private ChunkLoadBehaviour chunkLoadBehaviour;

        private readonly Stream inputStream;
        private InflaterInputStream idatIstream;
        private PngIDatChunkInputStream iIdatCstream;

        private int currentChunkGroup = -1;
        internal int CurrentChunkGroup { get { return currentChunkGroup; } }

        protected int rowNum = -1; // current row number
        private int offset = 0;
        private int bytesChunksLoaded = 0;

        private readonly ChunksList chunksList;
        private readonly PngMetadata metadata; // this a wrapper over chunks



        private ImageLine imgLine;
        // line as bytes, counting from 1 (index 0 is reserved for filter type)
        private byte[] rowb; // TODO: short would be nice
        private byte[] rowbprev; // rowb previous
        private byte[] rowbfilter; // current line 'filtered'

        private bool shouldCloseStream = true; // true: closes stream after ending read


        public PngReader(Stream inputStream)
            : this(inputStream, "[NO FILENAME AVAILABLE]")
        {
        }

        /// <summary>
        /// The constructor loads the header and first chunks, stopping at the
        /// beginning of the image data (IDAT chunks)
        /// </summary>
        ///
        /// <param name="filename">Path of image file</param>
        public PngReader(Stream inputStream, String filenameOrDescription)
        {
            this.filename = (filenameOrDescription == null) ? "" : filenameOrDescription;
            this.inputStream = inputStream;
            this.chunksList = new ChunksList(null);
            this.metadata = new PngMetadata(chunksList);
            this.offset = 0;
            this.rowNum = -1;
            this.rowb = null;
            this.rowbprev = null;
            this.rowbfilter = null;
            this.chunkLoadBehaviour = Ar.Com.Hjg.Pngcs.Chunks.ChunkLoadBehaviour.LOAD_CHUNK_ALWAYS;

            // reads header (magic bytes)
            byte[] pngid = new byte[PngHelperInternal.pngIdBytes.Length];
            PngHelperInternal.ReadBytes(inputStream, pngid, 0, pngid.Length);
            offset += pngid.Length;

            if (!PngCsUtils.arraysEqual(pngid, PngHelperInternal.pngIdBytes))
                throw new PngjInputException("Bad PNG signature");
            currentChunkGroup = ChunksList.CHUNK_GROUP_0_IDHR;
            // reads first chunk
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
            ChunkRaw chunk = new ChunkRaw(clen, chunkid, true);
            offset += chunk.ReadChunkData(inputStream);
            PngChunkIHDR ihdr = (PngChunkIHDR)AddChunkToList(chunk);
            bool alpha = (ihdr.colormodel & 0x04) != 0;
            bool palette = (ihdr.colormodel & 0x01) != 0;
            bool grayscale = (ihdr.colormodel == 0 || ihdr.colormodel == 4);
            ImgInfo = new ImageInfo(ihdr.cols, ihdr.rows, ihdr.bitspc, alpha, grayscale, palette);
            imgLine = new ImageLine(ImgInfo);
            if (ihdr.interlaced != 0)
                throw new PngjUnsupportedException("PNG interlaced not supported by this library");
            if (ihdr.filmeth != 0 || ihdr.compmeth != 0)
                throw new PngjInputException("compmethod o filtermethod unrecognized");
            if (ihdr.colormodel < 0 || ihdr.colormodel > 6 || ihdr.colormodel == 1
                    || ihdr.colormodel == 5)
                throw new PngjInputException("Invalid colormodel " + ihdr.colormodel);
            if (ihdr.bitspc != 1 && ihdr.bitspc != 2 && ihdr.bitspc != 4 && ihdr.bitspc != 8
                    && ihdr.bitspc != 16)
                throw new PngjInputException("Invalid bit depth " + ihdr.bitspc);
            // allocation
            rowb = new byte[ImgInfo.BytesPerRow + 1];
            rowbprev = new byte[rowb.Length];
            rowbfilter = new byte[rowb.Length];

        }

        private PngChunk AddChunkToList(ChunkRaw chunk)
        {
            PngChunk chunkType = PngChunk.Factory(chunk, ImgInfo);

            if (!chunkType.Crit)
            {
                bytesChunksLoaded += chunk.Length;
            }
            if (bytesChunksLoaded > maxBytesChunksToLoad)
            {
                logWarn("Chunk exceeded available space (" + maxBytesChunksToLoad + ") chunk: " + chunk
                 + " See PngReader.setMaxBytesChunksToLoad()\n");

            }
            else
            {
                chunksList.AppendReadChunk(chunkType, currentChunkGroup);
            }
            return chunkType;
        }

        private void ConvertRowFromBytes(int[] buffer)
        {
            // http://www.libpng.org/pub/png/spec/1.2/PNG-DataRep.html
            int i, j;
            if (ImgInfo.BitDepth <= 8)
            {
                for (i = 0, j = 1; i < ImgInfo.SamplesPerRowP; i++)
                {
                    buffer[i] = (rowb[j++]);
                }
            }
            else
            { // 16 bitspc
                for (i = 0, j = 1; i < ImgInfo.SamplesPerRowP; i++)
                {
                    buffer[i] = (rowb[j++] << 8) + rowb[j++];
                }
            }
        }



        //Reads last Internally called after having read the last line. It reads extra chunks after IDAT, if present.
        private void ReadLastAndClose()
        {
            offset = (int)iIdatCstream.GetOffset();
            idatIstream.Close();
            ReadLastChunks();
            if (shouldCloseStream)
                inputStream.Close();
        }


        /// <summary>
        /// Reads (and processes ... up to a point) chunks after last IDAT.
        /// </summary>
        ///
        private void ReadLastChunks()
        {
            currentChunkGroup = ChunksList.CHUNK_GROUP_5_AFTERIDAT;
            // PngHelper.logdebug("idat ended? " + iIdatCstream.isEnded());
            if (!iIdatCstream.IsEnded())
                iIdatCstream.ForceChunkEnd();
            int clen = iIdatCstream.GetLenLastChunk();
            byte[] chunkid = iIdatCstream.GetIdLastChunk();
            bool endfound = false;
            bool first = true;
            bool ignore = false;
            while (!endfound)
            {
                ignore = false;
                if (!first)
                {
                    clen = PngHelperInternal.ReadInt4(inputStream);
                    offset += 4;
                    if (clen < 0)
                        throw new PngjInputException("bad len " + clen);
                    PngHelperInternal.ReadBytes(inputStream, chunkid, 0, 4);
                    offset += 4;
                }
                first = false;
                if (PngCsUtils.arraysEqual4(chunkid, ChunkHelper.b_IDAT))
                {
                    // PngHelper.logdebug("extra IDAT chunk len - ignoring : ");
                    ignore = true;
                }
                else if (PngCsUtils.arraysEqual4(chunkid, ChunkHelper.b_IEND))
                {
                    currentChunkGroup = ChunksList.CHUNK_GROUP_6_END;
                    endfound = true;
                }
                ChunkRaw chunk = new ChunkRaw(clen, chunkid, true);
                String chunkids = ChunkHelper.ToString(chunkid);
                bool loadchunk = ChunkHelper.ShouldLoad(chunkids, chunkLoadBehaviour);
                offset += chunk.ReadChunkData(inputStream);
                if (loadchunk && !ignore)
                {
                    AddChunkToList(chunk);
                }
            }
            if (!endfound)
                throw new PngjInputException("end chunk not found - offset=" + offset);
            // PngHelper.logdebug("end chunk found ok offset=" + offset);
        }

        private void UnfilterRow()
        {
            int ftn = rowbfilter[0];
            FilterType ft = (FilterType)ftn;
            switch (ft)
            {
                case Ar.Com.Hjg.Pngcs.FilterType.FILTER_NONE:
                    UnfilterRowNone();
                    break;
                case Ar.Com.Hjg.Pngcs.FilterType.FILTER_SUB:
                    UnfilterRowSub();
                    break;
                case Ar.Com.Hjg.Pngcs.FilterType.FILTER_UP:
                    UnfilterRowUp();
                    break;
                case Ar.Com.Hjg.Pngcs.FilterType.FILTER_AVERAGE:
                    UnfilterRowAverage();
                    break;
                case Ar.Com.Hjg.Pngcs.FilterType.FILTER_PAETH:
                    UnfilterRowPaeth();
                    break;
                default:
                    throw new PngjInputException("Filter type " + ftn + " not implemented");
            }
        }

        private void UnfilterRowNone()
        {
            for (int i = 1; i <= ImgInfo.BytesPerRow; i++)
            {
                rowb[i] = (byte)(rowbfilter[i]);
            }
        }

        private void UnfilterRowSub()
        {
            int i, j;
            for (i = 1; i <= ImgInfo.BytesPixel; i++)
            {
                rowb[i] = (byte)(rowbfilter[i]);
            }
            for (j = 1, i = ImgInfo.BytesPixel + 1; i <= ImgInfo.BytesPerRow; i++, j++)
            {
                rowb[i] = (byte)(rowbfilter[i] + rowb[j]);
            }
        }

        private void UnfilterRowUp()
        {
            for (int i = 1; i <= ImgInfo.BytesPerRow; i++)
            {
                rowb[i] = (byte)(rowbfilter[i] + rowbprev[i]);
            }
        }

        private void UnfilterRowAverage()
        {
            int i, j, x;
            for (j = 1 - ImgInfo.BytesPixel, i = 1; i <= ImgInfo.BytesPerRow; i++, j++)
            {
                x = (j > 0) ? rowb[j] : 0;
                rowb[i] = (byte)(rowbfilter[i] + (x + (rowbprev[i] & 0xFF)) / 2);
            }
        }

        private void UnfilterRowPaeth()
        {
            int i, j, x, y;
            for (j = 1 - ImgInfo.BytesPixel, i = 1; i <= ImgInfo.BytesPerRow; i++, j++)
            {
                x = (j > 0) ? rowb[j] : 0;
                y = (j > 0) ? rowbprev[j] : 0;
                rowb[i] = (byte)(rowbfilter[i] + PngHelperInternal.FilterPaethPredictor(x, rowbprev[i], y));
            }
        }

        /// <summary>
        /// Reads chunks before first IDAT. Position before: after IDHR (crc included)
        /// Position after: just after the first IDAT chunk id Returns length of first
        /// IDAT chunk , -1 if not found
        /// </summary>
        ///
        private void ReadFirstChunks()
        {
            if (!firstChunksNotYetRead())
                return;
            int clen = 0;
            bool found = false;
            byte[] chunkid = new byte[4]; // it's important to reallocate in each
            currentChunkGroup = ChunksList.CHUNK_GROUP_1_AFTERIDHR;
            while (!found)
            {
                clen = PngHelperInternal.ReadInt4(inputStream);
                offset += 4;
                if (clen < 0)
                    break;
                PngHelperInternal.ReadBytes(inputStream, chunkid, 0, 4);
                offset += 4;
                if (PngCsUtils.arraysEqual4(chunkid, Ar.Com.Hjg.Pngcs.Chunks.ChunkHelper.b_IDAT))
                {
                    found = true;
                    currentChunkGroup = ChunksList.CHUNK_GROUP_4_IDAT;
                    // add dummy idat chunk to list
                    ChunkRaw chunk = new ChunkRaw(0, chunkid, false);
                    AddChunkToList(chunk);
                    break;
                }
                else if (PngCsUtils.arraysEqual4(chunkid, Ar.Com.Hjg.Pngcs.Chunks.ChunkHelper.b_IEND))
                {
                    throw new PngjInputException("END chunk found before image data (IDAT) at offset=" + offset);
                }
                ChunkRaw chunk_0 = new ChunkRaw(clen, chunkid, true);
                String chunkids = ChunkHelper.ToString(chunkid);
                bool loadchunk = ChunkHelper.ShouldLoad(chunkids, chunkLoadBehaviour);
                offset += chunk_0.ReadChunkData(inputStream);
                if (loadchunk)
                    AddChunkToList(chunk_0);
                if (chunkids.Equals(ChunkHelper.PLTE))
                    currentChunkGroup = ChunksList.CHUNK_GROUP_3_AFTERPLTE;
            }
            int idatLen = found ? clen : -1;
            if (idatLen < 0)
                throw new PngjInputException("first idat chunk not found!");
            iIdatCstream = new PngIDatChunkInputStream(inputStream, idatLen, offset);
            idatIstream = new InflaterInputStream(iIdatCstream);
        }

        private bool firstChunksNotYetRead()
        {
            return currentChunkGroup < ChunksList.CHUNK_GROUP_1_AFTERIDHR;
        }

        /// Logs/prints a warning.
        /// The default behaviour is print to stderr, but it can be overriden.
        /// This happens rarely - most errors are fatal.
        protected virtual void logWarn(String warn)
        {
            Console.Error.WriteLine(warn);
        }

        /// <summary>
        /// calls readRow(int[] buffer, int nrow), usin LineImage as buffer
        /// </summary>
        ///
        /// <returns>the ImageLine that also is available inside this object</returns>
        public ImageLine ReadRow(int nrow)
        {
            ReadRow(imgLine.Scanline, nrow);
            imgLine.SetRown(nrow);
            return imgLine;
        }

        /// <summary>
        /// Reads a line and returns it as a int array Buffer can be prealocated (in
        /// this case it must have enough len!) or can be null See also the other
        /// overloaded method
        /// </summary>
        ///
        /// <param name="buffer"></param>
        /// <param name="nrow"></param>
        /// <returns>The same buffer if it was allocated, a newly allocated one
        /// otherwise</returns>
        public int[] ReadRow(int[] buffer, int nrow)
        {
            if (nrow < 0 || nrow >= ImgInfo.Rows)
                throw new PngjInputException("invalid line");
            if (nrow != rowNum + 1)
                throw new PngjInputException("invalid line (expected: " + (rowNum + 1));
            if (nrow == 0 && firstChunksNotYetRead())
                ReadFirstChunks();
            rowNum++;
            if (buffer == null || buffer.Length < ImgInfo.SamplesPerRowP)
                buffer = new int[ImgInfo.SamplesPerRowP];
            byte[] tmp = rowb;
            rowb = rowbprev;
            rowbprev = tmp;
            // loads in rowbfilter "raw" bytes, with filter
            PngHelperInternal.ReadBytes(idatIstream, rowbfilter, 0, rowbfilter.Length);
            rowb[0] = 0;
            UnfilterRow();
            rowb[0] = rowbfilter[0];
            ConvertRowFromBytes(buffer);
            // new: if last row, automatically call end()
            if (nrow == ImgInfo.Rows - 1)
                ReadLastAndClose();
            return buffer;
        }


        /**
         * Dummy method
         * <p>
         * Since version 0.88 (Apr 2012) the ending chunks are read automatically, internally, after reading the last row.
         * This does nothing now, just kept for backward compatibily
         */
        public void End()
        {

        }

        public ChunkLoadBehaviour getChunkLoadBehaviour()
        {
            return chunkLoadBehaviour;
        }


        public override String ToString()
        { // basic info
            return "filename=" + filename + " " + ImgInfo.ToString();
        }

        public void SetChunkLoadBehaviour(ChunkLoadBehaviour chunkLoadBehaviour)
        {
            this.chunkLoadBehaviour = chunkLoadBehaviour;
        }

        /**
         * Total maximum bytes to load from ancillary ckunks (default: 1Mb)
         * <p>
         * If exceeded, chunks will be ignored
         */
        public void SetMaxBytesChunksToLoad(int maxBytesChunksToLoad)
        {
            this.maxBytesChunksToLoad = maxBytesChunksToLoad;
        }

        public ChunksList ChunksList
        {
            get
            {
                if (firstChunksNotYetRead())
                    ReadFirstChunks();
                return chunksList;
            }
        }

        public PngMetadata Metadata
        {
            get
            {
                if (firstChunksNotYetRead())
                    ReadFirstChunks();
                return metadata;
            }
        }

        /**
         * if true, input stream will be closed after ending read 
         * <p>
         * default=true
         */
        public void SetShouldCloseStream(bool shouldCloseStream)
        {
            this.shouldCloseStream = shouldCloseStream;
        }

        /// <summary>
        /// for debug 
        /// </summary>
        ///
        public static void ShowLineInfo(ImageLine line)
        {
            System.Console.Out.WriteLine(line);
            Ar.Com.Hjg.Pngcs.ImageLineHelper.ImageLineStats stats = new ImageLineHelper.ImageLineStats(line);
            System.Console.Out.WriteLine(stats);
            System.Console.Out.WriteLine(Ar.Com.Hjg.Pngcs.ImageLineHelper.InfoFirstLastPixels(line));
        }

    }
}
