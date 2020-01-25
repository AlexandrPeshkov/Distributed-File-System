using DFS.Balancer.Gateway;
using DFS.Balancer.Models;
using DFS.Node.Models;
using DFS.Node.Requests;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace DFS.Balancer.Services
{
    public class BalancerService
    {
        private readonly BalancerConfiguration _configuration;
        private Dictionary<string, NodeFileInfo> Files { get; set; }

        private readonly NodeGateway _nodeGateway;

        public BalancerService(IOptions<BalancerConfiguration> configuration, NodeGateway nodeGateway)
        {
            Files = new Dictionary<string, NodeFileInfo>();

            _configuration = configuration.Value;
            _nodeGateway = nodeGateway;
        }

        #region API Methods

        /// <summary>
        /// Присоеденить ноду
        /// </summary>
        /// <param name="node">Файловая нода</param>
        public void AddNode(NodeInfo node)
        {
            if (node != null && node.PartialFiles != null)
            {
                if (!node.PartialFiles.Any())
                {
                    node.PartialFiles.Add(new FileMeta(9)
                    {
                        FileName = "INIT_FILE",
                        Indexes = new List<int> { 0 },
                        TotalBlockCount = 1,
                        FileSize = 9
                    }
                    );
                }

                foreach (var file in node.PartialFiles)
                {
                    NodeFileInfo nodeFileInfo = null;

                    if (!Files.TryGetValue(file.FileName, out nodeFileInfo))
                    {
                        nodeFileInfo = new NodeFileInfo(file.TotalBlockCount, file.FileSize);
                        Files.Add(file.FileName, nodeFileInfo);
                    }
                    else
                    {
                        foreach (var info in nodeFileInfo.Nodes.ToList())
                        {
                            if (info.HostName == node.NodeUrl)
                            {
                                nodeFileInfo.Nodes.Remove(info);
                            }
                        }
                    }

                    nodeFileInfo.Nodes.Add(new NodeBlockInfo
                    {
                        HostName = node.NodeUrl,
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
            string checkSum = CalculateMD5(file.Data);

            List<Block> parts = SplitFile(file);

            byte[] blockData = parts.SelectMany(b => b.Data).ToArray();

            if (Files.TryGetValue(file.Name, out var nodeFile))
            {
                if (forceOwerwritte)
                {
                    nodeFile = new NodeFileInfo(parts.Count, file.Data.Length)
                    {
                        ContentType = file.ContentType,
                        CheckSum = checkSum,
                    };
                }
                else
                {
                    return;
                }
            }
            else
            {
                nodeFile = new NodeFileInfo(parts.Count, file.Data.Length)
                {
                    ContentType = file.ContentType,
                    CheckSum = checkSum
                };

                Files.Add(file.Name, nodeFile);
            }

            Dictionary<string, List<Block>> hostBlocksList = OptimalHosts(parts);

            foreach (var hostBlocks in hostBlocksList)
            {
                nodeFile.Nodes.Add(new NodeBlockInfo
                {
                    HostName = hostBlocks.Key,
                    Priority = 0,
                    Indexes = hostBlocks.Value.Select(v => v.Info.Index).ToList()
                });
                await _nodeGateway.UploadBlocks(hostBlocks.Key, hostBlocks.Value, forceOwerwritte);
            }
        }

        /// <summary>
        /// Скачать файл
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public async Task<SourceFile> DownloadFile(string fileName)
        {
            SourceFile sourceFile = null;
            int fileSize = 0;
            if (Files.TryGetValue(fileName, out var nodeInfos))
            {
                List<Block> blocks = new List<Block>(nodeInfos.BlockCount);

                for (var index = 0; index < nodeInfos.BlockCount; index++)
                {
                    string nodeHostName = GetOptimalNodeForDownloadBlock(fileName, index);

                    Block block = await _nodeGateway.DownloadBlock(nodeHostName, fileName, index);
                    if (block != null)
                    {
                        blocks.Add(block);
                    }
                    else
                    {
                        break;
                    }
                }

                byte[] data = blocks.OrderBy(b => b.Info.Index).SelectMany(b => b.Data).Take(fileSize).ToArray();
                sourceFile = new SourceFile
                {
                    Name = fileName,
                    Data = data,
                    ContentType = nodeInfos.ContentType
                };
            }
            return sourceFile;
        }

        /// <summary>
        /// Скачать i-й блок файла
        /// </summary>
        /// <param name="fileName">Имя файла</param>
        /// <param name="blockIndex">Блок</param>
        /// <returns></returns>
        public async Task<Block> DownloadBlock(string fileName, int blockIndex)
        {
            Block block = null;

            if (Files.TryGetValue(fileName, out var nodeInfos))
            {
                string nodeHostName = GetOptimalNodeForDownloadBlock(fileName, blockIndex);
                block = await _nodeGateway.DownloadBlock(nodeHostName, fileName, blockIndex);
            }
            return block;
        }

        /// <summary>
        /// Скачать выбранные блоки файла
        /// </summary>
        /// <param name="fileName">Имя файла</param>
        /// <param name="blockIndexes">Индексы блоков</param>
        /// <returns></returns>
        public async Task<List<Block>> DownloadBlocks(string fileName, List<int> blockIndexes)
        {
            List<Block> blocks = null;

            if (Files.TryGetValue(fileName, out var nodeInfos))
            {
                foreach (var blockIndex in blockIndexes)
                {
                    string nodeHostName = GetOptimalNodeForDownloadBlock(fileName, blockIndex);
                    Block block = await _nodeGateway.DownloadBlock(nodeHostName, fileName, blockIndex);
                    blocks.Add(block);
                }
            }
            return blocks;
        }

        /// <summary>
        /// Заменить блок файла во всех узлах
        /// </summary>
        /// <param name="fileName">Имя файла</param>
        /// <param name="data">Блок</param>
        /// <param name="blockIndex">Номер блока</param>
        /// <returns></returns>
        public async Task<State> OwerwritteBlock(string fileName, byte[] data, int blockIndex)
        {
            State state = new State();
            if (Files.TryGetValue(fileName, out var nodeInfos))
            {
                List<string> nodeNames = nodeInfos.Nodes.Where(n => n.Indexes.Contains(blockIndex)).Select(n => n.HostName).ToList();
                foreach (var nodeName in nodeNames)
                {
                    State addState = await _nodeGateway.AddOrOverwritteBlock(nodeName, fileName, data, blockIndex, nodeInfos.BlockCount, true);
                    state += addState;
                }
            }
            else
            {
                state.IsSuccess = false;
                state.Messages.Add("File not found");
            }
            return state;
        }

        /// <summary>
        /// Удалить файл
        /// </summary>
        /// <param name="fileName">Имя файла</param>
        /// <returns></returns>
        public async Task<State> DeleteFile(string fileName)
        {
            State state = new State();
            if (Files.TryGetValue(fileName, out var nodeInfos))
            {
                List<string> nodeNames = nodeInfos.Nodes.Select(n => n.HostName).ToList();
                foreach (var nodeName in nodeNames)
                {
                    await _nodeGateway.DeleteFile(nodeName, fileName);
                }
                Files.Remove(fileName);
            }
            return state;
        }

        /// <summary>
        /// Проверить наличие файла в системе
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public bool Search(string fileName)
        {
            return Files.ContainsKey(fileName);
        }

        #endregion API Methods

        #region Utils

        /// <summary>
        /// Найти оптимальную ноду для записи
        /// </summary>
        /// <returns>Адрес ноды</returns>
        private string GetOptimalNodeForUpload()
        {
            var group = Files.Values.SelectMany(x => x.Nodes)
                .GroupBy(x => x.HostName)
                .Select(x => new
                {
                    HostName = x.Key,
                    Sum = x.Sum(x => x.Indexes.Count),
                    Priority = x.Average(t => t.Priority)
                })
                .OrderBy(x => x.Sum)
                .ThenBy(x => x.Priority);

            return group.FirstOrDefault()?.HostName;
        }

        /// <summary>
        /// Оптимальная нода для скачивания блока файла
        /// </summary>
        /// <returns></returns>
        private string GetOptimalNodeForDownloadBlock(string fileName, int blockIndex)
        {
            string nodeHostName = null;
            if (Files.TryGetValue(fileName, out NodeFileInfo nodeFileInfo))
            {
                NodeBlockInfo nodeBlockInfo = nodeFileInfo.Nodes.Where(n => n.Indexes.Contains(blockIndex)).OrderBy(n => n.Priority).FirstOrDefault();
                nodeHostName = nodeBlockInfo?.HostName;
            }
            return nodeHostName;
        }

        /// <summary>
        /// Разделить файл на блоки
        /// </summary>
        /// <param name="sourceFile"></param>
        /// <returns></returns>
        private List<Block> SplitFile(SourceFile sourceFile)
        {
            int blockSize =
             //(int)Math.Pow(2, 1);
             _configuration.BlockSize;

            int blockCount = (int)Math.Ceiling(sourceFile.Data.Length * 1d / blockSize);
            List<Block> blocks = new List<Block>();

            if (sourceFile != null)
            {
                for (var i = 0; i < blockCount; i++)
                {
                    var data = sourceFile.Data.Skip(i * blockSize).Take(blockSize).ToArray();

                    Block block = new Block
                    {
                        Data = data,
                        Info = new BlockInfo
                        {
                            FileName = sourceFile.Name,
                            Index = i,
                            TotalBlockCount = blockCount
                        }
                    };
                    blocks.Add(block);
                }
            }
            return blocks;
        }

        private Dictionary<string, List<Block>> OptimalHosts(List<Block> blocks)
        {
            var optimalHosts = new Dictionary<string, List<Block>>();

            foreach (var block in blocks)
            {
                NodeBlockInfo nodeBlockInfo = Files.Values.SelectMany(f => f.Nodes).OrderBy(x => x.Indexes.Count).ThenBy(x => x.Priority).FirstOrDefault();
                nodeBlockInfo.Indexes.Add(block.Info.Index);
                if (!optimalHosts.TryGetValue(nodeBlockInfo.HostName, out var parts))
                {
                    parts = new List<Block>();
                    optimalHosts.Add(nodeBlockInfo.HostName, parts);
                }
                parts.Add(block);
            }

            return optimalHosts;
        }

        private bool ValidateSum(byte[] src, byte[] src2)
        {
            return CalculateMD5(src) == CalculateMD5(src2);
        }

        private string CalculateMD5(byte[] data)
        {
            using (var md5 = MD5.Create())
            {
                var hash = md5.ComputeHash(data);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        #endregion Utils
    }
}