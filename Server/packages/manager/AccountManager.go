package manager

//플레이어 정보 관리(CRUD)
//플레이어 인증 및 권한 관리

import (
	"errors"
	"net"
	"sync"
)

var (
	accountManager *AccountManager
	accountOnce    sync.Once
)

type Account struct {
	ID         string
	Name       string //닉네임
	GameUserID string
	Password   string //해시해야한다.
	Conn       net.Conn
}

type AccountManager struct {
	mcx      *ManagerContext
	accounts map[string]*Account
	mu       sync.RWMutex
}

func newAccountManager(ctx *ManagerContext) *AccountManager {
	accountOnce.Do(func() {
		accountManager = &AccountManager{
			mcx:      ctx,
			accounts: make(map[string]*Account),
		}
	})
	return accountManager
}

func (am *AccountManager) GetOnlineAccounts() []*Account {
	am.mu.RLock()
	defer am.mu.RUnlock()

	onlineAccounts := make([]*Account, 0)
	for _, account := range am.accounts {
		if account.Conn != nil {
			onlineAccounts = append(onlineAccounts, account)
		}
	}
	return onlineAccounts
}

// func (am *AccountManager) Authenticate(id, password string) bool {
// 	am.mu.RLock()
// 	account, exists := am.accounts[id]
// 	am.mu.RUnlock()

// 	if !exists {
// 		return false
// 	}

// 	// 실제 구현에서는 해시된 비밀번호를 비교해야 한다.
// 	if account.Password == password {
// 		am.mu.Lock()
// 		defer am.mu.Unlock()
// 		newStatus := pb.AccountStatus_ACCOUNT_IDLE // 계정 정보 초기화
// 		account.Status = &newStatus
// 		account.IsConfirm = false
// 		return true
// 	}
// 	return false
// }

// // 계정 생성
func (am *AccountManager) CreateAccount(id, name, gameUserID string, conn net.Conn) (*Account, error) {
	am.mu.Lock()
	defer am.mu.Unlock()

	if _, exists := am.accounts[id]; exists {
		return nil, errors.New("account already exists")
	}

	account := &Account{
		ID:         id,
		Name:       name,
		GameUserID: gameUserID,
		Conn:       conn,
	}

	am.accounts[id] = account
	return account, nil
}

// id로 계정 조회
func (am *AccountManager) GetAccount(id string) (*Account, bool) {
	am.mu.RLock()
	defer am.mu.RUnlock()

	account, exists := am.accounts[id]

	return account, exists
}

// 계정 업데이트
func (am *AccountManager) UpdateAccount(account *Account) {
	am.mu.Lock()
	defer am.mu.Unlock()

	if _, exists := am.accounts[account.ID]; exists {
		am.accounts[account.ID] = account
	}
}

// 계정 제거
func (am *AccountManager) RemoveAccount(id string) {
	am.mu.Lock()
	defer am.mu.Unlock()

	delete(am.accounts, id)
}

// 모든 계정의 배열을 반환
func (am *AccountManager) ListAccounts() []*Account {
	am.mu.RLock()
	defer am.mu.RUnlock()

	accounts := make([]*Account, 0, len(am.accounts))
	for _, account := range am.accounts {
		accounts = append(accounts, account)
	}
	return accounts
}

// 해당 ID의 플레이어가 접속중인지 bool값 반환
func (am *AccountManager) IsPlayerOnline(playerID string) bool {
	am.mu.RLock()
	defer am.mu.RUnlock()
	if account, exists := am.accounts[playerID]; exists {
		return account.Conn != nil
	}
	return false
}

// 계정을 온라인 상태로 설정
func (am *AccountManager) SetPlayerOnline(id string, conn net.Conn) bool {
	am.mu.Lock()
	defer am.mu.Unlock()

	account, exists := am.accounts[id]
	if !exists || account.Conn != nil {
		return false
	}

	account.Conn = conn
	return true
}

// 계정을 오프라인 상태로 설정
func (am *AccountManager) SetPlayerOffline(id string) {
	am.mu.Lock()
	defer am.mu.Unlock()

	if account, exists := am.accounts[id]; exists {
		account.Conn = nil
	}
}

// func (am *AccountManager) SetSessionID(playerID, sessionID string) error {
// 	am.mu.Lock()
// 	defer am.mu.Unlock()

// 	account, exists := am.accounts[playerID]
// 	if !exists {
// 		return errors.New("player not found")
// 	}

// 	account.SessionID = sessionID
// 	return nil
// }

// func (am *AccountManager) SetAccountStatus(id string, status pb.AccountStatus) error {
// 	am.mu.Lock()
// 	defer am.mu.Unlock()
// 	account, exists := am.accounts[id]
// 	if !exists {
// 		return errors.New("account not found")
// 	}
// 	account.Status = &status
// 	return nil
// }

// func (am *AccountManager) GetAccountStatus(id string) (pb.AccountStatus, error) {
// 	am.mu.Lock()
// 	defer am.mu.Unlock()
// 	account, exists := am.accounts[id]
// 	if !exists {
// 		return pb.AccountStatus_ACCOUNT_UNKNOWN, errors.New("account not found")
// 	}

// 	if account.Status == nil {
// 		return pb.AccountStatus_ACCOUNT_UNKNOWN, nil
// 	}

// 	return *account.Status, nil
// }
