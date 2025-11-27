using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientCore.FileFormats
{
    public class MiniFileSystem : OpenRA.FileSystem.IReadOnlyFileSystem
    {
        readonly string basePath;

        public MiniFileSystem(string basePath)
        {
            this.basePath = basePath;
        }

        public bool TryOpen(string filename, out Stream s)
        {
            var full = Path.Combine(basePath, filename);

            if (File.Exists(full))
            {
                s = File.OpenRead(full);
                return true;
            }

            s = null;
            return false;
        }

        public Stream Open(string filename)
        {
            if (TryOpen(filename, out var s))
                return s;

            throw new FileNotFoundException(filename);
        }

        public bool Exists(string filename)
        {
            return File.Exists(Path.Combine(basePath, filename));
        }

        public bool TryGetPackageContaining(string path, out OpenRA.FileSystem.IReadOnlyPackage package, out string filename)
        {
            package = null;
            filename = null;
            return false;
        }

        public bool IsExternalFile(string filename)
        {
            return false;
        }
    }
}
