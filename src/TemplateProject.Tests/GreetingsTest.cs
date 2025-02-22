namespace TemplateProject.Tests;

[TestFixture]
public class GreetingsTest
{
    [Test]
    public void Test()
    {
        Assert.That(Greetings.HelloWorld, Is.EqualTo("Hello world"));
    }
}
