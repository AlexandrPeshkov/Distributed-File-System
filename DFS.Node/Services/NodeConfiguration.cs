namespace DFS.Node.Services
{
    public class NodeConfiguration
    {
        public string RootPath { get; set; }

        public string NodeName { get; set; }

        public int BlockSize { get; set; }

        /// <summary>
        /// Адрес балансировщика
        /// </summary>
        public string BalancerHostName { get; set; }

        /// <summary>
        /// Порт балансировщика
        /// </summary>
        public string BalancerHostPort { get; set; }
    }
}