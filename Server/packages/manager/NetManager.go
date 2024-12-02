package manager

import (
	pb "Server/messages"
	"encoding/binary"
	"fmt"
	"log"
	"net"
	"sync"

	"google.golang.org/protobuf/proto"
)

var (
	netManager *NetManager
	netOnce    sync.Once
)

type NetManager struct {
	mcx *ManagerContext
	// mu  sync.RWMutex
}

func newNetManager(ctx *ManagerContext) *NetManager {
	netOnce.Do(func() {
		netManager = &NetManager{
			mcx: ctx,
		}
	})
	return netManager
}

func (nm *NetManager) SendMessage(msg *pb.GameMessage, conn net.Conn) error {
	response, err := proto.Marshal(msg)
	if err != nil {
		log.Printf("Failed to marshal message: %v", err)
		return fmt.Errorf("failed to marshal message: %w", err)
	}

	lengthBuf := make([]byte, 4)
	binary.LittleEndian.PutUint32(lengthBuf, uint32(len(response)))
	lengthBuf = append(lengthBuf, response...)

	_, err = conn.Write(lengthBuf)
	if err != nil {
		log.Printf("Failed to send message: %v", err)
		return fmt.Errorf("failed to send message: %w", err)
	}

	return nil
	// log.Printf("Message sent successfully: Type=%v", msg.Type)
}

func (nm *NetManager) SendMessageToAll(msg *pb.GameMessage) error {
	am := nm.mcx.AccountManager()
	accounts := am.GetOnlineAccounts()

	var firstErr error
	for _, account := range accounts {
		if err := nm.SendMessage(msg, account.Conn); err != nil {
			log.Printf("Failed to send message to %s: %v", account.ID, err)
			am.SetPlayerOffline(account.ID)
			if firstErr == nil {
				firstErr = err
			}
		}
	}
	return firstErr
}

func (nm *NetManager) SendMessageToAllExcept(msg *pb.GameMessage, excludeID string) error {
	am := nm.mcx.AccountManager()
	accounts := am.GetOnlineAccounts()

	var firstErr error
	for _, account := range accounts {
		if account.ID == excludeID {
			continue
		}
		if err := nm.SendMessage(msg, account.Conn); err != nil {
			log.Printf("Failed to send message to %s: %v", account.ID, err)
			am.SetPlayerOffline(account.ID)
			if firstErr == nil {
				firstErr = err
			}
		}
	}
	return firstErr
}
