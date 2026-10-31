using Microsoft.VisualStudio.TestTools.UnitTesting;
using MyHttpServer.Controllers;

namespace HttpServa.Tests;

[TestClass]
public class ReviewStoreTests
{
    [TestMethod]
    public void Add_CreatesDraftWithNewId()
    {
        var store = new ReviewStore();
        var first = store.Add("Minecraft", 5);
        var second = store.Add("Portal 2", 4);

        Assert.AreEqual(1, first.Id);
        Assert.AreEqual(2, second.Id);
        Assert.AreEqual("draft", first.Status);
    }

    [TestMethod]
    public void GetAll_FiltersAndCountsAverage()
    {
        var store = new ReviewStore();
        store.Add("Game A", 2);
        store.Add("Game B", 4);
        store.Add("Game C", 5);

        var result = store.GetAll(4);

        Assert.AreEqual(2, result.Items.Count);
        Assert.AreEqual(4.5m, result.Average);
    }

    [TestMethod]
    public void Update_ChangesAllFields()
    {
        var store = new ReviewStore();
        var item = store.Add("Game A", 2);

        bool updated = store.Update(item.Id, "Game B", 5, "published");
        var saved = store.GetById(item.Id);

        Assert.IsTrue(updated);
        Assert.AreEqual("Game B", saved?.Game);
        Assert.AreEqual(5, saved?.Score);
        Assert.AreEqual("published", saved?.Status);
    }

    [TestMethod]
    public void Validate_RejectsWrongGameNameAndStatus()
    {
        Assert.IsNotNull(ReviewStore.Validate("A", 3));
        Assert.IsNotNull(ReviewStore.Validate("Valid Game", 3, "public"));
        Assert.IsNull(ReviewStore.Validate("Valid Game", 3, "published"));
    }
}
