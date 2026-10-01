using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using Unity.Mathematics;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct ServerGoInGameSystem : ISystem
{
    // 임시로 BurstCompile 제거 (Debug.Log 때문)
    public void OnCreate(ref SystemState state)
    {
        // 디버깅을 위해 RequireForUpdate 게이트를 임시로 다 뺐다.
        // 이제 이 시스템은 매 프레임 무조건 OnUpdate가 호출된다.
        UnityEngine.Debug.Log("[ServerGoInGameSystem] OnCreate (게이트 없음, 매 프레임 실행)");
    }

    // 임시로 BurstCompile 제거 - Debug.Log(매니지드 호출)가 섞여있어서 Burst와 호환 안 됨.
    // 디버깅 끝나고 로그를 지우면 다시 [BurstCompile]을 붙여도 된다.
    public void OnUpdate(ref SystemState state)
    {
       // UnityEngine.Debug.Log($"[ServerGoInGameSystem] OnUpdate");
        // 매 프레임 무조건 도는 상태라, SubScene이 아직 로드되기 전 이른 프레임에는
        // PlayerSpawnSettings 싱글턴이 없을 수 있다. 방어적으로 체크.
        if (!SystemAPI.HasSingleton<PlayerSpawnSettings>())
        {
            UnityEngine.Debug.Log($"[ServerGoInGameSystem] No Singleton<PlayerSpawnSettings> found");
            return;
        }

        // 매 프레임 무조건 실행되므로, 지금 이 프레임에 RPC 요청이 몇 개나 잡히는지 직접 세본다.
        var rpcCount = SystemAPI.QueryBuilder().WithAll<GoInGameRequest, ReceiveRpcCommandRequest>().Build().CalculateEntityCount();
        if (rpcCount > 0)
        {
            UnityEngine.Debug.Log($"[ServerGoInGameSystem] 이번 프레임 매치된 GoInGameRequest 수={rpcCount}");
        }

        var prefab = SystemAPI.GetSingleton<PlayerSpawnSettings>().PlayerPrefab;
        var commandBuffer = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (reqSrc, reqEntity) in
                 SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>>()
                     .WithAll<GoInGameRequest>().WithEntityAccess())
        {
            UnityEngine.Debug.Log($"[ServerGoInGameSystem] GoInGameRequest 수신. prefab={prefab}");

            var connectionEntity = reqSrc.ValueRO.SourceConnection;

            // 이 연결을 "인게임" 상태로 전환. 이 태그가 붙어야 고스트 스트리밍이 시작된다.
            commandBuffer.AddComponent<NetworkStreamInGame>(connectionEntity);

            var networkId = state.EntityManager.GetComponentData<NetworkId>(connectionEntity);

            // 실제 MMORPG라면 여기서 로그인/캐릭터 로드 로직(DB 조회 등)이 들어가야 함.
            // 지금은 최소 골격이므로 원점 근처에 바로 스폰만 한다.
            var playerEntity = commandBuffer.Instantiate(prefab);
            UnityEngine.Debug.Log($"[ServerGoInGameSystem] Instantiate 결과(ECB deferred) playerEntity={playerEntity}, networkId={networkId.Value}");

            commandBuffer.SetComponent(playerEntity, LocalTransform.FromPosition(new float3(0, 0, 0)));

            // GhostOwner: 이 엔티티를 어느 클라이언트가 "소유"하는지(=예측 권한을 갖는지) 표시.
            // 주의: SetComponent는 컴포넌트를 "추가"하지 않고 "값만 갱신"한다.
            // GhostOwner 자체는 Ghost Authoring Component의 Has Owner 체크박스를 켜야
            // 프리팹에 자동으로 베이킹되어 존재한다. Has Owner를 안 켰다면 이 줄에서
            // "component does not exist" 런타임 예외가 난다.
            commandBuffer.SetComponent(playerEntity, new GhostOwner { NetworkId = networkId.Value });

            // 연결 엔티티에 CommandTarget을 세팅해야 이 클라이언트의 입력이 이 플레이어 엔티티로 라우팅된다.
            commandBuffer.AddComponent(connectionEntity, new CommandTarget { targetEntity = playerEntity });

            commandBuffer.DestroyEntity(reqEntity);
        }

        commandBuffer.Playback(state.EntityManager);

        if (rpcCount > 0)
        {
            // playback 이후 실제로 GhostOwner를 가진 엔티티가 몇 개나 존재하는지 세어본다.
            // Instantiate가 진짜 성공했다면 최소 1개 이상 잡혀야 정상이다.
            var count = SystemAPI.QueryBuilder().WithAll<GhostOwner>().Build().CalculateEntityCount();
            UnityEngine.Debug.Log($"[ServerGoInGameSystem] playback 완료. 현재 GhostOwner를 가진 엔티티 수={count}");
        }
    }
}