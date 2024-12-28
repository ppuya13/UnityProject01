package manager

//세션 내 몬스터의 상태 변화와 위치 등을 관리

import (
	pb "Server/messages"
	"log"
	"sync"
)

var (
	monsterManager *MonsterManager
	monsterOnce    sync.Once
)

type MonsterManager struct {
	mcx *ManagerContext
	mu  sync.RWMutex
}

type Monster struct {
	MaxHp     float32
	CurrentHp float32
	MonsterId string
	targetId  string
	targetPos *pb.GoVector3
	Position  *pb.GoVector3
	// Velocity  *pb.GoVector3
	Rotation *pb.GoVector3
	// state    *pb.MonsterState
	mu sync.RWMutex
}

func newMonsterManager(ctx *ManagerContext) *MonsterManager {
	monsterOnce.Do(func() {
		monsterManager = &MonsterManager{
			mcx: ctx,
		}
	})
	return monsterManager
}

func (mm *MonsterManager) AddMonster(monster *Monster, playerId string) {
	am := mm.mcx.AccountManager()

	account, exists := am.GetAccount(playerId)
	if !exists {
		log.Printf("AddMonster: 몬스터 추가 실패(계정을 찾을 수 없음)")
		return
	}

	//이 시연용 게임엔 세션같은건 없고, 몬스터를 관리할 객체를 만들기 귀찮으므로 그냥 계정에 넣어서 관리한다.
	account.monster = monster
}

func (mm *MonsterManager) TakeDamage(request *pb.MonsterTakeDamage) {
	mm.mu.Lock()
	defer mm.mu.Unlock()

	am := mm.mcx.AccountManager()
	nm := mm.mcx.NetManager()

	account, exists := am.GetAccount(mm.mcx.HostId)
	if !exists {
		log.Printf("TakeDamage: 데미지 계산 실패(계정을 찾을 수 없음)")
		return
	}

	//이 시연용 게임엔 세션같은건 없고, 몬스터를 관리할 객체를 만들기 귀찮으므로 그냥 계정에 넣어서 관리한다.
	account.monster.CurrentHp -= request.Damage

	request.CurrentHp = account.monster.CurrentHp

	message := &pb.GameMessage{
		Payload: &pb.GameMessage_MonsterTakeDamage{
			MonsterTakeDamage: request,
		},
	}

	nm.SendMessageToAll(message)
}
