# ServiceNetwork — NfE와 분리된 저수준 서비스 채널

## 왜 분리하는가

NfE(Netcode for Entities)는 Ghost 동기화 + RPC로 **게임 시뮬레이션 상태**를 다루는 데 특화돼 있다.
로그인, 인증, 로비 채팅, 서버 배정 같은 **서비스성 트래픽**은 이 채널과 목적이 다르므로,
완전히 독립된 TCP 소켓 채널로 분리했다. 프레임워크 독립적이라 NfE 버전이 바뀌거나
프로젝트가 바뀌어도 이 레이어는 그대로 재사용 가능하다.

```
[Unity Client]                          [Servers]
  ServiceTcpClient  <--- TCP 7777 --->   ServiceTcpServer   (로그인/인증)
  NfE Client World  <--- UTP 7979 --->   NfE Server World   (게임 시뮬레이션)
```

## 흐름

1. 클라이언트가 `ServiceTcpClient`로 서비스 서버(7777)에 접속, `C2S_LoginRequest` 전송
2. 서버가 계정 검증 후 `S2C_LoginResponse` + `S2C_GameServerHandoff`(NfE 서버 주소/포트/세션 토큰) 전송
3. 클라이언트가 핸드오프 정보로 NfE 클라이언트 월드를 실제 게임 서버(7979, UTP)에 접속
4. NfE 접속 직후 첫 RPC로 세션 토큰을 다시 보내 서버 측 인증과 대조 (TODO로 표시된 부분, 프로젝트 상황에 맞춰 RPC 정의 필요)

## 파일 구성

| 파일 | 역할 |
|---|---|
| `NetPacket.cs` | 오퍼코드 enum, `PacketWriter`/`PacketReader` (바이너리 직렬화) |
| `ServiceTcpServer.cs` | 서버 측 TCP 리스너, 프레임 파싱, 커넥션 관리 |
| `ServiceTcpClient.cs` | Unity 클라이언트 측 TCP 클라이언트, 백그라운드 수신 + 메인 스레드 디스패치 |
| `Packets/LoginPackets.cs` | 로그인 요청/응답, 게임서버 핸드오프 패킷 정의 |
| `ServiceServerExample.cs` | 서버 측 와이어링 예시 |
| `ServiceClientBehaviour.cs` | Unity MonoBehaviour 클라이언트 예시 + NfE 접속 브릿지 |

## 프레임 포맷

리틀엔디안 고정, 길이 프리픽스 방식:

```
[4 bytes] frameLength  (= opcode 2바이트 + payload 길이)
[2 bytes] opcode
[N bytes] payload
```

`ReadExactAsync`로 부분 수신(TCP 특성상 한 번에 다 안 올 수 있음)을 항상 완전히 채워서 읽으므로
패킷 경계가 깨지지 않는다.

## 확장하는 법

1. `PacketOpcode`에 새 오퍼코드 추가
2. `Packets/` 아래 새 패킷 struct 추가 (`Write`/`Read` 메서드 쌍으로)
3. 서버: `OnPacketReceived` 스위치문에 케이스 추가
4. 클라이언트: `HandlePacket` 스위치문에 케이스 추가

## 주의할 점 (몽군님 프로젝트 기준)

- **스레드 안전성**: `ServiceTcpClient`는 백그라운드 스레드에서 수신하고 `ConcurrentQueue`에 쌓아둔 뒤
  `Tick()`(메인 스레드, `Update()`에서 호출)에서만 콜백을 실행한다. `HandlePacket` 안에서 Unity API
  (`GameObject`, `Transform` 등)를 직접 건드려도 안전하다.
- **NfE API 버전 의존성**: `ServiceClientBehaviour.ConnectToGameServer()`의
  `NetworkStreamDriver.Connect()` 호출부는 설치된 `com.unity.netcode` 버전에 따라 시그니처가
  다를 수 있다. 프로젝트에 설치된 버전 기준으로 확인 후 맞춰야 한다.
- **비밀번호 처리**: 예시 코드는 `PasswordHash` 문자열을 그대로 전송한다. 실제 운영에서는
  TLS(TCP 위에 SslStream) 또는 최소한 클라이언트 측 해시 처리를 반드시 적용할 것 — 현재
  `TcpClient`는 평문 소켓이라 스니핑에 취약하다.
- **포트 분리**: 서비스 채널(7777, TCP)과 게임 채널(7979, UTP)을 물리적으로 다른 포트로 뒀다.
  나중에 두 서버를 별도 프로세스/머신으로 분리하기도 쉬운 구조다.
