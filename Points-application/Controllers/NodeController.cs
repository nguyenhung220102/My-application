using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Points_application.Models;
using Newtonsoft.Json.Linq;
using System.Web.UI.WebControls;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Points_application.Controllers
{
    public class NodeController : Controller
    {
        // GET: Node
        Uri baseAddress = new Uri("https://localhost:7293/");
        private readonly HttpClient _httpClient;
        public NodeController()
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = baseAddress;
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
        public async Task CreateNode(JObject obj)
        {
            try
            {
                HttpResponseMessage response = await _httpClient.PostAsJsonAsync("api/Node/", obj);
            }
            catch (HttpRequestException exception)
            {
                ViewBag.Res =  exception.Message;
            }
        }

    }
}