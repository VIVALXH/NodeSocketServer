using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;

namespace SocketServer
{
    public class Session
    {
        private Socket _SocketClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private byte[] _buffer = new byte[4096];
        private string _ip = "";
        private bool _isweb = false;

        public Socket SocketClient
        {
            set { _SocketClient = value; }
            get { return _SocketClient; }
        }

        public byte[] buffer
        {
            set { _buffer = value; }
            get { return _buffer; }
        }

        public string IP
        {
            set { _ip = value; }
            get { return _ip; }
        }

        public bool isWeb
        {
            set { _isweb = value; }
            get { return _isweb; }
        }
    }
}
