using Unity.NetCode;

// 클라이언트가 "월드 씬 로딩 끝났고, 이제 실제 게임 상태를 받을 준비 됐다"는
// 신호를 서버에 보내기 위한 RPC. NfE 공식 샘플(Tanks/Asteroids)에서 쓰는 표준 패턴이다.
//
// 왜 필요한가:
// 연결(Connection)이 맺어진 시점 == 플레이어를 스폰해도 되는 시점이 아니다.
// 클라이언트가 씬/에셋 로딩 중일 수 있으므로, 로딩이 끝났다는 명시적 신호를 받고 나서야
// 서버가 NetworkStreamInGame 태그를 붙이고 고스트 스트리밍을 시작해야 한다.
public struct GoInGameRequest : IRpcCommand
{
}
