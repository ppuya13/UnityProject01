using Game;
using UnityEngine;

namespace Monster
{
    public class MonsterStateIdle: State
    {
        public override void EnterState()
        {
            if(SuperManager.Instance.IsHost)
                Monster.SendTarget(Monster.SelectRandomTarget());
            Monster.ReadyToAction += ChoicePattern;
        }

        public override void ExitState()
        {
        }

        public override void UpdateState()
        {
        }

        public void ChoicePattern()
        {
            Debug.Log("패턴고를래");
            if (!Monster.currentTarget)
            {
                //타겟이 없으면 그냥 걸어다님
                Monster.SendChangeState(MonsterState.MonsterStatusMove);
            }
            else
            {
                //타겟이 있으면 타겟과의 거리를 측정
                float distance = Vector3.Distance(Monster.transform.position, Monster.currentTarget.transform.position);
                Debug.Log($"타겟과의 거리: {distance}");
                Monster.SendChangeState(MonsterState.MonsterStatusMove);
                
                //3이하가 근접패턴
                //3~5 근거리
                //5~10 중거리
                //10~20 장거리
                //20이상: 초장거리

                if (distance < 3.0f)
                {
                    //근접패턴
                    //이동 가중치 10
                    //카운터 가중치 20
                    //공격1 가중치 50
                    //공격2 가중치 50
                    
                }
                else if(distance < 5.0f)
                {
                    //전진성 있는 근접패턴
                    //옆으로 꽤 멀리 점프한 뒤 타겟을 향해 일섬
                }
                else if (distance < 10.0f)
                {
                    //중거리 패턴
                }
                else if (distance < 20.0f)
                {
                    //장거리 패턴
                }
                else
                {
                    //초장거리 패턴
                    //사라진 뒤 잠시 후에 타겟 옆에서 나타나서 공격
                }
            }
        }
    }
}