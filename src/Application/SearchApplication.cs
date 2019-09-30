using Aspenlaub.Net.GitHub.CSharp.PureSearch.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Vishizhukel.Application;
using Aspenlaub.Net.GitHub.CSharp.Vishizhukel.Entities.Application;
using Aspenlaub.Net.GitHub.CSharp.Vishizhukel.Interfaces.Application;

namespace Aspenlaub.Net.GitHub.CSharp.PureSearch.Application {
    public class SearchApplication {
        protected IApplicationCommandController Controller;
        private readonly ApplicationLog vLog;
        public IApplicationLog Log => vLog;

        public SearchApplication(IApplicationCommandController controller, ISearchFolder searchFolderProvider, ISearchArguments searchArgumentsProvider, ISearchFolderChanger searchFolderChanger) {
            vLog = new ApplicationLog();
            vLog.Add(new LogEntry() { Message = "Welcome back" });
            Controller = controller;
            Controller.AddCommand(new SelectFolderCommand(searchFolderProvider, searchFolderChanger), true);
            Controller.AddCommand(new SearchCommand(searchFolderProvider, searchArgumentsProvider), true);
        }
    }
}