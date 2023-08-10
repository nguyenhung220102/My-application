using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Points_application.Models;

namespace My_application_api.Services
{
    public class MongoDBService
    {
        private readonly IMongoCollection<Node> _nodeCollection;
        public MongoDBService()
        {
            MongoClient client = new MongoClient("mongodb://localhost:27017");
            IMongoDatabase database = client.GetDatabase("QuadTreeNodes");
            _nodeCollection = database.GetCollection<Node>("Node");
        }
    }
}
