using System;
using System.Collections;
using System.Collections.Generic;
using Game;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

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

        public AttackType currentAttack = AttackType.MonsterAttackUnknown;
    
        [HideInInspector] public NavMeshAgent agent;
        [HideInInspector] public Animator animator;
    
        public bool dummyMode = true; //true로 바꿀 경우 더미를 타겟으로 한다.
        
        public List<PlayerController> targetList = new();
        public PlayerController currentTarget; 
        
        public readonly int Spawn = Animator.StringToHash("Spawn");
        public readonly int Horizontal = Animator.StringToHash("Horizontal");
        public readonly int Vertical = Animator.StringToHash("Vertical");
        public readonly int Dash = Animator.StringToHash("Dash");
        public readonly int Dodge = Animator.StringToHash("Dodge");
        public readonly int TurnLeft = Animator.StringToHash("TurnLeft");
        public readonly int TurnRight = Animator.StringToHash("TurnRight");
        public readonly int MoveAnimSpeed = Animator.StringToHash("MoveAnimSpeed");
        
        //회전 관련 변수
        public AnimationClip turnLeftClip;
        public AnimationClip turnRightClip;
        private float turnLeftAnimationDuration;
        private float turnRightAnimationDuration;
        private bool isRotating = false;
        
        public Action ReadyToAction;

        public float moveDistance = 20.0f; // 랜덤 이동 반경
        
        // 이동 모션의 부드러운 전환을 위한 Damp값
        public float horizontalDamp = 0.1f;
        public float verticalDamp = 0.1f;
        public float speedDamp = 0.1f;


        private void Awake()
        {
            //상태머신 초기화
            Sm = new StateMachine(this);
            // CurrentState = MonsterState.MonsterStatusSpawn;
            
            //NavMesh 초기화
            agent = GetComponent<NavMeshAgent>();
            agent.speed = 1.52f;
            
            //애니메이터 초기화
            animator = GetComponent<Animator>();
            
            //기타 스테이터스 초기화
            turnLeftAnimationDuration = turnLeftClip ? turnLeftClip.length : 1.0f;
            turnRightAnimationDuration = turnRightClip ? turnRightClip.length : 1.0f;
        }

        private void Update()
        {
            Sm.Update();
        }

        public void SendChangeState(MonsterState state, AttackType attackType = AttackType.MonsterAttackUnknown)
        {
            if(!SuperManager.Instance.IsHost) return;
            TcpProtobufClient.Instance.SendMonsterChangeState(monsterId, state, attackType);
        }

        public void ChangeState(MonsterAction msg)
        {
            if (msg.MonsterState is MonsterState.MonsterStatusAttack)
            {
                //어택스테이트일 경우 어떤 공격인지도 받아온다.
                currentAttack = msg.AttackType;
            }
            CurrentState = msg.MonsterState;
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

        //타겟 리스트를 작성
        public void SetTargetList()
        {
            targetList.Clear();
            if (!dummyMode)
            {
                foreach (var player in SpawnManager.Instance.SpawnedPlayers.Values)
                {
                    targetList.Add(player);
                }
            }
            else
            {
                foreach (var dummy in SpawnManager.Instance.Dummys.Values)
                {
                    targetList.Add(dummy);
                }
            }
        }
        
        //타겟 리스트에서 랜덤한 타겟의 id를 반환한다. 거리가 가까운 적을 타겟으로 설정할 확률이 더 높다.
        public string SelectRandomTarget()
        {
            SetTargetList();
            if (targetList == null || targetList.Count == 0)
            {
                Debug.LogWarning("타겟 리스트가 비어있음!!");
                return string.Empty;
            }
            
            // 가중치 리스트와 총 가중치 초기화
            List<float> weights = new List<float>();
            float totalWeight = 0f;
            
            foreach (var target in targetList)
            {
                float distance = Vector3.Distance(transform.position, target.transform.position);
        
                // 거리가 너무 가까운 경우(예: 0)이면 최소 거리로 설정하여 무한대 가중치를 방지
                float adjustedDistance = Mathf.Max(distance, 0.1f);
        
                // 가중치는 거리의 역수로 설정 (거리가 짧을수록 가중치가 높아짐)
                float weight = 1f / adjustedDistance;
                weights.Add(weight);
                totalWeight += weight;
            }

            // 누적 가중치 리스트 생성
            float randomValue = Random.Range(0, totalWeight);
            float cumulativeWeight = 0f;

            for (int i = 0; i < targetList.Count; i++)
            {
                cumulativeWeight += weights[i];
                if (randomValue <= cumulativeWeight)
                {
                    return targetList[i].playerId;
                }
            }

            // 예외적으로 모든 가중치를 합쳐도 선택되지 않는 경우 마지막 타겟 반환
            return targetList[^1].playerId;
        }

        //선택한 타겟을 서버에 전송
        public void SendTarget(string targetId)
        {
            TcpProtobufClient.Instance.SendMonsterTarget(monsterId, targetId);
        }

        //전송받은 타겟을 받아서 실제로 타겟을 변경
        public void SetTarget(MonsterAction msg)
        {
            if (dummyMode)
            {
                if(SpawnManager.Instance.Dummys.TryGetValue(msg.TargetId, out PlayerController target))
                {
                    currentTarget = target;
                }
            }
            else
            {
                if(SpawnManager.Instance.SpawnedPlayers.TryGetValue(msg.TargetId, out PlayerController target))
                {
                    currentTarget = target;
                }
            }

            //타겟을 설정했을 때 idle상태이면 타겟을 향해 돌아본다.
            if (CurrentState == MonsterState.MonsterStatusIdle && currentTarget)
            {
                RotateTowardsTarget();
            }
        }
        
        //돌아보는 애니메이션을 재생
        private void RotateTowardsTarget()
        {
            if (!currentTarget)
                return;

            // 이미 회전 중이라면 추가로 회전하지 않음
            if (isRotating)
                return;
            
            Vector3 directionToTarget = currentTarget.transform.position - transform.position;
            directionToTarget.y = 0; // 수평 방향만 고려

            if (directionToTarget == Vector3.zero)
            {
                Debug.LogWarning("타겟과 몬스터의 위치가 동일합니다.");
                return;
            }
            
            Vector3 cross = Vector3.Cross(transform.forward, directionToTarget.normalized);
            string turnDirection = cross.y < 0 ? "Left" : "Right";

            StartCoroutine(RotateToTarget(directionToTarget.normalized, turnDirection));
        }
        
        //회전 애니메이션에 맞춰서 타겟을 실제로 바라보는 기능
        private IEnumerator RotateToTarget(Vector3 directionToTarget, string turnDirection)
        {
            isRotating = true;
            
            // 현재 각도 계산
            float angle = Vector3.Angle(transform.forward, directionToTarget);
            
            if (angle <= 30f)
            {
                // 30도 이내이면 애니메이션 없이 0.2초에 걸쳐 부드럽게 회전
                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                Quaternion startRotation = transform.rotation;

                float duration = 0.2f;
                float elapsed = 0f;

                while (elapsed < duration)
                {
                    float t = elapsed / duration;
                    transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                // 정확히 타겟 방향을 바라보도록 설정
                transform.rotation = targetRotation;
            }
            else
            {
                // 30도 초과 시 회전 애니메이션 재생

                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                Quaternion startRotation = transform.rotation;

                // 애니메이션 트리거 설정
                if(SuperManager.Instance.IsHost)
                    SendMonsterAnim(turnDirection == "Left" ? TurnLeft : TurnRight, ParameterType.ParameterTrigger);

                // 애니메이션 클립의 지속 시간에 맞춰 회전 시간 설정
                float turnDuration = (turnDirection == "Left") ? turnLeftAnimationDuration : turnRightAnimationDuration;
                float finalTurnDuration = (turnDirection == "Left") ? turnDuration * 0.25f : turnDuration * 0.6f;

                float elapsedRotation = 0f;

                while (elapsedRotation < finalTurnDuration)
                {
                    float t = Mathf.Clamp01(elapsedRotation / finalTurnDuration);
                    transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
                    elapsedRotation += Time.deltaTime;
                    yield return null;
                }

                // 정확히 타겟 방향을 바라보도록 설정
                transform.rotation = targetRotation;

                // 남은 애니메이션 시간 대기
                yield return new WaitForSeconds(turnDuration - finalTurnDuration);
            }

            isRotating = false;
            
            ReadyToAction?.Invoke();
        }

        //랜덤한 이동 목적지를 찾는 메소드
        public Vector3 FindMoveDestination()
        {
            NavMeshHit navHit;
            for (int i = 0; i < 10; i++)
            {
                Vector3 randomDirection = Random.insideUnitSphere * moveDistance;
                randomDirection += transform.position;
                
                if (NavMesh.SamplePosition(randomDirection, out navHit, moveDistance, NavMesh.AllAreas))
                {
                    return navHit.position;
                }
            }
            Debug.LogWarning("유효한 NavMesh 위치를 찾지 못했음");
            return transform.position;
        }

        //찾은 목적지를 서버에 보내는 메소드 
        public void SendDestination(Vector3 destination)
        {
            if(!SuperManager.Instance.IsHost) return;
            TcpProtobufClient.Instance.SendDestination(monsterId, destination);
        }

        //서버에서 받은 목적지로 실제로 이동하는 메소드
        public void SetDestination(MonsterAction msg)
        {
            Vector3 destination = new Vector3(msg.Destination.X, msg.Destination.Y, msg.Destination.Z);
            agent.SetDestination(destination);
        }
        
        public void UpdateMovementParameters()
        {
            if (!currentTarget || agent.velocity == Vector3.zero)
            {
                // 이동하지 않을 때는 Idle 상태로 설정
                animator.SetFloat(Horizontal, 0, horizontalDamp, Time.deltaTime);
                animator.SetFloat(Vertical, 0, verticalDamp, Time.deltaTime);
                animator.SetFloat(MoveAnimSpeed, 0, speedDamp, Time.deltaTime);
                return;
            }

            // 타겟을 향한 방향
            Vector3 targetForward = (currentTarget.transform.position - transform.position).normalized;
            targetForward.y = 0;

            // 현재 이동 방향
            Vector3 movementDirection = agent.velocity.normalized;
            movementDirection.y = 0;

            // 타겟의 로컬 좌표계 기준으로 이동 방향 계산
            Quaternion targetRotation = Quaternion.LookRotation(targetForward);
            Vector3 localDirection = Quaternion.Inverse(targetRotation) * movementDirection;

            // Horizontal과 Vertical 값 계산 (-1 ~ 1)
            float horizontal = Mathf.Clamp(localDirection.x, -1f, 1f);
            float vertical = Mathf.Clamp(localDirection.z, -1f, 1f);

            // 현재 속도 계산 (0 ~ 1)
            float speed = Mathf.Clamp(agent.velocity.magnitude / agent.speed, 0f, 1f);

            // 애니메이터 파라미터 설정 (부드러운 전환 적용)
            animator.SetFloat(Horizontal, horizontal, horizontalDamp, Time.deltaTime);
            animator.SetFloat(Vertical, vertical, verticalDamp, Time.deltaTime);
            animator.SetFloat(MoveAnimSpeed, speed, speedDamp, Time.deltaTime);
        }
        
    }
}
