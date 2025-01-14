using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Sirenix.Utilities;
using Sound;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ChatMessage = Game.ChatMessage;
using Debug = UnityEngine.Debug;

public class UIManager : DDSingletonManager<UIManager>
{
    private string serverPath;
    private Process serverProcess;

    public GameObject mainPanel;
    public Button createBtn;
    public Button joinBtn;
    public TMP_InputField joinInput;
    public TextMeshProUGUI serverStatusText;
    public TextMeshProUGUI errMsg;
    public TMP_InputField nickname;
    public Button startBtn;

    public ScrollRect scrollRect;
    public RectTransform content;
    public GameObject chatMessagePrefab;
    public GameObject otherChatMessagePrefab;
    public GameObject systemMessagePrefab;
    public TMP_InputField chatInput;
    public Button sendBtn;

    public GameObject inGamePanel;
    public GameObject invincibleMode;
    public Transform characterPanelParent;
    public InfoPanel myCharacterPanel;
    public InfoPanel characterPanel;
    public InfoPanel monsterPanel;
        
    private volatile bool serverReady = false;

    public Action<bool> ChattingActivated;
    

    protected override void Awake()
    {
        base.Awake();
        TcpProtobufClient.Instance.OnConnectionStatusChanged += OnConnectedToTcpServer;
        serverPath = Path.Combine(Application.dataPath, "../Server/main.exe");
        
        chatInput.onSubmit.AddListener(SendChat);
    }
    
    
    void Update()
    {
        if (serverReady)
        {
            serverStatusText.text = "서버 실행 중";
            serverReady = false; // 플래그 리셋

            // 서버가 준비되었으므로 클라이언트가 서버에 연결을 시도
            _ = TcpProtobufClient.Instance.ConnectToServerAsync("127.0.0.1", 8888);
        }
        
        
        // 입력 필드가 포커스되지 않은 상태에서 키보드 입력 활성화
        if (!chatInput.isFocused)
        {
            if(SpawnManager.Instance.MyCharacter)
                SpawnManager.Instance.MyCharacter.disableKeyboard = false;
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (!chatInput.isFocused)
            {
                // 입력 필드가 포커스되지 않은 상태에서 Enter 키를 누르면 포커스를 설정
                if(SpawnManager.Instance.MyCharacter)
                    SpawnManager.Instance.MyCharacter.disableKeyboard = true; // 채팅 중 키보드로 인한 캐릭터의 움직임 비활성화
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                ChattingActivated.Invoke(true);
                chatInput.ActivateInputField();
            }
        }
        // Escape 키를 눌렀을 때
        if (chatInput.isFocused && Input.GetKeyDown(KeyCode.Escape))
        {
            // 입력 필드의 포커스를 제거
            if(SpawnManager.Instance.MyCharacter)
                SpawnManager.Instance.MyCharacter.disableKeyboard = false; // 키보드 입력 활성화
            ChattingActivated.Invoke(false);
            chatInput.DeactivateInputField();
        }

        // 마우스 클릭으로 입력 필드 외부를 클릭했을 때 포커스 제거
        if (Input.GetMouseButtonDown(0))
        {
            // 현재 선택된 UI 요소가 입력 필드가 아닌 경우 포커스 제거
            if (EventSystem.current.currentSelectedGameObject != chatInput.gameObject)
            {
                ChattingActivated.Invoke(false);
                chatInput.DeactivateInputField();
            }
        }
    }

    public void CreateCharacterPanel(PlayerController character)
    {
        InfoPanel panel = Instantiate(characterPanel, characterPanelParent);
        panel.ConnectCharacter(character);
    }


    #region 서버 관련

    private void OnConnectedToTcpServer(bool success)
    {
        if (success)
        {
            Debug.Log("연결 성공");
            string nicknameText = !nickname.text.IsNullOrWhitespace() ? nickname.text : "Player";
            SuperManager.Instance.playerNickname = nicknameText;
            TcpProtobufClient.Instance.SendLoginMessage(nicknameText, string.Empty);
            if (SuperManager.Instance.isHost) startBtn.interactable = true;
            EnableChatting();
        }
        else
        {
            Debug.LogError("연결 실패");
            errMsg.text = "서버에 연결할 수 없습니다.";
            EnableButton();
        }
    }

    void StartServer()
    {
        // 서버 실행 파일 확인
        if (string.IsNullOrEmpty(serverPath) || !File.Exists(serverPath))
        {
            Debug.LogError($"서버 실행 파일을 찾을 수 없습니다: {serverPath}");
            serverStatusText.text = "서버 실행 파일 없음";
            EnableButton();
            return;
        }

        try
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = serverPath,
                Arguments = "", // 필요한 경우 인자를 추가하세요.
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true, // 표준 출력 리디렉션 활성화
                RedirectStandardError = true, // 표준 에러 리디렉션 활성화
                WorkingDirectory = Path.GetDirectoryName(serverPath) ?? string.Empty
            };

            serverProcess = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true // 프로세스 종료 이벤트 활성화
            };
            
            // 서버의 표준 출력 이벤트 핸들러
            serverProcess.OutputDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    Debug.Log($"[서버 출력] {args.Data}");
                    if (args.Data.Contains("Server is listening on"))
                    {
                        serverReady = true;
                    }
                }
            };

            // 서버의 표준 에러 이벤트 핸들러
            serverProcess.ErrorDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    Debug.LogError($"[서버 에러] {args.Data}");
                    errMsg.text = "서버 오류 발생";
                }
            };

            // 서버 프로세스 종료 이벤트 핸들러 (필요 시 추가 작업 가능)
            serverProcess.Exited += (sender, args) =>
            {
                Debug.Log("서버 프로세스가 종료되었습니다.");
                // 서버 종료에 대한 별도의 처리가 필요 없다면 이 부분은 비워둘 수 있습니다.
            };

            // 서버 프로세스 시작
            serverProcess.Start();

            // 비동기적으로 표준 출력과 에러 읽기 시작
            serverProcess.BeginOutputReadLine();
            serverProcess.BeginErrorReadLine();

            Debug.Log("서버 프로세스가 시작되었습니다.");
            serverStatusText.text = "서버 시작 중...";
        }
        catch (Exception ex)
        {
            Debug.LogError($"서버 시작에 실패했습니다: {ex.Message}");
            serverStatusText.text = "서버 시작 실패";
            EnableButton();
        }
    }

    //런타임에서 stopServer는 지원하지 않는다.
    //서버를 끄려면 unityEditor에서는 Play모드를 끄거나, build에서는 프로그램을 종료시켜야 한다.
    void StopServer()
    {
        if (serverProcess is { HasExited: false })
        {
            try
            {
                // 비동기로 서버 프로세스 종료
                Task.Run(() =>
                {
                    serverProcess.Kill();
                    serverProcess.WaitForExit();
                    serverProcess.Dispose();
                    serverProcess = null;
                    Debug.Log("Server stopped.");
                });
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to stop server: {ex.Message}");
            }
        }
    }

    void OnApplicationQuit()
    {
        // Unity 종료 시 서버 프로세스도 함께 종료
        StopServer();
    }
    #endregion

    #region 버튼 클릭
    
    public void OnCreateBtnClick()
    {
        SuperManager.Instance.isHost = true;
        StartServer();
        DisableButton();
    }

    public void OnJoinBtnClick()
    {
        SuperManager.Instance.isHost = false;
        // SuperManager.Instance.isHost = true; //디버그용, 시연할땐 false로 할 것.
        DisableButton();
        
        string ipAddress = "127.0.0.1"; // 기본 IP 주소
        string input = joinInput.text.Trim(); // 입력값을 공백 제거 후 저장
        
        if (!string.IsNullOrEmpty(input))
        {
            // 입력값이 비어있지 않으면 유효한 IPv4 주소인지 확인
            if (System.Net.IPAddress.TryParse(input, out System.Net.IPAddress address) &&
                address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                ipAddress = input; // 유효한 IPv4 주소이면 해당 주소로 설정
            }
            else
            {
                Debug.LogError("유효하지 않은 IPv4 주소입니다.");
                errMsg.text = "유효하지 않은 IPv4 주소입니다.";
                EnableButton();
                return; // 연결 시도 중단
            }
        }

        // 서버에 연결 시도
        _ = TcpProtobufClient.Instance.ConnectToServerAsync(ipAddress, 8888);
    }

    public void StartGame()
    {
        if (!SuperManager.Instance.isHost) return;
        startBtn.interactable = false;
        TcpProtobufClient.Instance.SendGameStart();
    }

    #endregion

    #region 채팅 관련

    //빌드에서 로그를 확인하기 위한 메소드.
    public void AddLogChat(string text)
    {
        GameObject go = Instantiate(systemMessagePrefab, content);
        TextMeshProUGUI chat = go.GetComponent<TextMeshProUGUI>();
        chat.text = $"{text}";
    }

    public void AddChat(ChatMessage msg)
    {
        if (msg.System)
        {
            // Debug.Log("시스템 메시지 수신");
            GameObject go = Instantiate(systemMessagePrefab, content);
            TextMeshProUGUI chat = go.GetComponent<TextMeshProUGUI>();
            chat.text = "System: " + msg.Message;
        }
        else
        {
            // Debug.Log("플레이어 메시지 수신");
            //누가 보냈냐에 따라서 다른 프리팹을 사용한다.
            GameObject go = Instantiate(msg.PlayerId == SuperManager.Instance.playerId ? chatMessagePrefab : otherChatMessagePrefab, content);

            TextMeshProUGUI chat = go.GetComponent<TextMeshProUGUI>();
            chat.text = $"{msg.Nickname}: " + msg.Message;
        }
        
        // 레이아웃을 즉시 재구성
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);

        // 스크롤을 하단으로 이동 (프레임 이후에 실행)
        StartCoroutine(ScrollToBottom());
    }
    
    private IEnumerator ScrollToBottom()
    {
        yield return null; // 한 프레임 대기

        // 스크롤 위치를 하단으로 설정
        scrollRect.verticalNormalizedPosition = 0f;
    }

    public void SendBtnClick()
    {
        SendChat(chatInput.text);
    }

    private void SendChat(string text)
    {
        if (text.IsNullOrWhitespace()) return;
        TcpProtobufClient.Instance.SendChatMessage(text);
        chatInput.text = string.Empty;
        chatInput.ActivateInputField();
    }
    
    #endregion
    
    public void CloseMenu()
    {
        SoundManager.Instance.PlayBGM(volume:0.1f);
        mainPanel.SetActive(false);
        inGamePanel.SetActive(true);
    }

    public void DisableButton()
    {
        createBtn.interactable = false;
        joinBtn.interactable = false;
        nickname.interactable = false;
    }

    public void EnableButton()
    {
        createBtn.interactable = true;
        joinBtn.interactable = true;
        nickname.interactable = true;
    }

    public void DisableChatting()
    {
        chatInput.interactable = false;
        sendBtn.interactable = false;
    }

    public void EnableChatting()
    {
        chatInput.interactable = true;
        sendBtn.interactable = true;
    }
}