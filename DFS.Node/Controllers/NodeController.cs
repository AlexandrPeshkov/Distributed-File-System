using DFS.Node.Models;
using DFS.Node.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace DFS.Node.Controllers
{
    /// <summary>
    /// Узел распределенной файловой системы
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class NodeController : ControllerBase
    {
        private readonly NodeService _nodeService;

        public NodeController(NodeService nodeService)
        {
            _nodeService = nodeService;
        }

        /// <summary>
        /// Сохранить или перезаписть блоки файла
        /// </summary>
        /// <param name="partilFile">Частичный файл</param>
        /// <returns></returns>
        [HttpPost]
        [Route(nameof(SaveFile))]
        public async Task<IActionResult> SaveFile(PartilFile partilFile)
        {
            if (ModelState.IsValid)
            {
                bool state = await _nodeService.AddOrRewriteFile(partilFile);
                if (state)
                {
                    return Ok(partilFile.FileName);
                }
                else
                {
                    return StatusCode(500);
                }
            }
            return BadRequest();
        }

        /// <summary>
        /// Удалить файл
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public IActionResult DeleteFile(string fileName)
        {
            if (ModelState.IsValid)
            {
                bool state = _nodeService.DeleteFile(fileName);
                if (state)
                {
                    return Ok(fileName);
                }
                else
                {
                    return StatusCode(500);
                }
            }
            return BadRequest();
        }
    }
}