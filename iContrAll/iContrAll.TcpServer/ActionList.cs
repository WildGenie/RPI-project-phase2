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

    class ActionEntity
    {
        private string deviceId;
        public string DeviceId
        {
            get { return deviceId; }
            set { deviceId = value; }
        }

        private int deviceChannel;
        public int DeviceChannel
        {
            get { return deviceChannel; }
            set { deviceChannel = value; }
        }


        private int actionTypeId;
        public int ActionTypeId
        {
            get { return actionTypeId; }
            set { actionTypeId = value; }
        }

        private int order;
        public int Order
        {
            get { return order; }
            set { order = value; }
        }

        //private Guid actionListId;
        //public Guid ActionListId
        //{
        //    get { return actionListId; }
        //    set { actionListId = value; }
        //}


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

        private List<ActionEntity> actions;
        public List<ActionEntity> Actions
        {
            get { return actions; }
            set { actions = value; }
        }

    }
}
