using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

// [WorldSystemFilterFlags] 로 클라이언트 월드에서만 생성되도록 제한.
// 서버 빌드에는 이 시스템 자체가 존재하지 않는다 (컴파일 상수 #if 안 써도 됨).
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct ClientGoInGameSystem : ISystem
{
    //[BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        UnityEngine.Debug.Log($"[ClientGoInGameSystem] OnCreate");
        var builder = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<NetworkId>()
            .WithNone<NetworkStreamInGame>();
        state.RequireForUpdate(state.GetEntityQuery(builder));
        builder.Dispose();
    }

    //[BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var commandBuffer = new EntityCommandBuffer(Allocator.Temp);
 
        //UnityEngine.Debug.Log($"[ClientGoInGameSystem] OnUpdate");
        int matched = 0;
        // 아직 인게임 상태가 아닌 연결 엔티티들을 찾아서
        // "인게임으로 전환해줘" 요청을 서버에 보낸다.
        foreach (var (id, entity) in
                 SystemAPI.Query<RefRO<NetworkId>>().WithEntityAccess().WithNone<NetworkStreamInGame>())
        {
            matched++;
            UnityEngine.Debug.Log($"[ClientGoInGameSystem] 아직 인게임 아닌 연결 발견. networkId={id.ValueRO.Value}, entity={entity}. GoInGameRequest RPC 전송 시도.");
 
            commandBuffer.AddComponent<NetworkStreamInGame>(entity);
 
            var reqEntity = commandBuffer.CreateEntity();
            commandBuffer.AddComponent<GoInGameRequest>(reqEntity);
            commandBuffer.AddComponent(reqEntity, new SendRpcCommandRequest { TargetConnection = entity });
        }
 
        if (matched == 0)
        {
            UnityEngine.Debug.Log("[ClientGoInGameSystem] OnUpdate 호출됐지만 조건에 맞는 연결이 없음 (이미 NetworkStreamInGame이 붙어있거나 NetworkId가 없음).");
        }
 
        commandBuffer.Playback(state.EntityManager);
    }
}
