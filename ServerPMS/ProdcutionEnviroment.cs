// PMS Project V1.0
// LSData - all rights reserved
// ProdcutionEnviroment.cs
//
//
using System;

namespace ServerPMS
{
    public class ProdcutionEnviroment
    {
        List<ProductionUnit> prodUnits;

        public List<ProductionUnit> Units => prodUnits;

        public ProdcutionEnviroment()
        {
            prodUnits = new List<ProductionUnit>();

        }

        public void AddUnit(UnitType type, string notes,params ProductionOrder[] orders)
        {
            prodUnits.Add(new ProductionUnit(type, notes,orders));
        }

        public bool RemoveUnit(int id)
        {
            return prodUnits.RemoveAll(x => x.ID == id)>0? true:false ;
        }
    }
}

