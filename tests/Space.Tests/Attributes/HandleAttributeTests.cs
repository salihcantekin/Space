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
}