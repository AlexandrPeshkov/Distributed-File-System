using DFS.Balancer.Models;
using System.ComponentModel.DataAnnotations;

namespace Models.Requests
{
    public class SaveFileRequest
    {
        [Required]
        public SourceFile SourceFile { get; set; }

        /// <summary>
        /// Расзрешить перезапись
        /// </summary>
        public bool ForceOwerrite { get; set; }
    }
}