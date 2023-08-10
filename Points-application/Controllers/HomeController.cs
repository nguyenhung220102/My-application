using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Points_application.Models;
using MongoDB.Driver;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using MongoDB.Bson;

namespace Points_application.Controllers
{
    public class HomeController : Controller
    {
        private IMongoCollection<Node> _nodeCollection;
        public ActionResult Main()
        {
            ViewBag.Message = "Your main page.";

            return View();
        }
        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }
        public ActionResult Tutorial()
        {
            ViewBag.Message = "Your tutorial page.";

            return View();
        }

        public ActionResult Output()
        {
            ViewBag.Message = "Your output page.";
            return View();
        }
        [HttpPost]
        public async Task<string> PostData(string name, HttpPostedFileBase file)
        {
            if (file != null && file.ContentLength > 0)
            {
                MongoClient client = new MongoClient("mongodb://localhost:27017");
                await client.DropDatabaseAsync("QuadTreeNode");
                IMongoDatabase database = client.GetDatabase("QuadTreeNode");
                _nodeCollection = database.GetCollection<Node>("Node");
                var collections = new Dictionary<string, IMongoCollection<Point>>();
                var writeModel = new Dictionary<string, HashSet<WriteModel<Point>>>();
                var bulkWrite = new BulkWriteOptions() { IsOrdered = false };
                var points = new HashSet<WriteModel<Point>>();
                var nodes = new List<Node>();

                using (var reader = new StreamReader(file.InputStream))
                {
                    var radiusRegex = @"\(x,y\) - Max value of X,y = \d{1,3}(?:\.\d{3})*$";
                    var radius = reader.ReadLine();
                    if (!Regex.IsMatch(radius, radiusRegex))
                    {
                        return "Line 1|   " + radius + " (Invalid format! Please resubmit a valid file)";
                    }
                    //return radius;
                    radius = radius.Substring(radius.LastIndexOf(' ') + 1);
                    radius = radius.Replace(".", string.Empty);
                    CreateTree(nodes, "1", Int32.Parse(radius), -Int32.Parse(radius), Int32.Parse(radius), -Int32.Parse(radius), 0, 4, collections, database, writeModel);
                    foreach (var node in nodes)
                    {
                        _nodeCollection.InsertOne(node);
                    }
                    var r = Int32.Parse(radius);

                    // Ghi giá tprị radius vào file config
                    string path = Server.MapPath("~/Scripts/Configuration.js");
                    string config = System.IO.File.ReadAllText(path);
                    config = Regex.Replace(config, @"var radius = [0-9]+;", "var radius = " + radius + ";");
                    System.IO.File.WriteAllText(path, config);
                    string line;
                    int i = 2;
                    var count = 0;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (!lineValidate(line))
                        {
                            if (name == "skip") continue;
                            return "Line " + i.ToString() + "|   " + line + " (Invalid data format!)";
                        } 
                        string[] ints= line.Split(' ');
                        int x = Int32.Parse(ints[0]);
                        int y = Int32.Parse(ints[1]);
                        if (x * x + y * y > r * r) continue;
                        count++;
                        InsertPoint(x, y, "1", Int32.Parse(radius), -Int32.Parse(radius), Int32.Parse(radius), -Int32.Parse(radius), writeModel);
                        if (count == 10000)
                        {
                            foreach(var key in collections.Keys)
                            {
                                try
                                {
                                    if (writeModel[key].Count() != 0)
                                    {
                                        await collections[key].BulkWriteAsync(writeModel[key], bulkWrite);
                                        writeModel[key].Clear();
                                    }
                                }
                                catch (Exception ex)
                                {
                                    return "Lỗi khi bulk write: " + ex.Message;
                                }
                            }
                            count = 0;
                        }
                    }
                    if (count > 0)
                    {
                        foreach (var key in collections.Keys)
                        {
                            if (writeModel[key].Count() != 0)
                            {
                                await collections[key].BulkWriteAsync(writeModel[key], bulkWrite);
                                writeModel[key].Clear();
                            }
                        }
                    }
                };
            } else
            {
                return "success";
            }
            return "success";
        }
        public void CreateTree(List<Node> nodes, string index, int xMax, int xMin, int yMax, int yMin,int stopLevel, int maxLevel, Dictionary<string, IMongoCollection<Point>> collections, IMongoDatabase database, Dictionary<string, HashSet<WriteModel<Point>>> writeModel)
        {
            if (stopLevel != maxLevel)
            {
                stopLevel += 1;
                Node node = new Node()
                {
                    Id = index,
                    xMax = xMax,
                    xMin = xMin,
                    yMax = yMax,
                    yMin = yMin,
                };
                var collection = database.GetCollection<Point>(index);
                var idxKeys = Builders<Point>.IndexKeys.Ascending("X").Ascending("Y");
                var idxModel = new CreateIndexModel<Point>(idxKeys);
                collection.Indexes.CreateOne(idxModel);

                collections.Add(index, collection);
                writeModel.Add(index, new HashSet<WriteModel<Point>>());
                nodes.Add(node);
                CreateTree(nodes, index + "1", (xMin + xMax)/2, xMin, yMax, (yMin + yMax) / 2, stopLevel, maxLevel, collections, database, writeModel);
                CreateTree(nodes, index + "2", xMax, (xMin + xMax) / 2, yMax, (yMin + yMax) / 2, stopLevel, maxLevel, collections, database, writeModel);
                CreateTree(nodes, index + "3", xMax, (xMin + xMax) / 2, (yMin + yMax) / 2, yMin, stopLevel, maxLevel, collections, database, writeModel);
                CreateTree(nodes, index + "4", (xMin + xMax) / 2, xMin, (yMin + yMax) / 2, yMin, stopLevel, maxLevel, collections, database, writeModel);
            }
            else
            {
                return;
            }
        }
        public void InsertPoint(int x, int y, string index, int xMax, int xMin, int yMax, int yMin, Dictionary<string, HashSet<WriteModel<Point>>> writeModel)
        {
            var idx = GetIndex(index, x, y, xMax, xMin, yMax, yMin, 0, 3);
            writeModel[idx.Substring(0, 1)].Add(new InsertOneModel<Point>(new Point(((int)((float)x / 40)) * 40, ((int)((float)y / 40)) * 40)));
            writeModel[idx.Substring(0, 2)].Add(new InsertOneModel<Point>(new Point(((int)((float)x / 20)) * 20, ((int)((float)y / 20)) * 20)));
            writeModel[idx.Substring(0, 3)].Add(new InsertOneModel<Point>(new Point(((int)((float)x / 10)) * 10, ((int)((float)y / 10)) * 10)));
            writeModel[idx].Add(new InsertOneModel<Point>(new Point(x, y)));
        }

        public async Task AddToDB(List<Node> nodes)
        {
            foreach (var node in nodes)
            {
                await _nodeCollection.InsertOneAsync(node);
            }
        }
        public string GetIndex(string index, int x, int y, int xMax, int xMin, int yMax, int yMin, int minLevel, int maxLevel)
        {
            if (minLevel != maxLevel)
            {
                minLevel += 1;
                var centerX = (xMin + xMax) / 2;
                var centerY = (yMin + yMax) / 2;
                if (x <= centerX && y >= centerY) return GetIndex(index + "1", x, y, centerX, xMin, yMax, centerY, minLevel, maxLevel);
                if (x > centerX && y >= centerY) return GetIndex(index + "2", x, y, xMax, centerX, yMax, centerY, minLevel, maxLevel);
                if (x > centerX && centerY > y) return GetIndex(index + "3", x, y, xMax, centerX, centerY, yMin, minLevel, maxLevel);
                return GetIndex(index + "4", x, y, centerX, xMin, centerY, yMin, minLevel, maxLevel);
            }
            else return index;
        }

        public bool lineValidate(string line)
        {
            string[] ints = line.Split(' ');
            if (ints.Length != 2) return false;
            if (int.TryParse(ints[0], out int m) && int.TryParse(ints[1], out int n)) return true;
            return false;
        }

        [HttpGet]
        public ActionResult GetDataByLevel(int level, int xMin, int xMax, int yMin, int yMax)
        {
            MongoClient client = new MongoClient("mongodb://localhost:27017");
            IMongoDatabase database = client.GetDatabase("QuadTreeNode");
            _nodeCollection = database.GetCollection<Node>("Node");
            if (level > 4) level = 4;
            var filter = Builders<Node>.Filter;
            var filter1 = filter.Where(p => p.Id.ToString().Length == level);
            var filter2 = filter.Where(p => p.xMax >= xMin && p.yMin <= yMax);
            var filter3 = filter.Where(p => p.xMin <= xMax && p.yMin <= yMax);
            var filter4 = filter.Where(p => p.xMax >= xMin && p.yMax >= yMin);
            var filter5 = filter.Where(p => p.xMin <= xMax && p.yMax >= yMin);
            var filterOr = filter.Or(filter2, filter3, filter4, filter5);
            var filterAnd = filter.And(filter1, filterOr);
            var names = _nodeCollection.Distinct<string>("_id", filterAnd).ToList();
            var points = new HashSet<BsonDocument>();
            foreach (var name in names)
            {
                var collection = database.GetCollection<Point>(name);
                var pointlist = collection.Aggregate()
                     .Match(new BsonDocument
                     {
                        { "X", new BsonDocument { { "$gte", xMin }, { "$lte", xMax } } },
                        { "Y", new BsonDocument { { "$gte", yMin }, { "$lte", yMax } } }
                     })
                     .Group(new BsonDocument
                     {
                        { "_id", new BsonDocument { { "X", "$X" }, { "Y", "$Y" } } }
                     })
                     .Project(new BsonDocument
                     {
                        { "_id", 0 },
                        { "X", "$_id.X" },
                        { "Y", "$_id.Y" }
                     })
                     .ToList();
                    points.UnionWith(pointlist);
            }
            string json = JsonConvert.SerializeObject(points);
            return Content(json, "application/json");
        }

    }
}