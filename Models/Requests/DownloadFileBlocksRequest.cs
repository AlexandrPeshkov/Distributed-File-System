using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models.Requests
{
    public class DownloadFileBlocksRequest
    {
        /// <summary>
        /// Имя файла
        /// </summary>
        [Required]
        public string FileName { get; set; }

        /// <summary>
        /// Номера блоков
        /// </summary>
        [Required]
        public List<int> Indexex { get; set; }
    }
}