using DFS.Node.Models;
using System.ComponentModel.DataAnnotations;

namespace DFS.Node.Requests
{
    public class SavePartialFileRequest
    {
        /// <summary>
        /// Часть файла
        /// </summary>
        [Required]
        public PartialFile PartilFile { get; set; }

        /// <summary>
        /// Расзрешить перезапись
        /// </summary>
        public bool ForceOwerrite { get; set; }
    }
}