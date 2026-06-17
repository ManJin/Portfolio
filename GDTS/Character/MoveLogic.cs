using UnityEngine;

namespace clone
{
    public static class MoveLogic
    {
        public static void ExcuteMove(Character character, Vector2 direction, float magnitude)
        {
            // 뭔가에 의한 스탑 필요시 
            bool isStop = false;
            if (MainSystem.Instance.State != GameState.Playing)
            {// 플레이 중이 아니면 계산 하지 않음. 
                return;
            }
            
            Vector2 startPos = character.Position;
            Bounds startBounds = character.HittableBounds;

            Vector2 move = direction * magnitude;

            Bounds b = startBounds;
            var list = MainSystem.Instance.GetFieldObjectsCollideBy(b, move);
            float collisionDistance = 0f;
            bool isCollide = false;
            for (int i = 0; i < list.Count; i++)
            {
                collisionDistance = Utils.GetDistanceBetween(b, list[i].HittableBounds); 
                if (collisionDistance >= magnitude)
                {
                    //둘사이 거리보다 이동 거리가 짧으면 무시   
                    continue;
                }

                if (ReferenceEquals(character, list[i]))
                {
                    // 본인이면 패스 
                    continue;
                }

                // 이동 후에 곂치는게 있다면 거기서 멈출수 있도록
                Vector2 snapPos = startPos + direction * Mathf.Max(0, collisionDistance);
                character.Position = snapPos;
                isCollide = true;
            }
            // 리스트 해제 
            list.Dispose();
            
            if (!isCollide)
            {// 충돌한게 없으면 
                Vector2 pos = startPos + move;
                character.Position = pos;
            }
        }
    }    
}

