using System;
namespace ServerPMS.Infrastructure.ClientCommunication
{
    public enum CID : byte
    {
        LOGIN = 0x00,
        GET_SNAPSHOT = 0x10,
        SUBSCRIBE_FEED = 0xF1,
        UNSUBSCRIBE_FEED = 0xF2,
        GET_DATA_SYSTEM = 0x40,
        SET_DATA_ORDERS = 0x81,
        SET_DATA_QUEUES = 0x82,
        SET_DATA_UNITS = 0x83,
        SET_DATA_SYSTEM = 0x84,
        DATA_DELIVERY = 0xDD,
        HEARTBEAT = 0xEA,
        AKC = 0xAA,
        NACK = 0xAB,
        LOGOUT = 0xFF
    }
}

