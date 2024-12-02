package handler

import (
	pb "Server/messages"
	"Server/packages/manager"
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
func (mh *MessageHandler) ProcessMessage(message *pb.GameMessage, conn *net.Conn) {
	switch payload := message.Payload.(type) {
	case *pb.GameMessage_LoginRequest:
		mh.handleLoginRequest(payload.LoginRequest, conn)
	case *pb.GameMessage_MonsterSpawn:
		mh.handleMonsterSpawn(payload.MonsterSpawn, conn)
	case *pb.GameMessage_MonsterAction:
		mh.handleMonsterAction(payload.MonsterAction, conn)
	default:
		//log.Printf("Unexpected message type received: %T", payload)
	}
}

func (mh *MessageHandler) handleLoginRequest(request *pb.LoginRequest, conn *net.Conn) {
	log.Printf("로그인 요청 수신 (ID: %s, nickname: %s, gameUserID: %s)", request.Id, request.Nickname, request.GameUserId)
	nm := mh.mcx.NetManager()
	am := mh.mcx.AccountManager()
	message := &pb.GameMessage{
		Payload: &pb.GameMessage_LoginResponse{
			LoginResponse: &pb.LoginResponse{
				Success:  true,
				PlayerId: request.Id,
				Username: request.Nickname,
			},
		},
	}

	err := am.CreateAccount(request.Id, request.Nickname, request.GameUserId, conn)
	if err != nil {
		log.Printf("handleLoginRequest: 계정 생성 실패 (%v)", err)
		message.GetLoginResponse().Success = false
		message.GetLoginResponse().ErrorMessage = err.Error()
	}

	log.Printf("로그인 결과 발신")
	nm.SendMessage(message, conn)
}

func (mh *MessageHandler) handleMonsterSpawn(request *pb.MonsterSpawn, conn *net.Conn) {
	log.Printf("몬스터 스폰 요청 수신")
	nm := mh.mcx.NetManager()
	monsterUUID, err := uuid.NewV4()
	var monsterId string
	if err != nil {
		log.Printf("handleMonsterSpawn: MonsterId 생성 실패: %v\n", err)
		monsterId = "monster01"
	} else {
		monsterId = monsterUUID.String()
	}

	request.MonsterId = monsterId

	message := &pb.GameMessage{
		Payload: &pb.GameMessage_MonsterSpawn{
			MonsterSpawn: request,
		},
	}

	nm.SendMessageToAll(message)
	log.Printf("몬스터 스폰 요청 발신")
}

func (mh *MessageHandler) handleMonsterAction(request *pb.MonsterAction, conn *net.Conn) {
	switch request.ActionType {
	case pb.ActionType_MONSTER_ACTION_SET_STATUS:

	}
}
