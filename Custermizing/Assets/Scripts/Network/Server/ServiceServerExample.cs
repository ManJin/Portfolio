using System;
using System.Reflection.PortableExecutable;
using ServiceNetwork;
using ServiceNetwork.Packets;
using UnityEngine;


namespace ServiceNetwork.Example
{
    /// <summary>
    /// 서비스 서버 구동 예시. 헤드리스 Unity 서버 프로세스 안에서 돌리거나,
    /// 완전히 별도의 .NET 콘솔 프로세스로 분리해서 돌릴 수도 있다.
    /// (분리하면 로그인/인증 서버와 게임 시뮬레이션 서버를 물리적으로도 나눌 수 있음)
    /// </summary>
    public static class ServiceServerExample
    {
        public static ServiceTcpServer Run(ushort servicePort = 7778, ushort gameServerPort = 7777, string gameServerHost = "127.0.0.1")
        {
            Debug.Log($"ServiceServerExample.Run:{servicePort}, {gameServerPort}");
            var server = new ServiceTcpServer(servicePort);

            server.OnClientConnected += conn =>
                Debug.Log($"[Service] Client {conn.Id} connected.");

            server.OnClientDisconnected += conn =>
                Debug.Log($"[Service] Client {conn.Id} disconnected.");

            server.OnPacketReceived += (conn, opcode, payload) =>
            {
                Debug.Log($"ServiceServerExample.Run:PacketOpcode.{opcode}");
                switch (opcode)
                {
                    case PacketOpcode.C2S_LoginRequest:
                        HandleLogin(server, conn, payload, gameServerHost, gameServerPort);
                        break;

                    case PacketOpcode.C2S_Heartbeat:
                        // 마지막 수신 시각만 갱신하는 식으로 처리 (별도 응답 없이도 됨)
                        break;
                }
            };

            server.Start();
            Debug.Log($"Service server started on port {servicePort}.");
            Debug.Log($"ServiceServerExample.Run:End");
            return server;
        }

        private static void HandleLogin(ServiceTcpServer server, ServiceConnection conn, byte[] payload, string gameServerHost, ushort gameServerPort)
        {
            Debug.Log($"ServiceServerExample.HandleLogin:Start");
            var request = LoginRequestPacket.Read(new PacketReader(payload));

            // TODO: 실제 계정 DB 조회 / 비밀번호 해시 검증 로직으로 교체
            bool valid = !string.IsNullOrEmpty(request.AccountId);

            if (!valid)
            {
                var fail = new LoginResponsePacket
                {
                    Result = LoginResult.InvalidCredentials,
                    SessionToken = null,
                };

                var failWriter = new PacketWriter();
                fail.Write(failWriter);
                server.Send(conn, PacketOpcode.S2C_LoginResponse, failWriter);
                Debug.Log($"ServiceServerExample.HandleLogin:Login Fail");
                return;
            }

            string token = Guid.NewGuid().ToString("N");
            conn.UserData = token; // 이후 이 커넥션이 어떤 세션인지 추적하는 용도

            var success = new LoginResponsePacket
            {
                Result = LoginResult.Success,
                SessionToken = token,
            };

            var successWriter = new PacketWriter();
            success.Write(successWriter);
            server.Send(conn, PacketOpcode.S2C_LoginResponse, successWriter);

            // 로그인 성공 직후, 실제 NfE(UTP) 게임 서버 접속 정보를 핸드오프한다.
            var handoff = new GameServerHandoffPacket
            {
                GameServerHost = gameServerHost,
                GameServerPort = gameServerPort, // GameBootstrap의 AutoConnectPort와 반드시 일치해야 함
                SessionToken = token,
            };

            var handoffWriter = new PacketWriter();
            handoff.Write(handoffWriter);
            server.Send(conn, PacketOpcode.S2C_GameServerHandoff, handoffWriter);
            Debug.Log($"ServiceServerExample.HandleLogin:Login Success");
        }
    }
}