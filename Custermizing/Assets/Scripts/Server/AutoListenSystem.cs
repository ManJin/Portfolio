/*
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;

// 에디터의 PlayMode Tools는 서버 월드가 생기면 버튼 클릭으로 Listen을 걸어주지만,
// 실제 빌드된 데디케이트 서버 실행 파일에는 그 UI가 없다.
// 그래서 서버 월드가 만들어진 직후 딱 한 번, 코드로 포트를 열어줘야 한다.
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct AutoListenSystem : ISystem, ISystemStartStop
{
    public void OnCreate(ref SystemState state)
    {
        // NetworkStreamDriver 싱글턴이 준비될 때까지 대기 (서버 월드 생성 직후 자동으로 생김)
        state.RequireForUpdate<NetworkStreamDriver>();
        UnityEngine.Debug.Log($"[Server] OnCreate {state.DebugName}");
    }

    ///
    /// RequireForUpdate 조건이 false→true→false→true로 바뀌거나, state.Enabled를 껐다 켰다 하면 그때마다 다시 호출
    /// 
    public void OnStartRunning(ref SystemState state)
    {
        ushort port = 7777; // 커맨드라인 인자가 없을 때 쓰는 기본값
        
                var args = System.Environment.GetCommandLineArgs();
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i] == "-port" && i + 1 < args.Length && ushort.TryParse(args[i + 1], out var parsed))
                    {
                        port = parsed;
                        break;
                    }
                }
        
                var driver = SystemAPI.GetSingletonRW<NetworkStreamDriver>();
                var endpoint = NetworkEndpoint.AnyIpv4.WithPort(port);
        
                if (driver.ValueRW.Listen(endpoint))
                {
                    UnityEngine.Debug.Log($"[Server] Listening on port {port}");
                }
                else
                {
                    UnityEngine.Debug.LogError($"[Server] Failed to listen on port {port}");
                }
    }
    
    /// 이 시스템은 껐다 켤 일이 딱히 없어서 비워두지만,
    /// ISystemStartStop 인터페이스가 요구하는 메서드라 구현은 필수다.
    public void OnStopRunning(ref SystemState state)
    {
        UnityEngine.Debug.Log($"[Server] OnStopRunning Called");
    }
    
    public void OnUpdate(ref SystemState state)
    {
        // 한 번만 실행하고 스스로 꺼진다. 매 프레임 Listen을 다시 호출할 필요가 없다.
        state.Enabled = false;

        UnityEngine.Debug.Log($"[Server] OnUpdate Called");
        
        /*ushort port = 7777; // 커맨드라인 인자가 없을 때 쓰는 기본값

        var args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-port" && i + 1 < args.Length && ushort.TryParse(args[i + 1], out var parsed))
            {
                port = parsed;
                break;
            }
        }

        var driver = SystemAPI.GetSingletonRW<NetworkStreamDriver>();
        var endpoint = NetworkEndpoint.AnyIpv4.WithPort(port);

        if (driver.ValueRW.Listen(endpoint))
        {
            UnityEngine.Debug.Log($"[Server] Listening on port {port}");
        }
        else
        {
            UnityEngine.Debug.LogError($"[Server] Failed to listen on port {port}");
        }#1#
    }
}
*/
