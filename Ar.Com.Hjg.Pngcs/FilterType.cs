namespace Ar.Com.Hjg.Pngcs
{

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Internal PNG predictor filter, or strategy to select it.
    /// </summary>
    ///
    public enum FilterType
    {
        FILTER_NONE = 0,
        FILTER_SUB = 1, FILTER_UP = 2, FILTER_AVERAGE = 3,
        FILTER_PAETH = 4,
        FILTER_DEFAULT = -1, // Default strategy: select one of the above filters depending on global image parameters
        FILTER_AGGRESSIVE = -2,// Aggressive strategy: select one of the above filters trying each of the filters

        FILTER_ALTERNATE = -3,// Uses all fiters, one for lines, cyciclally. Only for tests.
        /// <summary>
        /// Very aggressive strategy: select one of the above filters trying each of the filters (this is done for every row!)
        /// </summary>
        FILTER_VERYAGGRESSIVE = -4,
        FILTER_NULL = -100
    }


}
