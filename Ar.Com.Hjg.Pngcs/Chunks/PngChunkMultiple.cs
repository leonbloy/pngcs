using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ar.Com.Hjg.Pngcs.Chunks
{
    public abstract class PngChunkMultiple : PngChunk
    {
        internal PngChunkMultiple(String id, ImageInfo imgInfo)
            : base(id, imgInfo)
        {
            
        }

        public override bool AllowsMultiple() {
	        return true;
	    }
     
    }
}
