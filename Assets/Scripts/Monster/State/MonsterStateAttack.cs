using Game;
using UnityEngine;

namespace Monster
{
    public class MonsterStateAttack: State
    {
        
        public override void EnterState()
        {
            Monster.agent.ResetPath();
            // Debug.Log("공격상태진입");
            switch (Monster.currentAttack)
            {
                case AttackType.MonsterAttackClose01:
                    AttackClose01();
                    break;
                case AttackType.MonsterAttackClose02:
                    AttackClose02();
                    break;
                case AttackType.MonsterAttackClose03:
                    AttackClose03();
                    break;
                case AttackType.MonsterAttackCloseCounter:
                    AttackCounter();
                    break;
                case AttackType.MonsterAttackShortRange01:
                    AttackShortRange01();
                    break;
                case AttackType.MonsterAttackShortRange02:
                    AttackShortRange02();
                    break;
                case AttackType.MonsterAttackShortRange03:
                    AttackShortRange03();
                    break;

                default:
                    Debug.LogError($"정의되지 않은 공격 타입: {Monster.currentAttack}");
                    break;
            }
        }

        public override void ExitState()
        {
        }

        public override void UpdateState()
        {
        }

        private void AttackClose01()
        {
            Monster.animator.SetTrigger(Monster.AttackClose01);
        }
        private void AttackClose02()
        {
            Monster.animator.SetTrigger(Monster.AttackClose02);
        }
        private void AttackClose03()
        {
            Monster.animator.SetTrigger(Monster.AttackClose03);
        }
        private void AttackCounter()
        {
            Monster.animator.SetTrigger(Monster.AttackCounter);
        }

        private void AttackShortRange01()
        {
            Monster.animator.SetTrigger(Monster.AttackShortRange01);
        }
        private void AttackShortRange02()
        {
            Monster.animator.SetTrigger(Monster.AttackShortRange02);
        }
        private void AttackShortRange03()
        {
            Monster.animator.SetTrigger(Monster.AttackShortRange03);
        }

    }
}