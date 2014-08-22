using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iContrAll.TcpServer
{
    public enum MessageType
    {
        IdentityMsg = -1,
        DebugMsg = 0,
        RadioMsg = 1,
        QueryIPSettings = 2,
        AnswerIPSettings = 3,
        QueryServerSettings = 4,
        AnswerServerSettings = 5,
        QueryDeviceList = 6,
        AnswerDeviceList= 7,
        SetDeviceList = 8,
        LoginRequest = 14,
        AnswerToLoginRequest = 15,
        QueryDeviceDetails = 16,
        AnswerDeviceDetails = 17,
        QueryMessageHistory = 18,
        AddDevice = 21,
        DelDevice = 22,
        CmdSetPassword = 23,
        eCmdGetPlaceList = 24,
        ePlaceList = 25,
        eCmdAddPlace = 26,
        eCmdDelPlace = 27,
        eCmdAddDeviceToPlace = 28,
        eCmdDelDeviceFromPlace = 29,
        eCmdRenamePlace = 30,
        eGetActionLists = 31,
        eActionLists = 32,
        eCmdAddActionList = 33,
        eCmdAddActionToActionList = 34,
        eCmdDelActionList = 35,
        eCmdDelActionFromActionList = 36,
        eCmdExecActionList = 37
    }

    public class Message
    {
        private MessageType type;
        public MessageType Type { get { return type; } }

        private int length;
        public int Length { get { return length; } }

        private string content;
        public string Content { get { return content; } }

        public Message(MessageType type, int length, string content)
        {
            this.type = type;
            this.length = length;
            this.content = content;
        }

        public Message(int type, int length, string content)
        {
            this.type = (MessageType)type;
            this.length = length;   
            this.content = content;
        }
    }
}
