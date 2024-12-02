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
func (mh *MessageHandler) ProcessMessage(message *pb.GameMessage, conn net.Conn, playerId *string) {
	switch payload := message.Payload.(type) {
	case *pb.GameMessage_LoginRequest:
		mh.handleLoginRequest(payload.LoginRequest, conn, playerId)
	case *pb.GameMessage_LogoutRequest:
		mh.handleLogoutRequest(conn, playerId)
	case *pb.GameMessage_MonsterSpawn:
		mh.handleMonsterSpawn(payload.MonsterSpawn)
	case *pb.GameMessage_MonsterAnim:
		mh.handleMonsterAnim(payload.MonsterAnim)
	case *pb.GameMessage_MonsterAction:
		mh.handleMonsterAction(payload.MonsterAction)
	default:
		//log.Printf("Unexpected message type received: %T", payload)
	}
}

func (mh *MessageHandler) handleLoginRequest(request *pb.LoginRequest, conn net.Conn, playerId *string) {
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

	account, err := am.CreateAccount(request.Id, request.Nickname, request.GameUserId, conn)
	if err != nil {
		log.Printf("handleLoginRequest: 계정 생성 실패 (%v)", err)
		message.GetLoginResponse().Success = false
		message.GetLoginResponse().ErrorMessage = err.Error()
	}
	nm.SendMessage(message, conn)

	*playerId = account.ID
}
func (mh *MessageHandler) handleLogoutRequest(conn net.Conn, playerId *string) {
	if *playerId == "" {
		log.Printf("handleLogoutRequest: PlayerId is empty")
		return
	}
	log.Printf("로그아웃 요청 수신 (ID: %s)", *playerId)
	am := mh.mcx.AccountManager()

	am.SetPlayerOffline(*playerId)
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

func (mh *MessageHandler) handleMonsterSpawn(request *pb.MonsterSpawn) {
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
}

func (mh *MessageHandler) handleMonsterAction(request *pb.MonsterAction) {
	log.Printf("몬스터 액션 수신")
	nm := mh.mcx.NetManager()
	var message *pb.GameMessage

	switch request.ActionType {
	case pb.ActionType_MONSTER_ACTION_SET_STATUS:
		message = &pb.GameMessage{
			Payload: &pb.GameMessage_MonsterAction{
				MonsterAction: request,
			},
		}
	}

	nm.SendMessageToAll(message)
}
