using System.Collections.Generic;

namespace DFS.Node.Models
{
    /// <summary>
    /// Часть файла
    /// </summary>
    public class PartialFile
    {
        /// <summary>
        /// Имя файла
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Общее число блоков в файле
        /// </summary>
        public int TotalBlockCount { get; set; }

        /// <summary>
        /// Блоки данных
        /// </summary>
        public List<Block> Blocks { get; set; }
    }
}