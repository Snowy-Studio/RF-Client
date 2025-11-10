using System.IO;

namespace DTAConfig.Entity
{
    public class MemoryFile : VirtualFile
    {

        public MemoryFile(byte[] buffer, bool isBuffered = true) :
            base(new MemoryStream(buffer), "MemoryFile", 0, buffer.Length, isBuffered)
        { }
    }
}
