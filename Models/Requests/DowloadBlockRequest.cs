using System.ComponentModel.DataAnnotations;

namespace Models.Requests
{
    public class DowloadBlockRequest
    {
        /// <summary>
        /// Имя файла
        /// </summary>
        [Required]
        public string FileName { get; set; }

        /// <summary>
        /// Индекс блока
        /// </summary>
        [Required]
        public int BlockIndex { get; set; }
    }
}