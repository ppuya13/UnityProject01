

using System;
using UnityEngine;

public class UIManager: DDSingletonManager<UIManager>
{
    
    protected override void Awake()
    {
        base.Awake();
        TcpProtobufClient.Instance.OnConnectionStatusChanged += OnConnectedToTcpServer;
    }

    private void OnConnectedToTcpServer(bool success)
    {
        if (success)
        {
            Debug.Log("연결 성공");
            TcpProtobufClient.Instance.SendLoginMessage(SuperManager.Instance.PlayerNickname, string.Empty);
        }
        else
        {
            Debug.LogError("연결 실패");
        }
    }
}
