using IRSv2;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;

namespace IRSv2Tests;

[TestClass]
public class POrderTests
{
    //new ProductionOrder("partCode", "partDescription", 1, "customerOrderRef", 2, "moldID", "moldLocation", "moldNotes", "customerName","deliveryFacility", "1/04/2014");
    [TestMethod]

    public void PartCodeTest()
    {
        bool ExceptionThrown = false;
        try
        {
            ProductionOrder o = new ProductionOrder("", "partDescription", 1, "customerOrderRef", 2, "moldID", "moldLocation", "moldNotes", "customerName", "deliveryFacility", "1/04/2014");
        }
        catch (Exception ex)
        {
            ExceptionThrown = true;
        }
        Assert.IsTrue(ExceptionThrown);
    }

    [TestMethod]
    public void PartDescriptionTest()
    {
        bool ExceptionThrown = false;
        try
        {
            ProductionOrder o = new ProductionOrder("customerOrderRef", "", 1, "customerOrderRef", 2, "moldID", "moldLocation", "moldNotes", "customerName", "deliveryFacility", "1/04/2014");
        }
        catch (Exception ex)
        {
            ExceptionThrown = true;
        }
        Assert.IsTrue(ExceptionThrown);
    }

    [TestMethod]
    public void QtyTest()
    {
        bool ExceptionThrown = false;
        try
        {
            ProductionOrder o = new ProductionOrder("customerOrderRef", "customerOrderRef", -1, "customerOrderRef", 2, "moldID", "moldLocation", "moldNotes", "customerName", "deliveryFacility", "1/04/2014");
        }
        catch (Exception ex)
        {
            ExceptionThrown = true;
        }
        Assert.IsTrue(ExceptionThrown);
    }

    [TestMethod]
    public void CustomerOrderRefTest()
    {
        bool ExceptionThrown = false;
        try
        {
            ProductionOrder o = new ProductionOrder("stringa", "partDescription", 1, "", 2, "moldID", "moldLocation", "moldNotes", "customerName", "deliveryFacility", "1/04/2014");
        }
        catch (Exception ex)
        {
            ExceptionThrown = true;
        }
        Assert.IsTrue(ExceptionThrown);
    }

    [TestMethod]
    public void DefProdUnitTest()
    {
        bool ExceptionThrown = false;
        try
        {
            ProductionOrder o = new ProductionOrder("customerOrderRef", "string", 1, "customerOrderRef", -1, "moldID", "moldLocation", "moldNotes", "customerName", "deliveryFacility", "1/04/2014");
        }
        catch (Exception ex)
        {
            ExceptionThrown = true;
        }
        Assert.IsTrue(ExceptionThrown);
    }

    [TestMethod]
    public void MoldIDTest()
    {
        bool ExceptionThrown = false;
        try
        {
            ProductionOrder o = new ProductionOrder("customerOrderRef", "customerOrderRef", 1, "customerOrderRef", 2, "", "moldLocation", "moldNotes", "customerName", "deliveryFacility", "1/04/2014");
        }
        catch (Exception ex)
        {
            ExceptionThrown = true;
        }
        Assert.IsTrue(ExceptionThrown);
    }

    [TestMethod]
    public void MoldPositionTest()
    {
        bool ExceptionThrown = false;
        try
        {
            ProductionOrder o = new ProductionOrder("customerOrderRef", "customerOrderRef", 1, "customerOrderRef", 2, "moldLocation", "", "moldNotes", "customerName", "deliveryFacility", "1/04/2014");
        }
        catch (Exception ex)
        {
            ExceptionThrown = true;
        }
        Assert.IsTrue(ExceptionThrown);
    }

    [TestMethod]
    public void MoldNotesTest()
    {
        bool ExceptionThrown = false;
        try
        {
            ProductionOrder o = new ProductionOrder("customerOrderRef", "customerOrderRef", 1, "customerOrderRef", 2, "moldLocation", "PINO", "", "customerName", "deliveryFacility", "1/04/2014");
        }
        catch (Exception ex)
        {
            ExceptionThrown = true;
        }
        Assert.IsFalse(ExceptionThrown);
    }
    [TestMethod]
    public void CustomerNameTest()
    {
        bool ExceptionThrown = false;
        try
        {
            ProductionOrder o = new ProductionOrder("customerOrderRef", "customerOrderRef", 1, "customerOrderRef", 2, "moldid","moldLocation", "moldNotes", "", "deliveryFacility", "1/04/2014");
        }
        catch (Exception ex)
        {
            ExceptionThrown = true;
        }
        Assert.IsTrue(ExceptionThrown);
    }

    [TestMethod]
    public void DeliveryFacilityTest()
    {
        bool ExceptionThrown = false;
        try
        {
            ProductionOrder o = new ProductionOrder("customerOrderRef", "customerOrderRef", 1, "customerOrderRef", 2, "moldid", "moldLocation", "moldNotes", "CustomerName", "", "1/04/2014");
        }
        catch (Exception ex)
        {
            ExceptionThrown = true;
        }
        Assert.IsTrue(ExceptionThrown);
    }

    [TestMethod]
    public void DeliveryDateTest()
    {
        bool ExceptionThrown = false;
        try
        {
            ProductionOrder o = new ProductionOrder("customerOrderRef", "customerOrderRef", 1, "customerOrderRef", 2, "moldid", "moldLocation", "moldNotes", "CustomerName", "no", "");
            DateTime.Parse(o.DeliveryDate);
        }
        catch (Exception ex)
        {
            ExceptionThrown = true;
        }
        Assert.IsTrue(ExceptionThrown);
    }
}