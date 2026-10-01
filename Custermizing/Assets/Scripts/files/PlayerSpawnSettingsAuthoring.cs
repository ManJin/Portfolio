using Unity.Entities;
using UnityEngine;

// 서버가 "새 플레이어 접속 시 무엇을 스폰할지" 알아야 하므로,
// 씬(SubScene)에 배치한 오브젝트를 통해 Player 고스트 프리팹 엔티티를 등록해둔다.
public struct PlayerSpawnSettings : IComponentData
{
    public Entity PlayerPrefab;
}

public class PlayerSpawnSettingsAuthoring : MonoBehaviour
{
    // 인스펙터에서 "Player" 고스트 프리팹(GhostAuthoringComponent가 붙은 프리팹)을 드래그해서 연결
    public GameObject PlayerPrefab;

    class Baker : Baker<PlayerSpawnSettingsAuthoring>
    {
        public override void Bake(PlayerSpawnSettingsAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            Debug.Log($"[client] Bake {entity}");
            AddComponent(entity, new PlayerSpawnSettings
            {
                PlayerPrefab = GetEntity(authoring.PlayerPrefab, TransformUsageFlags.Dynamic)
            });
            Debug.Log($"[client] PlayerPrefab = {authoring.PlayerPrefab.name}");    
            
        }
    }
}
