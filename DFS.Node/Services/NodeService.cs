using DFS.Node.Models;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DFS.Node.Services
{
    public class NodeService
    {
        private readonly NodeConfiguration _configuration;

        private readonly string _blockNamePrefix = "Block_";

        private string DataPath => $"{_configuration.RootPath}\\{_configuration.NodeName}";

        private Dictionary<string, List<BlockInfo>> Files { get; set; }

        public NodeService(IOptions<NodeConfiguration> configuration)
        {
            _configuration = configuration.Value;
            Files = new Dictionary<string, List<BlockInfo>>();
            InitNode();
        }

        #region API Methods

        /// <summary>
        /// Добавить или перезаписать файл
        /// </summary>
        /// <param name="file"></param>
        public async Task<bool> AddOrRewriteFile(PartilFile file)
        {
            try
            {
                bool state = false;
                if (file != null)
                {
                    if (Files.ContainsKey(file.FileName))
                    {
                        state = DeleteFile(file.FileName);
                    }
                    else
                    {
                        state = true;
                    }
                    bool isCreated = await AddFile(file);
                    state = isCreated && state;
                }
                return state;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// Удалить файл
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public bool DeleteFile(string fileName)
        {
            try
            {
                if (Files.ContainsKey(fileName))
                {
                    Files.Remove(fileName);
                }

                string fileDirectoryPath = $"{DataPath}/{fileName}";
                if (Directory.Exists(fileDirectoryPath))
                {
                    Directory.Delete(fileDirectoryPath, true);
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// Добавить или перезаписать блок
        /// </summary>
        /// <param name="block"></param>
        public async Task<bool> AddOrRewriteBlock(Block block)
        {
            try
            {
                bool state = false;

                if (block != null && block.Info != null && !string.IsNullOrEmpty(block.Info.FileName))
                {
                    if (Files.TryGetValue(block.Info.FileName, out var blocks))
                    {
                        if (blocks.Exists(b => b.Index == block.Info.Index))
                        {
                            state = RemoveBlock(block);
                        }
                        state = await AddBlock(block);
                    }
                    else
                    {
                        state = await AddOrRewriteFile(new PartilFile
                        {
                            FileName = block.Info.FileName,
                            Blocks = new List<Block> { block }
                        });
                    }
                }
                return state;
            }
            catch (Exception ex)
            {
                return false;
            }
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

        public bool BlockExist(string fileName, int blockIndex)
        {
            bool state = false;
            if (Files.TryGetValue(fileName, out var blocks))
            {
                state = blocks.Exists(b => b.Index == blockIndex);
            }
            return state;
        }

        public bool BlockExist(BlockInfo blockInfo)
        {
            return BlockExist(blockInfo.FileName, blockInfo.Index);
        }

        #endregion API Methods

        /// <summary>
        /// Инициализация
        /// </summary>
        private void InitNode()
        {
            if (!Directory.Exists(_configuration.RootPath))
            {
                Directory.CreateDirectory(_configuration.RootPath);
            }

            if (!Directory.Exists(DataPath))
            {
                Directory.CreateDirectory(DataPath);
            }

            Files = ParseDataDirectory();
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

        private async Task<bool> AddFile(PartilFile file)
        {
            try
            {
                Files.Add(file.FileName, file.Blocks.Select(b => b.Info).ToList());
                string fileDirectoryPath = $"{DataPath}/{file.FileName}";
                Directory.CreateDirectory(fileDirectoryPath);

                foreach (var block in file.Blocks)
                {
                    await AddBlock(block);
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private bool RemoveBlock(Block block)
        {
            try
            {
                if (BlockExist(block.Info))
                {
                    string blockFileName = BlockFileName(block);
                    string blockPath = $"{DataPath}/{block.Info.FileName}/{blockFileName}";

                    if (File.Exists(blockPath))
                    {
                        File.Delete(blockPath);
                    }

                    if (Files.TryGetValue(block.Info.FileName, out var blocks))
                    {
                        BlockInfo blockInfo = blocks.FirstOrDefault(b => b.Index == b.Index);
                        if (blockInfo != null)
                        {
                            blocks.Remove(blockInfo);
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private async Task<bool> AddBlock(Block block)
        {
            try
            {
                string blockFileName = BlockFileName(block);
                string blockPath = $"{DataPath}/{block.Info.FileName}/{blockFileName}";

                using (FileStream fs = File.Create(blockPath, block.Data.Length))
                {
                    await fs.WriteAsync(block.Data, 0, block.Data.Length);
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
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

        private string BlockFileName(Block block)
        {
            return $"{_blockNamePrefix}{block.Info.Index}";
        }

        #endregion Utils
    }
}