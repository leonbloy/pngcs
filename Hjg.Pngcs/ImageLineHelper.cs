namespace Hjg.Pngcs {

    using Hjg.Pngcs.Chunks;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Bunch of utility static methods to process/analyze an image line. 
    /// 
    /// Not essential at all, some methods are probably to be removed if future releases.
    /// </summary>
    ///
    public class ImageLineHelper {
        private const double BIG_VALUE = System.Double.MaxValue * 0.5d;
        private const double BIG_VALUE_NEG = System.Double.MaxValue * (-0.5d);

        /// <summary>
        /// Given an indexed line with a palette, unpacks as a RGB array
        /// </summary>
        /// <param name="line">ImageLine as returned from PngReader</param>
        /// <param name="pal">Palette chunk</param>
        /// <param name="buf">Preallocated array, optional</param>
        /// <returns>R G B (one byte per sample)</returns>
        public int[] PalIdx2RGB(ImageLine line, PngChunkPLTE pal, int[] buf) {
            // TODO: test! Add alpha palette info?
            int nbytes = line.ImgInfo.Cols * 3;
            if (buf == null || buf.Length < nbytes) buf = new int[nbytes];
            int[] src; // from where to read the indexes as bytes
            if (line.ImgInfo.Packed) { // requires unpacking
                line.Unpack(buf, false); // use buf temporarily (have space)
                src = buf;
            } else {
                src = line.Scanline;
            }
            for (int c = line.ImgInfo.Cols - 1; c >= 0; c--) {
                // scan from right to left to not overwrite myself  
                pal.GetEntryRgb(src[c], buf, c * 3);
            }
            return buf;
        }

        /** what follows is pretty uninteresting/untested/obsolete, subject to change */
        /// <summary>
        /// Just for basic info or debugging. Shows values for first and last pixel.
        /// Does not include alpha
        /// </summary>
        ///
        public static String InfoFirstLastPixels(ImageLine line) {
            return "not implemented";
            //return (line.imgInfo.channels == 1) ? ILOG.J2CsMapping.Util.StringUtil.Format("first=(%d) last=(%d)",line.scanline[0],line.scanline[line.scanline.Length - 1]) : ILOG.J2CsMapping.Util.StringUtil.Format("first=(%d %d %d) last=(%d %d %d)",line.scanline[0],line.scanline[1],line.scanline[2],line.scanline[line.scanline.Length - line.imgInfo.channels],line.scanline[line.scanline.Length - line.imgInfo.channels + 1],line.scanline[line.scanline.Length - line.imgInfo.channels + 2]);
        }

        public static String InfoFull(ImageLine line) {
            ImageLineHelper.ImageLineStats stats = new ImageLineHelper.ImageLineStats(line);
            return "row=" + line.GetRown() + " " + stats.ToString() + "\n  "
                    + InfoFirstLastPixels(line);
        }

        /// <summary>
        /// Computes some statistics for the line. Not very efficient or elegant,
        /// mainly for tests. Only for RGB/RGBA Outputs values as doubles (0.0 - 1.0)
        /// </summary>
        ///
        public class ImageLineStats {
            public double[] prom; // channel averages
            public double[] maxv; // maximo
            public double[] minv;
            public double promlum; // maximum global (luminance)
            public double maxlum; // max luminance
            public double minlum;
            public double[] maxdif; // maxima
            public readonly int channels; // diferencia

            public override String ToString() {
                return "not implemented !!!asdasd456y";
                /*return (channels == 3) ? ILOG.J2CsMapping.Util.StringUtil.Format("prom=%.1f (%.1f %.1f %.1f) max=%.1f (%.1f %.1f %.1f) min=%.1f (%.1f %.1f %.1f)",promlum,prom[0],prom[1],prom[2],maxlum,maxv[0],maxv[1],maxv[2],minlum,minv[0],minv[1],minv[2])
                        + ILOG.J2CsMapping.Util.StringUtil.Format(" maxdif=(%.1f %.1f %.1f)",maxdif[0],maxdif[1],maxdif[2])
                        : ILOG.J2CsMapping.Util.StringUtil.Format("prom=%.1f (%.1f %.1f %.1f %.1f) max=%.1f (%.1f %.1f %.1f %.1f) min=%.1f (%.1f %.1f %.1f %.1f)",promlum,prom[0],prom[1],prom[2],prom[3],maxlum,maxv[0],maxv[1],maxv[2],maxv[3],minlum,minv[0],minv[1],minv[2],minv[3])
                                + ILOG.J2CsMapping.Util.StringUtil.Format(" maxdif=(%.1f %.1f %.1f %.1f)",maxdif[0],maxdif[1],maxdif[2],maxdif[3]);
                 */
            }

            public ImageLineStats(ImageLine line) {
                this.prom = new double[] { 0.0d, 0.0d, 0.0d, 0.0d };
                this.maxv = new double[] { Hjg.Pngcs.ImageLineHelper.BIG_VALUE_NEG, Hjg.Pngcs.ImageLineHelper.BIG_VALUE_NEG, Hjg.Pngcs.ImageLineHelper.BIG_VALUE_NEG, Hjg.Pngcs.ImageLineHelper.BIG_VALUE_NEG };
                this.minv = new double[] { Hjg.Pngcs.ImageLineHelper.BIG_VALUE, Hjg.Pngcs.ImageLineHelper.BIG_VALUE, Hjg.Pngcs.ImageLineHelper.BIG_VALUE, Hjg.Pngcs.ImageLineHelper.BIG_VALUE };
                this.promlum = 0.0d;
                this.maxlum = Hjg.Pngcs.ImageLineHelper.BIG_VALUE_NEG;
                this.minlum = Hjg.Pngcs.ImageLineHelper.BIG_VALUE;
                this.maxdif = new double[] { Hjg.Pngcs.ImageLineHelper.BIG_VALUE_NEG, Hjg.Pngcs.ImageLineHelper.BIG_VALUE_NEG, Hjg.Pngcs.ImageLineHelper.BIG_VALUE_NEG, Hjg.Pngcs.ImageLineHelper.BIG_VALUE };
                this.channels = line.channels;
                if (line.channels < 3)
                    throw new PngjException("ImageLineStats only works for RGB - RGBA");
                int ch = 0;
                double lum, x, d;
                for (int i = 0; i < line.ImgInfo.Cols; i++) {
                    lum = 0;
                    for (ch = channels - 1; ch >= 0; ch--) {
                        x = Hjg.Pngcs.ImageLineHelper.Int2double(line, line.Scanline[i * channels]);
                        if (ch < 3)
                            lum += x;
                        prom[ch] += x;
                        if (x > maxv[ch])
                            maxv[ch] = x;
                        if (x < minv[ch])
                            minv[ch] = x;
                        if (i >= channels) {
                            d = Math.Abs(x - Hjg.Pngcs.ImageLineHelper.Int2double(line, line.Scanline[i - channels]));
                            if (d > maxdif[ch])
                                maxdif[ch] = d;
                        }
                    }
                    promlum += lum;
                    if (lum > maxlum)
                        maxlum = lum;
                    if (lum < minlum)
                        minlum = lum;
                }
                for (ch = 0; ch < channels; ch++) {
                    prom[ch] /= line.ImgInfo.Cols;
                }
                promlum /= (line.ImgInfo.Cols * 3.0d);
                maxlum /= 3.0d;
                minlum /= 3.0d;
            }
        }

        /// <summary>
        /// integer packed R G B only for bitdepth=8! (does not check!)
        /// </summary>
        ///
        public static int GetPixelRGB8(ImageLine line, int column) {
            int offset = column * line.channels;
            return (line.Scanline[offset] << 16) + (line.Scanline[offset + 1] << 8)
                    + (line.Scanline[offset + 2]);
        }

        public static int GetPixelARGB8(ImageLine line, int column) {
            int offset = column * line.channels;
            return (line.Scanline[offset + 3] << 24) + (line.Scanline[offset] << 16)
                    + (line.Scanline[offset + 1] << 8) + (line.Scanline[offset + 2]);
        }

        public static void SetPixelsRGB8(ImageLine line, int[] rgb) {
            for (int i = 0; i < line.ImgInfo.Cols; i++) {
                line.Scanline[i * line.channels] = ((rgb[i] & 0xFF0000) >> 16);
                line.Scanline[i * line.channels + 1] = ((rgb[i] & 0xFF00) >> 8);
                line.Scanline[i * line.channels + 2] = ((rgb[i] & 0xFF));
            }
        }

        public static void SetPixelRGB8(ImageLine line, int col, int rgb) {
            line.Scanline[col * line.channels] = ((rgb & 0xFF0000) >> 16);
            line.Scanline[col * line.channels + 1] = ((rgb & 0xFF00) >> 8);
            line.Scanline[col * line.channels + 2] = ((rgb & 0xFF));
        }

        public static void SetPixelRGB8(ImageLine line, int col, int r, int g, int b) {
            line.Scanline[col * line.channels] = r;
            line.Scanline[col * line.channels + 1] = g;
            line.Scanline[col * line.channels + 2] = b;
        }

        public static void SetValD(ImageLine line, int i, double d) {
            line.Scanline[i] = Double2int(line, d);
        }

        public static double Int2double(ImageLine line, int p) {
            return (line.bitDepth == 16) ? p / 65535.0d : p / 255.0d;
            // TODO: replace my multiplication? check for other bitdepths
        }

        public static double Int2doubleClamped(ImageLine line, int p) {
            // TODO: replace my multiplication?
            double d = (line.bitDepth == 16) ? p / 65535.0d : p / 255.0d;
            return (d <= 0.0d) ? (double)(0) : (double)((d >= 1.0d ? 1.0d : d));
        }

        public static int Double2int(ImageLine line, double d) {
            d = (d <= 0.0d) ? (double)(0) : (double)((d >= 1.0d ? 1.0d : d));
            return (line.bitDepth == 16) ? (int)(d * 65535.0d + 0.5d) : (int)(d * 255.0d + 0.5d); //
        }

        public static int Double2intClamped(ImageLine line, double d) {
            d = (d <= 0.0d) ? (double)(0) : (double)((d >= 1.0d ? 1.0d : d));
            return (line.bitDepth == 16) ? (int)(d * 65535.0d + 0.5d) : (int)(d * 255.0d + 0.5d); //
        }
    }
}
