using UnityEngine;
using ServiceNetwork;
using ServiceNetwork.Packets;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine.Serialization;

namespace ServiceNetwork.Example
{
    /// <summary>
    /// 서비스 채널(로그인)을 담당하는 Unity 측 MonoBehaviour.
    /// 로그인 성공 시 서버가 알려준 접속 정보로 NfE 게임 월드에 접속을 트리거한다.
    ///
    /// 주의: 아래 ConnectToGameServer()의 NetworkStreamDriver.Connect() 호출부는
    /// 설치된 com.unity.netcode 패키지 버전에 따라 시그니처가 조금씩 달라질 수 있다.
    /// 정확한 API는 프로젝트에 설치된 NfE 버전의 NetworkStreamDriver를 확인해서 맞춰줄 것.
    /// </summary>
    public class ServiceClientBehaviour : MonoBehaviour
    {
        [SerializeField] private string serviceHost = "127.0.0.1";
        [SerializeField] private int servicePort = 7777;

        private ServiceTcpClient client;

        private async void Start()
        {
            Debug.Log($"ServiceClientBehavioiur.Start");
            client = new ServiceTcpClient();
            client.OnConnected += () => Debug.Log("[Service] Connected.");
            client.OnConnectFailed += err => Debug.LogError($"[Service] Connect failed: {err}");
            client.OnDisconnected += () => Debug.Log("[Service] Disconnected.");
            client.OnPacketReceived += HandlePacket;

            await client.ConnectAsync(serviceHost, servicePort);
        }

        private void Update()
        {
            client?.Tick(); // 큐에 쌓인 수신 패킷을 메인 스레드에서 안전하게 처리
        }

        private void OnDestroy()
        {
            client?.Disconnect();
        }

        public void Login(string accountId, string passwordHash)
        {
            Debug.Log($"ServiceClientBehavioiur.Login : {accountId}, {passwordHash}");
            var packet = new LoginRequestPacket
            {
                AccountId = accountId,
                PasswordHash = passwordHash,
            };

            var writer = new PacketWriter();
            packet.Write(writer);
            client.Send(PacketOpcode.C2S_LoginRequest, writer);
        }

        private void HandlePacket(PacketOpcode opcode, byte[] payload)
        {
            var reader = new PacketReader(payload);

            switch (opcode)
            {
                case PacketOpcode.S2C_LoginResponse:
                {
                    var response = LoginResponsePacket.Read(reader);
                    if (response.Result == LoginResult.Success)
                        Debug.Log($"[Service] Login OK. Token={response.SessionToken}");
                    else
                        Debug.LogWarning($"[Service] Login failed: {response.Result}");
                    break;
                }

                case PacketOpcode.S2C_GameServerHandoff:
                {
                    var handoff = GameServerHandoffPacket.Read(reader);
                    Debug.Log($"[Service] Handoff to game server {handoff.GameServerHost}:{handoff.GameServerPort}");
                    ConnectToGameServer(handoff);
                    break;
                }
            }
        }

        private void ConnectToGameServer(GameServerHandoffPacket handoff)
        {
            // 서비스 채널(TCP)과는 완전히 별개로 동작하는 NfE(UTP) 클라이언트 월드 접속.
            // 세션 토큰은 접속 완료 직후 첫 RPC(예: 별도로 정의할 AuthenticateRequestRpc)로
            // 서버에 다시 전달해, 서비스 서버가 발급한 토큰과 대조 검증하는 흐름으로 이어간다.
            foreach (var world in World.All)
            {
                if (!world.IsClient())
                    continue;

                var endpoint = NetworkEndpoint.Parse(handoff.GameServerHost, handoff.GameServerPort);

                using var query = world.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
                var driver = query.GetSingletonRW<NetworkStreamDriver>();
                driver.ValueRW.Connect(world.EntityManager, endpoint);

                // TODO: NetworkId 부여(연결 완료) 시점을 감지하는 시스템에서
                // handoff.SessionToken을 RPC로 전송해 서버 측 인증과 매칭시키는 로직 추가
                break;
            }
        }
    }
}
