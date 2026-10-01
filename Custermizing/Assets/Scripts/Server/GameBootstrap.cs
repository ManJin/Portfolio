using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.Scripting;

// 기본 ClientServerBootstrap은 서버/클라이언트 월드를 "만들기만" 하고,
// 실제로 리슨하거나 접속하는 건 개발자가 직접 해야 한다 (NfE 공식 문서 기준).
//
// AutoConnectPort를 월드 생성 전에 설정해두면:
// - 서버 월드는 생성되자마자 자동으로 그 포트에서 리슨을 시작한다.
// - 클라이언트 월드는 생성되자마자 자동으로 그 포트로 접속을 시도한다
//   (기본 접속 주소는 loopback, 즉 127.0.0.1).
// - 에디터의 PlayMode Tools도 이 값을 인식해서 override할 수 있게 된다.
//
// 이 파일이 있으면 AutoListenSystem.cs는 더 이상 필요 없다 (지워도 된다) -
// 서버 쪽 자동 리슨 기능이 여기로 흡수됐다.
[Preserve]
public class GameBootstrap : ClientServerBootstrap
{
    public override bool Initialize(string defaultWorldName)
    {
        Debug.Log($"GameBootstrap.Initialize");
        ushort gamePort = 7777; // 커맨드라인 인자가 없을 때 쓰는 기본값
        ushort servicePort = 7778;
        
        var args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-port" && i + 1 < args.Length && ushort.TryParse(args[i + 1], out var parsedGamePort))
            {
                gamePort = parsedGamePort;
            }
            else if (args[i] == "-servicePort" && i + 1 < args.Length && ushort.TryParse(args[i + 1], out var parsedServicePort))
            {
                servicePort = parsedServicePort;
            }
        }

        AutoConnectPort = gamePort;

        // 빌드 타입(서버 전용/클라이언트 전용/둘 다)이나 에디터의 PlayMode Tools 설정에 맞춰
        // 알맞은 월드를 만들어준다. 이 호출 자체는 원래 기본 Initialize()가 하던 일과 같다.
        CreateDefaultClientServerWorlds();
        
        // 서버 월드가 실제로 만들어지는 빌드(서버 전용, 또는 서버+클라이언트 겸용)에서만
        // 서비스 채널을 같이 띄운다. 순수 클라이언트 빌드에서는 실행하지 않는다.
        if (RequestedPlayType == PlayType.Server || RequestedPlayType == PlayType.ClientAndServer)
        {
            Debug.Log($"GameBootstrap.Initialize.Run");
            ServiceNetwork.Example.ServiceServerExample.Run(servicePort: servicePort, gameServerPort: gamePort);
        }
        return true;
    }
}
