using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

// GhostField가 붙은 값은 서버->클라이언트로 복제(스냅샷)되고,
// 필요시 델타 압축/양자화(quantize)까지 NfE가 자동 처리한다.
public struct MovementSpeed : IComponentData
{
    [GhostField] public float Value;
}

public class PlayerAuthoring : MonoBehaviour
{
    public float MoveSpeed = 5f;

    class Baker : Baker<PlayerAuthoring>
    {
        public override void Bake(PlayerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new MovementSpeed { Value = authoring.MoveSpeed });
            AddComponent<PlayerInput>(entity);
        }
    }
}

/*
에디터에서 해야 할 작업 (코드만으로는 불가능):
1. Player 프리팹 GameObject 생성 → PlayerAuthoring 컴포넌트 부착
2. 같은 프리팹에 "Ghost Authoring Component" 추가
   - Has Owner 체크 → 그 아래 Support Auto Command Target이 나타남 (자동 체크됨)
   - Default Ghost Mode: Owner Predicted로 설정 → 클라이언트가 "내 캐릭터"는 예측,
     "남의 캐릭터"는 보간(interpolation)하도록 자동 분기됨
     (참고: CommandTarget 자체는 ServerGoInGameSystem에서 코드로 직접 세팅하므로,
      Support Auto Command Target을 꼭 켜지 않아도 스폰/이동 자체는 동작한다)
3. 프리팹을 SubScene 안에 두고, 별도 GameObject에 PlayerSpawnSettingsAuthoring을
   붙여서 이 프리팹을 참조시킨다.
*/
