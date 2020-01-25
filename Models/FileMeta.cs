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

        /// <summary>
        /// Число блоков в файле
        /// </summary>
        public int TotalBlockCount { get; set; }

        public int FileSize { get; set; }

        public FileMeta(int fileSize)
        {
            Indexes = new List<int>();
            FileSize = fileSize;
        }
    }
}