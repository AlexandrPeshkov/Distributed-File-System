namespace DFS.Node.Models
{
    /// <summary>
    /// Метаданные блока
    /// </summary>
    public class BlockInfo
    {
        /// <summary>
        /// Порядок блока в файле
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Имя файла
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Общее число блоков в файле
        /// </summary>
        public int TotalBlockCount { get; set; }
    }
}