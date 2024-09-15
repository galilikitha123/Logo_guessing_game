using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using System.Collections.Generic;
using WiseWork.Services;
using System.Linq;

namespace WiseWork.Hubs
{
    public class RoomHub : Hub
    {
        private readonly RoomService _roomService;

        public RoomHub(RoomService roomService)
        {
            _roomService = roomService;
        }

        public async Task JoinRoom(string gamePin)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, gamePin);
        }
        public async Task GetConnectionId()
        {
            await Clients.Caller.SendAsync("ReceiveConnectionId", Context.ConnectionId);
        }

        public async Task LeaveRoom(string gamePin, string playerName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, gamePin);
            var room = _roomService.GetRoom(gamePin);
            if (room != null)
            {
                string playerIdToRemove = room.Members.FirstOrDefault(x => x.Value == playerName).Key;
                if (playerIdToRemove != null)
                {
                    if (playerIdToRemove == room.CreatorId)
                    {
                        // Creator is leaving, delete the room
                        _roomService.DeleteRoom(gamePin);
                        await Clients.Group(gamePin).SendAsync("RoomDeleted");
                    }
                    else
                    {
                        // Regular member leaving
                        _roomService.RemoveMember(gamePin, playerName);

                        // Update the room object after removing the member
                        room = _roomService.GetRoom(gamePin);

                        // Send updated members list to all clients in the room
                        await SendMembersUpdate(gamePin, room.Members);
                    }
                }
            }
        }

        public async Task SendMembersUpdate(string gamePin, Dictionary<string, string> members)
        {
            await Clients.Group(gamePin).SendAsync("ReceiveMembersUpdate", members);
        }

        public async Task StartGame(string gamePin)
        {
            await Clients.Group(gamePin).SendAsync("StartGame", gamePin);
        }

        public async Task RoomDeleted(string gamePin)
        {
            await Clients.Group(gamePin).SendAsync("RoomDeleted");
        }
        public async Task UpdatePlayerName(string gamePin, string playerName)
        {
            var room = _roomService.GetRoom(gamePin);
            if (room != null)
            {
                room.Members[Context.ConnectionId] = playerName;
                await Clients.Group(gamePin).SendAsync("ReceiveMembersUpdate", room.Members);
            }
        }

        public async Task UpdatePlayerScore(string gamePin, string playerName, int score)
        {
            var room = _roomService.GetRoom(gamePin);
            if (room != null && room.Members.ContainsValue(playerName))
            {
                var playerId = room.Members.First(x => x.Value == playerName).Key;
                _roomService.UpdatePlayerScore(gamePin, playerId, score);

                // Fetch updated room data
                room = _roomService.GetRoom(gamePin);

                // Send updated scores to all clients
                await Clients.Group(gamePin).SendAsync("UpdateScores", room.PlayerScores);

                // Send updated player list
                await Clients.Group(gamePin).SendAsync("ReceiveMembersUpdate", room.Members);
            }
        }



        public async Task NextQuestion(string gamePin, int questionIndex)
        {
            _roomService.UpdateCurrentQuestionIndex(gamePin, questionIndex);
            await Clients.Group(gamePin).SendAsync("NextQuestion", questionIndex);
        }

        public async Task EndGame(string gamePin)
        {
            var room = _roomService.GetRoom(gamePin);
            if (room != null)
            {
                var leaderboard = _roomService.GetFinalLeaderboard(gamePin);
                await Clients.Group(gamePin).SendAsync("GameOver", leaderboard);
                _roomService.ResetGame(gamePin);
            }
        }



    }
}






