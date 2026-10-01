namespace ServiceNetwork.Packets
{
    /// <summary>C2S_LoginRequest 페이로드.</summary>
    public struct LoginRequestPacket
    {
        public string AccountId;
        public string PasswordHash; // 평문 비밀번호는 절대 태우지 말 것. 클라 단에서 해시 후 전송 권장

        public void Write(PacketWriter w)
        {
            w.WriteString(AccountId);
            w.WriteString(PasswordHash);
        }

        public static LoginRequestPacket Read(PacketReader r)
        {
            return new LoginRequestPacket
            {
                AccountId = r.ReadString(),
                PasswordHash = r.ReadString(),
            };
        }
    }

    public enum LoginResult : byte
    {
        Success = 0,
        InvalidCredentials = 1,
        AccountBanned = 2,
        ServerFull = 3,
        AlreadyLoggedIn = 4,
    }

    /// <summary>S2C_LoginResponse 페이로드.</summary>
    public struct LoginResponsePacket
    {
        public LoginResult Result;
        public string SessionToken; // 성공 시에만 유효. NfE 접속 인증에 재사용

        public void Write(PacketWriter w)
        {
            w.WriteByte((byte)Result);
            w.WriteString(SessionToken ?? string.Empty);
        }

        public static LoginResponsePacket Read(PacketReader r)
        {
            return new LoginResponsePacket
            {
                Result = (LoginResult)r.ReadByte(),
                SessionToken = r.ReadString(),
            };
        }
    }

    /// <summary>
    /// S2C_GameServerHandoff 페이로드.
    /// 로그인 성공 직후, 클라이언트에게 실제 NfE 게임 서버 접속 정보를 넘겨준다.
    /// 서비스 채널(TCP, 이 프로토콜)과 게임 채널(UTP, NfE)은 서로 다른 포트/프로토콜이다.
    /// </summary>
    public struct GameServerHandoffPacket
    {
        public string GameServerHost;
        public ushort GameServerPort;
        public string SessionToken; // NfE 접속 직후 첫 RPC로 다시 보내 서버 측과 대조 검증

        public void Write(PacketWriter w)
        {
            w.WriteString(GameServerHost);
            w.WriteUShort(GameServerPort);
            w.WriteString(SessionToken);
        }

        public static GameServerHandoffPacket Read(PacketReader r)
        {
            return new GameServerHandoffPacket
            {
                GameServerHost = r.ReadString(),
                GameServerPort = r.ReadUShort(),
                SessionToken = r.ReadString(),
            };
        }
    }
}
