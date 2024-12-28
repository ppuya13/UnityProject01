using System;
using System.Collections;
using System.Collections.Generic;
using Character;
using Game;
using RootMotion.FinalIK;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace Monster
{
    public class MonsterController : SerializedMonoBehaviour
    {
        public string monsterId;

        public float maxHp = 1000;
        private float currentHp = 1000;

        public float CurrentHp
        {
            get => currentHp;
            set
            {
                currentHp = value;
                //UI 변경하기
            }
        }

        public Transform lookAtThis;
        protected StateMachine Sm;

        public MonsterState currentState;

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
        public bool scarecrowMode = false; //true로 바꿀 경우 움직이지 않는다.

        public List<PlayerController> targetList = new();
        public PlayerController currentTarget;

        [HideInInspector] public readonly int Spawn = Animator.StringToHash("Spawn");
        [HideInInspector] public readonly int Horizontal = Animator.StringToHash("Horizontal");
        [HideInInspector] public readonly int Vertical = Animator.StringToHash("Vertical");
        [HideInInspector] public readonly int Dash = Animator.StringToHash("Dash");
        [HideInInspector] public readonly int Dodge = Animator.StringToHash("Dodge");
        [HideInInspector] public readonly int LR = Animator.StringToHash("LR");
        [HideInInspector] public readonly int TurnLeft = Animator.StringToHash("TurnLeft");
        [HideInInspector] public readonly int TurnRight = Animator.StringToHash("TurnRight");
        [HideInInspector] public readonly int MoveAnimSpeed = Animator.StringToHash("MoveAnimSpeed");

        [HideInInspector] public readonly int AttackClose01 = Animator.StringToHash("AttackClose01");
        [HideInInspector] public readonly int AttackClose02 = Animator.StringToHash("AttackClose02");
        [HideInInspector] public readonly int AttackClose03 = Animator.StringToHash("AttackClose03");
        [HideInInspector] public readonly int AttackCounter = Animator.StringToHash("AttackCounter");

        [HideInInspector] public readonly int AttackShortRange01 = Animator.StringToHash("AttackShortRange01");

        //회전 관련 변수
        public AnimationClip turnLeftClip;
        public AnimationClip turnRightClip;
        private float turnLeftAnimationDuration;
        private float turnRightAnimationDuration;
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

        [SerializeField]
        public Dictionary<(AttackType attackType, int index), AttackConfig> AttackConfigs = new(); //공격 판정이 담긴 리스트

        private Dictionary<AttackType, bool> attackCooldown = new();

        private float patternCooldown = 0f; //패턴이 발동된 이후 일정시간동안 다른 패턴이 발동되지 못하게 함
        private const float PatternThreshold = 0.2f; //다른 패턴이 발동되지 못하게 하는 시간

        private Coroutine moveCoroutine;
        private Coroutine rotateCoroutine;
        private bool isRotating = false;

        private const float TakeDamageThreshold = 0.2f; // 같은 공격에 다시 맞지 않는 시간
        private Dictionary<(PlayerAttackConfig, Transform), bool> hitDict = new();
        private bool isCounter = false;

        ///
        /// 이동 중에도 일정시간? 거리에 따라? 스킬이 발동하도록 변경하는 게 좋을 듯.
        /// 
        /// <summary>
        /// 스킬 추가 시 해야하는 것:
        ///
        /// 애니메이터 등록:
        /// 1.애니메이션 해시 변수를 등록하고 애니메이터에 파라미터를 등록
        /// 2.스킬에 사용할 서브스테이트머신을 애니메이터에 생성 후 애니메이션 클립 등록
        /// 3.Move에 트랜지션을 연결
        ///
        /// 변수 생성:
        /// 1.proto파일 enum AttackType에 해당 공격 추가
        /// 
        /// 애니메이션 조정:
        /// 1.공격의 시작에 SetAttackIdx 할당 후 해당 공격의 idx를 설정(1~)
        /// 2.공격 전 이동 시작 부분에 MoveStart 할당
        /// 3.공격 전 이동 끝 부분에 MoveStop 할당
        /// 4.회전도 동일하게 RotateStart, RotateStop 할당
        /// 5.파티클 생성 부분에 CreateAttackParticle 할당
        /// 6.타격판정 부분에 HitCheck 할당
        /// 7.콤보 공격의 마지막 애니메이션의 마지막에 AttackEnd 할당
        ///
        /// 패턴 등록:
        /// 1.AttackConfig 생성
        /// 2.몬스터의 인스펙터에 생성한 AttackConfig 등록
        /// 3.ChoicePattern() 메소드에 공격 패턴과 가중치 등록
        /// 4.ChoicePattern() 메소드의 SendChangeState부분도 정의
        /// 5.MonsterStateAttack의 EnterState에 애니메이션 트리거 정의
        /// 
        /// </summary>
        ///
        /// 
        private void Awake()
        {
            //상태머신 초기화
            Sm = new StateMachine(this);
            // CurrentState = MonsterState.MonsterStatusSpawn;

            //NavMesh 초기화
            agent = GetComponent<NavMeshAgent>();
            agent.speed = 1.52f;
            agent.updateRotation = false;

            //애니메이터 초기화
            animator = GetComponent<Animator>();

            //기타 스테이터스 초기화
            ReadyToAction += ChoicePattern;
            turnLeftAnimationDuration = turnLeftClip ? turnLeftClip.length : 1.0f;
            turnRightAnimationDuration = turnRightClip ? turnRightClip.length : 1.0f;
            lookAtIK = GetComponent<LookAtIK>();

            //플레이어 캐릭터들의 시선을 가져옴
            foreach (var players in SpawnManager.Instance.SpawnedPlayers.Values)
            {
                Debug.Log("시선설정");
                players.lookAtIK.solver.target = lookAtThis;
            }
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
            if (!SuperManager.Instance.isHost) return;
            TcpProtobufClient.Instance.SendMonsterChangeState(monsterId, state, AttackType.MonsterAttackUnknown);
        }

        public void SendChangeState(MonsterState state, AttackType attackType)
        {
            if (!SuperManager.Instance.isHost) return;
            TcpProtobufClient.Instance.SendMonsterChangeState(monsterId, state, attackType);
        }

        public void SendChangeState(MonsterState state, float dodgeOption)
        {
            Debug.Log("닷지발신");
            if (!SuperManager.Instance.isHost) return;
            TcpProtobufClient.Instance.SendMonsterChangeState(monsterId, state, AttackType.MonsterAttackUnknown,
                dodgeOption);
        }

        public void ChangeState(MonsterAction msg)
        {
            if (msg.MonsterState is MonsterState.MonsterStatusAttack)
            {
                //어택스테이트일 경우 어떤 공격인지도 받아온다.
                currentAttack = msg.AttackType;
            }
            else if (msg.MonsterState is MonsterState.MonsterStatusDodge)
            {
                //닷지일경우 방향을 받아온다.
                animator.SetFloat(LR, msg.DodgeOption);
                // Debug.Log($"닷지수신, {msg.DodgeOption}");
            }

            CurrentState = msg.MonsterState;
        }

        //서버에 애니메이터 파라미터값을 변경하기 위해 보내는 값
        public void SendMonsterAnim(int hash, ParameterType type, int intValue = 0, float floatValue = 0,
            bool boolValue = false)
        {
            if (!SuperManager.Instance.isHost) return;
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
                if (SuperManager.Instance.isHost)
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

            if (!scarecrowMode)
            {
                ReadyToAction?.Invoke();
            }
        }

        //랜덤한 이동 목적지를 찾는 메소드
        public Vector3 FindMoveDestination()
        {
            NavMeshHit navHit;
            for (int i = 0; i < 20; i++)
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
            if (!SuperManager.Instance.isHost) return;
            TcpProtobufClient.Instance.SendDestination(monsterId, destination);
        }

        //서버에서 받은 목적지로 실제로 이동하는 메소드
        public void SetDestination(MonsterAction msg)
        {
            Vector3 destination = TcpProtobufClient.Instance.ConvertToVector3(msg.Destination);
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

            //타겟을 향해 정확히 회전하는건 idle이나 move상태일 때만
            if (currentState is not (MonsterState.MonsterStatusIdle or MonsterState.MonsterStatusMove)) return;

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
            Debug.Log("패턴고를래");
            if (!SuperManager.Instance.isHost || patternCooldown < PatternThreshold) return;
            patternCooldown = 0;

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
                bool v; //TryGetValue를 사용하기 위한 임시 변수. 실제로 사용되진 않음.
                if (distance < 3.0f) //타겟과의 거리에 따라 패턴리스트에 패턴을 추가한다.
                {
                    // if (!attackCooldown.TryGetValue(AttackType.MonsterAttackCloseCounter, out v))
                    //     patternList.Add(("Attack", AttackType.MonsterAttackCloseCounter, 20));
                    // if (!attackCooldown.TryGetValue(AttackType.MonsterAttackClose01, out v))
                    //     patternList.Add(("Attack", AttackType.MonsterAttackClose01, 50));
                    // if (!attackCooldown.TryGetValue(AttackType.MonsterAttackClose02, out v))
                    //     patternList.Add(("Attack", AttackType.MonsterAttackClose02, 50));
                    // if (!attackCooldown.TryGetValue(AttackType.MonsterAttackClose03, out v))
                    //     patternList.Add(("Attack", AttackType.MonsterAttackClose03, 50));

                    patternList.Add(("Move", AttackType.MonsterAttackUnknown, 10));
                    // patternList.Add(("DodgeBack", AttackType.MonsterAttackUnknown, 14));
                    // patternList.Add(("DodgeLeft", AttackType.MonsterAttackUnknown, 7));
                    // patternList.Add(("DodgeRight", AttackType.MonsterAttackUnknown, 7));
                }
                else if (distance < 5.0f)
                {
                    //전진성 있는 근접패턴
                    //옆으로 꽤 멀리 점프한 뒤 타겟을 향해 일섬
                    // if (!attackCooldown.TryGetValue(AttackType.MonsterAttackCloseCounter, out v))
                    //     patternList.Add(("Attack", AttackType.MonsterAttackCloseCounter, 20));
                    // if (!attackCooldown.TryGetValue(AttackType.MonsterAttackShortrange01, out v))
                    //     patternList.Add(("Attack", AttackType.MonsterAttackShortrange01, 20));
                    
                    patternList.Add(("Move", AttackType.MonsterAttackUnknown, 10));
                    // patternList.Add(("DodgeBack", AttackType.MonsterAttackUnknown, 14));
                    // patternList.Add(("DodgeLeft", AttackType.MonsterAttackUnknown, 7));
                    // patternList.Add(("DodgeRight", AttackType.MonsterAttackUnknown, 7));
                }
                else if (distance < 10.0f)
                {
                    //중거리 패턴
                    // if (!attackCooldown.TryGetValue(AttackType.MonsterAttackShortrange01, out v))
                    //     patternList.Add(("Attack", AttackType.MonsterAttackShortrange01, 20));
                    
                    patternList.Add(("Move", AttackType.MonsterAttackUnknown, 10));
                    // patternList.Add(("DodgeLeft", AttackType.MonsterAttackUnknown, 7));
                    // patternList.Add(("DodgeRight", AttackType.MonsterAttackUnknown, 7));
                }
                else if (distance < 20.0f)
                {
                    //장거리 패턴
                    // if (!attackCooldown.TryGetValue(AttackType.MonsterAttackShortrange01, out v))
                    //     patternList.Add(("Attack", AttackType.MonsterAttackShortrange01, 20));
                    
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
                            StartCoroutine(CooldownTimer(selectedPattern.Value.attackType,
                                AttackConfigs[(selectedPattern.Value.attackType, 1)].cooldown)); //쿨타임 돌림
                            SendChangeState(MonsterState.MonsterStatusAttack, selectedPattern.Value.attackType);

                            // switch (selectedPattern.Value.attackType)
                            // {
                            //     case AttackType.MonsterAttackClose01:
                            //         SendChangeState(MonsterState.MonsterStatusAttack, AttackType.MonsterAttackClose01);
                            //         break;
                            //     case AttackType.MonsterAttackClose02:
                            //         SendChangeState(MonsterState.MonsterStatusAttack, AttackType.MonsterAttackClose02);
                            //         break;
                            //     case AttackType.MonsterAttackClose03:
                            //         SendChangeState(MonsterState.MonsterStatusAttack, AttackType.MonsterAttackClose03);
                            //         break;
                            //     case AttackType.MonsterAttackCloseCounter:
                            //         SendChangeState(MonsterState.MonsterStatusAttack, AttackType.MonsterAttackCloseCounter);
                            //         break;
                            //     default:
                            //         Debug.LogError($"정의되지 않은 패턴: {selectedPattern.Value.attackType}");
                            //         break;
                            // }
                            break;
                        case "DodgeBack":
                            // Debug.Log("구를래");
                            SendChangeState(MonsterState.MonsterStatusDodge, 0f);
                            break;
                        case "DodgeLeft":
                            // Debug.Log("구를래");
                            SendChangeState(MonsterState.MonsterStatusDodge, -1);
                            break;
                        case "DodgeRight":
                            // Debug.Log("구를래");
                            SendChangeState(MonsterState.MonsterStatusDodge, 1);
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

        //공격 쿨타임을 돌리는 코루틴
        IEnumerator CooldownTimer(AttackType attackType, float time)
        {
            attackCooldown.Add(attackType, true);
            yield return new WaitForSeconds(time);
            attackCooldown.Remove(attackType);
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
            Debug.Log("어택엔드");
            currentAttack = AttackType.MonsterAttackUnknown;
            attackIdx = 0;
            if (SuperManager.Instance.isHost) SendChangeState(MonsterState.MonsterStatusIdle);
        }

        //어택 인덱스를 설정(애니메이션에서 함)
        public void SetAttackIdx(int i)
        {
            attackIdx = i;
        }

        public void CreateProjectile()
        {
            if (!AttackConfigs.TryGetValue((currentAttack, attackIdx), out AttackConfig attackConfig))
            {
                Debug.LogError($"InitializeAttackConfigs에서 공격이 정의되지 않음: {currentAttack}, AttackIdx: {attackIdx}");
                return;
            }

            if (attackConfig.RangeType is RangeAttack rangeAttack)
            {
                foreach (Projectile projectile in rangeAttack.Projectiles)
                {
                    GameObject go = Instantiate(projectile.Particle, transform.position + projectile.Position,
                        projectile.Rotation);
                    DamageField df = go.AddComponent<DamageField>();
                    df.SetDamageField(currentAttack, attackIdx, attackConfig, projectile);
                }
            }
            else
            {
                Debug.LogError(
                    $"공격이 RangeAttack이 아닌데 CreateProjectile가 불렸음!!! ({currentAttack}, {attackIdx}, {attackConfig.RangeType})");
            }
        }

        //애니메이션의 히트판정에서 이벤트로 호출
        public void HitCheck()
        {
            if (!AttackConfigs.TryGetValue((currentAttack, attackIdx), out AttackConfig attackConfig))
            {
                Debug.LogError($"InitializeAttackConfigs에서 공격이 정의되지 않음: {currentAttack}, AttackIdx: {attackIdx}");
                return;
            }


            if (attackConfig.RangeType is MeleeAttack config)
            {
                if (config.ColliderConfigs == null || config.ColliderConfigs.Length == 0)
                {
                    Debug.LogError($"Inspector에서 공격의 Collider가 정의되지 않음: {currentAttack}, AttackIdx: {attackIdx}");
                    return;
                }

                // 공격 위치 계산
                Vector3 basePosition = transform.position + transform.forward * config.Distance +
                                       transform.TransformDirection(config.AttackPositionOffset);

                List<Collider> allHitColliders = new List<Collider>();

                foreach (var colliderConfig in config.ColliderConfigs)
                {
                    Collider[] hitColliders = Array.Empty<Collider>();

                    switch (colliderConfig.ColliderType)
                    {
                        case ColliderType.Sphere:
                            if (colliderConfig is SphereColliderConfig sphereConfig)
                            {
                                hitColliders = Physics.OverlapSphere(basePosition, sphereConfig.Radius, targetLayer);
                            }

                            break;

                        case ColliderType.Box:
                            if (colliderConfig is BoxColliderConfig boxConfig)
                            {
                                Vector3 boxCenterWorld = basePosition + transform.TransformDirection(boxConfig.Center);
                                Quaternion worldRotation = transform.rotation * boxConfig.Rotation;
                                hitColliders = Physics.OverlapBox(boxCenterWorld, boxConfig.Size * 0.5f, worldRotation,
                                    targetLayer);
                            }

                            break;

                        case ColliderType.Capsule:
                            if (colliderConfig is CapsuleColliderConfig capsuleConfig)
                            {
                                Vector3 point1 = basePosition + capsuleConfig.Direction *
                                    (capsuleConfig.Height / 2 - capsuleConfig.Radius);
                                Vector3 point2 = basePosition - capsuleConfig.Direction *
                                    (capsuleConfig.Height / 2 - capsuleConfig.Radius);
                                hitColliders =
                                    Physics.OverlapCapsule(point1, point2, capsuleConfig.Radius, targetLayer);
                            }

                            break;

                        default:
                            Debug.LogWarning($"Unknown ColliderType: {colliderConfig.ColliderType}.");
                            break;
                    }

                    allHitColliders.AddRange(hitColliders);
                }

                foreach (var hitCollider in allHitColliders)
                {
                    PlayerController player = hitCollider.GetComponent<PlayerController>();
                    if (player)
                    {
                        // 데미지 적용
                        player.AttackValidation(attackConfig, currentAttack, attackIdx, transform);
                    }
                }
            }
            else
            {
                Debug.LogError(
                    $"공격이 MeleeAttack이 아닌데 HitCheck가 불렸음!!! ({currentAttack}, {attackIdx}, {attackConfig.RangeType})");
            }
        }

        public void HitCheckIdx(int idx)
        {
            if (!AttackConfigs.TryGetValue((currentAttack, attackIdx), out AttackConfig attackConfig))
            {
                Debug.LogError($"InitializeAttackConfigs에서 공격이 정의되지 않음: {currentAttack}, AttackIdx: {attackIdx}");
                return;
            }

            if (attackConfig.RangeType is MeleeAttack config)
            {
                if (idx < 0 || idx >= config.ColliderConfigs.Length)
                {
                    Debug.LogError($"HitCheck 호출 시 유효하지 않은 인덱스: {idx}. ColliderConfigs 배열의 범위를 벗어났음.");
                    return;
                }

                var colliderConfig = config.ColliderConfigs[idx];

                // 공격 위치 계산
                Vector3 basePosition = transform.position + transform.forward * config.Distance +
                                       transform.TransformDirection(config.AttackPositionOffset);

                List<Collider> hitColliders = new List<Collider>();

                // 콜라이더 타입에 따른 처리
                switch (colliderConfig.ColliderType)
                {
                    case ColliderType.Sphere:
                        if (colliderConfig is SphereColliderConfig sphereConfig)
                        {
                            hitColliders.AddRange(Physics.OverlapSphere(basePosition, sphereConfig.Radius,
                                targetLayer));
                        }

                        break;

                    case ColliderType.Box:
                        if (colliderConfig is BoxColliderConfig boxConfig)
                        {
                            Vector3 boxCenterWorld = basePosition + transform.TransformDirection(boxConfig.Center);
                            Quaternion worldRotation = transform.rotation * boxConfig.Rotation;
                            hitColliders.AddRange(Physics.OverlapBox(boxCenterWorld, boxConfig.Size * 0.5f,
                                worldRotation,
                                targetLayer));
                        }

                        break;

                    case ColliderType.Capsule:
                        if (colliderConfig is CapsuleColliderConfig capsuleConfig)
                        {
                            Vector3 point1 = basePosition +
                                             capsuleConfig.Direction *
                                             (capsuleConfig.Height / 2 - capsuleConfig.Radius);
                            Vector3 point2 = basePosition -
                                             capsuleConfig.Direction *
                                             (capsuleConfig.Height / 2 - capsuleConfig.Radius);
                            hitColliders.AddRange(Physics.OverlapCapsule(point1, point2, capsuleConfig.Radius,
                                targetLayer));
                        }

                        break;

                    default:
                        Debug.LogWarning($"알 수 없는 ColliderType: {colliderConfig.ColliderType}");
                        break;
                }

                // 피격된 플레이어 처리
                foreach (var hitCollider in hitColliders)
                {
                    PlayerController player = hitCollider.GetComponent<PlayerController>();
                    if (player)
                    {
                        player.AttackValidation(attackConfig, currentAttack, attackIdx, transform);
                    }
                }
            }
            else
            {
                Debug.LogError(
                    $"공격이 MeleeAttack이 아닌데 HitCheck가 불렸음!!! ({currentAttack}, {attackIdx}, {attackConfig.RangeType})");
            }
        }

        //HitCheck처럼 현재 공격에 따라서 해당하는 AttackConfig의 이펙트 파티클을 소환한다. 애니메이션 이벤트로 호출.
        public void CreateAttackParticle(int idx)
        {
            // 현재 공격 타입과 인덱스에 해당하는 AttackConfig를 가져옵니다.
            if (!AttackConfigs.TryGetValue((currentAttack, attackIdx), out AttackConfig config))
            {
                Debug.LogError($"InitializeAttackConfigs에서 공격이 정의되지 않음: {currentAttack}, AttackIdx: {attackIdx}");
                return;
            }

            // 파티클 이펙트가 설정되어 있는지 확인합니다.
            if (config.EffectConfigs != null && config.EffectConfigs.Length > idx)
            {
                EffectConfig effect = config.EffectConfigs[idx];
                if (config.EffectConfigs[idx].ParticleEffect)
                {
                    // // 공격 위치 계산 (HitCheck와 유사하게)
                    // Vector3 attackPosition = transform.position + transform.forward * config.distance +
                    //                          transform.TransformDirection(config.attackPositionOffset);

                    // 이펙트의 위치, 회전, 크기를 설정합니다.
                    Vector3 effectPosition = transform.position + transform.TransformDirection(effect.EffectPosition);
                    Quaternion effectRotation = transform.rotation * effect.EffectRotation;
                    Vector3 effectScale = effect.EffectScale != Vector3.zero ? effect.EffectScale : Vector3.one;

                    // 파티클 이펙트를 인스턴스화합니다.
                    GameObject particle = Instantiate(effect.ParticleEffect, effectPosition, effectRotation, transform);
                    particle.transform.localScale = effectScale;

                    // 이펙트가 일정 시간 후에 자동으로 파괴되도록 설정 (선택 사항)
                    ParticleSystem ps = particle.GetComponent<ParticleSystem>();
                    if (ps)
                    {
                        Destroy(particle, ps.main.duration + ps.main.startLifetime.constantMax);
                    }
                    else
                    {
                        // ParticleSystem이 없을 경우 기본적으로 5초 후 파괴
                        Destroy(particle, 5f);
                    }
                }
                else
                {
                    Debug.LogWarning($"AttackConfig에 파티클 이펙트가 설정되어 있지 않습니다: {currentAttack}, AttackIdx: {attackIdx}");
                }
            }
            else
            {
                Debug.LogWarning("AttackConfig의 EffectConfigs가 null이거나 idx가 배열을 초과합니다.");
            }

            // 사운드 이펙트가 설정되어 있는지 확인하고 재생합니다.
            if (config.soundEffects is { Length: > 0 })
            {
                // AudioSource가 존재하는지 확인하고 없으면 추가합니다.
                AudioSource audioSource = GetComponent<AudioSource>();
                if (!audioSource)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }

                // 사운드 클립을 랜덤으로 선택하여 재생합니다.
                AudioClip selectedClip = config.soundEffects[Random.Range(0, config.soundEffects.Length)];
                audioSource.PlayOneShot(selectedClip);
            }
        }

        //공격 중에 이동을 하는 메소드, 애니메이션을 재생하지 않으며, AnimationEvent로 호출된다.
        public void MoveStart()
        {
            if (moveCoroutine != null)
            {
                StopCoroutine(moveCoroutine);
                moveCoroutine = null;
            }

            if (AttackConfigs.TryGetValue((currentAttack, attackIdx), out AttackConfig config))
            {
                moveCoroutine = StartCoroutine(AttackMove(config));
            }
            else
            {
                Debug.LogError($"AttackConfig을 찾을 수 없습니다: {currentAttack}, {attackIdx}");
            }
        }

        public void MoveStop()
        {
            if (moveCoroutine != null)
            {
                StopCoroutine(moveCoroutine);
                moveCoroutine = null;
            }
        }

        IEnumerator AttackMove(AttackConfig config)
        {
            Vector3 startPosition = transform.position;
            Vector3 direction;

            if (config.moveDirection == Vector3.zero && currentTarget)
            {
                direction = (currentTarget.transform.position - startPosition).normalized;
            }
            else
            {
                direction = transform.TransformDirection(config.moveDirection.normalized);
            }

            float elapsed = 0f;
            AttackType type = currentAttack; //에러 출력을 위한 지역변수

            while (elapsed < config.moveTime)
            {
                if (type != currentAttack)
                {
                    Debug.LogWarning($"AttackMove 코루틴이 끝나지 않은 채로 공격이 바뀌었음. {type} -> {currentAttack}");
                    type = currentAttack;
                }

                // 이동 방향이 타겟을 향하도록 설정
                if (config.moveDirection == Vector3.zero && currentTarget)
                {
                    direction = (currentTarget.transform.position - startPosition).normalized;
                }

                // 이동 속도와 방향에 따라 위치 업데이트
                Vector3 movement = direction * (config.moveSpeed * Time.deltaTime);
                transform.position += movement;
                agent.SetDestination(transform.position);

                elapsed += Time.deltaTime;
                yield return null;
            }

            Debug.LogWarning($"{type} 애니메이션에 MoveStop 이벤트가 설정되지 않음.");
        }

        //공격 중에 회전을 하는 메소드, 애니메이션을 재생하지 않으며, AnimationEvent로 호출된다.
        public void RotateStart()
        {
            // Debug.Log("로테이트 시작");
            if (rotateCoroutine != null)
            {
                StopCoroutine(rotateCoroutine);
                rotateCoroutine = null;
            }

            if (AttackConfigs.TryGetValue((currentAttack, attackIdx), out AttackConfig config))
            {
                rotateCoroutine = StartCoroutine(AttackRotate(config));
            }
            else
            {
                Debug.LogError($"AttackConfig을 찾을 수 없습니다: {currentAttack}, {attackIdx}");
            }
        }

        //공격 중에 회전을 하는 메소드, 애니메이션을 재생하지 않으며, AnimationEvent로 호출된다.
        public void RotateStop()
        {
            // Debug.Log("로테이트 종료");
            if (rotateCoroutine != null)
            {
                StopCoroutine(rotateCoroutine);
                rotateCoroutine = null;
            }
        }

        IEnumerator AttackRotate(AttackConfig config)
        {
            if (!currentTarget)
                yield break;


            float rotateSpeed = config.rotateSpeed;
            float elapsed = 0f; // 코루틴 실행 시간 누적 변수
            float rotateTime = config.rotateTime; // 설정된 최대 회전 시간

            AttackType type = currentAttack; //에러 출력을 위한 지역변수

            // Debug.Log($"AttackRotate started with rotateSpeed: {rotateSpeed}");

            while (elapsed < rotateTime) // 코루틴이 종료될 때까지 반복
            {
                if (type != currentAttack)
                {
                    Debug.LogWarning($"AttackRotate 코루틴이 끝나지 않은 채로 공격이 바뀌었음. {type} -> {currentAttack}");
                    type = currentAttack;
                }

                // 타겟을 향한 방향 계산
                Vector3 directionToTarget = (currentTarget.transform.position - transform.position).normalized;
                directionToTarget.y = 0; // 수평 방향만 고려

                // 현재 몬스터가 타겟을 향해 있는지 확인
                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                float angle = Quaternion.Angle(transform.rotation, targetRotation);

                if (angle > 1f) // 타겟을 바라보지 않을 때
                {
                    // 타겟을 향해 회전
                    float step = rotateSpeed * Time.deltaTime; // 회전 속도
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, step);
                }
                else
                {
                    // 타겟을 바라보고 있다면 회전을 멈춤
                    yield return null;
                }

                elapsed += Time.deltaTime; // 경과 시간 업데이트
                yield return null; // 다음 프레임까지 대기
            }

            Debug.LogWarning($"{type} 애니메이션에 RotateStop 이벤트가 설정되지 않음.");
        }

        //dodge 패턴 시 애니메이션 이벤트로 루트모션을 on/off한다. 
        public void DodgeStart()
        {
            animator.applyRootMotion = true;
        }

        public void DodgeEnd()
        {
            animator.applyRootMotion = false;
            if (SuperManager.Instance.isHost) SendChangeState(MonsterState.MonsterStatusIdle);
        }

        public void SetAnimatorSpeed(float value)
        {
            animator.speed = value;
        }

        //플레이어의 공격은 서버를 통한다.
        public void AttackValidation(PlayerAttackConfig attackConfig, Transform player)
        {
            if (hitDict.TryGetValue((attackConfig, player), out bool value))
            {
                //value값은 의미없고, 일단 true면 같은 공격에 맞았다는 뜻
                Debug.Log($"이미 맞은 공격임(AttackType: {attackConfig}, {player})");
                return;
            }

            StartCoroutine(HitIntervalTimer(attackConfig, player));

            //서버 통신
            TcpProtobufClient.Instance.SendMonsterTakeDamage(monsterId, attackConfig.damageAmount);

            //피격 이펙트 발생
        }

        //OtherPlayer의 공격은 피격이펙트만 생성한다.
        public void OtherPlayerAttackValidation(PlayerAttackConfig attackConfig, Transform player)
        {
            if (hitDict.TryGetValue((attackConfig, player), out bool value))
            {
                //value값은 의미없고, 일단 true면 같은 공격에 맞았다는 뜻
                Debug.Log($"이미 맞은 공격임(AttackType: {attackConfig}, {player})");
                return;
            }

            StartCoroutine(HitIntervalTimer(attackConfig, player));

            //피격 이펙트 발생
        }

        //실제 데미지를 적용하는 메소드(서버에서 메시지를 받아서 호출)
        public void TakeDamage(float damage, float msgHp)
        {
            Debug.Log($"몬스터가 데미지를 {damage} 받음. 체력: {currentHp} -> {msgHp}");
            UIManager.Instance.AddLogChat($"몬스터가 데미지를 {damage} 받음. 체력: {currentHp} -> {msgHp}");
            
            currentHp = msgHp;
            //이후 체력바 줄어드는 연출이나 데미지 표기 등 연출을 하면 됨.
        }

        IEnumerator HitIntervalTimer(PlayerAttackConfig attackConfig, Transform player)
        {
            hitDict.Add((attackConfig, player), true);
            yield return new WaitForSeconds(TakeDamageThreshold);
            hitDict.Remove((attackConfig, player));
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!AttackConfigs.TryGetValue((currentAttack, attackIdx), out AttackConfig attackConfig))
            {
                return;
            }

            if (attackConfig.RangeType is MeleeAttack config)
            {
                if (config.ColliderConfigs == null || config.ColliderConfigs.Length == 0)
                {
                    //메시지는 이미 HitCheck에서 출력했으므로 메시지는 넘어감
                    return;
                }

                Vector3 basePosition = transform.position + transform.forward * config.Distance +
                                       transform.TransformDirection(config.AttackPositionOffset);

                Gizmos.color = Color.red;

                foreach (var colliderConfig in config.ColliderConfigs)
                {
                    switch (colliderConfig.ColliderType)
                    {
                        case ColliderType.Sphere:
                            if (colliderConfig is SphereColliderConfig sphereConfig)
                            {
                                Gizmos.DrawWireSphere(basePosition, sphereConfig.Radius);
                            }

                            break;

                        case ColliderType.Box:
                            if (colliderConfig is BoxColliderConfig boxConfig)
                            {
                                Vector3 boxCenterWorld = basePosition + transform.TransformDirection(boxConfig.Center);
                                Quaternion worldBoxRotation = transform.rotation * boxConfig.Rotation;

                                Matrix4x4 oldMatrix = Gizmos.matrix;
                                Gizmos.matrix = Matrix4x4.TRS(boxCenterWorld, worldBoxRotation, Vector3.one);
                                Gizmos.DrawWireCube(Vector3.zero, boxConfig.Size);
                                Gizmos.matrix = oldMatrix;
                            }

                            break;

                        case ColliderType.Capsule:
                            if (colliderConfig is CapsuleColliderConfig capsuleConfig)
                            {
                                Vector3 point1 = basePosition + capsuleConfig.Direction *
                                    (capsuleConfig.Height / 2 - capsuleConfig.Radius);
                                Vector3 point2 = basePosition - capsuleConfig.Direction *
                                    (capsuleConfig.Height / 2 - capsuleConfig.Radius);
                                Gizmos.DrawWireSphere(point1, capsuleConfig.Radius);
                                Gizmos.DrawWireSphere(point2, capsuleConfig.Radius);
                                Gizmos.DrawLine(point1, point2);
                            }

                            break;

                        default:
                            Debug.LogWarning($"Unknown ColliderType: {colliderConfig.ColliderType}");
                            break;
                    }
                }
            }
        }
#endif

        #endregion
    }
}