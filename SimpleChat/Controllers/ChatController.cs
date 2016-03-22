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

namespace SimpleChat.Controllers
{
    public class ChatMessage
    {
        public string UserName { get; set; }
        public string Message { get; set; }
        public string UserReceiver { get; set; }
    }

    public class ChatClient
    {
        public string User { get; set; }
        public WebSocket Socket { get; set; }
    }

    public class ChatController : Controller
    {
        private static readonly List<ChatClient> Clients = new List<ChatClient>();
        private static readonly ReaderWriterLockSlim Locker = new ReaderWriterLockSlim();

        private string userId;

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
        public void MessagesProvider(string user)
        {
            if (System.Web.HttpContext.Current.IsWebSocketRequest)
            {
                userId = user;
                System.Web.HttpContext.Current.AcceptWebSocketRequest(ProcessPublicMessage);
            }
            //return new HttpResponseMessage(HttpStatusCode.SwitchingProtocols);
        }

        private async Task ProcessPublicMessage(AspNetWebSocketContext context)
        {
            WebSocket socket = context.WebSocket;

            Locker.EnterWriteLock();
            try
            {
                Clients.Add(new ChatClient
                {
                    User = userId,
                    Socket = socket
                });
                userId = null;
            }
            finally
            {
                Locker.ExitWriteLock();
            }

            while (true)
            {
                ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[1024]);
                WebSocketReceiveResult result = await socket.ReceiveAsync(buffer, CancellationToken.None);
                if (socket.State == WebSocketState.Open)
                {
                    var messageJSON = Encoding.UTF8.GetString(buffer.Array, 0, result.Count);

                    var messageObj = JsonConvert.DeserializeObject<ChatMessage>(messageJSON);

                    string userMessage = string.Format("{0} {1} {2}", DateTime.Now.ToLongTimeString(), messageObj.UserName, messageObj.Message);

                    buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(userMessage));

                    for (int i = 0; i < Clients.Count; i++)
                    {
                        ChatClient client = Clients[i];
                        try
                        {
                            if (!string.IsNullOrEmpty(messageObj.UserReceiver))
                            {
                                if (client.User == messageObj.UserName || client.User == messageObj.UserReceiver)
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

    }
}
