using System.Collections.Generic;

namespace DFS.Balancer.Models
{
    /// <summary>
    /// Узел
    /// </summary>
    public class NodeInfo
    {
        /// <summary>
        /// Адерс хоста
        /// </summary>
        public string HostName { get; set; }

        /// <summary>
        /// Описания файлов
        /// </summary>
        public List<FileMeta> Files { get; set; }
    }
}