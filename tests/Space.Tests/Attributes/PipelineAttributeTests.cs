using Space.Abstraction.Attributes;

namespace Space.Tests.Attributes;

[TestClass]
public class PipelineAttributeTests
{
    [TestMethod]
    public void PipelineAttribute_Default_Order_Is_100()
    {
        var attr = new PipelineAttribute();
        Assert.AreEqual(100, attr.Order);
    }

    [TestMethod]
    public void PipelineAttribute_HandleName_CanBeSet()
    {
        var attr = new PipelineAttribute("TestHandle");
        Assert.AreEqual("TestHandle", attr.HandleName);
    }
}