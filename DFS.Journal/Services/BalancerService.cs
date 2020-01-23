using DFS.Balancer.Gateway;
using DFS.Balancer.Models;
using DFS.Node.Models;
using DFS.Node.Requests;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
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
                    node.PartialFiles.Add(new FileMeta
                    {
                        FileName = "INIT_FILE",
                        Indexes = new List<int> { 0 },
                        TotalBlockCount = 1
                    }
                    );
                }

                foreach (var file in node.PartialFiles)
                {
                    NodeFileInfo nodeFileInfo = null;

                    if (!Files.TryGetValue(file.FileName, out nodeFileInfo))
                    {
                        nodeFileInfo = new NodeFileInfo(file.TotalBlockCount);
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
            List<Block> blocks = SplitFile(file);

            if (Files.TryGetValue(file.Name, out var nodeFile))
            {
                if (forceOwerwritte)
                {
                    nodeFile = new NodeFileInfo(blocks.Count)
                    {
                        ContentType = file.ContentType
                    };
                }
                else
                {
                    return;
                }
            }
            else
            {
                nodeFile = new NodeFileInfo(blocks.Count)
                {
                    ContentType = file.ContentType
                };

                Files.Add(file.Name, nodeFile);
            }

            Dictionary<string, List<Block>> nodeBlocks = new Dictionary<string, List<Block>>();

            foreach (var block in blocks)
            {
                string hostName = GetOptimalNodeForUpload();
                if (!string.IsNullOrEmpty(hostName))
                {
                    if (!nodeBlocks.TryGetValue(hostName, out var tempBlocks))
                    {
                        tempBlocks = new List<Block>();
                        nodeBlocks.Add(hostName, tempBlocks);
                    }
                    tempBlocks.Add(block);
                }
            }

            //List<Task> tasks = new List<Task>();
            foreach (var nodeBlock in nodeBlocks)
            {
                await _nodeGateway.UploadBlocks(nodeBlock.Key, nodeBlock.Value, forceOwerwritte);

                NodeBlockInfo nodeBlockInfo = new NodeBlockInfo
                {
                    HostName = nodeBlock.Key,
                    Priority = 0,
                    Indexes = nodeBlock.Value.Select(b => b.Info.Index).ToList()
                };

                nodeFile.Nodes.Add(nodeBlockInfo);

                //tasks.Add(task);
            }
            //await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Скачать файл
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public async Task<SourceFile> DownloadFile(string fileName)
        {
            SourceFile sourceFile = null;

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

                byte[] data = blocks.OrderBy(b => b.Info.Index).SelectMany(b => b.Data).ToArray();
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
            return Files.Values.SelectMany(x => x.Nodes)
                .GroupBy(x => x.HostName)
                .Select(x => new
                {
                    HostName = x.Key,
                    Sum = x.Sum(x => x.Indexes.Count),
                    Priority = x.Average(t => t.Priority)
                })
                .OrderBy(x => x.Sum)
                .ThenBy(x => x.Priority)
                .FirstOrDefault()
                ?.HostName;
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
            List<Block> blocks = new List<Block>();
            int blockCount = (int)Math.Ceiling(sourceFile.Data.Length * 1d / _configuration.BlockSize);

            if (sourceFile != null)
            {
                for (var i = 0; i < sourceFile.Data.Length; i += _configuration.BlockSize)
                {
                    var data = sourceFile.Data.Skip(i * _configuration.BlockSize).Take(_configuration.BlockSize).ToArray();

                    Block block = new Block
                    {
                        Data = data,
                        Info = new BlockInfo
                        {
                            FileName = sourceFile.Name,
                            Index = i / _configuration.BlockSize,
                            TotalBlockCount = blockCount
                        }
                    };
                    blocks.Add(block);
                }
            }
            return blocks;
        }

        #endregion Utils
    }
}