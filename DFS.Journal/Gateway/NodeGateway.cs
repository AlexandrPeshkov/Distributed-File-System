using DFS.Balancer.Services;
using DFS.Node.Models;
using DFS.Node.Requests;
using Models.Requests;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace DFS.Balancer.Gateway
{
    public class NodeGateway
    {
        private HttpClient _httpClient { get; set; }

        private readonly string _controller = "Node";

        public NodeGateway()
        {
            _httpClient = new HttpClient();
        }

        /// <summary>
        /// Загрузить блоки на узел
        /// </summary>
        /// <param name="hostName">Узел</param>
        /// <param name="blocks">Блоки</param>
        /// <param name="forceOwerrite">Разрешить перезапись</param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> UploadBlocks(string hostName, List<Block> blocks, bool forceOwerrite)
        {
            string action = "SaveFile";

            SavePartialFileRequest request = new SavePartialFileRequest
            {
                ForceOwerrite = forceOwerrite,
                PartilFile = new PartialFile
                {
                    FileName = blocks.FirstOrDefault().Info.FileName,
                    Blocks = blocks,
                    TotalBlockCount = blocks.Count
                }
            };

            JsonContent content = new JsonContent(request);
            string url = $"{hostName}/{_controller}/{action}";

            HttpResponseMessage response = await _httpClient.PostAsync(url, content);
            return response;
        }

        public async Task<Block> DownloadBlock(string hostName, string fileName, int blockIndex)
        {
            if (hostName != null)
            {
                string action = "DownloadBlock";
                string url = $"{hostName}/{_controller}/{action}?FileName={fileName}&BlockIndex={blockIndex}";
                HttpResponseMessage response = await _httpClient.GetAsync(url);

                Block block = null;
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    block = JsonConvert.DeserializeObject<Block>(json);
                }
                return block;
            }
            return null;
        }

        public async Task<State> AddOrOverwritteBlock(string hostName, string fileName, byte[] data, int blockIndex, int totalBlockCount, bool alowOverwrite)
        {
            string action = "AddOrReplaceBlock";
            string url = $"{hostName}/{_controller}/{action}";

            AddBlockRequest request = new AddBlockRequest
            {
                AllowOverwrite = alowOverwrite,
                Data = data,
                BlockIndex = blockIndex,
                FileName = fileName,
                TotalBlockCount = totalBlockCount
            };

            JsonContent content = new JsonContent(request);
            HttpResponseMessage response = await _httpClient.PostAsync(url, content);

            string json = await response.Content.ReadAsStringAsync();
            State state = JsonConvert.DeserializeObject<State>(json);
            return state;
        }

        public async Task<State> DeleteFile(string hostName, string fileName)
        {
            string action = "DeleteFile";
            string url = $"{hostName}/{_controller}/{action}?fileName={fileName}";
            HttpResponseMessage response = await _httpClient.DeleteAsync(url);
            State state = new State(false);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                string json = await response.Content.ReadAsStringAsync();
                state = JsonConvert.DeserializeObject<State>(json);
            }
            return state;
        }

        //public async Task<HttpResponseMessage> SearchFile(string hostName, string fileName)
        //{
        //    string action = "SearchFile";
        //    string url = $"{hostName}/{_controller}/{action}?fileName = {fileName}";
        //    HttpResponseMessage response = await _httpClient.GetAsync(url);
        //    return response;
        //}
    }
}