using System;

namespace clone
{
    /// <summary>
    /// 방향 관련
    /// </summary>
    public enum Direction
    {
        None = 0,
        Left = 1 << 1,
        Right = 1 << 2,
        Up = 1 << 3,
        Down = 1 << 4,
    }

    /// <summary>
    /// UI 종류 관련
    /// </summary>
    public enum UIType
    {
        MoveLeft,
        MoveRight,
        Action1,
        Action2, 
        Action3,
    }

    /// <summary>
    /// ui 입력 상태 관련
    /// </summary>
    public enum TouchEventType
    {
        None = 0,
        MoveLeftTouchDown,
        MoveLeftTouchUp,
        MoveRightTouchDown,
        MoveRightTouchUp,
        Action1TouchDown,
        Action1TouchUp, 
        Action2TouchDown, 
        Action2TouchUp,
        Action3TouchDown,
        Action3TouchUp,
    }

    public enum GameState
    {
        Prepare,
        Playing,
        Paused,
        End,
    }

    public enum EntityGroup
    {
        None,
        //장애물
        Obstacle,
        //플레이어
        Player,
        //적
        Enemy,
    }
    public enum HitResultType
    {
        // 히트한 결과로 아무동작 하지 않음. 
        None,
        // 히트한 대상에 데미지 
        Damage,
    }

    public enum PenetratingType
    {
        // 관통하지 않음. 
        None,
        // 적만 관통
        Enemy,
        // 모두 관통
        All,
    }
}
