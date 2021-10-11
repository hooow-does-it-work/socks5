﻿using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace IocpSharp.Socks5.Commands
{

    public delegate void ServerConnectedCallback(Socket server, PortTransfer transfer);
    /// <summary>
    /// 直接继承SocketAsyncEventArgs，作为端口映射服务器
    /// </summary>
    public class PortTransfer : SocketAsyncEventArgs
    {
        private Socket _socket = null;
        private IPEndPoint _localEndPoint = null;
        private ServerConnectedCallback _serverConnectedCallback = null;

        /// <summary>
        /// 本地监听终结点
        /// </summary>
        public IPEndPoint LocalEndPoint => _localEndPoint;

        /// <summary>
        /// 实例化服务器
        /// </summary>
        public PortTransfer() : base() { }

        /// <summary>
        /// 启动服务器
        /// </summary>
        /// <returns>true成功，false失败</returns>
        protected virtual void Start()
        {
            if (_localEndPoint == null) throw new ArgumentNullException("LocalEndPoint");

            _socket = new Socket(_localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            _socket.Bind(_localEndPoint);
            _socket.Listen(256);
            _localEndPoint = _socket.LocalEndPoint as IPEndPoint;

            StartAccept();
        }

        /// <summary>
        /// 使用本地终结点启动服务器
        /// </summary>
        /// <param name="localEndPoint">本地终结点</param>
        /// <returns></returns>
        public void Start(EndPoint localEndPoint, ServerConnectedCallback callback)
        {
            _localEndPoint = localEndPoint as IPEndPoint;
            _serverConnectedCallback = callback;
            Start();
        }
        /// <summary>
        /// 停止服务器
        /// </summary>
        public virtual void Stop()
        {
            InternalRelease();
        }

        private int _released = 0;
        private void InternalRelease()
        {
            if (Interlocked.CompareExchange(ref _released, 1, 0) == 1) return;
            try
            {
                _socket?.Close();
                Dispose();
            }
            catch { }
            _serverConnectedCallback = null;
            Release();
        }
        protected virtual void Release() { }
        /// <summary>
        /// 开始接受客户端请求
        /// </summary>
        private void StartAccept()
        {
            AcceptSocket = null;

            try
            {
                if (!_socket.AcceptAsync(this))
                {
                    OnCompleted(this);
                }
            }
            catch (SocketException e)
            {
                Stop();
            }
            catch (ObjectDisposedException)
            {
            }
        }

        /// <summary>
        /// 重写OnCompleted方法
        /// </summary>
        /// <param name="e"></param>
        protected sealed override void OnCompleted(SocketAsyncEventArgs e)
        {
            if (SocketError != SocketError.Success)
            {
                if (SocketError == SocketError.OperationAborted) return;
                Stop();
                return;
            }

            Socket client = AcceptSocket;
            client.NoDelay = true;
            _serverConnectedCallback?.Invoke(client, this);
            Stop();
        }
    }
}
