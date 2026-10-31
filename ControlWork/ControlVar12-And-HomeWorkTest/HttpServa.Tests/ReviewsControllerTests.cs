using Microsoft.VisualStudio.TestTools.UnitTesting;
using MyHttpServer.Controllers;
using MyHttpServer.Core.ContentTypes;
using MyHttpServer.Core.Handlers;
using HttpServa.Tests.TestSupport;

namespace HttpServa.Tests;

[TestClass]
public class ReviewsControllerTests
{
    [TestMethod]
    public async Task Create_SavesReviewAndRedirects()
    {
        var store = new ReviewStore();
        var controller = new ReviewsController(new ResponseWriter(new ContentTypeResolver()), store);
        var result = await HttpTestHost.RunAsync("/reviews/create", "POST", controller.Create, "game=Half-Life&score=5");

        Assert.AreEqual(303, result.StatusCode);
        Assert.AreEqual("draft", store.GetById(1)?.Status);
    }

    [TestMethod]
    public async Task Update_Returns204AndSavesFields()
    {
        var store = new ReviewStore();
        store.Add("Half-Life", 4);
        var controller = new ReviewsController(new ResponseWriter(new ContentTypeResolver()), store);
        var result = await HttpTestHost.RunAsync("/reviews/update?id=1", "PUT", controller.Update,
            "game=Portal+2&score=5&status=published");

        Assert.AreEqual(204, result.StatusCode);
        Assert.AreEqual("Portal 2", store.GetById(1)?.Game);
        Assert.AreEqual(5, store.GetById(1)?.Score);
        Assert.AreEqual("published", store.GetById(1)?.Status);
    }

    [TestMethod]
    public async Task Details_Returns404ForMissingReview()
    {
        var controller = new ReviewsController(new ResponseWriter(new ContentTypeResolver()), new ReviewStore());
        var result = await HttpTestHost.RunAsync("/reviews/details?id=7", "GET", controller.Details);

        Assert.AreEqual(404, result.StatusCode);
    }

    [TestMethod]
    public async Task List_ShowsAverageForFilteredReviews()
    {
        var store = new ReviewStore();
        store.Add("Game A", 2);
        store.Add("Game B", 4);
        store.Add("Game C", 5);
        var controller = new ReviewsController(new ResponseWriter(new ContentTypeResolver()), store);
        var result = await HttpTestHost.RunAsync("/reviews?minScore=4", "GET", controller.List);

        Assert.AreEqual(200, result.StatusCode);
        StringAssert.Contains(result.Body, "Средняя оценка: <b>4.5</b>");
        StringAssert.Contains(result.Body, "Game B");
        Assert.IsFalse(result.Body.Contains("Game A"));
    }

    [TestMethod]
    public async Task Create_Returns400ForWrongScore()
    {
        var store = new ReviewStore();
        var controller = new ReviewsController(new ResponseWriter(new ContentTypeResolver()), store);
        var result = await HttpTestHost.RunAsync("/reviews/create", "POST", controller.Create, "game=Half-Life&score=8");

        Assert.AreEqual(400, result.StatusCode);
        Assert.IsNull(store.GetById(1));
    }

    [TestMethod]
    public async Task List_Returns400ForWrongMinScore()
    {
        var controller = new ReviewsController(new ResponseWriter(new ContentTypeResolver()), new ReviewStore());
        var result = await HttpTestHost.RunAsync("/reviews?minScore=0", "GET", controller.List);

        Assert.AreEqual(400, result.StatusCode);
    }
}
