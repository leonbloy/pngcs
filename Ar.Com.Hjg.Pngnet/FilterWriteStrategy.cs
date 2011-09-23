using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ar.Com.Hjg.Pngcs
{
   /**
 * Manages the writer strategy for selecting the internal png "filter"
 */
    class FilterWriteStrategy
    {
        private static readonly int COMPUTE_STATS_EVERY_N_LINES = 8;

        private readonly ImageInfo imgInfo;
        private readonly PngFilterType configuredType; // can be negative (fin dout) 
        private PngFilterType currentType; // 0-4 
        private int lastRowTested = -1000000;
        private long[] lastSums = new long[5];
        private int discoverEachLines = -1;

        public FilterWriteStrategy(ImageInfo imgInfo, PngFilterType configuredType)
        {
            this.imgInfo = imgInfo;
            this.configuredType = configuredType;
            if (configuredType < 0)
            { // first guess
                if ((imgInfo.rows < 8 && imgInfo.cols < 8) || imgInfo.indexed
                        || imgInfo.bitDepth < 8)
                    currentType = PngFilterType.FILTER_NONE;
                else
                    currentType = PngFilterType.FILTER_PAETH;
            }
            else
            {
                currentType = configuredType;
            }
            if (configuredType == PngFilterType.FILTER_AGRESSIVE)
                discoverEachLines = COMPUTE_STATS_EVERY_N_LINES;
        }

        public bool shouldTestAll(int rown)
        {
            if (discoverEachLines > 0 && lastRowTested + discoverEachLines <= rown)
                return true;
            else
                return false;
        }

        public void fillResultsForFilter(int rown, PngFilterType type, long sum)
        {
            lastRowTested = rown;
            lastSums[(int)type] = sum;
            currentType = PngFilterType.FILTER_NULL;
        }

        public PngFilterType gimmeFilterType(int rown)
        {
            if (currentType == PngFilterType.FILTER_NULL)
            { // get better
                long bestsum = long.MaxValue;
                for (int i = 0; i < 5; i++)
                    if (lastSums[i] <= bestsum)
                    {
                        bestsum = lastSums[i];
                        currentType = (PngFilterType)i;
                    }
            }
            if (configuredType == PngFilterType.FILTER_ALTERNATE)
            {
                currentType = (PngFilterType)(((int)currentType + 1) % 5);
            }
            return currentType;
        }
    }
}

