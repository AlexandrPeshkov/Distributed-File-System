using System.ComponentModel.DataAnnotations;

namespace Models.Requests
{
    public class AddBlockRequest
    {
        /// <summary>
        /// Имя файла
        /// </summary>
        [Required]
        public string FileName { get; set; }

        /// <summary>
        /// Номер блока
        /// </summary>
        [Required]
        public int BlockIndex { get; set; }

        /// <summary>
        /// Данные
        /// </summary>
        [Required]
        public byte[] Data { get; set; }

        /// <summary>
        /// Разрешить перезапись
        /// </summary>
        [Required]
        public bool AllowOverwrite { get; set; }

        /// <summary>
        /// Всего блоков в файле
        /// </summary>
        [Required]
        public int TotalBlockCount { get; set; }
    }
}