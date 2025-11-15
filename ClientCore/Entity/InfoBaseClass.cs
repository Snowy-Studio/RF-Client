using System.Collections.Generic;
using Rampastring.Tools;

namespace DTAConfig.Entity
{
    public abstract class InfoBaseClass
    {

        protected InfoBaseClass() { }

        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 注册名
        /// </summary>
        public string ID { get; set; }
        /// <summary>
        /// 介绍
        /// </summary>
        public string Description { get; set; } = string.Empty;
        /// <summary>
        /// 在重聚中的路径
        /// </summary>
        public string FilePath { get; set; }
        /// <summary>
        /// 版本
        /// </summary>
        public string Version { get; set; } = string.Empty;
        /// <summary>
        /// 注册于哪个文件
        /// </summary>
        public string FileName { get; set; }
        /// <summary>
        /// 作者
        /// </summary>
        public string Author { get; set; } = string.Empty;
        public bool CanDel { get; set; }
        /// <summary>
        /// 获取属性名和值的映射
        /// </summary>
        /// <returns>属性名和对应值的字典</returns>
        public abstract Dictionary<string, string> GetProperties();
        /// <summary>
        /// 写入INI以便下次加载读取。
        /// </summary>
        public abstract void Create();
        /// <summary>
        /// 必须重写ToString方法
        /// </summary>
        /// <returns>返回这个类型的信息</returns>
        public abstract override string ToString();
        /// <summary>
        /// 初始化读取基础属性
        /// </summary>
        /// <param name="iniFile"></param>
        /// <param name="ID"></param>
        public static InfoBaseClass Init(IniFile iniFile,string ID, InfoBaseClass t)
        {

            t.ID = ID;
            t.Name = iniFile.GetValue(ID, "Name", ID);
            t.Description = iniFile.GetValue(ID, "Description", t.Name);
            t.FileName = iniFile.FileName;
            t.Version = iniFile.GetValue(ID, "Version", string.Empty);
            t.Author = iniFile.GetValue(ID, "Author", string.Empty);
            t.CanDel = iniFile.GetValue(ID, "CanDel", true);
            return t;
        }

    }
}
