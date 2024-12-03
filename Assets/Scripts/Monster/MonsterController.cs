using System;
using System.Collections;
using System.Collections.Generic;
using Game;
using RootMotion.FinalIK;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Monster
{
    public class MonsterController : MonoBehaviour
    {
        public string monsterId;

        protected StateMachine Sm;

        private MonsterState currentState;

        public MonsterState CurrentState
        {
            get => currentState;
            private set
            {
                // Debug.Log($"몬스터 스테이트 변경: {currentState} => {value}");
                currentState = value;
                Sm.ChangeState(value);
            }
        }

        public AttackType currentAttack = AttackType.MonsterAttackUnknown;

        [HideInInspector] public NavMeshAgent agent;
        [HideInInspector] public Animator animator;
        [HideInInspector] public LookAtIK lookAtIK;

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

        public readonly int AttackClose01 = Animator.StringToHash("AttackClose01");
        public readonly int AttackClose02 = Animator.StringToHash("AttackClose02");
        public readonly int AttackCounter = Animator.StringToHash("AttackCounter");

        //회전 관련 변수
        public AnimationClip turnLeftClip;
        public AnimationClip turnRightClip;
        private float turnLeftAnimationDuration;
        private float turnRightAnimationDuration;
        private bool isRotating = false;

        public Action ReadyToAction;

        public float moveDistance = 15.0f; // 랜덤 이동 반경
        public bool moveStart = false; //이동을 시작하면 true, 이동이 끝났을 때 false

        // 이동 모션의 부드러운 전환을 위한 Damp값
        public float horizontalDamp = 0.1f;
        public float verticalDamp = 0.1f;
        public float speedDamp = 0.1f;

        //같은 공격에 여러번 히트하지 않게 하기 위한 인덱스
        public int attackIdx = 0; //-1일 경우 다단히트
        public LayerMask targetLayer;
        public Dictionary<(AttackType attackType, int index), AttackConfig> AttackConfigs = new(); //공격 판정이 담긴 리스트

        private float patternCooldown = 0f; //패턴이 발동된 이후 일정시간동안 다른 패턴이 발동되지 못하게 함
        private const float PatternThreshold = 0.2f; //다른 패턴이 발동되지 못하게 하는 시간

        /// <summary>
        /// 스킬 추가 시 해야하는 것:
        /// InitializeAttackConfigs에 정보 추가
        /// </summary>
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
            lookAtIK = GetComponent<LookAtIK>();

            // AttackConfigs 초기화
            InitializeAttackConfigs();
        }

        private void InitializeAttackConfigs()
        {
            // 예시: MonsterAttackClose01 공격 설정
            AttackConfigs.Add((AttackType.MonsterAttackClose01, 1), new AttackConfig
            {
                DamageAmount = 10f,
                Distance = 2.0f,
                AttackPositionOffset = Vector3.zero,
                ColliderConfig = new SphereColliderConfig { Radius = 1.0f }
            });
            AttackConfigs.Add((AttackType.MonsterAttackClose01, 2), new AttackConfig
            {
                DamageAmount = 10f,
                Distance = 2.0f,
                AttackPositionOffset = Vector3.zero,
                ColliderConfig = new BoxColliderConfig { Size = new Vector3(2.0f, 2.0f, 2.0f), Center = Vector3.zero }
            });
            AttackConfigs.Add((AttackType.MonsterAttackClose01, 3), new AttackConfig
            {
                DamageAmount = 30f,
                Distance = 3.0f,
                AttackPositionOffset = Vector3.zero,
                ColliderConfig = new CapsuleColliderConfig { Height = 3.0f, Radius = 0.5f, Direction = Vector3.up }
            });
            AttackConfigs.Add((AttackType.MonsterAttackClose02, 1), new AttackConfig
            {
                DamageAmount = 20f,
                Distance = 4.0f,
                AttackPositionOffset = Vector3.zero,
                ColliderConfig = new SphereColliderConfig { Radius = 2.0f }
            });
        }

        private void Update()
        {
            Sm.Update();
            //이동 애니메이션 파라미터 업데이트
            UpdateMovementParameters();
            patternCooldown += Time.deltaTime;
        }

        public void SendChangeState(MonsterState state)
        {
            if (!SuperManager.Instance.IsHost) return;
            TcpProtobufClient.Instance.SendMonsterChangeState(monsterId, state, AttackType.MonsterAttackUnknown);
        }

        public void SendChangeState(MonsterState state, AttackType attackType)
        {
            if (!SuperManager.Instance.IsHost) return;
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
        public void SendMonsterAnim(int hash, ParameterType type, int intValue = 0, float floatValue = 0,
            bool boolValue = false)
        {
            if (!SuperManager.Instance.IsHost) return;
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
                if (SpawnManager.Instance.Dummys.TryGetValue(msg.TargetId, out PlayerController target))
                {
                    currentTarget = target;
                    lookAtIK.solver.target = target.transform;
                }
            }
            else
            {
                if (SpawnManager.Instance.SpawnedPlayers.TryGetValue(msg.TargetId, out PlayerController target))
                {
                    currentTarget = target;
                    lookAtIK.solver.target = target.transform;
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
                if (SuperManager.Instance.IsHost)
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
                randomDirection += currentTarget ? currentTarget.transform.position : transform.position;

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
            if (!SuperManager.Instance.IsHost) return;
            TcpProtobufClient.Instance.SendDestination(monsterId, destination);
        }

        //서버에서 받은 목적지로 실제로 이동하는 메소드
        public void SetDestination(MonsterAction msg)
        {
            Vector3 destination = new Vector3(msg.Destination.X, msg.Destination.Y, msg.Destination.Z);
            agent.SetDestination(destination);
            moveStart = true;
        }

        private void UpdateMovementParameters()
        {
            if (!currentTarget || agent.velocity == Vector3.zero)
            {
                // 이동하지 않을 때는 Idle 상태로 설정
                animator.SetFloat(Horizontal, 0, horizontalDamp, Time.deltaTime);
                animator.SetFloat(Vertical, 0, verticalDamp, Time.deltaTime);
                animator.SetFloat(MoveAnimSpeed, 0.5f, speedDamp, Time.deltaTime);
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

            //타겟 방향을 쳐다봄(필요하다면 targetForward의 y값을 0으로 해서 targetRotation을 다시 계산해야 함.)
            transform.rotation = targetRotation;

            // Horizontal과 Vertical 값 계산 (-1 ~ 1)
            float horizontal = Mathf.Clamp(localDirection.x, -1f, 1f);
            float vertical = Mathf.Clamp(localDirection.z, -1f, 1f);

            // 현재 속도 계산 (0 ~ 1)
            float speed = Mathf.Clamp(agent.velocity.magnitude / agent.speed, 0.5f, 1f);


            // 애니메이터 파라미터 설정 (부드러운 전환 적용)
            animator.SetFloat(Horizontal, horizontal, horizontalDamp, Time.deltaTime);
            animator.SetFloat(Vertical, vertical, verticalDamp, Time.deltaTime);
            animator.SetFloat(MoveAnimSpeed, speed, speedDamp, Time.deltaTime);
        }

        #region 공격, 패턴 관련

        //타겟과의 거리에 따라서 패턴을 선택한다. 호스트만 사용함.
        public void ChoicePattern()
        {
            if (!SuperManager.Instance.IsHost || patternCooldown < PatternThreshold) return;
            patternCooldown = 0;

            Debug.Log("패턴고를래");
            if (!currentTarget)
            {
                //타겟이 없으면 그냥 걸어다님
                SendChangeState(MonsterState.MonsterStatusMove);
            }
            else
            {
                //타겟이 있으면 타겟과의 거리를 측정
                float distance = Vector3.Distance(transform.position, currentTarget.transform.position);
                // Debug.Log($"타겟과의 거리: {distance}");
                // SendChangeState(MonsterState.MonsterStatusMove);

                //3이하가 근접패턴
                //3~5 근거리
                //5~10 중거리
                //10~20 장거리
                //20이상: 초장거리

                List<(string, AttackType, int)> patternList = new();
                if (distance < 3.0f)
                {
                    patternList.Add(("Move", AttackType.MonsterAttackUnknown, 10));
                    // patternList.Add(("Attack", AttackType.MonsterAttackCloseCounter, 20));
                    patternList.Add(("Attack", AttackType.MonsterAttackClose01, 50));
                    // patternList.Add(("Attack", AttackType.MonsterAttackClose02, 50));
                }
                else if (distance < 5.0f)
                {
                    //전진성 있는 근접패턴
                    //옆으로 꽤 멀리 점프한 뒤 타겟을 향해 일섬
                    patternList.Add(("Move", AttackType.MonsterAttackUnknown, 10));
                }
                else if (distance < 10.0f)
                {
                    //중거리 패턴
                    patternList.Add(("Move", AttackType.MonsterAttackUnknown, 10));
                }
                else if (distance < 20.0f)
                {
                    //장거리 패턴
                    patternList.Add(("Move", AttackType.MonsterAttackUnknown, 10));
                }
                else
                {
                    //초장거리 패턴
                    //사라진 뒤 잠시 후에 타겟 옆에서 나타나서 공격
                    patternList.Add(("Move", AttackType.MonsterAttackUnknown, 10));
                }

                // 가중치 기반 패턴 선택
                var selectedPattern = SelectWeightedRandomPattern(patternList);

                if (selectedPattern.HasValue)
                {
                    switch (selectedPattern.Value.Category)
                    {
                        case "Move":
                            Debug.Log("이동할래");
                            SendChangeState(MonsterState.MonsterStatusMove);
                            break;
                        case "Attack":
                            Debug.Log("공격할래");
                            switch (selectedPattern.Value.attackType)
                            {
                                case AttackType.MonsterAttackClose01:
                                    SendChangeState(MonsterState.MonsterStatusAttack, AttackType.MonsterAttackClose01);
                                    break;
                                case AttackType.MonsterAttackClose02:
                                    SendChangeState(MonsterState.MonsterStatusAttack, AttackType.MonsterAttackClose02);
                                    break;
                                case AttackType.MonsterAttackCloseCounter:
                                    SendChangeState(MonsterState.MonsterStatusAttack,
                                        AttackType.MonsterAttackCloseCounter);
                                    break;
                                default:
                                    Debug.LogError($"정의되지 않은 패턴: {selectedPattern.Value.attackType}");
                                    break;
                            }

                            break;
                        default:
                            Debug.LogError($"정의되지 않은 카테고리: {selectedPattern.Value.Category}");
                            break;
                    }
                }
                else
                {
                    Debug.LogWarning("선택된 패턴이 없습니다.");
                }
            }
        }

        //가중치를 기반으로 패턴을 선택하는 메서드
        private (string Category, AttackType attackType)? SelectWeightedRandomPattern(
            List<(string Category, AttackType attackType, int Weight)> patternList)
        {
            if (patternList == null || patternList.Count == 0)
            {
                Debug.LogWarning("패턴 리스트가 비어있습니다.");
                return null;
            }

            // 전체 가중치 합계 계산
            int totalWeight = 0;
            foreach (var pattern in patternList)
            {
                totalWeight += pattern.Weight;
            }

            if (totalWeight <= 0)
            {
                Debug.LogWarning("전체 가중치가 0 이하입니다.");
                return null;
            }

            // 0부터 전체 가중치 사이의 랜덤 값 생성
            int randomValue = Random.Range(0, totalWeight);
            int cumulativeWeight = 0;

            // 가중치 누적 합을 통해 패턴 선택
            foreach (var pattern in patternList)
            {
                cumulativeWeight += pattern.Weight;
                if (randomValue < cumulativeWeight)
                {
                    return (pattern.Category, pattern.attackType);
                }
            }

            // 예외적으로 모든 가중치를 합쳐도 선택되지 않는 경우 마지막 패턴 반환
            Debug.LogWarning("패턴 선택 과정에서 예외가 발생하여 마지막 패턴을 선택합니다.");
            var lastPattern = patternList[^1];
            return (lastPattern.Category, lastPattern.attackType);
        }

        public void AttackEnd()
        {
            currentAttack = AttackType.MonsterAttackUnknown;
            attackIdx = 0;
            if (SuperManager.Instance.IsHost) SendChangeState(MonsterState.MonsterStatusIdle);
        }

        //어택 인덱스를 설정(애니메이션에서 함)
        public void SetAttackIdx(int i)
        {
            attackIdx = i;
        }

        //애니메이션의 히트판정에서 이벤트로 호출
        public void HitCheck()
        {
            if (!AttackConfigs.TryGetValue((currentAttack, attackIdx), out AttackConfig config))
            {
                Debug.LogError($"InitializeAttackConfigs에서 공격이 정의되지 않음: {currentAttack}, AttackIdx: {attackIdx}");
                return;
            }

            //데미지를 설정
            float damageAmount = config.DamageAmount;
            float distance = config.Distance;
            Vector3 attackPositionOffset = config.AttackPositionOffset;

            // 공격 위치 계산
            Vector3 attackPosition = transform.position + transform.forward * distance +
                                     transform.TransformDirection(attackPositionOffset);


            // 히트 판정 수행
            Collider[] hitColliders = Array.Empty<Collider>();

            switch (config.ColliderConfig.ColliderType)
            {
                case ColliderType.Sphere:
                    if (config.ColliderConfig is SphereColliderConfig sphereConfig)
                    {
                        hitColliders = Physics.OverlapSphere(attackPosition, sphereConfig.Radius, targetLayer);
                    }
                    else
                    {
                        Debug.LogWarning("SphereCollider 설정이 올바르지 않습니다.");
                    }

                    break;
                case ColliderType.Box:
                    if (config.ColliderConfig is BoxColliderConfig boxConfig)
                    {
                        Vector3 boxCenterWorld = attackPosition + transform.TransformDirection(boxConfig.Center);
                        hitColliders = Physics.OverlapBox(boxCenterWorld, boxConfig.Size * 0.5f, transform.rotation,
                            targetLayer);
                    }
                    else
                    {
                        Debug.LogWarning("BoxCollider 설정이 올바르지 않습니다.");
                    }

                    break;

                case ColliderType.Capsule:
                    if (config.ColliderConfig is CapsuleColliderConfig capsuleConfig)
                    {
                        Vector3 point1 = attackPosition +
                                         capsuleConfig.Direction * (capsuleConfig.Height / 2 - capsuleConfig.Radius);
                        Vector3 point2 = attackPosition -
                                         capsuleConfig.Direction * (capsuleConfig.Height / 2 - capsuleConfig.Radius);
                        hitColliders = Physics.OverlapCapsule(point1, point2, capsuleConfig.Radius, targetLayer);
                    }
                    else
                    {
                        Debug.LogWarning("CapsuleCollider 설정이 올바르지 않습니다.");
                    }

                    break;

                default:
                    Debug.LogWarning($"Unknown ColliderType: {config.ColliderConfig.ColliderType}.");
                    break;
            }

            Debug.Log(
                $"HitCheck - AttackType: {currentAttack}, AttackIdx: {attackIdx}, Distance: {distance}, ColliderType: {config.ColliderConfig.ColliderType}, Hits: {hitColliders.Length}");
            foreach (var hitCollider in hitColliders)
            {
                PlayerController player = hitCollider.GetComponent<PlayerController>();
                if (player)
                {
                    // 데미지 적용
                    player.TakeDamage(damageAmount, currentAttack, attackIdx);
                    Debug.Log($"Hit: {player.playerId}에게 {damageAmount} 데미지 적용");
                }
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!AttackConfigs.TryGetValue((currentAttack, attackIdx), out AttackConfig config))
            {
                // 기본 공격 설정 (필요 시 수정)
                return;
            }

            // 공격 위치 계산 (로컬 좌표계 적용)
            Vector3 attackPosition = transform.position + transform.forward * config.Distance +
                                     transform.TransformDirection(config.AttackPositionOffset);

            Gizmos.color = Color.red;

            // ColliderType에 따라 시각화
            switch (config.ColliderConfig.ColliderType)
            {
                case ColliderType.Sphere:
                    if (config.ColliderConfig is SphereColliderConfig sphereConfig)
                    {
                        Gizmos.DrawWireSphere(attackPosition, sphereConfig.Radius);
                    }

                    break;

                case ColliderType.Box:
                    if (config.ColliderConfig is BoxColliderConfig boxConfig)
                    {
                        Vector3 boxCenterWorld = attackPosition + transform.TransformDirection(boxConfig.Center);
                        Gizmos.DrawWireCube(boxCenterWorld, boxConfig.Size);
                    }

                    break;

                case ColliderType.Capsule:
                    if (config.ColliderConfig is CapsuleColliderConfig capsuleConfig)
                    {
                        Vector3 point1 = attackPosition +
                                         capsuleConfig.Direction * (capsuleConfig.Height / 2 - capsuleConfig.Radius);
                        Vector3 point2 = attackPosition -
                                         capsuleConfig.Direction * (capsuleConfig.Height / 2 - capsuleConfig.Radius);
                        Gizmos.DrawWireSphere(point1, capsuleConfig.Radius);
                        Gizmos.DrawWireSphere(point2, capsuleConfig.Radius);
                        Gizmos.DrawLine(point1, point2);
                    }

                    break;

                default:
                    // 기본적으로 Sphere로 시각화
                    Gizmos.DrawWireSphere(attackPosition, config.Distance);
                    break;
            }

            // 공격 방향 표시 (선택 사항)
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, attackPosition);
        }
#endif

        #endregion
    }
}