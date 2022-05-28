using Aspenlaub.Net.GitHub.CSharp.PureSearch.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.PureSearch.Test;

public class FakeSearchFolder : ISearchFolder {
    public string SearchInFolder { get; set; }
}