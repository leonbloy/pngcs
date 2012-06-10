using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ar.Com.Hjg.Pngcs.Chunks
{
    // match if have same id and, if Text (or SPLT) if have the asame key
    public class ChunkPredicateId2 : ChunkPredicate
    {

        private readonly string id;
        private readonly string innerid;
        public ChunkPredicateId2(string id, string inner)
        {
            this.id = id;
            this.innerid = inner;
        }
        public bool Matches(PngChunk c)
        {
            if (!c.Id.Equals(id))
                return false;
            if (c is PngChunkTextVar && !((PngChunkTextVar)c).GetKey().Equals(innerid))
                return false;
            if (c is PngChunkSPLT && !((PngChunkSPLT)c).GetPalName().Equals(innerid))
                return false;

            return true;
        }

    }
}
