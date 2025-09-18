using System;
using ServerPMS.Infrastructure.External;
using ServerPMS.Abstractions.Managers;

namespace ServerPMS.Abstractions.Core
{
    public interface IAppCore
    {
        IIntegratedEventsManager IEM { get; }
        public IOrdersManager OrdersManager { get; }
        public IUnitsManager UnitsManger { get; }
        public IQueuesManager QueuesManager { get; }
        void InitializeWALEnviroment();
        Task InitializeManagersAsync();
        Task WALReplayAsync();
        string ToInfo();
        //void ImportOrdersFromExcelFile(string filename, ExcelOrderParserParams parserParams);

    }
}
