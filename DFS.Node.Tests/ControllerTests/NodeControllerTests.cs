using DFS.Node.Controllers;
using DFS.Node.Models;
using DFS.Node.Requests;
using DFS.Node.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace DFS.Node.Tests.ControllerTests
{
    public class NodeControllerTests
    {
        private readonly NodeConfiguration _nodeConfiguration;
        private readonly HostConfig _hostConfig;
        private readonly NodeService _nodeService;
        private readonly IConfigurationRoot _configuration;
        private readonly IServiceProvider _serviceProvider;

        private readonly NodeController _nodeController;

        public NodeControllerTests()
        {
            var configBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();
            _configuration = configBuilder.Build();

            var services = new ServiceCollection();
            services.AddOptions(); // this statement is required if you wanna use IOption Pattern.

            services.Configure<NodeConfiguration>(_configuration.GetSection(nameof(NodeConfiguration)));
            services.Configure<HostConfig>(_configuration.GetSection(nameof(HostConfig)));

            _serviceProvider = services.BuildServiceProvider();

            IOptions<NodeConfiguration> optionAccessor = _serviceProvider.GetService<IOptions<NodeConfiguration>>();
            _nodeConfiguration = optionAccessor.Value;

            IOptions<HostConfig> optionAccessorHost = _serviceProvider.GetService<IOptions<HostConfig>>();
            _hostConfig = optionAccessorHost.Value;

            _nodeService = new NodeService(optionAccessor, optionAccessorHost);
            _nodeController = new NodeController(_nodeService);
        }

        [Fact]
        public async Task Write_File()
        {
            string fileName = "testFile";
            int totalBlockCount = 2;
            PartialFile partilFile = new PartialFile
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
                            Index = 0,
                            TotalBlockCount = totalBlockCount
                        }
                    },

                    new Block
                    {
                        Data = new byte[]{ 6,7,8,9},
                        Info = new BlockInfo
                        {
                            FileName = fileName,
                            Index = 1,
                            TotalBlockCount = totalBlockCount
                        }
                    }
                }
            };

            SavePartialFileRequest request = new SavePartialFileRequest
            {
                PartilFile = partilFile,
                ForceOwerrite = true
            };

            IActionResult actionResult = await _nodeController.SaveFile(request);

            OkObjectResult ok = actionResult as OkObjectResult;
            Assert.NotNull(ok);
            State state = ok.Value as State;
            Assert.NotNull(state);
            Assert.True(state.IsSuccess);

            Assert.True(_nodeService.FileExist(fileName));
            Assert.True(_nodeService.TryGetBlockInfo(fileName, 0, out var info));
            Assert.True(_nodeService.TryGetBlockInfo(fileName, 1, out var info2));
        }

        [Fact]
        public void Delete_File()
        {
            string fileName = "testFile";

            IActionResult actionResult = _nodeController.DeleteFile(fileName);
            OkObjectResult ok = actionResult as OkObjectResult;
            Assert.NotNull(ok);
            State state = ok.Value as State;
            Assert.NotNull(state);
            Assert.True(state.IsSuccess);

            Assert.False(_nodeService.FileExist(fileName));
            Assert.True(_nodeService.TryGetBlockInfo(fileName, 0, out var info));
            Assert.True(_nodeService.TryGetBlockInfo(fileName, 1, out var info2));

            string filePath = $"{_nodeConfiguration.RootPath}\\{_nodeConfiguration.NodeName}\\{fileName}";
            Assert.False(Directory.Exists(filePath));
        }
    }
}