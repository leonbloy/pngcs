using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ar.Com.Hjg.Pngcs.Chunks
{
    /**
 * We consider "image metadata" every info inside the image except for the most basic image info (IHDR chunk - ImageInfo
 * class) and the pixels values.
 * <p>
 * This includes the palette (if present) and all the ancillary chunks
 * <p>
 * This class provides a wrapper over the collection of chunks of a image (read or to write) and provides some high
 * level methods to access them
 */
    public class PngMetadata
    {
        private readonly ChunksList chunkList;
        private readonly bool ReadOnly; // readonly

        public PngMetadata(ChunksList chunks)
        {
            this.chunkList = chunks;
            if (chunks is ChunksListForWrite)
            {
                this.ReadOnly = false;
            }
            else
            {
                this.ReadOnly = true;
            }
        }


        /**
	 * Queues the chunk at the writer
	 * <p>
	 * lazyOverwrite: if true, checks if there is a queued "equivalent" chunk and if so, overwrites it. However if that
	 * not check for already written chunks.
	 */
        public void QueueChunk(PngChunk c, bool lazyOverwrite)
        {
            ChunksListForWrite cl = getChunkListW();
            if (ReadOnly)
                throw new PngjException("cannot set chunk : readonly metadata");
            if (lazyOverwrite)
            {

                ChunkHelper.TrimList(cl.GetQueuedChunks(), new ChunkPredicateEquiv(c));
            }
            cl.queue(c);
        }

        public void QueueChunk(PngChunk c)
        {
            QueueChunk(c, true);
        }

        private ChunksListForWrite getChunkListW()
        {
            return (ChunksListForWrite)chunkList;
        }

        // ///// high level utility methods follow ////////////

        // //////////// DPI

        /**
         * returns -1 if not found or dimension unknown
         */
        public double[] GetDpi()
        {
            PngChunk c = chunkList.GetById1(ChunkHelper.pHYs, true);
            if (c == null)
                return new double[] { -1, -1 };
            else
                return ((PngChunkPHYS)c).GetAsDpi2();
        }

        public void SetDpi(double x)
        {
            SetDpi(x, x);
        }

        public void SetDpi(double x, double y)
        {
            PngChunkPHYS c = new PngChunkPHYS(chunkList.imageInfo);
            c.SetAsDpi2(x, y);
            QueueChunk(c);
        }

        // //////////// TIME

        /**
         * Creates a time chunk with current time, less secsAgo seconds
         * <p>
         * 
         * @return Returns the created-queued chunk, just in case you want to examine or modify it
         */
        public PngChunkTIME setTimeNow(int secsAgo)
        {
            PngChunkTIME c = new PngChunkTIME(chunkList.imageInfo);
            c.SetNow(secsAgo);
            QueueChunk(c);
            return c;
        }

        public PngChunkTIME SetTimeNow()
        {
            return setTimeNow(0);
        }

        /**
         * Creates a time chunk with diven date-time
         * <p>
         * 
         * @return Returns the created-queued chunk, just in case you want to examine or modify it
         */
        public PngChunkTIME SetTimeYMDHMS(int yearx, int monx, int dayx, int hourx, int minx, int secx)
        {
            PngChunkTIME c = new PngChunkTIME(chunkList.imageInfo);
            c.SetYMDHMS(yearx, monx, dayx, hourx, minx, secx);
            QueueChunk(c, true);
            return c;
        }

        /**
         * null if not found
         */
        public PngChunkTIME GetTime()
        {
            return (PngChunkTIME)chunkList.GetById1(ChunkHelper.tIME);
        }

        public String GetTimeAsString()
        {
            PngChunkTIME c = GetTime();
            return c == null ? "" : c.GetAsString();
        }

        // //////////// TEXT

        /**
         * Creates a text chunk and queue it.
         * <p>
         * 
         * @param k
         *            : key (latin1)
         * @param val
         *            (arbitrary, should be latin1 if useLatin1)
         * @param useLatin1
         * @param compress
         * @return Returns the created-queued chunks, just in case you want to examine, touch it
         */
        public PngChunkTextVar SetText(String k, String val, bool useLatin1, bool compress)
        {
            if (compress && !useLatin1)
                throw new PngjException("cannot compress non latin text");
            PngChunkTextVar c;
            if (useLatin1)
            {
                if (compress)
                {
                    c = new PngChunkZTXT(chunkList.imageInfo);
                }
                else
                {
                    c = new PngChunkTEXT(chunkList.imageInfo);
                }
            }
            else
            {
                c = new PngChunkITXT(chunkList.imageInfo);
                ((PngChunkITXT)c).SetLangtag(k); // we use the same orig tag (this is not quite right)
            }
            c.SetKeyVal(k, val);
            QueueChunk(c, true);
            return c;
        }

        public PngChunkTextVar SetText(String k, String val)
        {
            return SetText(k, val, false, false);
        }

        /**
         * gets all text chunks with a given key
         * <p>
         * returns null if not found
         * <p>
         * Warning: this does not check the "lang" key of iTxt
         */
        public List<PngChunkTextVar> GetTxtsForKey(String k)
        {
            List<PngChunkTextVar> li = new List<PngChunkTextVar>();
            foreach (PngChunk c in chunkList.GetById(ChunkHelper.tEXt, k))
                li.Add((PngChunkTextVar)c);
            foreach (PngChunk c in chunkList.GetById(ChunkHelper.zTXt, k))
                li.Add((PngChunkTextVar)c);
            foreach (PngChunk c in chunkList.GetById(ChunkHelper.iTXt, k))
                li.Add((PngChunkTextVar)c);
            return li;
        }

        /**
         * Returns empty if not found, concatenated (with newlines) if multiple! - and trimmed
         * <p>
         * Use getTxtsForKey() if you don't want this behaviour
         */
        public String GetTxtForKey(String k)
        {
            String t = "";
            List<PngChunkTextVar> li = GetTxtsForKey(k);
            if (li.Count == 0)
                return t;
            foreach (PngChunkTextVar c in li)
                t = t + c.GetVal() + "\n";
            return t.Trim();
        }
    }
}
