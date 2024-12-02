package manager

type ManagerContext struct {
	am *AccountManager
	mm *MonsterManager
	nm *NetManager
}

func NewManagerContext() *ManagerContext {
	mcx := &ManagerContext{}
	mcx.am = newAccountManager(mcx)
	mcx.mm = newMonsterManager(mcx)
	mcx.nm = newNetManager(mcx)
	return mcx
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
