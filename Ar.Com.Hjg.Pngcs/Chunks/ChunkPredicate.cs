using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ar.Com.Hjg.Pngcs.Chunks
{
    public interface ChunkPredicate
    {
            bool Matches(PngChunk c);
    }
}
