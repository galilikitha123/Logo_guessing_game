using WiseWork.Models;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Driver.Linq;

namespace WiseWork.Services
{
    public class RoomService
    {
        private readonly IMongoCollection<Room> _rooms;
        private const string AllowedCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private const int PinLength = 6;

        public RoomService(MongoDBService mongoDBService)
        {
            _rooms = mongoDBService.GetCollection<Room>("Rooms");
        }

        public (Room, string) CreateRoom(string playerId, string playerName)
        {
            var room = new Room
            {
                GamePin = GenerateGamePin(),
                Members = new Dictionary<string, string> { { playerId, playerName } },
                CreatorId = playerId
            };
            _rooms.InsertOne(room);
            return (room, playerName);
        }

        public (bool, string) JoinRoom(string gamePin, string playerId, string playerName)
        {
            var room = _rooms.Find(r => r.GamePin == gamePin).FirstOrDefault();
            if (room == null)
                return (false, null);
            room.Members[playerId] = playerName;
            _rooms.ReplaceOne(r => r.GamePin == gamePin, room);
            return (true, playerName);
        }

        public Room GetRoom(string gamePin)
        {
            return _rooms.Find(r => r.GamePin == gamePin).FirstOrDefault();
        }

        public void DeleteRoom(string gamePin)
        {
            _rooms.DeleteOne(r => r.GamePin == gamePin);
        }

        public void RemoveMember(string gamePin, string playerName)
        {
            var room = _rooms.Find(r => r.GamePin == gamePin).FirstOrDefault();
            if (room != null)
            {
                var memberIdToRemove = room.Members.FirstOrDefault(m => m.Value == playerName).Key;
                if (memberIdToRemove != null)
                {
                    room.Members.Remove(memberIdToRemove);
                    _rooms.ReplaceOne(r => r.GamePin == gamePin, room);
                }
            }
        }

        private string GenerateGamePin()
        {
            var random = new Random();
            var pinBuilder = new StringBuilder(PinLength);
            for (int i = 0; i < PinLength; i++)
            {
                pinBuilder.Append(AllowedCharacters[random.Next(AllowedCharacters.Length)]);
            }
            return pinBuilder.ToString();
        }

        //public void UpdatePlayerScore(string gamePin, string playerId, int score)
        //{
        //    var filter = Builders<Room>.Filter.Eq(r => r.GamePin, gamePin);
        //    var update = Builders<Room>.Update
        //        .Set($"PlayerScores.{playerId}", score)
        //        .Set(r => r.Players.FirstMatchingElement().Score, score);

        //    _rooms.UpdateOne(filter & Builders<Room>.Filter.ElemMatch(r => r.Players, p => p.Id == playerId), update);
        //}



        public void UpdatePlayerScore(string gamePin, string playerId, int score)
        {
            var room = _rooms.Find(r => r.GamePin == gamePin).FirstOrDefault();
            if (room != null)
            {
                if (room.PlayerScores.ContainsKey(playerId))
                {
                    room.PlayerScores[playerId] = score;
                }
                else
                {
                    room.PlayerScores.Add(playerId, score);
                }
                _rooms.ReplaceOne(r => r.GamePin == gamePin, room);
            }
        }



        // Add this method to update the current question index
        public void UpdateCurrentQuestionIndex(string gamePin, int index)
        {
            var filter = Builders<Room>.Filter.Eq(r => r.GamePin, gamePin);
            var update = Builders<Room>.Update.Set(r => r.CurrentQuestionIndex, index);
            _rooms.UpdateOne(filter, update);
        }

        //public Dictionary<string, int> GetFinalLeaderboard(string gamePin)
        //{
        //    var room = GetRoom(gamePin);
        //    if (room != null)
        //    {
        //        return room.Members.ToDictionary(
        //            m => m.Value,
        //            m => room.PlayerScores.ContainsKey(m.Key) ? room.PlayerScores[m.Key] : 0
        //        );
        //    }
        //    return new Dictionary<string, int>();
        //}

        public Dictionary<string, int> GetFinalLeaderboard(string gamePin)
        {
            var room = GetRoom(gamePin);
            if (room != null)
            {
                return room.PlayerScores.OrderByDescending(x => x.Value).ToDictionary(x => room.Members[x.Key], x => x.Value);
            }
            return new Dictionary<string, int>();
        }

        public void ResetGame(string gamePin)
        {
            var filter = Builders<Room>.Filter.Eq(r => r.GamePin, gamePin);
            var update = Builders<Room>.Update
                .Set(r => r.PlayerScores, new Dictionary<string, int>())
                .Set(r => r.CurrentQuestionIndex, 0);
            _rooms.UpdateOne(filter, update);
        }


    }
}






