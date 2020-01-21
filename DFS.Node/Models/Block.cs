namespace DFS.Node.Models
{
    /// <summary>
    /// Блок данных
    /// </summary>
    public class Block
    {
        /// <summary>
        /// Информация о блоке
        /// </summary>
        public BlockInfo Info { get; set; }

        /// <summary>
        /// Данные
        /// </summary>
        public byte[] Data { get; set; }
    }
}