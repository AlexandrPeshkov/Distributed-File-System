using DFS.Balancer.Models;
using DFS.Balancer.Services;
using DFS.Node.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;

namespace DFS.Balancer.Controllers
{
    /// <summary>
    /// Распределенная файловая система
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class DFSController : ControllerBase
    {
        private readonly BalancerService _balancerService;

        public DFSController(BalancerService balancerService)
        {
            _balancerService = balancerService;
        }

        /// <summary>
        /// Зарегестрировать узел
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route(nameof(RegNode))]
        public IActionResult RegNode(NodeInfo node)
        {
            return Ok(_balancerService.AddNode(node));
        }

        /// <summary>
        /// Добавить файл в хранилище
        /// </summary>
        /// <param name="file">Файл</param>
        /// <param name="forceOwerrite">Разрешить перезапись</param>
        /// <returns></returns>
        [HttpPost]
        [Route(nameof(UploadFile))]
        public async Task<IActionResult> UploadFile(IFormFile file, bool forceOwerrite)
        {
            using (var ms = new MemoryStream())
            {
                file.CopyTo(ms);
                var data = ms.ToArray();
                var sourceFile = new SourceFile
                {
                    Data = data,
                    Name = file.FileName,
                    ContentType = file.ContentType
                };
                await _balancerService.UploadFile(sourceFile, forceOwerrite);
                return Ok($"File {file.Name} has been uploaded");
            }
        }

        /// <summary>
        /// Скачать файл
        /// </summary>
        /// <param name="fileName">Имя файла</param>
        /// <returns></returns>
        [HttpGet]
        [Route(nameof(DownloadFile))]
        public async Task<IActionResult> DownloadFile(string fileName)
        {
            SourceFile file = await _balancerService.DownloadFile(fileName);
            if (file != null)
            {
                var result = File(file.Data, file.ContentType, file.Name);
                return result;
            }
            return NotFound(fileName);
        }

        /// <summary>
        /// Скачать блоки файла
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route(nameof(DownloadFileBlock))]
        public async Task<IActionResult> DownloadFileBlock(string fileName, int blockIndex)
        {
            Block block = await _balancerService.DownloadBlock(fileName, blockIndex);
            if (block != null)
            {
                string contentType = "application/octet-stream";
                var result = File(block.Data, contentType, block.Info.FileName);
                return result;
            }
            return NotFound(fileName);
        }

        /// <summary>
        /// Заменить блок файла
        /// </summary>
        /// <param name="fileName">Имя файла</param>
        /// <param name="blockIndex">Индекс</param>
        /// <param name="file">файл</param>
        /// <returns></returns>
        [HttpPatch]
        [Route(nameof(OverwriteBlock))]
        public async Task<IActionResult> OverwriteBlock(IFormFile file, string fileName, int blockIndex)
        {
            using (var ms = new MemoryStream())
            {
                file.CopyTo(ms);
                var data = ms.ToArray();
                State state = await _balancerService.OwerwritteBlock(fileName, data, blockIndex);
                return Ok(state);
            }
        }

        /// <summary>
        /// Проверить наличие файла в системе
        /// </summary>
        /// <param name="fileName">Имя файла</param>
        /// <returns></returns>
        [HttpGet]
        [Route(nameof(SearchFile))]
        public IActionResult SearchFile(string fileName)
        {
            return Ok(_balancerService.Search(fileName));
        }

        /// <summary>
        /// Удалить файл
        /// </summary>
        /// <param name="fileName">Имя файла</param>
        /// <returns></returns>
        [HttpDelete]
        [Route(nameof(DeleteFile))]
        public async Task<IActionResult> DeleteFile(string fileName)
        {
            State state = await _balancerService.DeleteFile(fileName);
            return Ok(state);
        }
    }
}