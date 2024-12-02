using System;
using Game;
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
            Debug.Log($"MessageHandler processing: {msg.PayloadCase}");


            try
            {
                switch (msg.PayloadCase)
                {
                    case GameMessage.PayloadOneofCase.LoginResponse:
                        Debug.Log("로그인 리스폰스 수신");
                        if (msg.LoginResponse.Success)
                        {
                            SuperManager.Instance.PlayerId = msg.LoginResponse.PlayerId;
                            SuperManager.Instance.PlayerNickname = msg.LoginResponse.Username;
                            if (SuperManager.Instance.IsHost)
                            {
                                TcpProtobufClient.Instance.SendMonsterSpawn();
                            }
                        }
                        else
                        {
                            Debug.LogError(msg.LoginResponse.ErrorMessage);
                        }

                        break;
                    case GameMessage.PayloadOneofCase.LogoutResponse:
                        Debug.Log("로그아웃 리스폰스 수신");
                        break;
                    
                    case GameMessage.PayloadOneofCase.MonsterSpawn:
                        Debug.Log("몬스터 스폰 메시지 수신");
                        SpawnManager.Instance.SpawnMonster(msg.MonsterSpawn);
                        
                        break;
                    case GameMessage.PayloadOneofCase.MonsterAction:
                        Debug.Log("몬스터 액션 메시지 수신");
                        HandleMonsterAction(msg.MonsterAction);
                        break;
                    default:
                        Debug.LogError($"정의되지 않은 케이스 ({msg.PayloadCase})");
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[MessageHandler] Error processing message: {e.Message}\n{e.StackTrace}");
            }
        }
    }

    #region 몬스터 액션

    private void HandleMonsterAction(MonsterAction msg)
    {
        switch (msg.ActionType)
        {
            case ActionType.MonsterActionSetStatus:
                
                break;
            default:
                Debug.LogError($"정의되지 않은 액션 타입 ({msg.ActionType})");
                break;
        }
    }

    #endregion

    //게임 종료 시 로그아웃 메시지를 보내는 메소드
    void OnApplicationQuit()
    {
        TcpProtobufClient.Instance.SendLogoutMessage(SuperManager.Instance.PlayerId);
    }
}