using Microsoft.AspNetCore.Mvc;

namespace DFS.Balancer.Models
{
    public class ErrorFileModel : FileResult
    {
        public ErrorFileModel(string contentType) : base(contentType)
        {
        }
    }
}
