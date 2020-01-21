using System.Collections.Generic;

namespace DFS.Node.Models
{
    /// <summary>
    /// Часть файла
    /// </summary>
    public class PartilFile
    {
        /// <summary>
        /// Имя файла
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Блоки данных
        /// </summary>
        public List<Block> Blocks { get; set; }
    }
}