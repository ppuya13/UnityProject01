using Game;
using UnityEngine;

namespace Monster
{
    public class StateMachine
    {
        private MonsterState currentMonsterState;
        private MonsterController monster;
        private State currentState;

        public StateMachine(MonsterController monsterController)
        {
            monster = monsterController;
        }

        public void ChangeState(MonsterState newMonsterState)
        {
            State newState;
            switch (newMonsterState)
            {
                case MonsterState.MonsterStatusSpawn:
                    newState = new MonsterStateSpawn();
                    break;
                case MonsterState.MonsterStatusIdle:
                    newState = new MonsterStateIdle();
                    break;
                case MonsterState.MonsterStatusMove:
                    newState = new MonsterStateMove();
                    break;
                // case MonsterState.MonsterStatusDash:
                //     break;
                // case MonsterState.MonsterStatusDodge:
                //     break;
                case MonsterState.MonsterStatusAttack:
                    newState = new MonsterStateAttack();
                    break;
                default:
                    Debug.LogError($"정의되지 않은 State: {newMonsterState}");
                    return;
            }
            
            currentState?.ExitState();
            newState.SetStateMachine(monster);
            currentState = newState;
            currentState.EnterState();
        }

        public void Update()
        {
            currentState?.UpdateState();
        }
    }
}