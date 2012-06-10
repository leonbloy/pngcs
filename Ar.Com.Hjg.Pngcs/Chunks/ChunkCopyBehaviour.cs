using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ar.Com.Hjg.Pngcs.Chunks
{
    public class ChunkCopyBehaviour
    {
        /** dont copy anywhing */
	public static readonly int COPY_NONE = 0;

	/** copy the palette */
	public static readonly int COPY_PALETTE = 1;

	/** copy all 'safe to copy' chunks */
	public static readonly int COPY_ALL_SAFE = 1 << 2;
	public static readonly int COPY_ALL = 1 << 3; // includes palette!
	public static readonly int COPY_PHYS = 1 << 4; // dpi
	public static readonly int COPY_TEXTUAL = 1 << 5; // all textual types
    public static readonly int COPY_TRANSPARENCY = 1 << 6; //
	public static readonly int COPY_UNKNOWN = 1 << 7; // all unknown (by the factory!)
	public static readonly int COPY_ALMOSTALL = 1 << 8; // almost all known (except HIST and TIME and textual)
    }
}
