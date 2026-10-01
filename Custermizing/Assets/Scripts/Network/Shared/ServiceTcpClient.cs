using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceNetwork
{
    /// <summary>
    /// Unity 클라이언트에서 서비스 채널(로그인/채팅 등)에 접속하기 위한 클라이언트.
    /// 백그라운드 스레드에서 수신하고, 메인 스레드에서는 Tick()을 통해서만
    /// 콜백이 실행되도록 큐잉한다 (Unity API는 메인 스레드에서만 호출 가능하므로).
    ///
    /// Unity 엔진에 의존하는 코드가 전혀 없으므로 순수 .NET 콘솔 클라이언트나
    /// 다른 엔진에서도 그대로 재사용 가능하다.
    /// </summary>
    public sealed class ServiceTcpClient
    {
        private const int MaxPayloadSize = 64 * 1024;

        private TcpClient _client;
        private NetworkStream _stream;
        private CancellationTokenSource _cts;

        private readonly ConcurrentQueue<(PacketOpcode opcode, byte[] payload)> _incoming = new();
        private readonly object _sendLock = new();

        public bool IsConnected => _client?.Connected ?? false;

        public event Action OnConnected;
        public event Action<string> OnConnectFailed;
        public event Action OnDisconnected;
        public event Action<PacketOpcode, byte[]> OnPacketReceived;

        public async Task ConnectAsync(string host, int port)
        {
            _cts = new CancellationTokenSource();
            _client = new TcpClient();

            try
            {
                await _client.ConnectAsync(host, port);
            }
            catch (Exception ex)
            {
                OnConnectFailed?.Invoke(ex.Message);
                return;
            }

            _stream = _client.GetStream();
            OnConnected?.Invoke();
            _ = ReceiveLoopAsync(_cts.Token);
        }

        public void Disconnect()
        {
            _cts?.Cancel();
            try { _stream?.Close(); } catch { /* ignore */ }
            try { _client?.Close(); } catch { /* ignore */ }
        }

        /// <summary>MonoBehaviour.Update() 등 메인 스레드에서 매 프레임 호출해야 함.</summary>
        public void Tick()
        {
            while (_incoming.TryDequeue(out var packet))
                OnPacketReceived?.Invoke(packet.opcode, packet.payload);
        }

        public void Send(PacketOpcode opcode, PacketWriter writer)
        {
            if (!IsConnected) return;

            byte[] payload = writer.ToArray();
            int frameLength = 2 + payload.Length;
            var frame = new byte[4 + frameLength];
            Buffer.BlockCopy(BitConverter.GetBytes(frameLength), 0, frame, 0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes((ushort)opcode), 0, frame, 4, 2);
            Buffer.BlockCopy(payload, 0, frame, 6, payload.Length);

            lock (_sendLock)
            {
                try
                {
                    _stream.Write(frame, 0, frame.Length);
                }
                catch (Exception)
                {
                    // 수신 루프 쪽에서 끊김을 감지해 OnDisconnected를 호출함
                }
            }
        }

        private async Task ReceiveLoopAsync(CancellationToken token)
        {
            var lengthBuffer = new byte[4];

            try
            {
                while (!token.IsCancellationRequested)
                {
                    if (!await ReadExactAsync(lengthBuffer, 4, token))
                        break;

                    int frameLength = BitConverter.ToInt32(lengthBuffer, 0);
                    if (frameLength <= 2 || frameLength > MaxPayloadSize)
                        break; // 비정상 패킷

                    var frameBuffer = new byte[frameLength];
                    if (!await ReadExactAsync(frameBuffer, frameLength, token))
                        break;

                    ushort opcodeRaw = BitConverter.ToUInt16(frameBuffer, 0);
                    int payloadLength = frameLength - 2;
                    var payload = new byte[payloadLength];
                    Array.Copy(frameBuffer, 2, payload, 0, payloadLength);

                    _incoming.Enqueue(((PacketOpcode)opcodeRaw, payload));
                }
            }
            catch (IOException) { /* 연결 끊김 */ }
            catch (SocketException) { /* 연결 끊김 */ }
            finally
            {
                OnDisconnected?.Invoke();
            }
        }

        private async Task<bool> ReadExactAsync(byte[] buffer, int count, CancellationToken token)
        {
            int offset = 0;
            while (offset < count)
            {
                int read = await _stream.ReadAsync(buffer, offset, count - offset, token);
                if (read == 0) return false;
                offset += read;
            }
            return true;
        }
    }
}
