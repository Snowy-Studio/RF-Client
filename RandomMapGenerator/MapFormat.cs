using System.Drawing;
using System.Text;

namespace RandomMapGenerator
{
    public class Overlay : NumberedMapObject
    {
        public byte OverlayID { get; set; }
        public byte OverlayValue { get; set; }
        public Overlay(byte overlayID, byte overlayValue)
        {
            OverlayID = overlayID;
            OverlayValue = overlayValue;
        }
        public override int Number
        {
            get { return OverlayID; }
            set { OverlayID = (byte)value; }
        }
        public Overlay Clone()
        {
            return (Overlay)this.MemberwiseClone();
        }
    }
    public class MemoryFile : VirtualFile
    {//定义见VirtualFile.cs,它的基类是Stream

        public MemoryFile(byte[] buffer, bool isBuffered = true) :
            base(new MemoryStream(buffer), "MemoryFile", 0, buffer.Length, isBuffered)
        { }// //TODO Calling the base class method?
           //MemoryStream:Creates a stream whose backing store is memory.
           //base是什么函数
    }
    public class VirtualFile : Stream
    {//Stream是一个抽象类。有写入、读取、查找三个功能
        public Stream BaseStream { get; internal protected set; }//TODO Returns the underlying stream.
        protected int BaseOffset;
        protected long Size;
        protected long Pos;
        virtual public string FileName { get; set; }
        //In C#, for overriding the base class method in a derived class, you have to declare a base class method as virtual and derived class method asoverride:

        byte[] _buff;
        readonly bool _isBuffered;
        bool _isBufferInitialized;

        public VirtualFile(Stream baseStream, string filename, int baseOffset, long fileSize, bool isBuffered = false)
        {
            Size = fileSize;
            BaseOffset = baseOffset;
            BaseStream = baseStream;
            _isBuffered = isBuffered;
            FileName = filename;
        }

        public VirtualFile(Stream baseStream, string filename = "", bool isBuffered = false)
        {//重载
            BaseStream = baseStream;
            BaseOffset = 0;
            Size = baseStream.Length;
            _isBuffered = isBuffered;
            FileName = filename;
        }

        public override bool CanRead
        {
            get { return Pos < Size; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override long Length
        {
            get { return Size; }
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            count = Math.Min(count, (int)(Length - Position));
            if (_isBuffered)
            {
                if (!_isBufferInitialized)
                    InitBuffer();

                Array.Copy(_buff, Pos, buffer, offset, count);
            }
            else
            {
                // ensure
                BaseStream.Position = BaseOffset + Pos;
                BaseStream.Read(buffer, offset, count);
            }
            Pos += count;
            return count;
        }

        public string ReadCString(int count)
        {
            var arr = Read(count);
            var sb = new StringBuilder();
            int i = 0;
            while (i < count && arr[i] != 0)
                sb.Append((char)arr[i++]);
            return sb.ToString();
        }

        public unsafe int Read(byte* buffer, int count)
        {
            count = Math.Min(count, (int)(Length - Position));
            if (_isBuffered)
            {
                if (!_isBufferInitialized)
                    InitBuffer();

                for (int i = 0; i < count; i++)
                    *buffer++ = _buff[Pos + i];
            }
            else
            {
                // ensure
                BaseStream.Position = BaseOffset + Pos;
                byte[] rbuff = new byte[count];
                BaseStream.Read(rbuff, 0, count);
                for (int i = 0; i < count; i++)
                    *buffer++ = rbuff[i];
            }
            Pos += count;
            return count;
        }

        private void InitBuffer()
        {
            // ensure
            BaseStream.Position = BaseOffset + Pos;
            _buff = new byte[Size];
            BaseStream.Read(_buff, 0, (int)Size);
            _isBufferInitialized = true;
        }

        public byte[] Read(int numBytes)
        {//read的定义
            var ret = new byte[numBytes];//TODO ret是一个numBytes长度的bytes数组，那它为什么作为参数输入到read里
            Read(ret, 0, numBytes);//public abstract int Read (byte[] buffer, int offset, int count);
                                   //Parameters
                                   //buffer Byte[]
                                   //An array of bytes. When this method returns, the buffer contains the specified byte array with the values between offset and (offset + count - 1) replaced by the bytes read from the current source.

            //offset Int32
            //The zero-based byte offset in buffer at which to begin storing the data read from the current stream.

            //count Int32
            //The maximum number of bytes to be read from the current stream.

            //Returns Int32
            //The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero (0) if the end of the stream has been reached.
            return ret;
        }

        public sbyte[] ReadSigned(int numBytes)
        {
            var b = new byte[numBytes];
            Read(b, 0, numBytes);
            sbyte[] ret = new sbyte[numBytes];
            Buffer.BlockCopy(b, 0, ret, 0, b.Length);
            return ret;
        }

        public new byte ReadByte()
        {
            return ReadUInt8();
        }

        public sbyte ReadSByte()
        {
            return unchecked((sbyte)ReadUInt8());
        }

        public byte ReadUInt8()
        {
            return Read(1)[0];
        }

        public int ReadInt32()
        {
            return BitConverter.ToInt32(Read(sizeof(Int32)), 0);
        }

        public uint ReadUInt32()
        {
            return BitConverter.ToUInt32(Read(sizeof(UInt32)), 0);
        }

        public short ReadInt16()
        {
            return BitConverter.ToInt16(Read(sizeof(Int16)), 0);
        }

        public ushort ReadUInt16()
        {
            return BitConverter.ToUInt16(Read(sizeof(UInt16)), 0);
        }

        public float ReadFloat()
        {
            return BitConverter.ToSingle(Read(sizeof(Single)), 0);
        }

        public float ReadFloat2()
        {
            var ori = Read(sizeof(Single)).ToList();
            byte[] rev = new[] { ori[3], ori[2], ori[1], ori[0] };
            return BitConverter.ToSingle(rev, 0);
        }

        public double ReadDouble()
        {
            return BitConverter.ToDouble(Read(sizeof(Double)), 0);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override void Close()
        {
            base.Close();
            BaseStream.Close();
        }

        public override void SetLength(long value)
        {
            Size = value;
        }

        public override long Position
        {
            get
            {
                return Pos;
            }
            set
            {
                Pos = value;
                if (!_isBuffered && Pos + BaseOffset != BaseStream.Position)
                    BaseStream.Seek(Pos + BaseOffset, SeekOrigin.Begin);
            }
        }

        public long Remaining
        {
            get { return Length - Pos; }
        }

        public bool Eof
        {
            get { return Remaining <= 0; }
        }

        public override bool CanSeek
        {
            get { return true; }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;
                case SeekOrigin.Current:
                    Position += offset;
                    break;
                case SeekOrigin.End:
                    Position = Length - offset;
                    break;
            }
            return Position;
        }

        public override string ToString()
        {
            return FileName;
        }
    }
    public static class Format80
    {
        private static void ReplicatePrevious(byte[] dest, int destIndex, int srcIndex, int count)
        {
            if (srcIndex > destIndex)
                throw new NotImplementedException(string.Format("srcIndex > destIndex  {0}  {1}", srcIndex, destIndex));

            if (destIndex - srcIndex == 1)
            {
                for (int i = 0; i < count; i++)
                    dest[destIndex + i] = dest[destIndex - 1];
            }
            else
            {
                for (int i = 0; i < count; i++)
                    dest[destIndex + i] = dest[srcIndex + i];
            }
        }

        public static int DecodeInto(byte[] src, byte[] dest)
        {
            VirtualFile ctx = new MemoryFile(src);
            int destIndex = 0;

            while (true)
            {
                byte i = ctx.ReadByte();
                if ((i & 0x80) == 0)
                {
                    // case 2
                    byte secondByte = ctx.ReadByte();
                    int count = ((i & 0x70) >> 4) + 3;
                    int rpos = ((i & 0xf) << 8) + secondByte;

                    ReplicatePrevious(dest, destIndex, destIndex - rpos, count);
                    destIndex += count;
                }
                else if ((i & 0x40) == 0)
                {
                    // case 1
                    int count = i & 0x3F;
                    if (count == 0)
                        return destIndex;

                    ctx.Read(dest, destIndex, count);
                    destIndex += count;
                }
                else
                {
                    int count3 = i & 0x3F;
                    if (count3 == 0x3E)
                    {
                        // case 4
                        int count = ctx.ReadInt16();
                        byte color = ctx.ReadByte();

                        for (int end = destIndex + count; destIndex < end; destIndex++)
                            dest[destIndex] = color;
                    }
                    else if (count3 == 0x3F)
                    {
                        // case 5
                        int count = ctx.ReadInt16();
                        int srcIndex = ctx.ReadInt16();
                        if (srcIndex >= destIndex)
                            throw new NotImplementedException(string.Format("srcIndex >= destIndex  {0}  {1}", srcIndex, destIndex));

                        for (int end = destIndex + count; destIndex < end; destIndex++)
                            dest[destIndex] = dest[srcIndex++];
                    }
                    else
                    {
                        // case 3
                        int count = count3 + 3;
                        int srcIndex = ctx.ReadInt16();
                        if (srcIndex >= destIndex)
                            throw new NotImplementedException(string.Format("srcIndex >= destIndex  {0}  {1}", srcIndex, destIndex));

                        for (int end = destIndex + count; destIndex < end; destIndex++)
                            dest[destIndex] = dest[srcIndex++];
                    }
                }
            }
        }

        public static unsafe uint DecodeInto(byte* src, byte* dest)
        {
            byte* pdest = dest;
            byte* readp = src;
            byte* writep = dest;

            while (true)
            {
                byte code = *readp++;
                byte* copyp;
                int count;
                if ((~code & 0x80) != 0)
                {
                    //bit 7 = 0
                    //command 0 (0cccpppp p): copy
                    count = (code >> 4) + 3;
                    copyp = writep - (((code & 0xf) << 8) + *readp++);
                    while (count-- != 0)
                        *writep++ = *copyp++;
                }
                else
                {
                    //bit 7 = 1
                    count = code & 0x3f;
                    if ((~code & 0x40) != 0)
                    {
                        //bit 6 = 0
                        if (count == 0)
                            //end of image
                            break;
                        //command 1 (10cccccc): copy
                        while (count-- != 0)
                            *writep++ = *readp++;
                    }
                    else
                    {
                        //bit 6 = 1
                        if (count < 0x3e)
                        {
                            //command 2 (11cccccc p p): copy
                            count += 3;
                            copyp = &pdest[*(ushort*)readp];

                            readp += 2;
                            while (count-- != 0)
                                *writep++ = *copyp++;
                        }
                        else if (count == 0x3e)
                        {
                            //command 3 (11111110 c c v): fill
                            count = *(ushort*)readp;
                            readp += 2;
                            code = *readp++;
                            while (count-- != 0)
                                *writep++ = code;
                        }
                        else
                        {
                            //command 4 (copy 11111111 c c p p): copy
                            count = *(ushort*)readp;
                            readp += 2;
                            copyp = &pdest[*(ushort*)readp];
                            readp += 2;
                            while (count-- != 0)
                                *writep++ = *copyp++;
                        }
                    }
                }
            }

            return (uint)(dest - pdest);
        }

        static int CountSame(byte[] src, int offset, int maxCount)
        {
            maxCount = Math.Min(src.Length - offset, maxCount);
            if (maxCount <= 0)
                return 0;

            var first = src[offset++];
            var count = 1;

            while (count < maxCount && src[offset++] == first)
                count++;

            return count;
        }

        static void WriteCopyBlocks(byte[] src, int offset, int count, MemoryStream output)
        {
            while (count > 0)
            {
                var writeNow = Math.Min(count, 0x3F);
                output.WriteByte((byte)(0x80 | writeNow));
                output.Write(src, offset, writeNow);

                count -= writeNow;
                offset += writeNow;
            }
        }

        // Quick and dirty Format80 encoder version 2
        // Uses raw copy and RLE compression
        public static byte[] Encode(byte[] src)
        {
            using (var ms = new MemoryStream())
            {
                var offset = 0;
                var left = src.Length;
                var blockStart = 0;

                while (offset < left)
                {
                    var repeatCount = CountSame(src, offset, 0xFFFF);
                    if (repeatCount >= 4)
                    {
                        // Write what we haven't written up to now
                        WriteCopyBlocks(src, blockStart, offset - blockStart, ms);

                        // Command 4: Repeat byte n times
                        ms.WriteByte(0xFE);
                        // Low byte
                        ms.WriteByte((byte)(repeatCount & 0xFF));
                        // High byte
                        ms.WriteByte((byte)(repeatCount >> 8));
                        // Value to repeat
                        ms.WriteByte(src[offset]);

                        offset += repeatCount;
                        blockStart = offset;
                    }
                    else
                    {
                        offset++;
                    }
                }

                // Write what we haven't written up to now
                WriteCopyBlocks(src, blockStart, offset - blockStart, ms);

                // Write terminator
                ms.WriteByte(0x80);

                return ms.ToArray();
            }
        }

    }
    public static class MiniLZO
    {

        unsafe static uint lzo1x_1_compress_core(byte* @in, uint in_len, byte* @out, ref uint out_len, uint ti, void* wrkmem)
        {
            byte* ip;
            byte* op;
            byte* in_end = @in + in_len;
            byte* ip_end = @in + in_len - 20;
            byte* ii;
            ushort* dict = (ushort*)wrkmem;
            op = @out;
            ip = @in;
            ii = ip;
            ip += ti < 4 ? 4 - ti : 0;

            byte* m_pos;
            uint m_off;
            uint m_len;

            for (; ; )
            {

                uint dv;
                uint dindex;
            literal:
                ip += 1 + ((ip - ii) >> 5);
            next:
                if (ip >= ip_end)
                    break;
                dv = (*(uint*)(void*)(ip));
                dindex = ((uint)(((((((uint)((0x1824429d) * (dv)))) >> (32 - 14))) & (((1u << (14)) - 1) >> (0))) << (0)));
                m_pos = @in + dict[dindex];
                dict[dindex] = ((ushort)((uint)((ip) - (@in))));
                if (dv != (*(uint*)(void*)(m_pos)))
                    goto literal;

                ii -= ti; ti = 0;
                {
                    uint t = ((uint)((ip) - (ii)));
                    if (t != 0)
                    {
                        if (t <= 3)
                        {
                            op[-2] |= ((byte)(t));
                            *(uint*)(op) = *(uint*)(ii);
                            op += t;
                        }
                        else if (t <= 16)
                        {
                            *op++ = ((byte)(t - 3));
                            *(uint*)(op) = *(uint*)(ii);
                            *(uint*)(op + 4) = *(uint*)(ii + 4);
                            *(uint*)(op + 8) = *(uint*)(ii + 8);
                            *(uint*)(op + 12) = *(uint*)(ii + 12);
                            op += t;
                        }
                        else
                        {
                            if (t <= 18)
                                *op++ = ((byte)(t - 3));
                            else
                            {
                                uint tt = t - 18;
                                *op++ = 0;
                                while (tt > 255)
                                {
                                    tt -= 255;
                                    *(byte*)op++ = 0;
                                }

                                *op++ = ((byte)(tt));
                            }
                            do
                            {
                                *(uint*)(op) = *(uint*)(ii);
                                *(uint*)(op + 4) = *(uint*)(ii + 4);
                                *(uint*)(op + 8) = *(uint*)(ii + 8);
                                *(uint*)(op + 12) = *(uint*)(ii + 12);
                                op += 16; ii += 16; t -= 16;
                            } while (t >= 16); if (t > 0) { do *op++ = *ii++; while (--t > 0); }
                        }
                    }
                }
                m_len = 4;
                {
                    uint v;
                    v = (*(uint*)(void*)(ip + m_len)) ^ (*(uint*)(void*)(m_pos + m_len));
                    if (v == 0)
                    {
                        do
                        {
                            m_len += 4;
                            v = (*(uint*)(void*)(ip + m_len)) ^ (*(uint*)(void*)(m_pos + m_len));
                            if (ip + m_len >= ip_end)
                                goto m_len_done;
                        } while (v == 0);
                    }
                    m_len += (uint)lzo_bitops_ctz32(v) / 8;
                }
            m_len_done:
                m_off = ((uint)((ip) - (m_pos)));
                ip += m_len;
                ii = ip;
                if (m_len <= 8 && m_off <= 0x0800)
                {
                    m_off -= 1;
                    *op++ = ((byte)(((m_len - 1) << 5) | ((m_off & 7) << 2)));
                    *op++ = ((byte)(m_off >> 3));
                }
                else if (m_off <= 0x4000)
                {
                    m_off -= 1;
                    if (m_len <= 33)
                        *op++ = ((byte)(32 | (m_len - 2)));
                    else
                    {
                        m_len -= 33;
                        *op++ = 32 | 0;
                        while (m_len > 255)
                        {
                            m_len -= 255;
                            *(byte*)op++ = 0;
                        }
                        *op++ = ((byte)(m_len));
                    }
                    *op++ = ((byte)(m_off << 2));
                    *op++ = ((byte)(m_off >> 6));
                }
                else
                {
                    m_off -= 0x4000;
                    if (m_len <= 9)
                        *op++ = ((byte)(16 | ((m_off >> 11) & 8) | (m_len - 2)));
                    else
                    {
                        m_len -= 9;
                        *op++ = ((byte)(16 | ((m_off >> 11) & 8)));
                        while (m_len > 255)
                        {
                            m_len -= 255;
                            *(byte*)op++ = 0;
                        }
                        *op++ = ((byte)(m_len));
                    }
                    *op++ = ((byte)(m_off << 2));
                    *op++ = ((byte)(m_off >> 6));
                }
                goto next;
            }
            out_len = ((uint)((op) - (@out)));
            return ((uint)((in_end) - (ii - ti)));
        }

        static int[] MultiplyDeBruijnBitPosition = {
              0, 1, 28, 2, 29, 14, 24, 3, 30, 22, 20, 15, 25, 17, 4, 8,
              31, 27, 13, 23, 21, 19, 16, 7, 26, 12, 18, 6, 11, 5, 10, 9
            };
        private static int lzo_bitops_ctz32(uint v)
        {
            return MultiplyDeBruijnBitPosition[((uint)((v & -v) * 0x077CB531U)) >> 27];
        }

        unsafe static int lzo1x_1_compress(byte* @in, uint in_len, byte* @out, ref uint out_len, byte* wrkmem)
        {
            byte* ip = @in;
            byte* op = @out;
            uint l = in_len;
            uint t = 0;
            while (l > 20)
            {
                uint ll = l;
                ulong ll_end;
                ll = ((ll) <= (49152) ? (ll) : (49152));
                ll_end = (ulong)ip + ll;
                if ((ll_end + ((t + ll) >> 5)) <= ll_end || (byte*)(ll_end + ((t + ll) >> 5)) <= ip + ll)
                    break;

                for (int i = 0; i < (1 << 14) * sizeof(ushort); i++)
                    wrkmem[i] = 0;
                t = lzo1x_1_compress_core(ip, ll, op, ref out_len, t, wrkmem);
                ip += ll;
                op += out_len;
                l -= ll;
            }
            t += l;
            if (t > 0)
            {
                byte* ii = @in + in_len - t;
                if (op == @out && t <= 238)
                    *op++ = ((byte)(17 + t));
                else if (t <= 3)
                    op[-2] |= ((byte)(t));
                else if (t <= 18)
                    *op++ = ((byte)(t - 3));
                else
                {
                    uint tt = t - 18;
                    *op++ = 0;
                    while (tt > 255)
                    {
                        tt -= 255;
                        *(byte*)op++ = 0;
                    }

                    *op++ = ((byte)(tt));
                }
                do *op++ = *ii++; while (--t > 0);
            }
            *op++ = 16 | 1;
            *op++ = 0;
            *op++ = 0;
            out_len = ((uint)((op) - (@out)));
            return 0;
        }

        public unsafe static int lzo1x_decompress(byte* @in, uint in_len, byte* @out, ref uint out_len, void* wrkmem)
        {
            byte* op;
            byte* ip;
            uint t;
            byte* m_pos;
            byte* ip_end = @in + in_len;
            out_len = 0;
            op = @out;
            ip = @in;
            bool gt_first_literal_run = false;
            bool gt_match_done = false;
            if (*ip > 17)
            {
                t = (uint)(*ip++ - 17);
                if (t < 4)
                {
                    match_next(ref op, ref ip, ref t);
                }
                else
                {
                    do *op++ = *ip++; while (--t > 0);
                    gt_first_literal_run = true;
                }
            }
            while (true)
            {
                if (gt_first_literal_run)
                {
                    gt_first_literal_run = false;
                    goto first_literal_run;
                }

                t = *ip++;
                if (t >= 16)
                    goto match;
                if (t == 0)
                {
                    while (*ip == 0)
                    {
                        t += 255;
                        ip++;
                    }
                    t += (uint)(15 + *ip++);
                }
                *(uint*)op = *(uint*)ip;
                op += 4; ip += 4;
                if (--t > 0)
                {
                    if (t >= 4)
                    {
                        do
                        {
                            *(uint*)op = *(uint*)ip;
                            op += 4; ip += 4; t -= 4;
                        } while (t >= 4);
                        if (t > 0) do *op++ = *ip++; while (--t > 0);
                    }
                    else
                        do *op++ = *ip++; while (--t > 0);
                }
            first_literal_run:
                t = *ip++;
                if (t >= 16)
                    goto match;
                m_pos = op - (1 + 0x0800);
                m_pos -= t >> 2;
                m_pos -= *ip++ << 2;

                *op++ = *m_pos++; *op++ = *m_pos++; *op++ = *m_pos;
                gt_match_done = true;

            match:
                do
                {
                    if (gt_match_done)
                    {
                        gt_match_done = false;
                        goto match_done;
                        ;
                    }
                    if (t >= 64)
                    {
                        m_pos = op - 1;
                        m_pos -= (t >> 2) & 7;
                        m_pos -= *ip++ << 3;
                        t = (t >> 5) - 1;

                        copy_match(ref op, ref m_pos, ref t);
                        goto match_done;
                    }
                    else if (t >= 32)
                    {
                        t &= 31;
                        if (t == 0)
                        {
                            while (*ip == 0)
                            {
                                t += 255;
                                ip++;
                            }
                            t += (uint)(31 + *ip++);
                        }
                        m_pos = op - 1;
                        m_pos -= (*(ushort*)(void*)(ip)) >> 2;
                        ip += 2;
                    }
                    else if (t >= 16)
                    {
                        m_pos = op;
                        m_pos -= (t & 8) << 11;
                        t &= 7;
                        if (t == 0)
                        {
                            while (*ip == 0)
                            {
                                t += 255;
                                ip++;
                            }
                            t += (uint)(7 + *ip++);
                        }
                        m_pos -= (*(ushort*)ip) >> 2;
                        ip += 2;
                        if (m_pos == op)
                            goto eof_found;
                        m_pos -= 0x4000;
                    }
                    else
                    {
                        m_pos = op - 1;
                        m_pos -= t >> 2;
                        m_pos -= *ip++ << 2;
                        *op++ = *m_pos++; *op++ = *m_pos;
                        goto match_done;
                    }

                    if (t >= 2 * 4 - (3 - 1) && (op - m_pos) >= 4)
                    {
                        *(uint*)op = *(uint*)m_pos;
                        op += 4; m_pos += 4; t -= 4 - (3 - 1);
                        do
                        {
                            *(uint*)op = *(uint*)m_pos;
                            op += 4; m_pos += 4; t -= 4;
                        } while (t >= 4);
                        if (t > 0) do *op++ = *m_pos++; while (--t > 0);
                    }
                    else
                    {
                        // copy_match:
                        *op++ = *m_pos++; *op++ = *m_pos++;
                        do *op++ = *m_pos++; while (--t > 0);
                    }
                match_done:
                    t = (uint)(ip[-2] & 3);
                    if (t == 0)
                        break;
                    // match_next:
                    *op++ = *ip++;
                    if (t > 1) { *op++ = *ip++; if (t > 2) { *op++ = *ip++; } }
                    t = *ip++;
                } while (true);
            }
        eof_found:

            out_len = ((uint)((op) - (@out)));
            return (ip == ip_end ? 0 :
                   (ip < ip_end ? (-8) : (-4)));
        }

        private static unsafe void match_next(ref byte* op, ref byte* ip, ref uint t)
        {
            do *op++ = *ip++; while (--t > 0);
            t = *ip++;
        }

        private static unsafe void copy_match(ref byte* op, ref byte* m_pos, ref uint t)
        {
            *op++ = *m_pos++; *op++ = *m_pos++;
            do *op++ = *m_pos++; while (--t > 0);
        }



        public static unsafe byte[] Decompress(byte[] @in, byte[] @out)
        {
            uint out_len = 0;
            fixed (byte* @pIn = @in, wrkmem = new byte[IntPtr.Size * 16384], pOut = @out)
            {
                lzo1x_decompress(pIn, (uint)@in.Length, @pOut, ref @out_len, wrkmem);
            }
            return @out;
        }

        public static unsafe void Decompress(byte* r, uint size_in, byte* w, ref uint size_out)
        {
            fixed (byte* wrkmem = new byte[IntPtr.Size * 16384])
            {
                lzo1x_decompress(r, size_in, w, ref size_out, wrkmem);
            }
        }

        public static unsafe byte[] Compress(byte[] input)
        {
            byte[] @out = new byte[input.Length + (input.Length / 16) + 64 + 3];
            uint out_len = 0;
            fixed (byte* @pIn = input, wrkmem = new byte[IntPtr.Size * 16384], pOut = @out)
            {
                lzo1x_1_compress(pIn, (uint)input.Length, @pOut, ref @out_len, wrkmem);
            }
            Array.Resize(ref @out, (int)out_len);
            return @out;
        }

        public static unsafe void Compress(byte* r, uint size_in, byte* w, ref uint size_out)
        {
            fixed (byte* wrkmem = new byte[IntPtr.Size * 16384])
            {
                lzo1x_1_compress(r, size_in, w, ref size_out, wrkmem);
            }
        }
    }
    public class MapObject
    {
        public IsoTile Tile;
    }
    public class NamedMapObject : MapObject
    {
        public string Name { get; set; }
    }
    public class NumberedMapObject : MapObject
    {
        public virtual int Number { get; set; }
    }


    // all the stuff found on maps
    public class IsoTile : NumberedMapObject
    {
        public ushort Dx;//ushort Unsigned 16-bit integer 2-bytes
        public ushort Dy;
        public ushort Rx;
        public ushort Ry;
        public byte Z;//1 bytes
        public short TileNum;//16-bit
        public byte SubTile;//1 bytes
        public Color RadarLeft = Color.White;
        public Color RadarRight = Color.White;

        public IsoTile(ushort p1, ushort p2, ushort rx, ushort ry, byte z, short tilenum, byte subtile)
        {
            Dx = p1;
            Dy = p2;
            Rx = rx;
            Ry = ry;
            Z = z;
            TileNum = tilenum;
            SubTile = subtile;
        }

        public List<byte> ToMapPack5Entry()
        {
            var ret = new List<byte>();
            ret.AddRange(BitConverter.GetBytes(Rx));//2 bytes
            ret.AddRange(BitConverter.GetBytes(Ry));//2 bytes
            ret.AddRange(BitConverter.GetBytes(TileNum));//2 bytes
            ret.Add(0); ret.Add(0);//1+1 bytes
            ret.Add(SubTile);//1 bytes
            ret.Add(Z);//1 bytes
            ret.Add(0);//1 bytes
            return ret;
        }
        public IsoTile Clone()
        {
            return (IsoTile)this.MemberwiseClone();
        }

    }
    public class Format5
    {
        public static unsafe uint DecodeInto(byte[] src, byte[] dest, int format = 5)
        {
            fixed (byte* pr = src, pw = dest)
            {
                byte* r = pr, w = pw;
                byte* w_end = w + dest.Length;

                while (w < w_end)
                {
                    ushort size_in = *(ushort*)r;
                    r += 2;
                    uint size_out = *(ushort*)r;
                    r += 2;

                    if (size_in == 0 || size_out == 0)
                        break;

                    if (format == 80)
                        Format80.DecodeInto(r, w);
                    else
                        MiniLZO.Decompress(r, size_in, w, ref size_out);
                    r += size_in;
                    w += size_out;
                }
                return (uint)(w - pw);
            }
        }

        public static byte[] EncodeSection(byte[] s)
        {
            return MiniLZO.Compress(s);
        }

        public static byte[] Encode(byte[] source, int format)
        {
            var dest = new byte[source.Length * 2];
            var src = new MemoryFile(source);

            int w = 0;
            while (!src.Eof)
            {
                var cb_in = (short)Math.Min(src.Remaining, 8192);
                var chunk_in = src.Read(cb_in);
                var chunk_out = format == 80 ? Format80.Encode(chunk_in) : EncodeSection(chunk_in);
                uint cb_out = (ushort)chunk_out.Length;

                Array.Copy(BitConverter.GetBytes(cb_out), 0, dest, w, 2);
                w += 2;
                Array.Copy(BitConverter.GetBytes(cb_in), 0, dest, w, 2);
                w += 2;
                Array.Copy(chunk_out, 0, dest, w, chunk_out.Length);
                w += chunk_out.Length;
            }
            Array.Resize(ref dest, w);
            return dest;
        }
    }
}
