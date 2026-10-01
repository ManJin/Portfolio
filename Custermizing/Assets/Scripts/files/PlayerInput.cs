using Unity.Mathematics;
using Unity.NetCode;

// IInputComponentData 는 Netcode for Entities가 자동으로
// ICommandData(입력 히스토리 버퍼 + 예측/재생 로직)로 감싸서 처리해준다.
// 여기 담기는 값은 "그 틱에 어떤 입력이 있었는가"이지, 결과 상태(위치 등)가 아니다.
// 서버는 이 입력을 받아서 시뮬레이션을 재현하고, 클라이언트는 이 입력으로 예측(prediction)한다.
public struct PlayerInput : IInputComponentData
{
    public float2 Move; // WASD / 스틱 이동 방향 (-1~1로 정규화해서 넣는 걸 권장)

    // 점프, 스킬 사용처럼 "눌렀다/뗐다" 같은 엣지 이벤트는 InputEvent를 쓴다.
    // bool을 그냥 쓰면 프레임레이트/틱레이트 차이로 입력 유실이 생길 수 있음.
    public InputEvent PrimaryAction;
}
