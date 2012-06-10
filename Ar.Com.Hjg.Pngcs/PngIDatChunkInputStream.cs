namespace Ar.Com.Hjg.Pngcs
{

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Runtime.CompilerServices;
    using ICSharpCodePngcs.SharpZipLib.Checksums;

    /// <summary>
    /// Reads IDAT chunks
    /// </summary>
    ///
    internal class PngIDatChunkInputStream : Stream
    {
        private readonly Stream inputStream;
        private readonly Crc32 crcEngine;
        private int lenLastChunk;
        private byte[] idLastChunk;
        private int toReadThisChunk;
        private bool ended;
        private long offset; // offset inside inputstream

        // just informational
        public class IdatChunkInfo
        {
            public readonly int len;
            public readonly int offset;

            public IdatChunkInfo(int len_0, int offset_1)
            {
                this.len = len_0;
                this.offset = offset_1;
            }
        }

        public override void Write(byte[] buffer, int offset, int count) { }
        public override void SetLength(long value) { }
        public override long Seek(long offset, SeekOrigin origin) { return -1; }
        public override void Flush() { }
        public override long Position { get; set; }
        public override long Length { get { return 0; } }
        public override bool CanWrite { get { return false; } }
        public override bool CanRead { get { return true; } }
        public override bool CanSeek { get { return false; } }

        public IList<IdatChunkInfo> foundChunksInfo;

        /// <summary>
        /// Constructor must be called just after reading length and id of first IDAT
        /// chunk
        /// </summary>
        ///
        public PngIDatChunkInputStream(Stream iStream, int lenFirstChunk, int offset_0)
        {
            this.idLastChunk = new byte[4];
            this.toReadThisChunk = 0;
            this.ended = false;
            this.foundChunksInfo = new List<IdatChunkInfo>();
            this.offset = (long)offset_0;
            inputStream = iStream;
            crcEngine = new Crc32();
            this.lenLastChunk = lenFirstChunk;
            toReadThisChunk = lenFirstChunk;
            // we know it's a IDAT
            System.Array.Copy((Array)(Ar.Com.Hjg.Pngcs.Chunks.ChunkHelper.b_IDAT), 0, (Array)(idLastChunk), 0, 4);
            crcEngine.Update(idLastChunk, 0, 4);
            foundChunksInfo.Add(new PngIDatChunkInputStream.IdatChunkInfo(lenLastChunk, offset_0 - 8));
            // PngHelper.logdebug("IDAT Initial fragment: len=" + lenLastChunk);
            if (this.lenLastChunk == 0)
                EndChunkGoForNext(); // rare, but...
        }

        /// <summary>
        /// does NOT close the associated stream!
        /// </summary>
        ///
        public override void Close()
        {
            base.Close(); // nothing
        }

        private void EndChunkGoForNext()
        {
            // Called after readging the last byte of chunk
            // Checks CRC, and read ID from next CHUNK
            // Those values are left in idLastChunk / lenLastChunk
            // Skips empty IDATS
            do
            {
                int crc = Ar.Com.Hjg.Pngcs.PngHelperInternal.ReadInt4(inputStream); //
                offset += 4;
                int crccalc = (int)crcEngine.Value;
                if (lenLastChunk > 0 && crc != crccalc)
                    throw new PngjBadCrcException("error reading idat; offset: " + offset);
                crcEngine.Reset();
                lenLastChunk = Ar.Com.Hjg.Pngcs.PngHelperInternal.ReadInt4(inputStream);
                if (lenLastChunk < 0)
                    throw new PngjInputException("invalid len for chunk: " + lenLastChunk);
                toReadThisChunk = lenLastChunk;
                Ar.Com.Hjg.Pngcs.PngHelperInternal.ReadBytes(inputStream, idLastChunk, 0, 4);
                offset += 8;

                ended = !PngCsUtils.arraysEqual4(idLastChunk, Ar.Com.Hjg.Pngcs.Chunks.ChunkHelper.b_IDAT);
                if (!ended)
                {
                    foundChunksInfo.Add(new PngIDatChunkInputStream.IdatChunkInfo(lenLastChunk, (int)(offset - 8)));
                    crcEngine.Update(idLastChunk, 0, 4);
                }
                // PngHelper.logdebug("IDAT ended. next len= " + lenLastChunk + " idat?" +
                // (!ended));
            } while (lenLastChunk == 0 && !ended);
            // rarely condition is true (empty IDAT ??)
        }

        /// <summary>
        /// sometimes last row read does not fully consumes the chunk here we read the
        /// reamaing dummy bytes
        /// </summary>
        ///
        public void ForceChunkEnd()
        {
            if (!ended)
            {
                byte[] dummy = new byte[toReadThisChunk];
                Ar.Com.Hjg.Pngcs.PngHelperInternal.ReadBytes(inputStream, dummy, 0, toReadThisChunk);
                crcEngine.Update(dummy, 0, toReadThisChunk);
                EndChunkGoForNext();
            }
        }

        /// <summary>
        /// This can return less than len, but never 0 Returns -1 nothing more to read, -2 if "pseudo file" 
        /// ended prematurely. That is our error.
        /// </summary>
        ///
        public override int Read(byte[] b, int off, int len_0)
        {
            if (ended) return -1;
            if (toReadThisChunk == 0) throw new Exception("this should not happen");
            int n = inputStream.Read(b, off, (len_0 >= toReadThisChunk) ? toReadThisChunk : len_0);
            if (n == -1) n = -2;
            if (n > 0)
            {
                crcEngine.Update(b, off, n);
                this.offset += n;
                toReadThisChunk -= n;
            }
            if (n >= 0 && toReadThisChunk == 0)
            { // end of chunk: prepare for next
                EndChunkGoForNext();
            }
            return n;
        }

        public int Read(byte[] b)
        {
            return this.Read(b, 0, b.Length);
        }

        public override int ReadByte()
        {
            // PngHelper.logdebug("read() should go here");
            // inneficient - but this should be used rarely
            byte[] b1 = new byte[1];
            int r = this.Read(b1, 0, 1);
            return (r < 0) ? -1 : (int)b1[0];
        }

        public int GetLenLastChunk()
        {
            return lenLastChunk;
        }

        public byte[] GetIdLastChunk()
        {
            return idLastChunk;
        }

        public long GetOffset()
        {
            return offset;
        }

        public bool IsEnded()
        {
            return ended;
        }
    }
}
