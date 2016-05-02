namespace Hjg.Pngcs
{

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// stream that outputs to memory and allows to flush fragments every 'size'
    /// bytes to some other destination
    /// </summary>
    ///
    abstract internal class ProgressiveOutputStream : MemoryStream
    {
        private readonly int size;
        private long countFlushed = 0;

        public ProgressiveOutputStream(int size_0)
        {
            this.size = size_0;
            if (size < 8) throw new PngjException("bad size for ProgressiveOutputStream: " + size);
        }

#if PORTABLE
        public virtual void Close()
        {
#else
        public override void Close()
        {
#endif
            Flush();
            base.Dispose();
        }

        public override void Flush()
        {
            base.Flush();
            CheckFlushBuffer(true);
        }

        public override void Write(byte[] b, int off, int len)
        {
            base.Write(b, off, len);
            CheckFlushBuffer(false);
        }

        public void Write(byte[] b)
        {
            Write(b, 0, b.Length);
            CheckFlushBuffer(false);
        }


        /// <summary>
        /// if it's time to flush data (or if forced==true) calls abstract method
        /// flushBuffer() and cleans those bytes from own buffer
        /// </summary>
        ///
        private void CheckFlushBuffer(bool forced)
        {
            int count = (int)Position;

            while (forced || count >= size)
            {
                int nb = size;
                if (nb > count)
                    nb = count;
                if (nb == 0)
                    return;

                byte[] buf = ToArray();

                FlushBuffer(buf, nb);
                countFlushed += nb;
                int bytesleft = count - nb;
                count = bytesleft;

                Position = 0;
                if (bytesleft > 0)
                    base.Write(buf, nb, bytesleft);
            }
        }

        protected abstract void FlushBuffer(byte[] b, int n);

        public long GetCountFlushed()
        {
            return countFlushed;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.Close();
                // Free any other managed objects here.
                //
            }

            // Free any unmanaged objects here.
            //

            // Call base class implementation.
            base.Dispose(disposing);
        }
    }
}
