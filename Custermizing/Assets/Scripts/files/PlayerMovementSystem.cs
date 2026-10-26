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
        const float turnSpeed = 2f; // 클수록 방향 전환이 빠름
        float t = 1f - math.exp(-turnSpeed * deltaTime);
        // PredictedSimulationSystemGroup 안에서는 이번 틱에 실제로 시뮬레이션해야 하는
        // 엔티티만 처리해야 한다. Simulate는 NetCode가 롤백/예측 시점에 맞춰 켜고 끄는
        // 활성화 컴포넌트(enableable component)라서, .WithAll<Simulate>()로 반드시
        // 명시적으로 걸러줘야 한다 - 이게 없으면 이번 틱에 관련 없는 엔티티까지 건드릴 수 있다.
        foreach (var (transform, input, speed, smooth) in
                 SystemAPI.Query<RefRW<LocalTransform>, RefRO<PlayerInput>, RefRO<MovementSpeed>, RefRW<SmoothMoveState>>().WithAll<Simulate>())
        {
            // 입력은 normalizesafe로 대각선 보정 (길이 0이면 0 반환)
            float2 target = math.normalizesafe(input.ValueRO.Move);

            // 목표 방향을 따라가도록 보간
            float2 dir = math.lerp(smooth.ValueRO.CurrentDir, target, t);

            // 아주 작은 값은 0으로 스냅해서 미세하게 미끄러지는 현상 방지
            if (math.lengthsq(dir) < 0.00001f && math.lengthsq(target) == 0f)
                dir = float2.zero;

            // 입력이 있을 때만 움직임. 
            if (math.lengthsq(target) > 0f)
            {
                float angle = math.atan2(dir.y, dir.x);
                // 스프라이트 정면이 +X 기준. +Y가 정면이면 angle - math.PIHALF
                transform.ValueRW.Rotation = quaternion.RotateZ(angle - math.PIHALF);
                
                smooth.ValueRW.CurrentDir = dir;
                transform.ValueRW.Position += new float3(dir.x, dir.y, 0) * speed.ValueRO.Value * deltaTime;
            }
        }
    }
}
