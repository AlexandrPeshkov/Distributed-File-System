using DFS.Node.Models;
using DFS.Node.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace DFS.Node.Tests.ServiceTests
{
    public class NodeServiceTests : IDisposable
    {
        private readonly NodeConfiguration _nodeConfiguration;
        private readonly NodeService _nodeService;

        private readonly IConfigurationRoot _configuration;
        private readonly IServiceProvider _serviceProvider;

        public NodeServiceTests()
        {
            var configBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();
            _configuration = configBuilder.Build();

            var services = new ServiceCollection();
            services.AddOptions(); // this statement is required if you wanna use IOption Pattern.

            services.Configure<NodeConfiguration>(_configuration.GetSection(nameof(NodeConfiguration)));
            _serviceProvider = services.BuildServiceProvider();

            IOptions<NodeConfiguration> optionAccessor = _serviceProvider.GetService<IOptions<NodeConfiguration>>();
            _nodeConfiguration = optionAccessor.Value;

            _nodeService = new NodeService(optionAccessor);
        }

        [Fact]
        public void All_Directories_Exist_On_Init()
        {
            Assert.True(Directory.Exists(_nodeConfiguration.RootPath));
            string nodeDataPath = $"{_nodeConfiguration.RootPath}\\{_nodeConfiguration.NodeName}";
            Assert.True(Directory.Exists(nodeDataPath));
        }

        [Fact]
        public async Task Write_Test_File_By_2_Blocks()
        {
            string fileName = "testFile";
            PartilFile partilFile = new PartilFile
            {
                FileName = fileName,
                Blocks = new List<Block>
                {
                    new Block
                    {
                        Data = new byte[]{1, 2, 3, 4, 5},
                        Info = new BlockInfo
                        {
                            FileName = fileName,
                            Index = 0
                        }
                    },

                    new Block
                    {
                        Data = new byte[]{ 6,7,8,9},
                        Info = new BlockInfo
                        {
                            FileName = fileName,
                            Index = 1
                        }
                    }
                }
            };

            bool state = await _nodeService.TryAddFile(partilFile);
            Assert.True(state);
            Assert.True(_nodeService.FileExist(fileName));
            Assert.True(_nodeService.BlockExist(fileName, 0));
            Assert.True(_nodeService.BlockExist(fileName, 1));
        }

        [Fact]
        public void Delete_File()
        {
            string fileName = "testFile";
            bool state = _nodeService.RemoveFile(fileName);

            Assert.True(state);
            Assert.False(_nodeService.FileExist(fileName));
            Assert.False(_nodeService.BlockExist(fileName, 0));
            Assert.False(_nodeService.BlockExist(fileName, 1));

            string filePath = $"{_nodeConfiguration.RootPath}\\{_nodeConfiguration.NodeName}\\{fileName}";
            Assert.False(Directory.Exists(filePath));
        }

        public void Dispose()
        {
        }
    }
}