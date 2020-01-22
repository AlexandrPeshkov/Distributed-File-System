using DFS.Balancer.Models;
using DFS.Node.Models;
using DFS.Node.Requests;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace DFS.Balancer.Services
{
    public class BalancerService
    {
        private readonly BalancerConfiguration _configuration;
        private Dictionary<string, List<NodeBlockInfo>> Files { get; set; }

        private HttpClient _httpClient { get; set; }

        public BalancerService(IOptions<BalancerConfiguration> configuration)
        {
            _configuration = configuration.Value;

            Files = new Dictionary<string, List<NodeBlockInfo>>();
            _httpClient = new HttpClient();
        }

        #region API Methods

        /// <summary>
        /// Присоеденить ноду
        /// </summary>
        /// <param name="node">Файловая нода</param>
        public void AddNode(NodeInfo node)
        {
            if (node != null && node.Files != null)
            {
                foreach (var file in node.Files)
                {
                    List<NodeBlockInfo> nodesInfo = new List<NodeBlockInfo>();

                    if (!Files.TryGetValue(file.FileName, out nodesInfo))
                    {
                        Files.Add(file.FileName, nodesInfo);
                    }
                    else
                    {
                        foreach (var info in nodesInfo.ToList())
                        {
                            if (info.HostName == node.HostName)
                            {
                                nodesInfo.Remove(info);
                            }
                        }
                    }

                    nodesInfo.Add(new NodeBlockInfo
                    {
                        HostName = node.HostName,
                        Indexes = file.Indexes
                    });
                }
            }
        }

        /// <summary>
        /// Загрузить новый файл
        /// </summary>
        /// <param name="file">Файл</param>
        /// <param name="forceOwerwritte">Разрешить перезапись</param>
        /// <returns></returns>
        public async Task UploadFile(SourceFile file, bool forceOwerwritte = false)
        {
            string controller = "Node";
            string action = "SaveFile";

            List<Block> blocks = SplitFile(file);

            Dictionary<string, List<Block>> nodeBlocks = new Dictionary<string, List<Block>>();

            foreach (var block in blocks)
            {
                string hostName = GetOptimalNode();

                List<Block> tempBlocks = new List<Block>();
                if (!nodeBlocks.TryGetValue(hostName, out tempBlocks))
                {
                    nodeBlocks.Add(hostName, tempBlocks);
                }

                tempBlocks.Add(block);
            }

            foreach (var nodeBlock in nodeBlocks)
            {
                SavePartialFileRequest request = new SavePartialFileRequest
                {
                    ForceOwerrite = false,
                    PartilFile = new PartilFile
                    {
                        FileName = nodeBlock.Value.FirstOrDefault().Info.FileName,
                        Blocks = nodeBlock.Value
                    }
                };
                JsonContent content = new JsonContent(request);
                string hostName = nodeBlock.Key;
                string url = $"{hostName}/{controller}/{action}";

                HttpResponseMessage response = await _httpClient.PostAsync(url, content);
            }
        }

        public async Task<SourceFile> DownloadFile(string fileName)
        {
            SourceFile sourceFile = null;

            if (Files.TryGetValue(fileName, out var blocks))
            {
                sourceFile = new SourceFile
                {
                    Name = fileName,
                }
            }
        }

        #endregion API Methods

        #region Utils

        /// <summary>
        /// Найти оптимальную ноду для записи
        /// </summary>
        /// <returns>Адрес ноды</returns>
        private string GetOptimalNode()
        {
            return Files.SelectMany(x => x.Value)
                .GroupBy(x => x.HostName)
                .Select(x => new
                {
                    HostName = x.Key,
                    Sum = x.Sum(x => x.Indexes.Count)
                })
                .OrderBy(x => x.Sum)
                .FirstOrDefault()
                ?.HostName;
        }

        /// <summary>
        /// Разделить файл на блоки
        /// </summary>
        /// <param name="sourceFile"></param>
        /// <returns></returns>
        private List<Block> SplitFile(SourceFile sourceFile)
        {
            List<Block> blocks = null;
            int blockCount = (int)Math.Ceiling(sourceFile.Data.Length * 1d / _configuration.BlockSize);

            if (sourceFile != null)
            {
                for (var i = 0; i < sourceFile.Data.Length; i += _configuration.BlockSize)
                {
                    var data = sourceFile.Data.Skip(i * _configuration.BlockSize).Take(_configuration.BlockSize).ToList();

                    Block block = new Block
                    {
                        Data = data,
                        Info = new BlockInfo
                        {
                            FileName = sourceFile.Name,
                            Index = i
                        }
                    };
                    blocks.Add(block);
                }
            }
            return blocks;
        }

        private async Task DownloadBlock(string nodeUrl, string fileName, )
        {
        }

        #endregion Utils
    }
}