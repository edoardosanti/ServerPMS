// PMS Project V1.0
// LSData - all rights reserved
// ICloneableGeneric.cs
//
//
using System;
namespace ServerPMS.Infrastructure.Generic
{
    public interface ICloneable<T>
    {
        T Clone();
    }
}

