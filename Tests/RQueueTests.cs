using ServerPMS;

namespace ServerPMSTests;

[TestClass]
public class RQueueTests
{
    ReorderableQueue<string> RQueue = new ReorderableQueue<string>();


    [TestMethod]
    public void EnqueueTest()
    {
        for (int i = 0; i < 10; i++)
        {
            RQueue.Enqueue(i.ToString());
        }

       
        Assert.IsTrue(RQueue.ElementAt(2) == "2" && RQueue.ElementAt(9) == "9");
    }

    public void DequeueTest()
    {
        for (int i = 0; i < 4; i++)
        {
            RQueue.Enqueue(i.ToString());
        }

        RQueue.Dequeue("1");
        RQueue.Dequeue("3");

        string[] k = { "0", "2", "4" };

        Assert.IsTrue(RQueue.Equals(new ReorderableQueue<string>(k)));
    }


    [TestMethod]
    public void MoveUpTest()
    {
        for (int i = 0; i < 10; i++)
        {
            RQueue.Enqueue(i.ToString());
        }

        RQueue.MoveUp(x=>x=="2", 5);
        Assert.IsTrue(RQueue.ElementAt(0) == "2");
    }

    [TestMethod]
    public void MoveDownTest()
    {
        for (int i = 0; i < 10; i++)
        {
            RQueue.Enqueue(i.ToString());
        }

        RQueue.MoveDown(x => x == "8", 5);
        Assert.IsTrue(RQueue.ElementAt(RQueue.Count-1) == "8");
    }

    [TestMethod]
    public void NextTest()
    {
        for (int i = 0; i < 10; i++)
        {
            RQueue.Enqueue(i.ToString());
        }

        Assert.IsTrue(RQueue.Next == "0");
    }

    [TestMethod]
    public void CoherenceTest()
    {
        for (int i = 0; i < 10; i++)
        {
            RQueue.Enqueue(i.ToString());
        }
        RQueue.MoveUp(x=>x=="3");
        RQueue.MoveDown(x=>x=="3");

        bool isOk = true;
        for (int i = 0; i < 10; i++)
        {
            if(RQueue.ElementAt(i) != i.ToString())
            {
                isOk = false;
            }
            
        }

        Assert.IsTrue(isOk);
    }

    [TestMethod]
    public void NextAndDequeueTest()
    {
        for (int i = 0; i < 10; i++)
        {
            RQueue.Enqueue(i.ToString());
        }
        string deq = RQueue.GetNextAndDequeue();
        Assert.IsTrue(deq=="0"&&RQueue.Next=="1");
    }
}
