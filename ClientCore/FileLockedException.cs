using System;
using System.IO;

namespace ClientCore
{
    public class FileLockedException : IOException
    {
        // 默认构造函数
        public FileLockedException()
            : base("文件操作失败，文件可能被占用。")
        {
        }

        // 带有自定义消息的构造函数
        public FileLockedException(string message)
            : base(message)
        {
        }

        // 带有自定义消息和内部异常的构造函数，用于异常链
        public FileLockedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        // 可以添加额外的属性或方法来提供有关文件操作的更多信息
        // 例如，添加一个属性来指示哪个文件被占用
        public string LockedFilePath { get; set; }

        // 构造函数，其中包括被锁定文件的路径
        public FileLockedException(string message, string lockedFilePath)
            : this(message)
        {
            LockedFilePath = lockedFilePath;
        }
    }
}
