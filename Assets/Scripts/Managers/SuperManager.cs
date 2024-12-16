using UnityEngine.Serialization;

public class SuperManager: DDSingletonManager<SuperManager>
{
    public string playerId; //서버에서 받아오는 고유한 id

    public string playerNickname; //플레이어가 설정하는 닉네임

    public bool isHost = false; //플레이어가 서버를 팠는지

    protected override void Awake()
    {
        base.Awake();

        // isHost = true; //UI만들고부터는 UI에 연계하기 (방파기 버튼 누르면 true)
    }
    
    //게임 종료 시 로그아웃 메시지를 보낸다.
    void OnApplicationQuit()
    {
        TcpProtobufClient.Instance.SendLogoutMessage(playerId);
    }
}