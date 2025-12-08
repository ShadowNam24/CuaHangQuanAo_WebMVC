using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Linq;

namespace LiveChatApp.Hubs
{
    public class ChatHub : Hub
    {
        // Lưu danh sách users đang online
        private static ConcurrentDictionary<string, UserInfo> OnlineUsers = new ConcurrentDictionary<string, UserInfo>();
        
        // Lưu lịch sử chat của từng customer
        private static ConcurrentDictionary<string, List<ChatMessage>> ChatHistory = new ConcurrentDictionary<string, List<ChatMessage>>();

        // Gửi tin nhắn từ customer đến admin
        public async Task SendMessageToAdmin(string username, string message)
        {
            var chatMessage = new ChatMessage
            {
                User = username,
                Message = message,
                Timestamp = DateTime.Now,
                ConnectionId = Context.ConnectionId,
                IsFromCustomer = true
            };

            // Lưu lịch sử chat
            var history = ChatHistory.GetOrAdd(Context.ConnectionId, new List<ChatMessage>());
            history.Add(chatMessage);

            // Gửi tin nhắn đến tất cả admin đang online
            await Clients.Group("Admins").SendAsync("ReceiveCustomerMessage", 
                Context.ConnectionId, username, message, DateTime.Now.ToString("HH:mm"));

            // Xác nhận cho customer
            await Clients.Caller.SendAsync("MessageSent", message, DateTime.Now.ToString("HH:mm"));
        }

        // Gửi tin nhắn từ admin đến customer
        public async Task SendMessageToCustomer(string customerConnectionId, string adminName, string message)
        {
            var chatMessage = new ChatMessage
            {
                User = adminName,
                Message = message,
                Timestamp = DateTime.Now,
                ConnectionId = Context.ConnectionId,
                IsFromCustomer = false
            };

            // Lưu lịch sử chat của customer
            var history = ChatHistory.GetOrAdd(customerConnectionId, new List<ChatMessage>());
            history.Add(chatMessage);

            // Gửi tin nhắn đến customer cụ thể
            await Clients.Client(customerConnectionId).SendAsync("ReceiveAdminMessage", 
                adminName, message, DateTime.Now.ToString("HH:mm"));

            // Xác nhận cho admin
            await Clients.Caller.SendAsync("AdminMessageSent", customerConnectionId, message, DateTime.Now.ToString("HH:mm"));
        }

        public async Task JoinChat(string username, bool isAdmin = false)
        {
            var userInfo = new UserInfo
            {
                ConnectionId = Context.ConnectionId,
                Username = username,
                JoinedAt = DateTime.Now,
                IsAdmin = isAdmin
            };

            OnlineUsers.TryAdd(Context.ConnectionId, userInfo);

            if (isAdmin)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
                
                // Gửi danh sách customer đang chờ cho admin
                var waitingCustomers = OnlineUsers.Values
                    .Where(u => !u.IsAdmin)
                    .Select(u => new { u.ConnectionId, u.Username, u.JoinedAt })
                    .ToList();
                
                await Clients.Caller.SendAsync("UpdateWaitingCustomers", waitingCustomers);
            }
            else
            {
                // Thông báo cho admin có customer mới
                await Clients.Group("Admins").SendAsync("NewCustomerJoined", Context.ConnectionId, username);
            }

            await Clients.All.SendAsync("UpdateOnlineUsers", OnlineUsers.Values.Where(u => !u.IsAdmin).Count());
        }

        public async Task GetChatHistory(string customerConnectionId)
        {
            if (ChatHistory.TryGetValue(customerConnectionId, out var history))
            {
                await Clients.Caller.SendAsync("ReceiveChatHistory", customerConnectionId, history);
            }
        }

        public async Task RequestAdminSupport(string username)
        {
            // Thông báo cho tất cả admin
            await Clients.Group("Admins").SendAsync("CustomerRequestSupport", Context.ConnectionId, username);
            
            // Xác nhận cho customer
            await Clients.Caller.SendAsync("SupportRequested");
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            if (OnlineUsers.TryRemove(Context.ConnectionId, out var userInfo))
            {
                if (userInfo.IsAdmin)
                {
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Admins");
                }
                else
                {
                    // Thông báo cho admin customer đã rời đi
                    await Clients.Group("Admins").SendAsync("CustomerLeft", Context.ConnectionId, userInfo.Username);
                }

                await Clients.All.SendAsync("UpdateOnlineUsers", OnlineUsers.Values.Where(u => !u.IsAdmin).Count());
            }

            await base.OnDisconnectedAsync(exception);
        }
    }

    public class UserInfo
    {
        public string ConnectionId { get; set; }
        public string Username { get; set; }
        public DateTime JoinedAt { get; set; }
        public bool IsAdmin { get; set; }
    }

    public class ChatMessage
    {
        public string User { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
        public string ConnectionId { get; set; }
        public bool IsFromCustomer { get; set; }
    }
}