using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceNetwork
{
    /// <summary>
    /// NfE(Netcode for Entities)와 완전히 분리된 서비스 채널 서버.
    /// 로그인/인증/채팅 등 실시간 게임 시뮬레이션 동기화가 필요 없는 트래픽을 담당한다.
    /// 순수 System.Net.Sockets 기반이라 Unity/UTP/NfE 어디에도 종속되지 않으며,
    /// 콘솔 앱, 헤드리스 서버, 다른 게임 프로젝트 등에 그대로 재사용 가능하다.
    ///
    /// 프레임 포맷 (리틀엔디안 고정):
    ///   [4 bytes: frameLength]      = opcode(2) + payload(N) 의 길이
    ///   [2 bytes: opcode]
    ///   [N bytes: payload]
    /// </summary>
    public sealed class ServiceTcpServer
    {
        private const int MaxPayloadSize = 64 * 1024;

        private readonly TcpListener _listener;
        private readonly ConcurrentDictionary<long, ServiceConnection> _connections = new();
        private long _nextConnectionId;
        private CancellationTokenSource _cts;

        public event Action<ServiceConnection> OnClientConnected;
        public event Action<ServiceConnection> OnClientDisconnected;
        public event Action<ServiceConnection, PacketOpcode, byte[]> OnPacketReceived;

        public ServiceTcpServer(int port)
        {
            _listener = new TcpListener(IPAddress.Any, port);
        }

        public void Start()
        {
            _cts = new CancellationTokenSource();
            _listener.Start();
            _ = AcceptLoopAsync(_cts.Token);
        }

        public void Stop()
        {
            _cts?.Cancel();
            _listener.Stop();
            foreach (var conn in _connections.Values)
                conn.Close();
            _connections.Clear();
        }

        private async Task AcceptLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = await _listener.AcceptTcpClientAsync();
                }
                catch (ObjectDisposedException)
                {
                    break; // 서버 종료
                }

                long connId = Interlocked.Increment(ref _nextConnectionId);
                var conn = new ServiceConnection(connId, client);
                _connections[connId] = conn;

                OnClientConnected?.Invoke(conn);
                _ = ReceiveLoopAsync(conn, token);
            }
        }

        private async Task ReceiveLoopAsync(ServiceConnection conn, CancellationToken token)
        {
            var stream = conn.Client.GetStream();
            var lengthBuffer = new byte[4];

            try
            {
                while (!token.IsCancellationRequested)
                {
                    if (!await ReadExactAsync(stream, lengthBuffer, 4, token))
                        break;

                    int frameLength = BitConverter.ToInt32(lengthBuffer, 0);
                    if (frameLength <= 2 || frameLength > MaxPayloadSize)
                        break; // 비정상 패킷 - 프로토콜 오염 방지를 위해 연결 종료

                    var frameBuffer = new byte[frameLength];
                    if (!await ReadExactAsync(stream, frameBuffer, frameLength, token))
                        break;

                    ushort opcodeRaw = BitConverter.ToUInt16(frameBuffer, 0);
                    var opcode = (PacketOpcode)opcodeRaw;

                    int payloadLength = frameLength - 2;
                    var payload = new byte[payloadLength];
                    Array.Copy(frameBuffer, 2, payload, 0, payloadLength);

                    OnPacketReceived?.Invoke(conn, opcode, payload);
                }
            }
            catch (IOException) { /* 연결 끊김 */ }
            catch (SocketException) { /* 연결 끊김 */ }
            finally
            {
                _connections.TryRemove(conn.Id, out _);
                conn.Close();
                OnClientDisconnected?.Invoke(conn);
            }
        }

        private static async Task<bool> ReadExactAsync(NetworkStream stream, byte[] buffer, int count, CancellationToken token)
        {
            int offset = 0;
            while (offset < count)
            {
                int read = await stream.ReadAsync(buffer, offset, count - offset, token);
                if (read == 0)
                    return false; // 상대가 연결을 닫음
                offset += read;
            }
            return true;
        }

        /// <summary>특정 커넥션에 패킷 전송.</summary>
        public void Send(ServiceConnection conn, PacketOpcode opcode, PacketWriter writer)
        {
            conn.Send(BuildFrame(opcode, writer.ToArray()));
        }

        /// <summary>연결된 모든 클라이언트에 브로드캐스트.</summary>
        public void Broadcast(PacketOpcode opcode, PacketWriter writer)
        {
            byte[] frame = BuildFrame(opcode, writer.ToArray());
            foreach (var conn in _connections.Values)
                conn.Send(frame);
        }

        private static byte[] BuildFrame(PacketOpcode opcode, byte[] payload)
        {
            int frameLength = 2 + payload.Length; // opcode(2) + payload
            var result = new byte[4 + frameLength];
            Buffer.BlockCopy(BitConverter.GetBytes(frameLength), 0, result, 0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes((ushort)opcode), 0, result, 4, 2);
            Buffer.BlockCopy(payload, 0, result, 6, payload.Length);
            return result;
        }
    }

    /// <summary>
    /// 단일 클라이언트 연결 래퍼. 전송 시 락으로 동시 쓰기를 보호한다.
    /// UserData에 인증된 세션 토큰/계정 ID 등을 담아 이후 패킷 처리에서 조회할 수 있다.
    /// </summary>
    public sealed class ServiceConnection
    {
        public long Id { get; }
        public TcpClient Client { get; }
        public object UserData { get; set; }

        private readonly object _sendLock = new();

        public ServiceConnection(long id, TcpClient client)
        {
            Id = id;
            Client = client;
        }

        public void Send(byte[] frame)
        {
            lock (_sendLock)
            {
                try
                {
                    Client.GetStream().Write(frame, 0, frame.Length);
                }
                catch (Exception)
                {
                    // 전송 실패는 ReceiveLoop 쪽에서 연결 종료로 자연스럽게 감지됨
                }
            }
        }

        public void Close()
        {
            try { Client.Close(); } catch { /* ignore */ }
        }
    }
}
