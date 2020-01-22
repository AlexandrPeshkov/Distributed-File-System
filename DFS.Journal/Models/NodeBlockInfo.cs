using System.Collections.Generic;

namespace DFS.Balancer.Models
{
    /// <summary>
    /// Запись о блоках файла
    /// </summary>
    public class NodeBlockInfo
    {
        /// <summary>
        /// Адресь ноды
        /// </summary>
        public string HostName { get; set; }

        /// <summary>
        /// Индексы блоков файла
        /// </summary>
        public List<int> Indexes { get; set; }
    }
}