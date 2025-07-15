// PMS Project V1.0
// LSData - all rights reserved
// IDBIdentifiable.cs
//
//
using System;
using DocumentFormat.OpenXml.Wordprocessing;

namespace ServerPMS
{
    public interface IDBIdentifiable
    {
        int DBId { set; get; }
    }
}

