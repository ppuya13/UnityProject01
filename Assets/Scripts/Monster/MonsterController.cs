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
    
        [HideInInspector]public NavMeshAgent agent;
    
        public bool dummyMode = false; //인스펙터에서 true로 바꿀 경우 더미를 타겟으로 한다.

        private GameObject characters; //타겟 캐릭터들을 담고 있는 루트 오브젝트
        private List<PlayerController> targetList = new();
    
    
        private void Awake()
        {
            //상태머신 초기화
            Sm = new StateMachine(this);
            // CurrentState = MonsterState.MonsterStatusSpawn;
            
            //NavMesh 초기화
            agent = GetComponent<NavMeshAgent>();
            
            //기타 스테이터스 초기화
            characters = GameObject.Find("Characters");
            if(characters)
            {
                ScanPlayer();
            }
            else
            {
                Debug.LogError("MonsterController: characters를 찾을 수 없음");
            }
        }

        private void Start()
        {
            ScanPlayer();
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
        
        private void ScanPlayer()
        {
            foreach (Transform obj in characters.transform)
            {
                PlayerController playerController = obj.GetComponent<PlayerController>();
                if (playerController)
                {
                    if (dummyMode && playerController.type == PlayerController.CharacterType.Dummy) 
                        targetList.Add(playerController);
                    else if (!dummyMode && playerController.type 
                                 is PlayerController.CharacterType.MyPlayer
                                 or PlayerController.CharacterType.OtherPlayer)
                    {
                        targetList.Add(playerController);
                    }
                }
            }

            if (targetList.Count == 0)
            {
                Debug.LogError("target을 찾지 못했음.");
            }
        }
    }
}
