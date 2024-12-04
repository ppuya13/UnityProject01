using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Game;
using UnityEngine.AI;

// TcpProtobufClient 클래스: Unity에서 TCP 연결을 통해 Protobuf 메시지를 주고받는 클라이언트
public class TcpProtobufClient : DDSingletonManager<TcpProtobufClient>
{
    // 싱글턴 패턴을 위한 정적 인스턴스
    private TcpClient tcpClient; // 서버와의 TCP 연결을 관리하는 객체
    private NetworkStream stream; // TCP 클라이언트의 네트워크 스트림
    private bool isRunning = false; // 클라이언트의 실행 상태를 나타내는 플래그

    // 서버 연결 정보
    public string SERVER_IP = "127.0.0.1"; // 서버 IP 주소
    public int SERVER_PORT = 8888; // 서버 포트 번호

    public event Action<bool> OnConnectionStatusChanged;


    protected override void Awake()
    {
        base.Awake();
        _ = ConnectToServerAsync("127.0.0.1", 8888);
    }

    #region 기본 구조

    // ConnectToServer: 서버에 연결을 시도하는 메서드
    public async Task<bool> ConnectToServerAsync(string ip, int port, int timeoutMilliseconds = 5000)
    {
        // 아랫줄 주석 시 TcpProtobufClient 오브젝트의 인스펙터 입력값이 서버의 ip가 된다.
        SERVER_IP = ip;
        SERVER_PORT = port;

        try
        {
            using var cts = new CancellationTokenSource(timeoutMilliseconds);
            tcpClient = new TcpClient();
            var connectTask = tcpClient.ConnectAsync(SERVER_IP, SERVER_PORT);
            if (await Task.WhenAny(connectTask, Task.Delay(timeoutMilliseconds, cts.Token)) == connectTask)
            {
                //연결 성공
                await connectTask;
                stream = tcpClient.GetStream();

                isRunning = true;
                StartReceiving();

                Debug.Log("Connected to server."); // 연결 성공 로그
                OnConnectionStatusChanged?.Invoke(true);
                return true;
            }
            else
            {
                //타임아웃 발생
                throw new TimeoutException("Connection attempt timed out");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error connecting to server: {e.Message}"); // 연결 실패 시 에러 로그
            OnConnectionStatusChanged?.Invoke(false);
            return false;
        }
    }

    // StartReceiving: 메시지 수신을 시작하는 메서드
    void StartReceiving()
    {
        byte[] lengthBuffer = new byte[4]; // 메시지 길이를 저장할 버퍼
        stream.BeginRead(lengthBuffer, 0, 4, OnLengthReceived, lengthBuffer); // 비동기적으로 메시지 길이 읽기 시작
    }

    // OnLengthReceived: 메시지 길이를 수신한 후 호출되는 콜백 메서드
    void OnLengthReceived(IAsyncResult ar)
    {
        try
        {
            int bytesRead = stream.EndRead(ar); // 읽기 작업 완료
            if (bytesRead == 0) return; // 연결 종료 시 처리

            byte[] lengthBuffer = (byte[])ar.AsyncState; // 길이 버퍼 가져오기
            int length = BitConverter.ToInt32(lengthBuffer, 0); // 버퍼에서 메시지 길이 추출

            byte[] messageBuffer = new byte[length]; // 메시지 내용을 저장할 버퍼
            stream.BeginRead(messageBuffer, 0, length, OnMessageReceived, messageBuffer); // 비동기적으로 메시지 내용 읽기 시작
        }
        catch (Exception e)
        {
            Debug.LogError($"Error receiving message length: {e.Message}"); // 에러 발생 시 로그
        }
    }


    // OnMessageReceived: 메시지 내용을 수신한 후 호출되는 콜백 메서드
    void OnMessageReceived(IAsyncResult ar)
    {
        try
        {
            int bytesRead = stream.EndRead(ar);

            if (bytesRead == 0)
            {
                Debug.LogWarning("[TCP] Received 0 bytes");
                return;
            }

            byte[] messageBuffer = (byte[])ar.AsyncState;
            GameMessage gameMessage = GameMessage.Parser.ParseFrom(messageBuffer);

            if (!UnityMainThreadDispatcher.Instance)
            {
                Debug.LogError("[TCP] UnityMainThreadDispatcher.Instance is null!");
                return;
            }

            UnityMainThreadDispatcher.Instance.Enqueue(gameMessage);
            // Debug.Log($"[TCP] Message successfully enqueued to dispatcher");

            StartReceiving();
        }
        catch (Exception e)
        {
            Debug.LogError($"Error receiving message: {e.Message}"); // 에러 발생 시 로그
        }
    }

    // SendMessage: 실제로 메시지를 서버로 전송하는 private 메서드
    private void SendMessage(GameMessage message)
    {
        if (tcpClient != null && tcpClient.Connected)
        {
            try
            {
                byte[] messageBytes = message.ToByteArray();
                byte[] lengthBytes = BitConverter.GetBytes(messageBytes.Length);

                stream.Write(lengthBytes, 0, 4);
                stream.Write(messageBytes, 0, messageBytes.Length);
                stream.Flush();

                // Debug.Log($"[TCP] Message sent successfully - Type: {message.PayloadCase}, Size: {messageBytes.Length} bytes");
            }
            catch (Exception e)
            {
                Debug.LogError($"[TCP] Error sending message: {e.Message}");
            }
        }
        else
        {
            Debug.LogError("[TCP] Cannot send message - Client is not connected");
        }
    }

    // OnDisable: 스크립트가 비활성화될 때 호출되는 Unity 생명주기 메서드
    void OnDisable()
    {
        isRunning = false; // 실행 상태 플래그 해제
        stream?.Close(); // 스트림 닫기
        tcpClient?.Close(); // TCP 클라이언트 연결 종료
    }
    
    #endregion

    #region 계정 관련

    // SendLoginMessage: 로그인 메시지를 서버에 전송하는 메서드
    public void SendLoginMessage(string nickname, string gameUserID)
    {
        Debug.Log("로그인 메시지 발신");

        //혼자 테스트를 위해 아이디와 닉네임을 임의로 생성한다.
        string playerId = GenerateGuid();

        var login = new LoginRequest()
        {
            Id = playerId,
            Nickname = nickname,
            GameUserId = gameUserID,
        };
        var message = new GameMessage
        {
            LoginRequest = login
        };
        SendMessage(message); // 메시지 전송
    }

    private string GenerateGuid()
    {
        return Guid.NewGuid().ToString();
    }

    // SendLogoutMessage: 로그아웃 메시지를 서버에 전송하는 메서드
    public void SendLogoutMessage(string playerId)
    {
        // Debug.Log("로그아웃 메시지 발신");
        var logout = new LogoutRequest()
        {
            PlayerId = playerId
        };
        var message = new GameMessage
        {
            LogoutRequest = logout
        };
        SendMessage(message); // 메시지 전송
    }

    #endregion

    #region 플레이어 액션

    public void SendPlayerTakeDamage(Vector3 knockBack, float stunDuration)
    {
        GoVector3 _knockback = new GoVector3()
        {
            X = knockBack.x,
            Y = knockBack.y,
            Z = knockBack.z
        };

        var message = new GameMessage()
        {
            PlayerInput = new PlayerInput()
            {
                PlayerId = SuperManager.Instance.PlayerId,
                PlayerActionType = PlayerActionType.PlayerActionTakedamage,
                Knockback = _knockback,
                StunDuration = stunDuration,
            }
        };
        SendMessage(message);
    }

    #endregion

    #region 몬스터 관련

    
    public void SendMonsterChangeState(string monsterId, MonsterState state, AttackType attackType)
    {
        Debug.Log($"몬스터 상태 변경 메시지 발신: {state}");
        var message = new GameMessage()
        {
            MonsterAction = new MonsterAction()
            {
                ActionType = ActionType.MonsterActionSetStatus,
                MonsterId = monsterId,
                MonsterState = state,
                AttackType = attackType,
            }
        };
        
        SendMessage(message);
    }
    
    public void SendMonsterSpawn()
    {
        Debug.Log("몬스터 스폰 메시지 발신");
        var message = new GameMessage()
        {
            MonsterSpawn = new MonsterSpawn()
        };
        SendMessage(message);
    }

    public void SendMonsterAnimMessage(string monsterId, int hash, ParameterType type, int intValue = 0,
        float floatValue = 0, bool boolValue = false)

    {
        var message = new GameMessage()
        {
            MonsterAnim = new MonsterAnim()
            {
                MonsterId = monsterId,
                AnimHash = hash,
                ParameterType = type,
                IntValue = intValue,
                FloatValue = floatValue,
                BoolValue = boolValue,
            }
        };
        SendMessage(message);
    }

    public void SendMonsterTarget(string monsterId, string targetId)
    {
        var message = new GameMessage()
        {
            MonsterAction = new MonsterAction()
            {
                MonsterId = monsterId,
                ActionType = ActionType.MonsterActionSetTarget,
                TargetId = targetId
            }
        };
        SendMessage(message);
    }

    public void SendDestination(string monsterId, Vector3 destination)
    {
        Debug.Log("이동목표발신");
        GoVector3 _destination = new GoVector3()
        {
            X = destination.x,
            Y = destination.y,
            Z = destination.z,
        };
        
        var message = new GameMessage()
        {
            MonsterAction = new MonsterAction()
            {
                MonsterId = monsterId,
                ActionType = ActionType.MonsterActionSetDestination,
                Destination = _destination
            }
        };
        SendMessage(message);
    }

    #endregion

}