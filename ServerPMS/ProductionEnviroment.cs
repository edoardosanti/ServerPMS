// PMS Project V1.0
// LSData - all rights reserved
// ProductionEnviroment.cs
//
//
using System;
using DocumentFormat.OpenXml.Drawing.Diagrams;

namespace ServerPMS
{
    public class ProductionEnviroment
    {
        List<ProductionUnit> prodUnits;

        public List<ProductionUnit> Units => prodUnits;

        public ProductionEnviroment()
        {
            prodUnits = new List<ProductionUnit>();
        }

        public void AllUnitOperation(UnitState newState)
        {
            prodUnits.ForEach(x =>
            {
                if (newState == UnitState.Standby)
                    x.Stop();
                else if (newState == UnitState.Running)
                    x.Start();
                else
                    x.ChangeOver();
            });
            
        }

        public int AddUnit(int dbid,UnitType type, string notes,params int[] ordersId)
        {
            prodUnits.Add(new ProductionUnit(dbid, type, notes, ordersId));
            return prodUnits.Last().DBId;
        }

        public bool RemoveUnit(int id)
        {
            return prodUnits.RemoveAll(x => x.DBId == id)>0? true:false;
        }
    }
}

