namespace Hjg.Pngcs.Chunks {

    using Hjg.Pngcs;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// tEXt chunk: latin1 uncompressed text
    /// </summary>
    public class PngChunkTEXT : PngChunkTextVar {
        public const String ID = ChunkHelper.tEXt;

        public PngChunkTEXT(ImageInfo info)
            : base(ID, info) {
        }

        public override ChunkRaw CreateRawChunk() {
            if (val.Length == 0 || key.Length == 0)
                return null;
            byte[] b = Hjg.Pngcs.PngHelperInternal.charsetLatin1.GetBytes(key + @"\0" + val);
            ChunkRaw chunk = createEmptyChunk(b.Length, false);
            chunk.Data = b;
            return chunk;
        }

        public override void ParseFromRaw(ChunkRaw c) {
            String[] k = Hjg.Pngcs.PngHelperInternal.charsetLatin1.GetString(c.Data).Split(new char[] { '\0' });
            key = k[0];
            val = k[1];
        }

        public override void CloneDataFromRead(PngChunk other) {
            PngChunkTEXT otherx = (PngChunkTEXT)other;
            key = otherx.key;
            val = otherx.val;
        }
    }
}
