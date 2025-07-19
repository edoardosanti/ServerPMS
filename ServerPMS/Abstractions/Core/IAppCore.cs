using System;
using ServerPMS.Infrastructure.External;

namespace ServerPMS.Abstractions.Core
{
    public interface IAppCore
    {

        void InitializeWALEnviroment();
        Task WALReplay();
        string ToInfo();
        //void ImportOrdersFromExcelFile(string filename, ExcelOrderParserParams parserParams);

    }
}
