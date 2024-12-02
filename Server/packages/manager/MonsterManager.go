package manager

//세션 내 몬스터의 상태 변화와 위치 등을 관리

import (
	pb "Server/messages"
	"sync"
)

var (
	monsterManager *MonsterManager
	monsterOnce    sync.Once
)

type MonsterManager struct {
	mcx *ManagerContext
}

type Monster struct {
	MonsterId string
	targetId  string
	targetPos *pb.GoVector3
	position  *pb.GoVector3
	// velocity  *pb.GoVector3
	rotation *pb.GoVector3
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
