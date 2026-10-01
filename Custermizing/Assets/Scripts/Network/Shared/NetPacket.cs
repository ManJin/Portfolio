using System;
using System.IO;
using System.Text;

namespace ServiceNetwork
{
    /// <summary>
    /// 서비스 채널(로그인/채팅/핸드오프 등)에서 사용하는 패킷 오퍼코드.
    /// NfE(Netcode for Entities)의 Ghost/RPC 시스템과는 완전히 독립적인 프로토콜이다.
    /// 게임 상태 동기화는 계속 NfE가 담당하고, 이 채널은 서비스성 트래픽만 처리한다.
    /// </summary>
    public enum PacketOpcode : ushort
    {
        None = 0,

        // 연결/인증
        C2S_LoginRequest = 100,
        S2C_LoginResponse = 101,
        C2S_Heartbeat = 102,
        S2C_Heartbeat = 103,

        // 예시: 채팅 (게임 월드 진입 전, 로비 채팅 등)
        C2S_ChatMessage = 200,
        S2C_ChatBroadcast = 201,

        // 로그인 성공 후, 실제 NfE 게임 서버 접속 정보를 넘겨주는 핸드오프
        S2C_GameServerHandoff = 300,
    }

    /// <summary>
    /// 패킷 페이로드 작성용 바이너리 라이터.
    /// 엔디안은 항상 리틀엔디안으로 고정한다 (플랫폼별 BitConverter 기본값에 의존하지 않음).
    /// </summary>
    public sealed class PacketWriter
    {
        private readonly MemoryStream _stream = new MemoryStream();

        public void WriteByte(byte value) => _stream.WriteByte(value);

        public void WriteBool(bool value) => WriteByte((byte)(value ? 1 : 0));

        public void WriteShort(short value) => WriteBytes(BitConverter.GetBytes(value));

        public void WriteUShort(ushort value) => WriteBytes(BitConverter.GetBytes(value));

        public void WriteInt(int value) => WriteBytes(BitConverter.GetBytes(value));

        public void WriteUInt(uint value) => WriteBytes(BitConverter.GetBytes(value));

        public void WriteLong(long value) => WriteBytes(BitConverter.GetBytes(value));

        public void WriteFloat(float value) => WriteBytes(BitConverter.GetBytes(value));

        public void WriteString(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                WriteUShort(0);
                return;
            }

            byte[] bytes = Encoding.UTF8.GetBytes(value);
            if (bytes.Length > ushort.MaxValue)
                throw new ArgumentException("문자열이 너무 깁니다 (65535바이트 초과).");

            WriteUShort((ushort)bytes.Length);
            WriteBytes(bytes);
        }

        private void WriteBytes(byte[] bytes)
        {
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            _stream.Write(bytes, 0, bytes.Length);
        }

        public byte[] ToArray() => _stream.ToArray();
    }

    /// <summary>
    /// 수신한 페이로드를 읽기 위한 바이너리 리더.
    /// </summary>
    public sealed class PacketReader
    {
        private readonly byte[] _buffer;
        private int _offset;

        public PacketReader(byte[] buffer, int offset = 0)
        {
            _buffer = buffer;
            _offset = offset;
        }

        public byte ReadByte() => _buffer[_offset++];

        public bool ReadBool() => ReadByte() != 0;

        public short ReadShort() => BitConverter.ToInt16(ReadBytes(2), 0);

        public ushort ReadUShort() => BitConverter.ToUInt16(ReadBytes(2), 0);

        public int ReadInt() => BitConverter.ToInt32(ReadBytes(4), 0);

        public uint ReadUInt() => BitConverter.ToUInt32(ReadBytes(4), 0);

        public long ReadLong() => BitConverter.ToInt64(ReadBytes(8), 0);

        public float ReadFloat() => BitConverter.ToSingle(ReadBytes(4), 0);

        public string ReadString()
        {
            ushort len = ReadUShort();
            if (len == 0) return string.Empty;
            byte[] bytes = ReadBytes(len);
            return Encoding.UTF8.GetString(bytes);
        }

        private byte[] ReadBytes(int count)
        {
            byte[] result = new byte[count];
            Array.Copy(_buffer, _offset, result, 0, count);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(result);
            _offset += count;
            return result;
        }
    }
}
