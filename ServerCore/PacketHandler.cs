using System;
using System.Collections.Generic;
using System.Text;

namespace ServerCore
{
    public class PacketHandler
    {
        public static void C_ChatHandler(Session session, ArraySegment<byte> buffer)
        {
            S_Chat chat = new S_Chat();
            chat.Read(buffer);

            ArraySegment<byte> sendBuffer = chat.Write();

            SessionManager.Instance.FindClientSession(session.sessionId).Send(sendBuffer);
        }

        public static void S_ChatHandler(Session session, ArraySegment<byte> buffer)
        {
            C_Chat chat = new C_Chat();
            chat.Read(buffer);

            Console.WriteLine($"[전체채팅] {chat.playerId}: {chat.chat}");
        }

        public static void C_FileRequestHandler(Session session, ArraySegment<byte> buffer)
        {
            C_FileRequest request = new C_FileRequest();
            request.Read(buffer);

            ArraySegment<byte> file = FileContainer.Instance.GetFile(request.seek, request.fileName);
            S_FileResponse response = new S_FileResponse(request.fileName, file);
            ArraySegment<byte> sendBuffer = response.Write();

            SessionManager.Instance.FindClientSession(session.sessionId).Send(sendBuffer);

            response = null;
        }

        public static void S_FileResponseHandler(Session session, ArraySegment<byte> buffer)
        {
            S_FileResponse response = new S_FileResponse();
            response.Read(buffer);

            FileRoom container = SessionManager.Instance.FindServerSession(session.sessionId).fileRoom;
            container.Alloc(response.originSize);
            container.AddData(response);

            Console.WriteLine($"{response.fileName} 다운로드 상황 {container.seek} / {container.fileSize}");

            if (!container.IsFull()) // 한 번에 데이터를 못 받은 경우 뒷 부분에 이어서 전송시켜준다.
            {
                C_FileRequest request = new C_FileRequest(response.fileName, container.seek);
                SessionManager.Instance.FindServerSession(session.sessionId).Send(request.Write());
            }

        }
    }
}
