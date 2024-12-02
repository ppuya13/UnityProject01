package main

import (
	"encoding/binary"
	"flag"
	"fmt"
	"log"
	"net"

	// Protocol Buffers 라이브러리를 임포트
	"google.golang.org/protobuf/proto"

	// Protocol Buffers 메시지 정의를 임포트
	pb "Server/messages"

	// 게임 매니저 패키지를 임포트
	hd "Server/packages/handler"
	mg "Server/packages/manager"
)

var (
	mc *mg.ManagerContext
	mh *hd.MessageHandler
)

func main() {
	port := flag.Int("port", 8888, "port to listen on")
	flag.Parse()

	mc = mg.NewManagerContext()
	mh = hd.NewMessageHandler(mc)

	// TCP 서버를 8888 포트에서 시작(이제 포트를 받아옴)
	listener, err := net.Listen("tcp", fmt.Sprintf(":%d", *port)) //tcp 리스너 생성
	if err != nil {                                               // 에러 체크
		log.Fatalf("Failed to listen: %v", err) // 실패 시 로그 출력 후 종료
	}
	defer listener.Close()                            // 함수 종료 시 리스너 닫기
	fmt.Printf("Server is listening on :%d\n", *port) // 서버 시작 메시지 출력

	// 무한 루프로 클라이언트의 연결을 받음
	for {
		conn, err := listener.Accept() // 클라이언트 연결 수락
		if err != nil {                // 에러 체크
			log.Printf("Failed to accept connection: %v", err) // 연결 실패 시 로그 출력
			continue                                           // 다음 연결 시도
		}
		// 각 연결에 대해 별도의 고루틴으로 처리
		go handleConnection(conn) // 연결 처리 함수를 고루틴으로 실행
	}
}

// handleConnection 함수: 개별 클라이언트 연결을 처리
func handleConnection(conn net.Conn) {
	defer conn.Close() // 함수 종료 시 연결 닫기

	// // 인증 처리
	// if !authenticateClient(client) {
	// 	log.Printf("Authentication failed for client")
	// 	return
	// }

	for {
		// 메시지 길이를 먼저 읽음 (4바이트)
		lengthBuf := make([]byte, 4)   // 4바이트 버퍼 생성
		_, err := conn.Read(lengthBuf) // 버퍼에 데이터 읽기
		if err != nil {                // 에러 체크
			log.Printf("Failed to read message length: %v", err) // 실패 시 로그 출력
			return                                               // 함수 종료
		}
		// 리틀 엔디안으로 메시지 길이를 해석
		length := binary.LittleEndian.Uint32(lengthBuf) // 바이트를 uint32로 변환

		// 메시지 본문을 읽음
		messageBuf := make([]byte, length) // 메시지 길이만큼의 버퍼 생성
		_, err = conn.Read(messageBuf)     // 버퍼에 데이터 읽기
		if err != nil {                    // 에러 체크
			log.Printf("Failed to read message body: %v", err) // 실패 시 로그 출력
			return                                             // 함수 종료
		}

		// Protocol Buffers 메시지를 파싱
		message := &pb.GameMessage{}               // GameMessage 구조체 인스턴스 생성
		err = proto.Unmarshal(messageBuf, message) // 바이트 슬라이스를 구조체로 변환
		if err != nil {                            // 에러 체크
			log.Printf("Failed to unmarshal message: %v", err) // 실패 시 로그 출력
			continue                                           // 다음 메시지 처리로 넘어감
		}

		// 메시지를 처리
		mh.ProcessMessage(message, &conn) // 메시지 처리 함수 호출
	}
}
