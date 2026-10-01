using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;

// PredictedSimulationSystemGroup 안에서 도는 시스템은
// 서버(정본 시뮬레이션)와 클라이언트(자기 캐릭터 예측) 양쪽에서 "완전히 동일한 코드"로 실행된다.
// 서버 권위(server-authoritative)를 유지하면서도 클라이언트가 입력 지연 없이
// 즉각 반응하는 것처럼 보이게 하는 핵심 메커니즘.
[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
public partial struct PlayerMovementSystem : ISystem
{
    //[BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;

        // PredictedSimulationSystemGroup 안에서는 이번 틱에 실제로 시뮬레이션해야 하는
        // 엔티티만 처리해야 한다. Simulate는 NetCode가 롤백/예측 시점에 맞춰 켜고 끄는
        // 활성화 컴포넌트(enableable component)라서, .WithAll<Simulate>()로 반드시
        // 명시적으로 걸러줘야 한다 - 이게 없으면 이번 틱에 관련 없는 엔티티까지 건드릴 수 있다.
        foreach (var (transform, input, speed) in
                 SystemAPI.Query<RefRW<LocalTransform>, RefRO<PlayerInput>, RefRO<MovementSpeed>>().WithAll<Simulate>())
        {
            var move = input.ValueRO.Move;
            
            if (math.lengthsq(move) > 1f)
                move = math.normalize(move);

            //Debug.Log($"Move: {move}");
            transform.ValueRW.Position += new float3(move.x, move.y, 0) * speed.ValueRO.Value * deltaTime;
        }
    }
}
