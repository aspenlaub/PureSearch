using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Components;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.PureSearch.Application;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aspenlaub.Net.GitHub.CSharp.PureSearch.Test {
    [TestClass]
    public class SearchCommandTest {
        private readonly IFolder vGitHubFolder;

        public SearchCommandTest() {
            var componentProvider = new ComponentProvider();
            var errorsAndInfos = new ErrorsAndInfos();
            vGitHubFolder = componentProvider.FolderResolver.Resolve(@"$(GitHub)", errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
        }

        [TestMethod]
        public async Task CannotSearchIfFolderDoesNotExist() {
            var searchFolder = new FakeSearchFolder { SearchInFolder = vGitHubFolder.FullName + @"\PureSearch\PureSearch.Test\" };
            var searchArguments = new FakeSearchArguments { MatchCase = false, NameContains = "Fake", SearchFor = "PureSearch", TextThatFollows = "", TextThatDoesNotFollow = "" };
            var searchCommand = new SearchCommand(searchFolder, searchArguments);
            var context = new FakeApplicationCommandExecutionContext();
            await searchCommand.Execute(context);
            Assert.IsFalse(context.Success);
        }

        [TestMethod]
        public async Task CanSearch() {
            var searchFolder = new FakeSearchFolder { SearchInFolder = vGitHubFolder.FullName + @"\PureSearch\src\Test\" };
            var searchArguments = new FakeSearchArguments { MatchCase = false, NameContains = "Fake", SearchFor = "PureSearch", TextThatFollows = "", TextThatDoesNotFollow = "" };
            var searchCommand = new SearchCommand(searchFolder, searchArguments);
            var context = new FakeApplicationCommandExecutionContext();
            await searchCommand.Execute(context);
            Assert.IsTrue(context.Success);
            Assert.AreEqual(3, context.Messages.Count);
        }

        [TestMethod]
        public async Task CanSearchOnFollowingLine() {
            var searchFolder = new FakeSearchFolder() { SearchInFolder = vGitHubFolder.FullName + @"\PureSearch\src\Test\" };
            var searchArguments = new FakeSearchArguments() { MatchCase = false, NameContains = "Fake", SearchFor = "PureSearch", TextThatFollows = "SearchArguments", TextThatDoesNotFollow = "" };
            var searchCommand = new SearchCommand(searchFolder, searchArguments);
            var context = new FakeApplicationCommandExecutionContext();
            await searchCommand.Execute(context);
            Assert.IsTrue(context.Success);
            Assert.AreEqual(1, context.Messages.Count);
        }

        [TestMethod]
        public async Task CanSearchOnNotFollowingLine() {
            var searchFolder = new FakeSearchFolder() { SearchInFolder = vGitHubFolder.FullName + @"\PureSearch\src\Test\" };
            var searchArguments = new FakeSearchArguments() { MatchCase = false, NameContains = "Fake", SearchFor = "PureSearch", TextThatFollows = "", TextThatDoesNotFollow = "SearchArguments" };
            var searchCommand = new SearchCommand(searchFolder, searchArguments);
            var context = new FakeApplicationCommandExecutionContext();
            await searchCommand.Execute(context);
            Assert.IsTrue(context.Success);
            Assert.AreEqual(2, context.Messages.Count);
        }
    }
}