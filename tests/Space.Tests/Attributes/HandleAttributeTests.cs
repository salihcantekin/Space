using Space.Abstraction.Attributes;

namespace Space.Tests.Attributes;

[TestClass]
public class HandleAttributeTests
{
    [TestMethod]
    public void HandleAttribute_Should_Set_Name_Property()
    {
        var attr = new HandleAttribute { Name = "TestHandle" };
        Assert.AreEqual("TestHandle", attr.Name);
    }

    [TestMethod]
    public void HandleAttribute_IsDefault_CanBeSet()
    {
        var attr = new HandleAttribute { IsDefault = true };
        Assert.IsTrue(attr.IsDefault);
    }
}