// PMS Project V1.0
// LSData - all rights reserved
// SDT.cs
//
//
using System;
namespace ServerPMS
{
    public struct SDT
    {
        string Author;
        string Version;
        string PackageName;
   
        public SDT(string author, string version, string packageName)
        {
            Author = author;
            Version = version;
            PackageName = packageName;
        }
    }
}

