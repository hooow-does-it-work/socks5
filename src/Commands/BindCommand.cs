﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace IocpSharp.Socks5.Commands
{
    public class BindCommand : Command, ICommand
    {
        public void Handle(Stream requestStream, ProxyRequest request)
        {
            int port = new PortBinder().Start(request.RemoteEndPoint) ;
            if (port == 0)
            {
                //没有可用端口
                SendConnectResult(new IPEndPoint(IPAddress.Any, port), requestStream, 0x05);
            }
            else
            {
                //连接成功后，发送响应数据到客户端
                SendConnectResult(new IPEndPoint(IPAddress.Parse("127.0.0.1"), port), requestStream);
            }
            requestStream.Close();
        }
    }
}
