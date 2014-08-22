using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace iContrAll.TcpServer
{
    class ActionType
    {
        public int Id { get; set; }

        private string deviceType;
        public string DeviceType
        {
            get { return deviceType; }
            set { deviceType = value; }
        }

        private string name;
        public string Name
        {
            get { return name; }
            set { name = value; }
        }
    }

    class Action
    {
        private string deviceId;
        public string DeviceId
        {
            get { return deviceId; }
            set { deviceId = value; }
        }

        private string actionTypeName;
        public string ActionTypeName
        {
            get { return actionTypeName; }
            set { actionTypeName = value; }
        }

        private int order;
        public int Order
        {
            get { return order; }
            set { order = value; }
        }

        private Guid actionListId;
        public Guid ActionListId
        {
            get { return actionListId; }
            set { actionListId = value; }
        }


    }

    class ActionList
    {
        private Guid id;
        public Guid Id
        {
            get { return id; }
            set { id = value; }
        }

        private string name;
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        private List<Action> actions;
        public List<Action> Actions
        {
            get { return actions; }
            set { actions = value; }
        }

    }
}
