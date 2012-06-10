using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ar.Com.Hjg.Pngcs.Chunks
{
    public abstract class PngChunkSingle : PngChunk
    {
        internal PngChunkSingle(String id, ImageInfo imgInfo) : base(id,imgInfo)
        {
            
        }

        public override bool AllowsMultiple() {
	        return false;
	    }

       	public override int GetHashCode() {
		int prime = 31;
		int result = 1;
        result = prime * result + ((Id == null) ? 0 : Id.GetHashCode());
		return result;
	}



        // !!! check if hashCode / equals should be implemented
    }
}
