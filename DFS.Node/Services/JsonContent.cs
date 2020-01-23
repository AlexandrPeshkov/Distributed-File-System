﻿using Newtonsoft.Json;
using System.Net.Http;
using System.Text;

namespace DFS.Balancer.Services
{
    public class JsonContent : StringContent
    {
        public JsonContent(object obj) : base(JsonConvert.SerializeObject(obj), Encoding.UTF8, "application/json")
        {
        }
    }
}