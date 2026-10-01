using Portfolio;
using Unity.Entities;
using Unity.NetCode;
using Unity.Mathematics;
using UnityEngine;

// 입력을 "수집"하는 시스템은 GhostInputSystemGroup에서 돌아야 한다.
// 이 그룹은 PredictedSimulationSystemGroup보다 먼저 실행되도록 NetCode가 순서를 보장해준다 -
// 즉 "이번 틱에 쓸 입력을 먼저 채워넣고 나서" 예측 이동 로직(PlayerMovementSystem)이 그걸 읽는 순서다.
// 클라이언트에서만 실제 키보드를 읽어야 하므로 ClientSimulation으로 제한한다.
[UpdateInGroup(typeof(GhostInputSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct PlayerInputGatherSystem : ISystem
{
    private InputManager inputManager
    {
        get
        {
            if (MainSystem.Instance != null)
                return MainSystem.Instance.InputManager;
            return null;
        }
    }
    public void OnCreate(ref SystemState state)
    {
    }

    public void OnUpdate(ref SystemState state)
    {
        if (inputManager == null)
            return;
        
        // GhostOwnerIsLocal: 이 클라이언트가 "소유"한 고스트에만 자동으로 붙는 태그.
        // 이걸로 필터링하지 않으면, 나중에 다른 플레이어의 고스트에도 PlayerInput이
        // 있는 경우(설계에 따라 다름) 남의 캐릭터 입력을 내가 덮어쓰는 사고가 날 수 있다.
        foreach (var input in
                 SystemAPI.Query<RefRW<PlayerInput>>().WithAll<GhostOwnerIsLocal>())
        {
            float2 move = float2.zero;

            // 이벤트를 받아서 정의된 Move Flags 로 이동값 갱신. 
            if (inputManager.MoveDown) move.y -= 1;
            if (inputManager.MoveUp) move.y += 1;
            if (inputManager.MoveLeft) move.x -= 1;
            if (inputManager.MoveRight) move.x += 1;

            // read write 프로퍼티
            input.ValueRW.Move = move;
        }
    }
    
    ///Mono 혹은 일반 클래스에서 SystemAPI 없이 호출 하는 경우에 이렇게 사용할것. 
    /*private World world;
    private EntityQuery inputQuery;
    
    public void OnUpdate(ref SystemState state)
    {
        if (inputManager == null)
            return;
        
        world = World.DefaultGameObjectInjectionWorld;
        inputQuery = world.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<PlayerInput>(),
            ComponentType.ReadOnly<GhostOwnerIsLocal>());
        // 엔티티 직접 뽑아서 갱신
        var entities = inputQuery.ToEntityArray(Allocator.Temp);
        if (entities.Length <= 0)
            return false;
        var entity = entities[0];
        var input = world.EntityManager.GetComponentData<PlayerInput>(entity);
        // 입력 등 보내는 경우 
        Vector2 move = Vector2.zero;
        move.x += 1;
        input.Move = move;
        world.EntityManager.SetComponentData(entity, input);
                    
        //입력 받아오는 경우에 
        foreach (var input in
                 SystemAPI.Query<RefRW<PlayerInput>>().WithAll<GhostOwnerIsLocal>())
        {
            float2 move = float2.zero;

            // 이벤트를 받아서 정의된 Move Flags 로 이동값 갱신. 
            if (inputManager.MoveDown) move.y -= 1;
            if (inputManager.MoveUp) move.y += 1;
            if (inputManager.MoveLeft) move.x -= 1;
            if (inputManager.MoveRight) move.x += 1;

            // read write 프로퍼티
            input.ValueRW.Move = move;
        }
    }*/
}
