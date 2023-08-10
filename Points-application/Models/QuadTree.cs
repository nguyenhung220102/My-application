using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;
using System.Drawing;
using MongoDB.Bson;
using System.Text.Json.Serialization;

namespace Points_application.Models
{
    public class Node
    {
        public string Id { get; set; }
        public int? xMin { get; set; }
        public int? xMax { get; set; }
        public int? yMin { get; set; }
        public int? yMax { get; set; }
    }
    public class Point
    {
        [BsonId]
        public ObjectId Id { get; set; }
        [BsonElement("X")]
        public int? X { get; set; }
        [BsonElement("Y")]
        public int? Y { get; set; }
        public Point(int? x, int? y)
        {
            X = x;
            Y = y;
        }
    }
}