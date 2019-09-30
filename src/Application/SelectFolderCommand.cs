using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.PureSearch.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Vishizhukel.Interfaces.Application;

namespace Aspenlaub.Net.GitHub.CSharp.PureSearch.Application {
    public class SelectFolderCommand : IApplicationCommand {
        protected ISearchFolderChanger SearchFolderChanger;
        protected ISearchFolder SearchFolderProvider;

        public bool MakeLogEntries => true;
        public string Name => Properties.Resources.SelectFolderCommandName;

        public SelectFolderCommand(ISearchFolder searchFolderProvider, ISearchFolderChanger searchFolderChanger) {
            SearchFolderChanger = searchFolderChanger;
            SearchFolderProvider = searchFolderProvider;
        }

        public bool CanExecute() { return true; }

        public Task Execute(IApplicationCommandExecutionContext context) {
            SearchFolderProvider.SearchInFolder = SearchFolderChanger.ChangeFolderFromThisOneToWhat(SearchFolderProvider.SearchInFolder);
            return Task.Delay(0);
        }
    }
}
