// PMS Project V1.0
// LSData - all rights reserved
// ICloneableGeneric.cs
//
//
using System;
namespace ServerPMS
{
    public interface ICloneable<T>
    {
        T Clone();
    }
}

