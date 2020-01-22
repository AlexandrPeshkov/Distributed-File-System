using DFS.Balancer.Services;
using DFS.Node.Requests;
using Microsoft.AspNetCore.Mvc;
using Models.Requests;
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
        /// Добавить файл в хранилище
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [Route(nameof(UploadFile))]
        public async Task<IActionResult> UploadFile([FromBody]SaveFileRequest request)
        {
            if (ModelState.IsValid)
            {
                _balancerService.UploadFile(request.SourceFile, request.ForceOwerrite);
            }
        }
    }
}