using System;
using System.Collections.Generic;
using System.Text;

namespace Ar.Com.Hjg.Pngcs
{
    interface IBytesConsumer
    {
        int consume(byte[] buf, int offset, int len);
    }
}
