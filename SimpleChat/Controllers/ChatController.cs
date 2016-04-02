using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.WebSockets;
using SimpleChat.Models;

namespace SimpleChat.Controllers
{
    public class ChatMessage
    {
        public int? infoMessageType { get; set; }
        public int UserId { get; set; }
        public string UserLogin { get; set; }
        public bool isOnline { get; set; }
        public int? MessageId { get; set; }
        public string Message { get; set; }
        public int? UserReceiverId { get; set; }
        public string RecDate { get; set; }
        public bool IsDeleted { get; set; }
    }

    public class ChatClient
    {
        public int UserId { get; set; }
        public WebSocket Socket { get; set; }
    }

    public class ChatController : Controller
    {
        private static readonly List<ChatClient> Clients = new List<ChatClient>();
        private static readonly ReaderWriterLockSlim Locker = new ReaderWriterLockSlim();
        private int? userIdHandler;

        ChatDataBaseEntities context = new ChatDataBaseEntities();

        // GET: Chat
        public ActionResult Index()
        {
            if (Session["LogedUserID"] != null)
            {
                return View();
            }
            else
            {
                return RedirectToAction("Login", "AccountController");
            }
        }

        [HttpGet]
        public void MessagesProvider(int userId)
        {
            if (System.Web.HttpContext.Current.IsWebSocketRequest)
            {
                userIdHandler = userId;
                System.Web.HttpContext.Current.AcceptWebSocketRequest(ProcessPublicMessage);
            }
        }

        private async Task ProcessPublicMessage(AspNetWebSocketContext socketContext)
        {
            WebSocket socket = socketContext.WebSocket;

            Locker.EnterWriteLock();
            try
            {
                Clients.Add(new ChatClient
                {
                    UserId = userIdHandler.Value,
                    Socket = socket
                });
                userIdHandler = null;
            }
            finally
            {
                Locker.ExitWriteLock();
            }

            while (true)
            {
                ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[1024]);
                WebSocketReceiveResult webSocketResult = await socket.ReceiveAsync(buffer, CancellationToken.None);
                if (socket.State == WebSocketState.Open)
                {
                    var JSONObj = Encoding.UTF8.GetString(buffer.Array, 0, webSocketResult.Count);
                    var messageObj = JsonConvert.DeserializeObject<ChatMessage>(JSONObj);

                    var currentUser = context.User.FirstOrDefault(x=>x.Id == messageObj.UserId);

                    if (messageObj.infoMessageType == 0)
                    {
                        messageObj.UserLogin = currentUser.ChatName;
                    }
                    else if(messageObj.infoMessageType == 1)
                    {
                        messageObj.UserLogin = currentUser.ChatName;
                    }
                    else if (messageObj.infoMessageType == 2)
                    {
                        UserMessage m = new UserMessage();
                        if (messageObj.MessageId.HasValue)
                        {
                            if (!messageObj.IsDeleted)
                            {
                                m = context.UserMessage.FirstOrDefault(x => x.Id == messageObj.MessageId);
                                m.Message = messageObj.Message;
                                m.RecDate = DateTime.Now;
                            }
                            else
                            {
                                m = context.UserMessage.FirstOrDefault(x => x.Id == messageObj.MessageId);
                                m.IsDeleted = true;
                                m.RecDate = DateTime.Now;
                            }
                        }
                        else
                        {
                            m.UserMessageSenderId = messageObj.UserId;
                            m.UserMessageReceiverId = messageObj.UserReceiverId;
                            m.Message = messageObj.Message;
                            m.RecDate = DateTime.Now;
                            m.IsDeleted = false;

                            context.UserMessage.Add(m);
                        }

                        context.SaveChanges();

                        messageObj = ConvertMessage(m);
                    }
                    //string userMessage = string.Format("{0} {1} {2}", DateTime.Now.ToLongTimeString(), messageObj.UserName, messageObj.Message);

                    buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(messageObj)));

                    for (int i = 0; i < Clients.Count; i++)
                    {
                        ChatClient client = Clients[i];
                        try
                        {
                            if (!string.IsNullOrEmpty(messageObj.Message))
                            {
                                if (client.UserId == messageObj.UserId || client.UserId == messageObj.UserReceiverId)
                                {
                                    if (client.Socket.State == WebSocketState.Open)
                                    {
                                        await client.Socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
                                    }
                                }
                            }
                            else
                            {
                                if (client.Socket.State == WebSocketState.Open)
                                {
                                    await client.Socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
                                }
                            }
                        }
                        catch (ObjectDisposedException)
                        {
                            Locker.EnterWriteLock();
                            try
                            {
                                Clients.Remove(client);
                                i--;
                            }
                            finally
                            {
                                Locker.ExitWriteLock();
                            }
                        }
                    }
                }
                else
                {
                    break;
                }
            }
        }
        
        [NonAction]
        public ChatMessage ConvertMessage(UserMessage um)
        {
            ChatMessage chatMessage = new ChatMessage()
            {
                MessageId = um.Id,
                UserId = um.UserMessageSenderId,
                UserLogin = um.User1.ChatName,
                Message = um.Message,
                UserReceiverId = um.UserMessageReceiverId,
                RecDate = um.RecDate.ToString("dd.MM.yyyy HH:mm:ss"),
                IsDeleted = um.IsDeleted
            };

            return chatMessage;
        }

    }
}
