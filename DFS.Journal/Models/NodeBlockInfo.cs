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

        /// <summary>
        /// Приоритет (пинг\скорость загрузки пр; 0 - высший)
        /// </summary>
        public int Priority { get; set; }

        public NodeBlockInfo()
        {
            Indexes = new List<int>();
        }
    }
}