using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ar.Com.Hjg.Pngcs.Chunks
{
    // match if have same id
    public class ChunkPredicateId : ChunkPredicate
    {
        private readonly string id;
        public ChunkPredicateId(String id)
        {
            this.id = id;
        }
        public bool Matches(PngChunk c)
        {
            return c.Id.Equals(id);
        }
    }
}
