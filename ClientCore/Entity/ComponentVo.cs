using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace DTAConfig.Entity;

public class ComponentVo
{
    public string id { get; set; }              // 组件ID
    public string name { get; set; }            // 组件名称

    public string author { get; set; }          // 作者

    public string version { get; set; }         // 版本号

    public string file { get; set; }            // 组件压缩包

    public int type { get; set; }            // 类型
    public string typeName { get; set; }

    public long size { get; set; }              // 文件大小

    public string hash { get; set; }            // 哈希值

    public long downCount { get; set; }

    public string uploadTime { get; set; }

    public string uploadUser { get; set; }
    public string uploadUserName { get; set; }

    public List<string> apply { get; set; }

    public string tags { get; set; }

    public string description { get; set; }

}

public struct StateItem
{
    public int Code { get; set; }

    public string Text { get; set; }

    public Color TextColor { get; set; }
}