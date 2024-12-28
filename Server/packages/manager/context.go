package manager

type ManagerContext struct {
	HostId      string
	PlayerCount int
	am          *AccountManager
	mm          *MonsterManager
	nm          *NetManager
}

func NewManagerContext() *ManagerContext {
	mcx := &ManagerContext{}
	mcx.am = newAccountManager(mcx)
	mcx.mm = newMonsterManager(mcx)
	mcx.nm = newNetManager(mcx)
	return mcx
}

func (ctx *ManagerContext) PlayerLoggedOut() {
	ctx.PlayerCount--
	if ctx.PlayerCount == 0 {
		ctx.HostId = ""
	}
}

func (ctx *ManagerContext) GetHostId() string {
	return ctx.HostId
}

func (ctx *ManagerContext) SetHostId(playerID string) {
	if ctx.PlayerCount == 0 {
		ctx.HostId = playerID
	}
	ctx.PlayerCount++
}

func (mc *ManagerContext) AccountManager() *AccountManager {
	return mc.am
}

func (mc *ManagerContext) MonsterManager() *MonsterManager {
	return mc.mm
}

func (mc *ManagerContext) NetManager() *NetManager {
	return mc.nm
}
