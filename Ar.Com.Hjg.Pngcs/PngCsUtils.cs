namespace Ar.Com.Hjg.Pngcs
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Utility functions for C#
    /// </summary>
    ///
    public class PngCsUtils
    {
        public static bool arraysEqual4(byte[] ar1, byte[] ar2)
        {
            return (ar1[0] == ar2[0]) &&
                 (ar1[1] == ar2[1]) &&
                  (ar1[2] == ar2[2]) &&
                   (ar1[3] == ar2[3]);
        }

    }
}
