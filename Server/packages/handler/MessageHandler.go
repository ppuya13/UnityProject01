package handler

import (
	pb "Server/messages"
	"Server/packages/manager"
	"fmt"
	"log"
	"net"

	"github.com/gofrs/uuid"
)

type MessageHandler struct {
	mcx *manager.ManagerContext
}

func NewMessageHandler(ctx *manager.ManagerContext) *MessageHandler {
	return &MessageHandler{
		mcx: ctx,
	}
}

// processMessage 함수: 받은 메시지의 타입에 따라 적절한 처리를 수행
func (mh *MessageHandler) ProcessMessage(message *pb.GameMessage, conn net.Conn, playerId *string) {
	switch payload := message.Payload.(type) {
	case *pb.GameMessage_Ping:
		mh.handlePing(conn)
	case *pb.GameMessage_LoginRequest:
		mh.handleLoginRequest(payload.LoginRequest, conn, playerId)
	case *pb.GameMessage_LogoutRequest:
		mh.handleLogoutRequest(conn, playerId)
	case *pb.GameMessage_GameStart:
		mh.handleGameStart()
	case *pb.GameMessage_ChatMessage:
		mh.handleChatMessage(message)
	case *pb.GameMessage_PlayerInput:
		mh.handlePlayerInput(payload.PlayerInput)
	case *pb.GameMessage_PlayerAttackAnim:
		mh.handlePlayerAttackAnim(message)
	case *pb.GameMessage_SetPlayerAttack:
		mh.handleSetPlayerAttack(message)
	case *pb.GameMessage_MonsterSpawn:
		mh.handleMonsterSpawn(payload.MonsterSpawn, *playerId)
	case *pb.GameMessage_MonsterAnim:
		mh.handleMonsterAnim(payload.MonsterAnim)
	case *pb.GameMessage_MonsterAction:
		mh.handleMonsterAction(payload.MonsterAction)
	case *pb.GameMessage_MonsterTakeDamage:
		mh.handleMonsterTakeDamage(payload.MonsterTakeDamage)
	default:
		log.Printf("메시지 처리가 정의되지 않았음: %T", payload)
	}
}
func (mh *MessageHandler) handlePing(conn net.Conn) {
	log.Printf("핑 수신")
	nm := mh.mcx.NetManager()
	response := &pb.GameMessage{
		Payload: &pb.GameMessage_Pong{
			Pong: &pb.Pong{},
		},
	}
	nm.SendMessage(response, conn)
	log.Printf("퐁 발신")
}

func (mh *MessageHandler) handleLoginRequest(request *pb.LoginRequest, conn net.Conn, playerId *string) {
	log.Printf("로그인 요청 수신 (ID: %s, nickname: %s, gameUserID: %s)", request.Id, request.Nickname, request.GameUserId)
	nm := mh.mcx.NetManager()
	am := mh.mcx.AccountManager()

	// 계정 생성 시도
	account, err := am.CreateAccount(request.Id, request.Nickname, request.GameUserId, conn)
	if err != nil {
		log.Printf("handleLoginRequest: 계정 생성 실패 (%v)", err)
		message := &pb.GameMessage{
			Payload: &pb.GameMessage_LoginResponse{
				LoginResponse: &pb.LoginResponse{
					Success:      false,
					ErrorMessage: err.Error(),
				},
			},
		}
		nm.SendMessage(message, conn)
		return
	}

	mh.mcx.SetHostId(account.ID)

	// 계정을 온라인 상태로 설정
	am.SetPlayerOnline(account.ID, conn)

	// 로그인 성공 응답 메시지 생성
	response := &pb.GameMessage{
		Payload: &pb.GameMessage_LoginResponse{
			LoginResponse: &pb.LoginResponse{
				Success:    true,
				PlayerId:   account.ID,
				Username:   account.Name,
				GameUserId: account.GameUserID,
			},
		},
	}

	// 로그인 성공 응답 전송
	nm.SendMessage(response, conn)

	// 플레이어 ID 업데이트
	*playerId = account.ID

	systemMessage := &pb.GameMessage{
		Payload: &pb.GameMessage_ChatMessage{
			ChatMessage: &pb.ChatMessage{
				System:  true,
				Message: fmt.Sprintf("%s 님이 게임에 참여했습니다.", account.Name),
			},
		},
	}
	nm.SendMessageToAll(systemMessage)
	log.Printf("Login: 현재 서버에 로그인된 인원: %v", mh.mcx.PlayerCount)
}

func (mh *MessageHandler) handleLogoutRequest(conn net.Conn, playerId *string) {
	if *playerId == "" {
		log.Printf("handleLogoutRequest: PlayerId is empty")
		return
	}
	log.Printf("로그아웃 요청 수신 (ID: %s)", *playerId)
	am := mh.mcx.AccountManager()

	am.SetPlayerOffline(*playerId)
	mh.mcx.PlayerLoggedOut()

	// nm := mh.mcx.NetManager()
	// am := mh.mcx.AccountManager()

	// message := &pb.GameMessage{
	// 	Payload: &pb.GameMessage_LogoutResponse{
	// 		LoginResponse: &pb.LoginResponse{
	// 			Success:  true,
	// 			PlayerId: request.Id,
	// 			Username: request.Nickname,
	// 		},
	// 	},
	// }

	// nm.SendMessage(message, conn)
	log.Printf("Logout: 현재 서버에 로그인된 인원: %v", mh.mcx.PlayerCount)
}

func (mh *MessageHandler) handleGameStart() {
	am := mh.mcx.AccountManager()
	nm := mh.mcx.NetManager()
	// 기존 온라인 플레이어들의 정보 수집
	onlineAccounts := am.GetOnlineAccounts()
	existingPlayers := []*pb.PlayerInfo{}
	for _, acc := range onlineAccounts {
		existingPlayers = append(existingPlayers, &pb.PlayerInfo{
			Id:       acc.ID,
			Nickname: acc.Name,
		})
	}

	// 플레이어 정보 전송
	spawnMessageForNewPlayer := &pb.GameMessage{
		Payload: &pb.GameMessage_PlayerSpawn{
			PlayerSpawn: &pb.PlayerSpawn{
				// PlayerId: account.ID,
				// Nickname: account.Name,
				Players: existingPlayers,
			},
		},
	}
	nm.SendMessageToAll(spawnMessageForNewPlayer)
}

func (mh *MessageHandler) handleChatMessage(msg *pb.GameMessage) {
	nm := mh.mcx.NetManager()

	nm.SendMessageToAll(msg)
}

func (mh *MessageHandler) handlePlayerInput(msg *pb.PlayerInput) {
	nm := mh.mcx.NetManager()

	inputMessage := &pb.GameMessage{
		Payload: &pb.GameMessage_PlayerInput{
			PlayerInput: msg,
		},
	}

	nm.SendMessageToAllExcept(inputMessage, msg.PlayerId)
}

func (mh *MessageHandler) handlePlayerAttackAnim(msg *pb.GameMessage) {
	nm := mh.mcx.NetManager()

	nm.SendMessageToAll(msg)
}

func (mh *MessageHandler) handleSetPlayerAttack(msg *pb.GameMessage) {
	nm := mh.mcx.NetManager()

	nm.SendMessageToAll(msg)
}

func (mh *MessageHandler) handleMonsterAnim(request *pb.MonsterAnim) {
	log.Printf("몬스터 애니메이션 스테이트 변경 요청 수신")
	nm := mh.mcx.NetManager()

	message := &pb.GameMessage{
		Payload: &pb.GameMessage_MonsterAnim{
			MonsterAnim: request,
		},
	}
	nm.SendMessageToAll(message)
}

func (mh *MessageHandler) handleMonsterSpawn(request *pb.MonsterSpawn, playerId string) {
	log.Printf("몬스터 스폰 요청 수신")
	nm := mh.mcx.NetManager()
	mm := mh.mcx.MonsterManager()
	monsterUUID, err := uuid.NewV4()
	var monsterId string
	if err != nil {
		log.Printf("handleMonsterSpawn: MonsterId 생성 실패: %v\n", err)
		monsterId = "monster01"
	} else {
		monsterId = monsterUUID.String()
	}

	monster := &manager.Monster{
		MonsterId: monsterId,
		MaxHp:     100,
		CurrentHp: 100,
		Position:  &pb.GoVector3{X: 0, Y: 0, Z: 0}, // 기본 스폰 위치
		Rotation:  &pb.GoVector3{X: 0, Y: 0, Z: 0}, // 기본 회전값
	}

	mm.AddMonster(monster, playerId)

	request.MonsterId = monsterId

	message := &pb.GameMessage{
		Payload: &pb.GameMessage_MonsterSpawn{
			MonsterSpawn: request,
		},
	}

	nm.SendMessageToAll(message)
}

func (mh *MessageHandler) handleMonsterAction(request *pb.MonsterAction) {
	nm := mh.mcx.NetManager()
	var message *pb.GameMessage

	switch request.ActionType {
	case pb.ActionType_MONSTER_ACTION_SET_STATUS:
		// log.Printf("몬스터 스테이터스 변경 수신")
		message = &pb.GameMessage{
			Payload: &pb.GameMessage_MonsterAction{
				MonsterAction: request,
			},
		}
	case pb.ActionType_MONSTER_ACTION_SET_TARGET:
		// log.Printf("몬스터 타겟 변경 수신")
		message = &pb.GameMessage{
			Payload: &pb.GameMessage_MonsterAction{
				MonsterAction: request,
			},
		}
	case pb.ActionType_MONSTER_ACTION_SET_DESTINATION:
		// log.Printf("몬스터 목적지 변경 수신")
		message = &pb.GameMessage{
			Payload: &pb.GameMessage_MonsterAction{
				MonsterAction: request,
			},
		}
	}

	nm.SendMessageToAll(message)
}

func (mh *MessageHandler) handleMonsterTakeDamage(request *pb.MonsterTakeDamage) {
	mm := mh.mcx.MonsterManager()
	mm.TakeDamage(request)
}
