using DFS.Node.Models;
using DFS.Node.Requests;
using DFS.Node.Services;
using Microsoft.AspNetCore.Mvc;
using Models.Requests;
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
        [Route(nameof(DeleteFile))]
        public IActionResult DeleteFile([FromQuery]string fileName)
        {
            if (ModelState.IsValid)
            {
                State state = _nodeService.DeleteFile(fileName);
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

        /// <summary>
        /// Скачать блок файла
        /// </summary>
        /// <param name="request">Имя файла и индекс блока</param>
        /// <returns></returns>
        [HttpGet]
        [Route(nameof(DownloadBlock))]
        public async Task<IActionResult> DownloadBlock([FromQuery]DowloadBlockRequest request)
        {
            if (ModelState.IsValid)
            {
                Block block = await _nodeService.GetBlock(request.FileName, request.BlockIndex);
                if (block != null)
                {
                    return Ok(block);
                }
                else
                {
                    return NotFound(request);
                }
            }
            return BadRequest(request);
        }

        /// <summary>
        /// Добавить или перезаписать блок
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [Route(nameof(AddOrReplaceBlock))]
        public async Task<IActionResult> AddOrReplaceBlock([FromBody]AddBlockRequest request)
        {
            if (ModelState.IsValid)
            {
                Block block = new Block
                {
                    Data = request.Data,
                    Info = new BlockInfo
                    {
                        FileName = request.FileName,
                        Index = request.BlockIndex,
                        TotalBlockCount = request.TotalBlockCount
                    }
                };
                State state = await _nodeService.TryAddBlock(block, request.AllowOverwrite);
                if (state)
                {
                    return Ok(state);
                }
                else
                {
                    return StatusCode(500, state);
                }
            }
            return BadRequest(request);
        }
    }
}