﻿using System.Collections.Generic;

namespace DFS.Balancer.Models
{
    public class NodeFileInfo
    {
        /// <summary>
        /// Число блоков в файле
        /// </summary>
        public int BlockCount { get; set; }

        /// <summary>
        /// Тип файла
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// MD5 сумма
        /// </summary>
        public string CheckSum { get; set; }

        /// <summary>
        /// Узлы
        /// </summary>
        public List<NodeBlockInfo> Nodes { get; set; }

        public NodeFileInfo(int blockCount)
        {
            Nodes = new List<NodeBlockInfo>();
            BlockCount = blockCount;
        }
    }
}