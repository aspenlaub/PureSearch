using Aspenlaub.Net.GitHub.CSharp.PureSearch.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Vishizhukel.Interfaces.Application;

namespace Aspenlaub.Net.GitHub.CSharp.PureSearch.Application;

public class SearchApplication {
    protected IApplicationCommandController Controller;

    public SearchApplication(IApplicationCommandController controller, ISearchFolder searchFolderProvider, ISearchArguments searchArgumentsProvider, ISearchFolderChanger searchFolderChanger) {
        Controller = controller;
        Controller.AddCommand(new SelectFolderCommand(searchFolderProvider, searchFolderChanger), true);
        Controller.AddCommand(new SearchCommand(searchFolderProvider, searchArgumentsProvider), true);
    }
}