using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using SocketServer;

namespace SocketServer
{
    public class WebSocket
    {

        private Dictionary<string, Session> SessionPool = new Dictionary<string, Session>();

        private static byte[] buffer = new byte[4096];

        #region 启动WebSocket服务
        /// <summary>
        /// 启动WebSocket服务
        /// </summary>
        public void start(int port)
        {
            var local_ip = Helper.GetLocalIP();
            Console.WriteLine("本地IP地址是：" + local_ip);

            Socket SocketServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            SocketServer.Bind(new IPEndPoint(IPAddress.Any, port));
            SocketServer.Listen(20);

            Console.WriteLine("服务已启动");

            //开启接收客户端
            SocketServer.BeginAccept(new AsyncCallback(Accept), SocketServer);


            while (true)
            {
                var message = Console.ReadLine();
                if (message == null)
                    continue;
                byte[] bufferGo = Helper.PackageServerData(message);

                //对每个连接此服务端的客户端发消息
                foreach (Session se in SessionPool.Values)
                {
                    se.SocketClient.Send(bufferGo, bufferGo.Length, SocketFlags.None);
                }
            }
        }
        #endregion
        #region 处理客户端连接请求
        /// <summary>
        /// 处理客户端连接请求
        /// </summary>
        /// <param name="result"></param>
        private void Accept(IAsyncResult socket)
        {
            if (socket.AsyncState != null)
            {
                // 还原传入的原始套接字
                Socket SockeServer = (Socket)socket.AsyncState;
                // 在原始套接字上调用EndAccept方法，返回新的套接字
                Socket SockeClient = SockeServer.EndAccept(socket);
                //byte[] buffer = new byte[4096];
                try
                {
                    //接收客户端的数据
                    SockeClient.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(Receive), SockeClient);

                    //准备接受下一个客户端
                    SockeServer.BeginAccept(new AsyncCallback(Accept), SockeServer);
                    Console.WriteLine(string.Format("Client {0} connected", SockeClient.RemoteEndPoint));
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error : " + ex.ToString());
                }
            }
        }
        #endregion

        #region 处理接收的数据
        /// <summary>
        /// 处理接受的数据
        /// </summary>
        /// <param name="socket"></param>
        private void Receive(IAsyncResult socket)
        {
            if (socket.AsyncState != null)
            {
                var SocketClient = (Socket)socket.AsyncState;
                if (SocketClient == null)
                    return;
                //防止出现null
                var ipa = SocketClient.RemoteEndPoint;
                if (ipa == null)
                    return;
                var IP = ipa.ToString();
                if (IP == null)
                    return;
                if (!SessionPool.ContainsKey(IP))
                {
                    //保存登录的客户端
                    Session session = new Session();
                    session.SocketClient = SocketClient;
                    session.IP = IP;
                    session.buffer = buffer;
                    lock (SessionPool)
                    {
                        if (SessionPool.ContainsKey(session.IP))
                        {
                            this.SessionPool.Remove(session.IP);
                        }
                        this.SessionPool.Add(session.IP, session);
                    }
                }
                try
                {
                    int length = SocketClient.EndReceive(socket);
                    if (length == 0)
                        return;
                    byte[] buffer = SessionPool[IP].buffer;
                    SocketClient.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(Receive), SocketClient);
                    string msg = Encoding.UTF8.GetString(buffer, 0, length);
                    string heartbeat;
                    // websocket建立连接的时候，除了TCP连接的三次握手，websocket协议中客户端与服务器想建立连接需要一次额外的握手动作
                    if (msg.Contains("Sec-WebSocket-Key"))
                    {
                        SocketClient.Send(Helper.PackageHandShakeData(buffer, length));
                        SessionPool[IP].isWeb = true;
                        return;
                    }
                    if (SessionPool[IP].isWeb)
                    {
                        msg = Helper.AnalyzeClientData(buffer, length);
                        Console.WriteLine(msg);
                        if (msg.Contains("heartbeat"))
                        {

                            heartbeat = "heartbeat";
                            //msg = Helper.AnalyzeClientData(buffer, length);

                            //将心跳转化为字节包
                            byte[] msgBuffer = Helper.PackageServerData(heartbeat);

                            //向每个连接此服务端的客户端发送心跳
                            //foreach (Session se in SessionPool.Values)
                            //{
                            //    se.SocketClient.Send(msgBuffer, msgBuffer.Length, SocketFlags.None);
                            //}
                            SocketClient.Send(msgBuffer, msgBuffer.Length, SocketFlags.None);
                        }
                    }



                }
                catch
                {
                    SocketClient.Disconnect(true);
                    Console.WriteLine("客户端 {0} 断开连接", IP);
                    SessionPool.Remove(IP);
                }
            }
        }
        #endregion

    }
}
