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
	public class PngChunkCHRM : PngChunk {
		// 	// http://www.w3.org/TR/PNG/#11cHRM

        private double whitex,whitey;
	private double redx,redy;
	private double greenx,greeny;
	private double bluex,bluey;

    public PngChunkCHRM(ImageInfo info)
        : base(Ar.Com.Hjg.Pngcs.Chunks.ChunkHelper.cHRM_TEXT, info)
    {
		}
	
		public override ChunkRaw CreateChunk() {
            ChunkRaw c = null;
            c = CreateEmptyChunk(32, true);
            PngHelper.WriteInt4tobytes(PngHelper.DoubleToInt100000(whitex), c.data, 0);
            PngHelper.WriteInt4tobytes(PngHelper.DoubleToInt100000(whitey), c.data, 4);
            PngHelper.WriteInt4tobytes(PngHelper.DoubleToInt100000(redx), c.data, 8);
            PngHelper.WriteInt4tobytes(PngHelper.DoubleToInt100000(redy), c.data, 12);
            PngHelper.WriteInt4tobytes(PngHelper.DoubleToInt100000(greenx), c.data, 16);
            PngHelper.WriteInt4tobytes(PngHelper.DoubleToInt100000(greeny), c.data, 20);
            PngHelper.WriteInt4tobytes(PngHelper.DoubleToInt100000(bluex), c.data, 24);
            PngHelper.WriteInt4tobytes(PngHelper.DoubleToInt100000(bluey), c.data, 28);
            return c;
		}
	
		public override void ParseFromChunk(ChunkRaw c) {
            if (c.len != 32)
                throw new PngjException("bad chunk " + c);
            whitex = PngHelper.IntToDouble100000(PngHelper.ReadInt4fromBytes(c.data, 0));
            whitey = PngHelper.IntToDouble100000(PngHelper.ReadInt4fromBytes(c.data, 4));
            redx = PngHelper.IntToDouble100000(PngHelper.ReadInt4fromBytes(c.data, 8));
            redy = PngHelper.IntToDouble100000(PngHelper.ReadInt4fromBytes(c.data, 12));
            greenx = PngHelper.IntToDouble100000(PngHelper.ReadInt4fromBytes(c.data, 16));
            greeny = PngHelper.IntToDouble100000(PngHelper.ReadInt4fromBytes(c.data, 20));
            bluex = PngHelper.IntToDouble100000(PngHelper.ReadInt4fromBytes(c.data, 24));
            bluey = PngHelper.IntToDouble100000(PngHelper.ReadInt4fromBytes(c.data, 28));
        }
	
		public override void CloneDataFromRead(PngChunk other) {
            PngChunkCHRM otherx = (PngChunkCHRM)other;
            whitex = otherx.whitex;
            whitey = otherx.whitex;
            redx = otherx.redx;
            redy = otherx.redy;
            greenx = otherx.greenx;
            greeny = otherx.greeny;
            bluex = otherx.bluex;
            bluey = otherx.bluey;
        }
	
	}
}
