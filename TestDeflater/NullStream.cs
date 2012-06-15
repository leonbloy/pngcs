using System;
using System.Collections.Generic;
using System.Text;

namespace TestDeflater
{
    public class NullStream : System.IO.Stream
    {
        private int cont = 0;
        public override bool CanRead
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            cont += count;
        }

        public override void WriteByte(byte value)
        {
            cont++;
        }

        public override long Length
        {
            get { return cont; }
        }

        public override long Position
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Flush()
        {
            
        }

        public override long Seek(long offset, System.IO.SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override bool CanSeek
        {
            get { throw new NotImplementedException(); }
        }
    }

}
