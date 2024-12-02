using System;
using System.Collections.Generic;
using Game;
using UnityEngine;
using UnityEngine.AI;

namespace Monster
{
    public class MonsterController: MonoBehaviour
    {
        public string monsterId;
        
        protected StateMachine Sm;

        private MonsterState currentState;
        public MonsterState CurrentState
        {
            get => currentState;
            private set
            {
                Debug.Log($"몬스터 스테이트 변경: {currentState} => {value}");
                currentState = value;
                Sm.ChangeState(value);
            }
        }
    
        [HideInInspector] public NavMeshAgent agent;
        [HideInInspector] public Animator animator;
    
        public bool dummyMode = true; //true로 바꿀 경우 더미를 타겟으로 한다.(런타임 중 변경해도 바뀌지 않음.)
        
        private List<PlayerController> targetList = new();
        
        public readonly int Spawn = Animator.StringToHash("Spawn");
        public readonly int Move = Animator.StringToHash("Move");
        public readonly int Dash = Animator.StringToHash("Dash");
        public readonly int Dodge = Animator.StringToHash("Dodge");


        private void Awake()
        {
            //상태머신 초기화
            Sm = new StateMachine(this);
            // CurrentState = MonsterState.MonsterStatusSpawn;
            
            //NavMesh 초기화
            agent = GetComponent<NavMeshAgent>();
            
            //기타 스테이터스 초기화
            animator = GetComponent<Animator>();
        }

        private void Update()
        {
            Sm.Update();
        }

        public void SendChangeState(MonsterState state)
        {
            if(!SuperManager.Instance.IsHost) return;
            TcpProtobufClient.Instance.SendMonsterChangeState(monsterId, state);
        }

        public void ChangeState(MonsterState state)
        {
            CurrentState = state;
        }

        //서버에 애니메이터 파라미터값을 변경하기 위해 보내는 값
        public void SendMonsterAnim(int hash, ParameterType type,  int intValue = 0, float floatValue = 0, bool boolValue = false)
        {
            if(!SuperManager.Instance.IsHost) return;
            TcpProtobufClient.Instance.SendMonsterAnimMessage(monsterId, hash, type, intValue, floatValue, boolValue);
        }

        //서버에서 보낸 메시지를 받아서 실제로 파라미터를 변경
        public void SetParameter(MonsterAnim msg)
        {
            switch (msg.ParameterType)
            {
                case ParameterType.ParameterInt:
                    animator.SetInteger(msg.AnimHash, msg.IntValue);
                    break;
                case ParameterType.ParameterFloat:
                    animator.SetFloat(msg.AnimHash, msg.FloatValue);
                    break;
                case ParameterType.ParameterBool:
                    animator.SetBool(msg.AnimHash, msg.BoolValue);
                    break;
                case ParameterType.ParameterTrigger:
                    animator.SetTrigger(msg.AnimHash);
                    break;
                default:
                    Debug.LogError($"정의되지 않은 파라미터 타입: {msg.ParameterType}");
                    return;
            }
        }

        //타겟을 선택
        public void SelectTarget()
        {
            
        }

        //선택한 타겟을 서버에 전송
        public void SendTarget()
        {
            
        }

        //전송받은 타겟을 받아서 실제로 타겟을 변경
        public void SetTarget()
        {
            
        }
        
    }
}
