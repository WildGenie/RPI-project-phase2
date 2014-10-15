using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace iContrAll.RemoteServer
{
    public enum MessageType
    {
        LoginRequest = 14,
        IdentityMsg = -1,
        CreateThreadFor = 63,
        Ping = 251
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
