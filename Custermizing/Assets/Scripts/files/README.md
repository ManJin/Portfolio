# Netcode for Entities 기반 MMORPG 서버 - 기본 골격

## 전제 패키지
- `com.unity.entities` 1.x
- `com.unity.netcode` (Netcode for Entities) 1.x
- `com.unity.transforms`, `com.unity.entities.graphics` (렌더링 필요시)

## 파일 구성과 역할

```
Assets/Scripts/
  Netcode/
    GoInGameRpc.cs            - "로딩 끝났다" 신호용 RPC 정의
    ClientGoInGameSystem.cs   - 클라이언트: 연결되면 RPC 전송
    ServerGoInGameSystem.cs   - 서버: RPC 받으면 인게임 전환 + 플레이어 스폰
  Player/
    PlayerInput.cs            - 입력 커맨드 데이터 (IInputComponentData)
    PlayerAuthoring.cs        - 플레이어 고스트 프리팹용 authoring/baker
    PlayerSpawnSettings.cs    - 서버가 스폰할 프리팹 참조용 싱글턴
    PlayerMovementSystem.cs   - 서버/클라이언트 공용 예측 이동 로직
```

## 에디터에서 해야 하는 작업 (코드만으로는 안 됨)
1. **Player 프리팹** 생성 → `PlayerAuthoring` 부착 → `Ghost Authoring Component` 추가
   - `Support Auto Command Target` 체크 → `PlayerInput`이 자동으로 소유 클라이언트에 라우팅됨
   - Ghost Mode를 `Owner Predicted`로: 내 캐릭터는 예측(prediction), 남의 캐릭터는 보간(interpolation)
2. **SubScene**에 Player 프리팹 배치, 별도 오브젝트에 `PlayerSpawnSettingsAuthoring` 붙이고 프리팹 연결
3. **Multiplayer PlayMode Tools**로 로컬에서 1 Server + N Client 테스트
4. 빌드 시 `Multiplayer Role`을 Server/Client/ClientAndServer로 지정해서 데디케이티드 서버 빌드 생성

## 동작 흐름
1. 클라이언트가 서버에 연결 (`NetworkStreamConnection` 생성됨)
2. 클라이언트가 씬 로딩 완료 → `GoInGameRequest` RPC 전송
3. 서버가 RPC 수신 → 연결에 `NetworkStreamInGame` 부착 (이때부터 고스트 스트리밍 시작) → Player 고스트 인스턴스화 → `CommandTarget` 연결
4. 이후 매 틱: 클라이언트는 `PlayerInput`을 서버로 전송, 서버는 이를 시뮬레이션의 정본으로 사용
5. `PlayerMovementSystem`이 서버/클라이언트 양쪽에서 동일 코드로 실행 → 클라이언트는 예측, 서버 결과가 오면 틀린 만큼 보정(reconciliation)

## 이 골격에서 아직 빠진 것 (실서비스 MMORPG로 가려면 필요)
- **관심 영역(AOI) / 릴러번시(relevancy) 관리**: 기본 설정은 모든 고스트를 모든 클라이언트에 브로드캐스트한다.
  플레이어 수백~수천 명 규모에서는 이게 그대로 대역폭 폭발로 이어진다.
  NfE는 `GhostConnectionPosition` + `GhostDistanceImportance` / `GhostImportance` 커스텀 함수로
  "이 연결과 가까운 고스트만 우선 전송"하는 걸 지원한다. 게임 클라이언트에서 dirty queue로
  업데이트 대상을 좁혔던 것과 개념적으로 같은 문제이고, 여기서는 서버가 그 역할을 한다.
- **월드 분할/존 서버**: 단일 서버 월드로는 결국 CCU 한계가 온다. Zone/Shard 단위로
  서버 프로세스를 나누고 경계에서 핸드오프하는 구조가 필요.
- **로그인/캐릭터 로드**: 지금은 접속하면 원점에 즉시 스폰. 실제로는 계정 인증 → DB에서
  캐릭터 상태 로드 → 해당 위치에 스폰하는 과정이 `ServerGoInGameSystem`에 들어가야 함.
- **치트 방지/유효성 검증**: 서버가 입력을 그대로 신뢰하고 있음. 이동 속도, 스킬 쿨다운 등
  서버 측 검증 로직 필요.
- **인터레스트 관리 + 컬링**: 전투/스킬 등 이벤트성 RPC도 근처 플레이어에게만 보내야 함.

## 참고
공식 샘플(Tanks, Asteroids - Unity-Technologies/NetcodeSamples 저장소)에 이 패턴들이
실제 동작하는 형태로 있으니, 버전별 API 디테일은 그쪽 최신 버전과 대조해서 맞추는 걸 권장.
NfE는 버전 간 API 변경이 잦은 편이라 사용 중인 Unity/Netcode 패키지 버전을 꼭 확인해줘.
