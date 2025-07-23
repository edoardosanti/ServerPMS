using System;
namespace ServerPMS.Infrastructure.ClientCommunication
{
    public enum CID : byte
    {
        LOGIN = 0x00,
        GET_DATA_ORDERS = 0x01,
        GET_DATA_QUEUES = 0x02,
        GET_DATA_UNITS = 0x03,
        GET_DATA_SYSTEM = 0x04,
        SET_DATA_ORDERS = 0x11,
        SET_DATA_QUEUES = 0x12,
        SET_DATA_UNITS = 0x13,
        SET_DATA_SYSTEM = 0x14,
        DATA_DELIVERY = 0x20,
        HEARTBEAT = 0xA0,
        AKC = 0xAA,
        NACK = 0xAB,
        LOGOUT = 0xFF
    }
}

