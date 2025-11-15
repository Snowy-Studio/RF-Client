using System.Collections.Generic;

namespace ClientCore.Entity
{
    public class Page<T>
    {
        public List<T> records { get; set; }  // 当前页的记录
        public long total { get; set; }       // 总记录数
        public int size { get; set; }         // 每页大小
        public int current { get; set; }      // 当前页码
        public int pages { get; set; }        // 总页数

        public Page() { }

        //public Page(List<T> records, long total, int size, int current, int pages)
        //{
        //    records = records;
        //    total = total;
        //    size = size;
        //    current = current;
        //    pages = pages;
        //}
    }
}
