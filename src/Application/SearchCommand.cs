using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.PureSearch.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Vishizhukel.Interfaces.Application;

namespace Aspenlaub.Net.GitHub.CSharp.PureSearch.Application {
    public class SearchCommand : IApplicationCommand {
        protected ISearchFolder SearchFolderProvider;
        protected ISearchArguments SearchArgumentsProvider;

        public bool MakeLogEntries => true;
        public string Name => Properties.Resources.SearchCommandName;

        public SearchCommand(ISearchFolder searchFolderProvider, ISearchArguments searchArgumentsProvider) {
            SearchFolderProvider = searchFolderProvider;
            SearchArgumentsProvider = searchArgumentsProvider;
        }

        public bool CanExecute() { return true; }

        public Task Execute(IApplicationCommandExecutionContext context) {
            return Task.Run(() => {
                var success = DoSearch(context, SearchFolderProvider.SearchInFolder, SearchArgumentsProvider.SearchFor, SearchArgumentsProvider.NameContains, SearchArgumentsProvider.TextThatFollows, SearchArgumentsProvider.TextThatDoesNotFollow, SearchArgumentsProvider.MatchCase, SearchArgumentsProvider.IfDifferentInFolder, SearchArgumentsProvider.EndsWith, SearchArgumentsProvider.NameDoesNotContain);
                var errorMessage = success ? "" : "Something went wrong";
                context.ReportExecutionResult(GetType(), success, errorMessage);
            });
        }

        protected bool FileContains(string fileName, string searchFor, string follows, string doesNotFollow, bool matchCase) {
            int i;

            var fileContents = File.ReadAllLines(fileName).ToList();
            if (!matchCase) {
                fileContents = fileContents.Select(x => x.ToUpper()).ToList();
            }

            for (i = 0; i < fileContents.Count; i++) {
                var s = fileContents[i];
                if (!s.Contains(searchFor)) {
                    continue;
                }

                bool okay;
                int j;
                if (follows.Length != 0) {
                    okay = false;
                    for (j = i + 1; j < i + 4 && j < fileContents.Count; j++) {
                        okay = okay || fileContents[j].Contains(follows);
                    }
                } else {
                    okay = true;
                }

                if (okay && doesNotFollow.Length != 0) {
                    for (j = i + 1; j < i + 4 && j < fileContents.Count; j++) {
                        okay = okay && !fileContents[j].Contains(doesNotFollow);
                    }
                }

                if (okay) {
                    return true;
                }
            }

            return false;
        }

        protected bool DoSearch(IApplicationCommandExecutionContext context, string folder, string searchFor, string nameContains, string follows, string doesNotFollow, bool matchCase, string ifDifferentInFolder, bool endsWith, string nameDoesNotContain) {
            if (!Directory.Exists(folder)) { return false; }

            if (!matchCase) {
                searchFor = searchFor.ToUpper();
                follows = follows.ToUpper();
                doesNotFollow = doesNotFollow.ToUpper();
            }
            var dirInfo = new DirectoryInfo(folder);
            try {
                if (dirInfo.GetDirectories().Any(subDirInfo => !DoSearch(context, folder + '\\' + subDirInfo.Name, searchFor, nameContains, follows, doesNotFollow, matchCase, ifDifferentInFolder == "" ? "" : ifDifferentInFolder + '\\' + subDirInfo.Name, endsWith, nameDoesNotContain))) {
                    return false;
                }

                // ReSharper disable once LoopCanBePartlyConvertedToQuery
                foreach (var fileInfo in from fileInfo in GetFiles(nameContains, dirInfo, endsWith, nameDoesNotContain) let addFileName = searchFor.Length == 0 || FileContains(fileInfo.FullName, searchFor, follows, doesNotFollow, matchCase) where addFileName select fileInfo) {
                    var differentFile = ifDifferentInFolder + '\\' + fileInfo.Name;
                    if (!string.IsNullOrEmpty(ifDifferentInFolder) && !File.Exists(differentFile)) {
                        continue;
                    }

                    if (!string.IsNullOrEmpty(ifDifferentInFolder) && File.ReadAllText(fileInfo.FullName) == File.ReadAllText(differentFile)) {
                        continue;
                    }

                    context.Report(fileInfo.FullName, false);
                }
            } catch {
                return false;
            }

            return true;
        }

        private static IEnumerable<FileInfo> GetFiles(string nameContains, DirectoryInfo dirInfo, bool endsWith, string nameDoesNotContain) {
            return dirInfo.GetFiles('*' + nameContains + '*')
                .Where(f => f.Name.Contains(nameContains)
                   && (!endsWith || f.Name.EndsWith(nameContains))
                   && (string.IsNullOrEmpty(nameDoesNotContain) || !f.Name.Contains(nameDoesNotContain))
                   && (f.Attributes & FileAttributes.Hidden) == 0);
        }
    }
}
