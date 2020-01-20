using System;

namespace DFS.Node.Models
{
    public class Block
    {
        public Guid Id { get; set; }

        public int Index { get; set; }

        public byte[] Data { get; set; }
    }
}