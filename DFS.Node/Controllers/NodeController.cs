using DFS.Node.Models;
using DFS.Node.Requests;
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
        /// <param name="request">Частичный файл</param>
        /// <returns></returns>
        [HttpPost]
        [Route(nameof(SaveFile))]
        public async Task<IActionResult> SaveFile(SavePartialFileRequest request)
        {
            if (ModelState.IsValid)
            {
                State state = await _nodeService.TryAddFile(request.PartilFile, request.ForceOwerrite);
                if (state)
                {
                    return Ok(state);
                }
                else
                {
                    return StatusCode(500, state);
                }
            }
            return BadRequest(request.PartilFile.FileName);
        }

        /// <summary>
        /// Удалить файл
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        [HttpDelete]
        [Route(nameof(RemovFile))]
        public IActionResult RemovFile(string fileName)
        {
            if (ModelState.IsValid)
            {
                State state = _nodeService.RemoveFile(fileName);
                if (state)
                {
                    return Ok(state);
                }
                else
                {
                    return StatusCode(500, state);
                }
            }
            return BadRequest(fileName);
        }
    }
}