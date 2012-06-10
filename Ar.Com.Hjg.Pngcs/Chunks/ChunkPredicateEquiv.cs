using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ar.Com.Hjg.Pngcs.Chunks
{
    // match if are "equivalent"
    public class ChunkPredicateEquiv : ChunkPredicate
    {

        private readonly PngChunk cori;
        public ChunkPredicateEquiv(PngChunk cori)
        {
            this.cori = cori;
        }
        public bool Matches(PngChunk c)
        {
            return ChunkHelper.Equivalent(c, cori);
        }
    }

}
