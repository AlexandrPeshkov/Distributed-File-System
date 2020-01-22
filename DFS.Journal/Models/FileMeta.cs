using System.Collections.Generic;

namespace DFS.Balancer.Models
{
    /// <summary>
    /// Описание части файла
    /// </summary>
    public class FileMeta
    {
        /// <summary>
        /// Имя файла
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Индексы
        /// </summary>
        public List<int> Indexes { get; set; }
    }
}