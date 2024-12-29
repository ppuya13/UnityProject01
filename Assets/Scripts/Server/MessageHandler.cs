using System;
using Game;
using Monster;
using UnityEngine;


//Game Manager를 대체한다.
//리팩토링 하는 김에 좀 더 명확한 이름으로 바꿈.
public class MessageHandler : DDSingletonManager<MessageHandler>
{
    private void Update()
    {
        if (!UnityMainThreadDispatcher.Instance)
        {
            Debug.LogError("[MessageHandler] UnityMainThreadDispatcher is null!");
            return;
        }

        if (UnityMainThreadDispatcher.Instance.ExecutionQueue == null)
        {
            Debug.LogError("[MessageHandler] ExecutionQueue is null!");
            return;
        }

        while (UnityMainThreadDispatcher.Instance.ExecutionQueue.Count > 0)
        {
            GameMessage msg = UnityMainThreadDispatcher.Instance.ExecutionQueue.Dequeue();
            // Debug.Log($"MessageHandler processing: {msg.PayloadCase}");

            switch (msg.PayloadCase)
            {
                case GameMessage.PayloadOneofCase.Pong:
                    Debug.Log("퐁");
                    break;
                case GameMessage.PayloadOneofCase.LoginResponse:
                    Debug.Log("로그인 리스폰스 수신");
                    HandleLoginResponse(msg.LoginResponse);
                    break;
                case GameMessage.PayloadOneofCase.LogoutResponse:
                    Debug.Log("로그아웃 리스폰스 수신");
                    break;
                case GameMessage.PayloadOneofCase.PlayerSpawn:
                    Debug.Log("플레이어 스폰 메시지 수신");
                    HandlePlayerSpawn(msg.PlayerSpawn);
                    break;
                case GameMessage.PayloadOneofCase.ChatMessage:
                    Debug.Log("채팅 메시지 수신");
                    HandleChatMessage(msg.ChatMessage);
                    break;
                case GameMessage.PayloadOneofCase.MonsterSpawn:
                    Debug.Log("몬스터 스폰 메시지 수신");
                    SpawnManager.Instance.SpawnMonster(msg.MonsterSpawn);
                    break;
                case GameMessage.PayloadOneofCase.MonsterAnim:
                    Debug.Log("몬스터 애니메이션 스테이트 변경 메시지 수신");
                    HandleMonsterAnim(msg.MonsterAnim);
                    break;
                case GameMessage.PayloadOneofCase.MonsterAction:
                    HandleMonsterAction(msg.MonsterAction);
                    break;
                case GameMessage.PayloadOneofCase.MonsterTakeDamage:
                    HandleMonsterTakeDamage(msg.MonsterTakeDamage);
                    break;
                case GameMessage.PayloadOneofCase.PlayerInput:
                    HandlePlayerInput(msg.PlayerInput);
                    break;
                case GameMessage.PayloadOneofCase.PlayerAttackAnim:
                    HandlePlayerAttackAnim(msg.PlayerAttackAnim);
                    break;
                default:
                    Debug.LogError($"정의되지 않은 케이스 ({msg.PayloadCase})");
                    break;
            }
        }
    }

    private void HandleLoginResponse(LoginResponse msg)
    {
        if (msg.Success)
        {
            
            SuperManager.Instance.playerId = msg.PlayerId;
            SuperManager.Instance.playerNickname = msg.Username;
        }
        else
        {
            Debug.LogError(msg.ErrorMessage);
        }
    }

    private void HandlePlayerSpawn(PlayerSpawn msg)
    {
        
        // if (msg.PlayerId == SuperManager.Instance.PlayerId)
        // {
        //     //메시지의 playerId가 클라이언트의 playerId와 같으면 이 클라이언트가 지금 접속한 캐릭터라는 뜻이기 때문에
        //     //기존 플레이어들을 스폰시킨다.
        //     if (msg.Players.Count > 0) //먼저 접속한 플레이어가 있으면
        //     {
        //         foreach (PlayerInfo playerInfo in msg.Players)
        //         {
        //             SpawnManager.Instance.SpawnPlayer(playerInfo.Id, playerInfo.Nickname);
        //         }
        //     }
        // }
        //
        // SpawnManager.Instance.SpawnPlayer(msg.PlayerId, msg.Nickname); //새로 접속한 캐릭터를 스폰시킨다.
        
        if (msg.Players.Count > 0)
        {
            foreach (PlayerInfo playerInfo in msg.Players)
            {
                SpawnManager.Instance.SpawnPlayer(playerInfo.Id, playerInfo.Nickname);
            }
        }
        else
        {
            Debug.LogError("msg.Players.Count가 0임!!!");
        }

        SpawnManager.Instance.ScanPlayer();
        if (SuperManager.Instance.isHost)
        {
            TcpProtobufClient.Instance.SendMonsterSpawn();
        }

        UIManager.Instance.CloseMenu();
    }

    #region 채팅 관련

    private void HandleChatMessage(ChatMessage msg)
    {
        //현재는 시스템 메시지 구분이나, 본인 메시지 구분을 안하고 있지만, ChatMessage에 해당 구분을 위한 변수는 다 있음.
        UIManager.Instance.AddChat(msg);
    }

    #endregion
    

    #region 몬스터 관련

    private void HandleMonsterAnim(MonsterAnim msg)
    {
        if (SpawnManager.Instance.SpawnedMonsters.TryGetValue(msg.MonsterId, out MonsterController mc))
        {
            mc.SetParameter(msg);
        }
        else
        {
            Debug.LogError("MonsterController를 찾을 수 없음.");
        }
    }

    private void HandleMonsterAction(MonsterAction msg)
    {
        switch (msg.ActionType)
        {
            case ActionType.MonsterActionSetStatus:
                Debug.Log("몬스터 셋스테이터스 메시지 수신");
            {
                if (SpawnManager.Instance.SpawnedMonsters.TryGetValue(msg.MonsterId, out MonsterController mc))
                {
                    mc.ChangeState(msg);
                }
                else
                {
                    Debug.LogError("MonsterController를 찾을 수 없음.");
                }
            }
                break;
            case ActionType.MonsterActionSetTarget:
                // Debug.Log("몬스터 셋타겟 메시지 수신");
            {
                if (SpawnManager.Instance.SpawnedMonsters.TryGetValue(msg.MonsterId, out MonsterController mc))
                {
                    mc.SetTarget(msg);
                }
                else
                {
                    Debug.LogError("MonsterController를 찾을 수 없음.");
                }
            }
                break;
            case ActionType.MonsterActionSetDestination:
                // Debug.Log("몬스터 이동 목표 설정 메시지 수신");
            {
                if (SpawnManager.Instance.SpawnedMonsters.TryGetValue(msg.MonsterId, out MonsterController mc))
                {
                    mc.SetDestination(msg);
                }
                else
                {
                    Debug.LogError("MonsterController를 찾을 수 없음.");
                }
            }
                break;
            default:
                Debug.LogError($"정의되지 않은 액션 타입 ({msg.ActionType})");
                break;
        }
    }

    private void HandleMonsterTakeDamage(MonsterTakeDamage msg)
    {
        Debug.Log("몬스터 테이크 데미지 메시지 수신");
        SpawnManager.Instance.SpawnedMonsters[msg.MonsterId].TakeDamage(msg.Damage, msg.CurrentHp);
    }

    #endregion

    #region 플레이어 관련

    private void HandlePlayerInput(PlayerInput msg)
    {
        switch (msg.PlayerActionType)
        {
            case PlayerActionType.PlayerActionTakedamage:
                Debug.Log("플레이어 테이크 데미지 메시지 수신");
                if (SuperManager.Instance.playerId == msg.PlayerId) return;
            {
                if (SpawnManager.Instance.SpawnedPlayers.TryGetValue(msg.PlayerId,
                        out PlayerController playerController))
                {
                    if (playerController is OtherPlayer otherPlayer)
                    {
                        otherPlayer.OtherPlayerTakeDamage(msg);
                    }
                    else
                    {
                        Debug.LogError($"{playerController}가 otherPlayer가 아님.");
                    }
                }
                else
                {
                    Debug.LogError($"{msg.PlayerId}가 SpawnManager.Instance.SpawnedPlayers에 없음.");
                }
            }
                break;
            case PlayerActionType.PlayerActionMove:
                Debug.Log("플레이어 이동 메시지 수신");
            {
                if (SpawnManager.Instance.SpawnedPlayers.TryGetValue(msg.PlayerId,
                        out PlayerController playerController))
                {
                    if (playerController is OtherPlayer otherPlayer)
                    {
                        otherPlayer.UpdatePosition(TcpProtobufClient.Instance.ConvertToVector3(msg.Position), msg.Horizontal, msg.Vertical, msg.IsRunning);
                        otherPlayer.UpdateVelocity(TcpProtobufClient.Instance.ConvertToVector3(msg.Velocity));
                        otherPlayer.UpdateRotation(TcpProtobufClient.Instance.ConvertToVector3(msg.Rotation));
                    }
                    else
                    {
                        Debug.LogError($"{playerController}가 otherPlayer가 아님.");
                    }
                }
                else
                {
                    Debug.LogError($"{msg.PlayerId}가 SpawnManager.Instance.SpawnedPlayers에 없음.");
                }
            }
                break;
            case PlayerActionType.PlayerActionDodge:
                Debug.Log("플레이어 회피 메시지 수신");
            {
                if (SpawnManager.Instance.SpawnedPlayers.TryGetValue(msg.PlayerId,
                        out PlayerController playerController))
                {
                    if (playerController is OtherPlayer otherPlayer)
                    {
                        otherPlayer.OtherPlayerDodge(msg.DodgeParams.MoveX, msg.DodgeParams.MoveY);
                    }
                    else
                    {
                        Debug.LogError($"{playerController}가 otherPlayer가 아님.");
                    }
                }
                else
                {
                    Debug.LogError($"{msg.PlayerId}가 SpawnManager.Instance.SpawnedPlayers에 없음.");
                }
            }
                
                break;
        }
    }

    private void HandlePlayerAttackAnim(PlayerAttackAnim msg)
    {
        Debug.Log("플레이어 어택 애니메이션 재생 메시지 수신");
        if (SpawnManager.Instance.SpawnedPlayers.TryGetValue(msg.PlayerId, out PlayerController player))
        {
            if (player is OtherPlayer otherPlayer)
            {
                otherPlayer.PlayAttackAnimation(msg.Hash, msg.Layer);
            }
        }
    }

    #endregion
}