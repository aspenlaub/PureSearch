using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.PureSearch.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Vishizhukel.Interfaces.Application;

namespace Aspenlaub.Net.GitHub.CSharp.PureSearch.Application;

public class SearchCommand : IApplicationCommand {
    protected ISearchFolder SearchFolderProvider;
    protected ISearchArguments SearchArgumentsProvider;

    public bool MakeLogEntries => true;
    public string Name => Properties.Resources.SearchCommandName;

    public SearchCommand(ISearchFolder searchFolderProvider, ISearchArguments searchArgumentsProvider) {
        SearchFolderProvider = searchFolderProvider;
        SearchArgumentsProvider = searchArgumentsProvider;
    }

    public async Task<bool> CanExecuteAsync() { return await Task.FromResult(true); }

    public async Task ExecuteAsync(IApplicationCommandExecutionContext context) {
        await DoSearchAsync(context, SearchFolderProvider.SearchInFolder, SearchArgumentsProvider.SearchFor, SearchArgumentsProvider.NameContains, SearchArgumentsProvider.TextThatFollows, SearchArgumentsProvider.TextThatDoesNotFollow, SearchArgumentsProvider.MatchCase, SearchArgumentsProvider.IfDifferentInFolder, SearchArgumentsProvider.EndsWith, SearchArgumentsProvider.NameDoesNotContain);
        await context.ReportExecutionResultAsync(GetType(), true, "");
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

    protected async Task DoSearchAsync(IApplicationCommandExecutionContext context, string folder, string searchFor, string nameContains, string follows, string doesNotFollow, bool matchCase, string ifDifferentInFolder, bool endsWith, string nameDoesNotContain) {
        if (!Directory.Exists(folder)) { return; }

        var ignoredSubFolders = new List<string> { "Addins", "AppData", "bin", "Debug", "Publish", "Release", "obj", ".cache", ".config", ".dotnet", ".git", ".librarymanager", ".nuget", ".templateengine", ".vs", ".vscode" };
        if (ignoredSubFolders.Any(d => folder.EndsWith('\\' + d))) { return; }

        if (!matchCase) {
            searchFor = searchFor.ToUpper();
            follows = follows.ToUpper();
            doesNotFollow = doesNotFollow.ToUpper();
        }
        var dirInfo = new DirectoryInfo(folder);
        try {
            foreach(var subDirInfo in dirInfo.GetDirectories().ToList()) {
                await DoSearchAsync(context, folder + '\\' + subDirInfo.Name, searchFor, nameContains, follows, doesNotFollow, matchCase,
                    ifDifferentInFolder == "" ? "" : ifDifferentInFolder + '\\' + subDirInfo.Name, endsWith, nameDoesNotContain);
            }
            // ReSharper disable once LoopCanBePartlyConvertedToQuery
            foreach (var fileInfo in from fileInfo in GetFiles(nameContains, dirInfo, endsWith, nameDoesNotContain) let addFileName = searchFor.Length == 0 || FileContains(fileInfo.FullName, searchFor, follows, doesNotFollow, matchCase) where addFileName select fileInfo) {
                var differentFile = ifDifferentInFolder + '\\' + fileInfo.Name;
                if (!string.IsNullOrEmpty(ifDifferentInFolder) && !File.Exists(differentFile)) {
                    continue;
                }

                if (!string.IsNullOrEmpty(ifDifferentInFolder) && await File.ReadAllTextAsync(fileInfo.FullName) == await File.ReadAllTextAsync(differentFile)) {
                    continue;
                }

                await context.ReportAsync(fileInfo.FullName, false);
            }
            // ReSharper disable once EmptyGeneralCatchClause
        } catch {
        }
    }

    private static IEnumerable<FileInfo> GetFiles(string nameContains, DirectoryInfo dirInfo, bool endsWith, string nameDoesNotContain) {
        var nameDoesNotContainList = string.IsNullOrWhiteSpace(nameDoesNotContain)
            ? new List<string>()
            : nameDoesNotContain.Split(';').ToList();
        return dirInfo.GetFiles('*' + nameContains + '*')
            .Where(f => f.Name.Contains(nameContains)
                && (!endsWith || f.Name.EndsWith(nameContains))
                && !nameDoesNotContainList.Any(n => f.Name.Contains(n))
                && (f.Attributes & FileAttributes.Hidden) == 0);
    }
}