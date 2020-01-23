using DFS.Balancer.Models;
using DFS.Balancer.Services;
using DFS.Node.Models;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace DFS.Node.Services
{
    public class NodeService
    {
        private readonly NodeConfiguration _configuration;
        private readonly HostConfig _hostConfig;

        private readonly string _blockNamePrefix = "Block_";

        private readonly string _apiPrefix = "DFS";

        private string DataPath => $"{_configuration.RootPath}\\{_configuration.NodeName}";

        private Dictionary<string, List<BlockInfo>> Files { get; set; }

        public NodeService(IOptions<NodeConfiguration> configuration, IOptions<HostConfig> hostConfig)
        {
            _configuration = configuration.Value;
            _hostConfig = hostConfig.Value;
            Files = new Dictionary<string, List<BlockInfo>>();
            InitNode();
            ConnectNode().Wait();
        }

        #region API Methods

        /// <summary>
        /// Добавить или перезаписать файл
        /// </summary>
        /// <param name="file">Файл</param>
        /// <param name="forceOwerrite">Перезаписать существующий</param>
        public async Task<State> TryAddFile(PartialFile file, bool forceOwerrite = false)
        {
            State state = new State();
            //try
            {
                if (file != null)
                {
                    if (Files.ContainsKey(file.FileName))
                    {
                        if (forceOwerrite)
                        {
                            State isDeleted = DeleteFile(file.FileName);
                            state += isDeleted;
                            if (!isDeleted)
                            {
                                return state;
                            }
                        }
                        else
                        {
                            state.IsSuccess = false;
                            state.Messages.Add($"File with name {file.FileName} already exist");
                            return state;
                        }
                    }
                    State isWritten = await WriteFile(file);
                    state += isWritten;
                    if (isWritten)
                    {
                        Files.Add(file.FileName, file.Blocks.Select(b => b.Info).ToList());
                    }
                }
                else
                {
                    state.IsSuccess = false;
                    state.Messages.Add("File is null");
                }
                return state;
            }
            //catch (Exception ex)
            //{
            //    state.IsSuccess = false;
            //    state.Messages.Add(ex.Message);
            //    return state;
            //}
        }

        /// <summary>
        /// Добавить или перезаписать блок
        /// </summary>
        /// <param name="block"></param>
        /// <param name="forceOwerrite">Перезаписать существующий</param>
        public async Task<State> TryAddBlock(Block block, bool forceOwerrite = false)
        {
            State state = new State();
            try
            {
                if (block != null && block.Info != null && !string.IsNullOrEmpty(block.Info.FileName))
                {
                    if (Files.TryGetValue(block.Info.FileName, out var blocks))
                    {
                        if (blocks.Exists(b => b.Index == block.Info.Index))
                        {
                            if (forceOwerrite)
                            {
                                State isRemoved = DeleteBlock(block.Info.FileName, block.Info.Index);
                                blocks.Add(new BlockInfo
                                {
                                    FileName = block.Info.FileName,
                                    Index = block.Info.Index,
                                    TotalBlockCount = block.Info.TotalBlockCount
                                });
                            }
                            else
                            {
                                state.IsSuccess = false;
                                state.Messages.Add($"Block with index {block.Info.Index} already exist for file {block.Info.FileName}");
                                return state;
                            }
                        }
                        state = await WriteBlock(block);
                    }
                    else
                    {
                        state = await TryAddFile(new PartialFile
                        {
                            FileName = block.Info.FileName,
                            Blocks = new List<Block> { block },
                            TotalBlockCount = block.Info.TotalBlockCount
                        });
                    }
                }
                else
                {
                    state.IsSuccess = false;
                    state.Messages.Add("Block is incorrect");
                }
                return state;
            }
            catch (Exception ex)
            {
                state.IsSuccess = false;
                state.Messages.Add(ex.Message);
                return state;
            }
        }

        /// <summary>
        /// Удалить файл
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public State DeleteFile(string fileName)
        {
            State state = new State();
            try
            {
                if (FileExist(fileName))
                {
                    Files.Remove(fileName);
                }
                else
                {
                    state.IsSuccess = false;
                    state.Messages.Add($"File {fileName} not registered");
                    //return state;
                }

                string fileDirectoryPath = $"{DataPath}/{fileName}";
                if (Directory.Exists(fileDirectoryPath))
                {
                    Directory.Delete(fileDirectoryPath, true);
                }
                return state;
            }
            catch (Exception ex)
            {
                state.IsSuccess = false;
                state.Messages.Add(ex.Message);
                return state;
            }
        }

        /// <summary>
        /// Удалить блок
        /// </summary>
        /// <param name="fileName">Имя файла</param>
        /// <param name="index">Индекс</param>
        /// <returns></returns>
        public State DeleteBlock(string fileName, int index)
        {
            State state = new State();
            try
            {
                if (TryGetBlockInfo(fileName, index, out var blockInfo))
                {
                    string blockFileName = BlockFileName(blockInfo);
                    string blockPath = $"{DataPath}/{blockInfo.FileName}/{blockFileName}";

                    if (File.Exists(blockPath))
                    {
                        File.Delete(blockPath);
                    }

                    if (Files.TryGetValue(blockInfo.FileName, out var blocks))
                    {
                        BlockInfo existBlock = blocks.FirstOrDefault(b => b.Index == b.Index);
                        if (blockInfo != null)
                        {
                            blocks.Remove(blockInfo);
                        }
                        else
                        {
                            state.IsSuccess = false;
                            state.Messages.Add($"Block {blockInfo.Index} not registered for file {blockInfo.FileName}");
                        }
                    }
                }
                else
                {
                    state.IsSuccess = false;
                    state.Messages.Add($"Block {blockInfo.Index} not registered for file {blockInfo.FileName}");
                }
                return state;
            }
            catch (Exception ex)
            {
                state.IsSuccess = false;
                state.Messages.Add(ex.Message);
                return state;
            }
        }

        /// <summary>
        /// Считать блок
        /// </summary>
        /// <param name="fileName">Имя файла</param>
        /// <param name="blockIndex">Индекс блока</param>
        /// <returns></returns>
        public async Task<Block> GetBlock(string fileName, int blockIndex)
        {
            Block block = null;

            if (TryGetBlockInfo(fileName, blockIndex, out var blockInfo))
            {
                string blockFileName = BlockFileName(blockInfo);
                string blockPath = $"{DataPath}/{blockInfo.FileName}/{blockFileName}";

                if (File.Exists(blockPath))
                {
                    byte[] buffer = new byte[_configuration.BlockSize];
                    using (var reader = File.OpenRead(blockPath))
                    {
                        await reader.ReadAsync(buffer, 0, _configuration.BlockSize);
                    }

                    block = new Block
                    {
                        Info = blockInfo,
                        Data = buffer
                    };
                }
            }
            return block;
        }

        /// <summary>
        /// Проверка наличия блоков принадлежащих файлу
        /// </summary>
        /// <param name="fileName">Имя файла</param>
        /// <returns></returns>
        public bool FileExist(string fileName)
        {
            return Files.ContainsKey(fileName);
        }

        public bool TryGetBlockInfo(string fileName, int blockIndex, out BlockInfo blockInfo)
        {
            blockInfo = null;
            if (Files.TryGetValue(fileName, out var blocks))
            {
                blockInfo = blocks.FirstOrDefault(b => b.Index == blockIndex);
            }
            return blockInfo != null;
        }

        #endregion API Methods

        /// <summary>
        /// Инициализация
        /// </summary>
        private void InitNode(bool clearOnStart = true)
        {
            if (!Directory.Exists(_configuration.RootPath))
            {
                Directory.CreateDirectory(_configuration.RootPath);
            }

            if (!Directory.Exists(DataPath))
            {
                Directory.CreateDirectory(DataPath);
            }

            if (clearOnStart)
            {
                ClearDirectory(DataPath);
            }
            else
            {
                Files = ParseDataDirectory();
            }
        }

        private async Task ConnectNode()
        {
            HttpClient httpClient = new HttpClient();

            string url = $"http://{_configuration.BalancerHostName}:{_configuration.BalancerHostPort}/{_apiPrefix}/RegNode";

            Dictionary<string, FileMeta> fileMeta = new Dictionary<string, FileMeta>();

            foreach (var fileInfo in Files)
            {
                FileMeta meta = new FileMeta
                {
                    FileName = fileInfo.Key,
                    TotalBlockCount = fileInfo.Value.FirstOrDefault().TotalBlockCount
                };
                if (!fileMeta.TryGetValue(fileInfo.Key, out meta))
                {
                    fileMeta.Add(fileInfo.Key, meta);
                }

                foreach (var info in fileInfo.Value)
                {
                    meta.Indexes.Add(info.Index);
                }
            }

            NodeInfo nodeInfo = new NodeInfo
            {
                NodeUrl = _hostConfig.Url,
                PartialFiles = fileMeta.Values.ToList()
            };

            //string json = JsonConvert.SerializeObject(nodeInfo);

            JsonContent content = new JsonContent(nodeInfo);
            await httpClient.PostAsync(url, content);
        }

        private Dictionary<string, List<BlockInfo>> ParseDataDirectory()
        {
            var files = new Dictionary<string, List<BlockInfo>>();
            var fileDirectories = Directory.GetDirectories(DataPath);
            foreach (var fileDirectory in fileDirectories)
            {
                var blocks = Directory.GetFiles(fileDirectory, $"{_blockNamePrefix}*");
                string fileName = Path.GetFileName(fileDirectory);

                var blockNames = blocks.Select(b => Path.GetFileName(b));
                var blocksMeta = new List<BlockInfo>();

                foreach (var blockName in blockNames)
                {
                    if (TryParseBlockIndex(blockName, out var index))
                    {
                        var blockInfo = new BlockInfo
                        {
                            Index = index,
                            FileName = fileName,
                        };
                        blocksMeta.Add(blockInfo);
                    }
                }
                files.Add(fileName, blocksMeta);
            }
            return files;
        }

        /// <summary>
        /// Записать файл на диск
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private async Task<State> WriteFile(PartialFile file)
        {
            State state = new State();
            //try
            {
                string fileDirectoryPath = $"{DataPath}/{file.FileName}";
                if (Directory.Exists(fileDirectoryPath))
                {
                    Directory.Delete(fileDirectoryPath, true);
                }
                Directory.CreateDirectory(fileDirectoryPath);

                foreach (var block in file.Blocks)
                {
                    State isWritten = await WriteBlock(block);
                    state.IsSuccess = state && isWritten;
                    if (!isWritten)
                    {
                        isWritten.Messages.AddRange(isWritten.Messages);
                        break;
                    }
                }
                return state;
            }
            //catch (Exception ex)
            //{
            //    state.IsSuccess = false;
            //    state.Messages.Add(ex.Message);
            //    return state;
            //}
        }

        /// <summary>
        /// Записать блок на диск
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        private async Task<State> WriteBlock(Block block)
        {
            State state = new State();
            //try
            {
                if (block != null && block.Info != null && !string.IsNullOrEmpty(block.Info.FileName))
                {
                    string blockFileName = BlockFileName(block.Info);
                    string blockPath = $"{DataPath}/{block.Info.FileName}/{blockFileName}";
                    if (File.Exists(blockPath))
                    {
                        File.Delete(blockPath);
                        state.Messages.Add($"Block #{block.Info.Index} has been owerwritten");
                    }

                    await File.WriteAllBytesAsync(blockPath, block.Data);
                }
                else
                {
                    throw new ArgumentException("Incorrect block object, please check file name");
                }
                return state;
            }
            //catch (Exception ex)
            //{
            //    state.IsSuccess = false;
            //    state.Messages.Add(ex.Message);
            //    return state;
            //}
        }

        #region Utils

        private bool TryParseBlockIndex(string blockPath, out int index)
        {
            index = -1;
            string name = Path.GetFileName(blockPath);
            int valueIndex = name.IndexOf(_blockNamePrefix);
            if (valueIndex > 0)
            {
                string value = name.Substring(valueIndex, name.Length - valueIndex - 1);
                return int.TryParse(value, out index);
            }
            return false;
        }

        private string BlockFileName(BlockInfo blockInfo)
        {
            return $"{_blockNamePrefix}{blockInfo.Index}";
        }

        private void ClearDirectory(string path)
        {
            DirectoryInfo di = new DirectoryInfo(path);
            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo dir in di.GetDirectories())
            {
                dir.Delete(true);
            }
        }

        #endregion Utils
    }
}