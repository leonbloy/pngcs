namespace Ar.Com.Hjg.Pngcs.Chunks {
	
	using Ar.Com.Hjg.Pngcs;
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.IO;
	using System.Runtime.CompilerServices;
	
	/*
	 */
	public class PngChunkBKGD : PngChunkSingle {
        public const String ID = ChunkHelper.bKGD;
		// http://www.w3.org/TR/PNG/#11bKGD
		// this chunk structure depends on the image type
		// only one of these is meaningful
		private int gray;
		private int red, green, blue;
		private int paletteIndex;
	
		public PngChunkBKGD(ImageInfo info) : base(ID, info) {
		}
        public override ChunkOrderingConstraint GetOrderingConstraint()
        {
            return ChunkOrderingConstraint.AFTER_PLTE_BEFORE_IDAT;
        }

        public override ChunkRaw CreateRawChunk()
        {
			ChunkRaw c = null;
			if (ImgInfo.Greyscale) {
				c = createEmptyChunk(2, true);
				Ar.Com.Hjg.Pngcs.PngHelperInternal.WriteInt2tobytes(gray, c.Data, 0);
			} else if (ImgInfo.Indexed) {
				c = createEmptyChunk(1, true);
				c.Data[0] = (byte) paletteIndex;
			} else {
				c = createEmptyChunk(6, true);
				PngHelperInternal.WriteInt2tobytes(red, c.Data, 0);
				Ar.Com.Hjg.Pngcs.PngHelperInternal.WriteInt2tobytes(green, c.Data, 0);
				Ar.Com.Hjg.Pngcs.PngHelperInternal.WriteInt2tobytes(blue, c.Data, 0);
			}
			return c;
		}
	
		public override void ParseFromRaw(ChunkRaw c) {
			if (ImgInfo.Greyscale) {
				gray = Ar.Com.Hjg.Pngcs.PngHelperInternal.ReadInt2fromBytes(c.Data, 0);
			} else if (ImgInfo.Indexed) {
				paletteIndex = (int) (c.Data[0] & 0xff);
			} else {
				red = Ar.Com.Hjg.Pngcs.PngHelperInternal.ReadInt2fromBytes(c.Data, 0);
				green = Ar.Com.Hjg.Pngcs.PngHelperInternal.ReadInt2fromBytes(c.Data, 2);
				blue = Ar.Com.Hjg.Pngcs.PngHelperInternal.ReadInt2fromBytes(c.Data, 4);
			}
		}
	
		public override void CloneDataFromRead(PngChunk other) {
			PngChunkBKGD otherx = (PngChunkBKGD) other;
			gray = otherx.gray;
			red = otherx.red;
			green = otherx.red;
			blue = otherx.red;
			paletteIndex = otherx.paletteIndex;
		}


        /**
	 * Set gray value (0-255 if bitdept=8)
	 * 
	 * @param gray
	 */
        public void setGray(int gray)
        {
            if (!ImgInfo.Greyscale)
                throw new PngjException("only gray images support this");
            this.gray = gray;
        }

        public int getGray()
        {
            if (!ImgInfo.Greyscale)
                throw new PngjException("only gray images support this");
            return gray;
        }

        /**
         * Set pallette index
         * 
         */
        public void setPaletteIndex(int i)
        {
            if (!ImgInfo.Indexed)
                throw new PngjException("only indexed (pallete) images support this");
            this.paletteIndex = i;
        }

        public int getPaletteIndex()
        {
            if (!ImgInfo.Indexed)
                throw new PngjException("only indexed (pallete) images support this");
            return paletteIndex;
        }

        /**
         * Set rgb values
         * 
         */
        public void setRGB(int r, int g, int b)
        {
            if (ImgInfo.Greyscale || ImgInfo.Indexed)
                throw new PngjException("only rgb or rgba images support this");
            red = r;
            green = g;
            blue = b;
        }

        public int[] getRGB()
        {
            if (ImgInfo.Greyscale || ImgInfo.Indexed)
                throw new PngjException("only rgb or rgba images support this");
            return new int[] { red, green, blue };
        }

	}
}
