using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Components;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.PureSearch.Application;
using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aspenlaub.Net.GitHub.CSharp.PureSearch.Test;

[TestClass]
public class SearchCommandTest {
    private readonly IFolder GitHubFolder;

    public SearchCommandTest() {
        var container = new ContainerBuilder().UsePegh("PureSearch", new DummyCsArgumentPrompter()).Build();
        var errorsAndInfos = new ErrorsAndInfos();
        GitHubFolder = container.Resolve<IFolderResolver>().ResolveAsync(@"$(GitHub)", errorsAndInfos).Result;
        Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
    }

    [TestMethod]
    public async Task CanSearch() {
        var searchFolder = new FakeSearchFolder { SearchInFolder = GitHubFolder.FullName + @"\PureSearch\src\Test\" };
        var searchArguments = new FakeSearchArguments { MatchCase = false, NameContains = "Fake", SearchFor = "PureSearch", TextThatFollows = "", TextThatDoesNotFollow = "" };
        var searchCommand = new SearchCommand(searchFolder, searchArguments);
        var context = new FakeApplicationCommandExecutionContext();
        await searchCommand.ExecuteAsync(context);
        Assert.IsTrue(context.Success);
        Assert.AreEqual(3, context.Messages.Count);
    }

    [TestMethod]
    public async Task CanSearchOnFollowingLine() {
        var searchFolder = new FakeSearchFolder { SearchInFolder = GitHubFolder.FullName + @"\PureSearch\src\Test\" };
        var searchArguments = new FakeSearchArguments { MatchCase = false, NameContains = "Fake", SearchFor = "PureSearch", TextThatFollows = "SearchArguments", TextThatDoesNotFollow = "" };
        var searchCommand = new SearchCommand(searchFolder, searchArguments);
        var context = new FakeApplicationCommandExecutionContext();
        await searchCommand.ExecuteAsync(context);
        Assert.IsTrue(context.Success);
        Assert.AreEqual(1, context.Messages.Count);
    }

    [TestMethod]
    public async Task CanSearchOnNotFollowingLine() {
        var searchFolder = new FakeSearchFolder { SearchInFolder = GitHubFolder.FullName + @"\PureSearch\src\Test\" };
        var searchArguments = new FakeSearchArguments { MatchCase = false, NameContains = "Fake", SearchFor = "public class", TextThatFollows = "", TextThatDoesNotFollow = "Contains" };
        var searchCommand = new SearchCommand(searchFolder, searchArguments);
        var context = new FakeApplicationCommandExecutionContext();
        await searchCommand.ExecuteAsync(context);
        Assert.IsTrue(context.Success);
        Assert.AreEqual(2, context.Messages.Count);
    }
}