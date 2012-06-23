namespace Hjg.Pngcs {

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Runtime.CompilerServices;


    /// <summary>
    /// Lightweight wrapper for an image scanline, for read and write
    /// </summary>
    /// <remarks>It can be (usually it is) reused while iterating over the image lines
    /// See <c>scanline</c> field doc, to understand the format.
    ///</remarks>
    public class ImageLine {
        /// <summary>
        /// ImageInfo (readonly inmutable)
        /// </summary>
        public readonly ImageInfo ImgInfo;
        /// <summary>
        /// Samples of an image line
        /// </summary>
        /// <remarks>
        /// 
        /// The 'scanline' is an array of integers, corresponds to an image line (row)
        /// Except for 'packed' formats (gray/indexed with 1-2-4 bitdepth) each int is a
        /// "sample" (one for channel), (0-255 or 0-65535) in the respective PNG sequence
        /// sequence : (R G B R G B...) or (R G B A R G B A...) or (g g g ...) or ( i i i
        /// ) (palette index)
        /// 
        /// For bitdepth 1/2/4 , each element is a PACKED byte! To get an unpacked copy,
        /// see <c>Pack()</c> and its inverse <c>Unpack()</c>
        /// 
        /// To convert a indexed line to RGB balues, see ImageLineHelper.PalIdx2RGB()
        /// (cant do the reverse)
        /// </remarks>
        public readonly int[] Scanline;

        /// <summary>
        /// tracks the current row number (from 0 to rows-1)
        /// </summary>
        private int rown;
        internal readonly int channels; // copied from imgInfo, more handy
        internal readonly int bitDepth; // copied from imgInfo, more handy
        /// <summary>
        /// informational only ; filled by the reader
        /// </summary>
        public FilterType FilterUsed {get;set;}

        public ImageLine(ImageInfo iminfo) {
            this.rown = 0;
            this.ImgInfo = iminfo;
            channels = iminfo.Channels;
            Scanline = new int[iminfo.SamplesPerRowP];
            this.bitDepth = iminfo.BitDepth;
            this.FilterUsed = FilterType.FILTER_UNKNOWN;
        }

        /// <summary>
        /// Current row number
        /// </summary>
        /// <returns>Current row number (0 : Rows-1)</returns>
        public int GetRown() {
            return rown;
        }

        /// <summary>
        /// Increments current row number
        /// </summary>
        public void IncRown() {
            this.rown++;
        }

        /// <summary>
        /// Sets current row number
        /// </summary>
        /// <param name="rown">Current row number (0 : Rows-1)</param>
        public void SetRown(int rown) {
            this.rown = rown;
        }

        /// <summary>
        /// Makes a deep copy
        /// </summary>
        /// <remarks>You should rarely use this</remarks>
        /// <param name="b"></param>
        public void SetScanLine(int[] b) { // makes copy
            System.Array.Copy((Array)(b), 0, (Array)(Scanline), 0, Scanline.Length);
        }

        /// <summary>
        /// Makes a deep copy
        /// </summary>
        /// <remarks>You should rarely use this</remarks>
        /// <param name="b"></param>
        public int[] GetScanLineCopy(int[] b) {
            if (b == null || b.Length < Scanline.Length)
                b = new int[Scanline.Length];
            System.Array.Copy((Array)(Scanline), 0, (Array)(b), 0, Scanline.Length);
            return b;
        }

        /// <summary>
        /// Unpacks scanline 
        /// </summary>
        /// <remarks>
        /// This should be used for scanlines that pack more than one sample per byte
        /// (for bitdepth 1-2-4).
        /// 
        /// You can pass a preallocated array</remarks>
        /// <param name="buf">Preallocated array, can be null</param>
        /// <param name="scale">flag:  scale the values (bit shift) towards 0-255</param>
        /// <returns>Unpacked buffer, one sample per byte</returns>
        public int[] Unpack(int[] buf, bool scale) {
            int len = ImgInfo.SamplesPerRow;
            if (buf == null || buf.Length < len)
                buf = new int[len];
            if (bitDepth >= 8)
                System.Array.Copy((Array)(Scanline), 0, (Array)(buf), 0, Scanline.Length);
            else {
                int mask, offset, v;
                int mask0 = GetMaskForPackedFormats();
                int offset0 = 8 - bitDepth;
                mask = mask0;
                offset = offset0;
                for (int i = 0, j = 0; i < len; i++) {
                    v = (Scanline[j] & mask) >> offset;
                    if (scale)
                        v <<= offset0;
                    buf[i] = v;
                    mask = mask >> bitDepth;
                    offset -= bitDepth;
                    if (mask == 0) { // new byte in source
                        mask = mask0;
                        offset = offset0;
                        j++;
                    }
                }
            }
            return buf;
        }

        /// <summary>
        /// Reverse of Unpack
        /// </summary>
        /// <param name="buf">Preallocated array, can be null</param>
        /// <param name="scale">flag:  scale the values (bit shift) towards 0-255</param>
        public void Pack(int[] buf, bool scale) { // writes scanline
            int len = ImgInfo.SamplesPerRow;
            if (buf == null || buf.Length < len)
                buf = new int[len];
            if (bitDepth >= 8)
                System.Array.Copy((Array)(buf), 0, (Array)(Scanline), 0, Scanline.Length);
            else {
                int offset0 = 8 - bitDepth;
                int mask0 = GetMaskForPackedFormats() >> offset0;
                int offset, v;
                offset = offset0;
                Array.Clear(Scanline, 0, Scanline.Length);
                for (int i = 0, j = 0; i < len; i++) {
                    v = buf[i];
                    if (scale)
                        v >>= offset0;
                    v = (v & mask0) << offset;
                    Scanline[j] |= v;
                    offset -= bitDepth;
                    if (offset < 0) { // new byte in scanline
                        offset = offset0;
                        j++;
                    }
                }
            }
        }

        private int GetMaskForPackedFormats() { // Utility function for pack/unpack
            if (bitDepth == 1)
                return 0x80;
            if (bitDepth == 2)
                return 0xc0;
            if (bitDepth == 4)
                return 0xf0;
            throw new Exception("invalid bitDepth " + bitDepth);
        }

        public override String ToString() {
            return "row=" + rown + " cols=" + ImgInfo.Cols + " bpc=" + ImgInfo.BitDepth
                    + " size=" + Scanline.Length;
        }

        /// <summary>
        /// prints debugging info to console
        /// </summary>
        /// <param name="line"></param>
        public static void showLineInfo(ImageLine line) {
            Console.WriteLine(line.ToString());
            ImageLineHelper.ImageLineStats stats = new ImageLineHelper.ImageLineStats(line);
            Console.WriteLine(stats.ToString());
            Console.WriteLine(ImageLineHelper.InfoFirstLastPixels(line));
        }
    }
}
