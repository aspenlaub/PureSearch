using Aspenlaub.Net.GitHub.CSharp.PureSearch.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.PureSearch.Test;

public class FakeSearchArguments : ISearchArguments {
    public bool MatchCase { get; set; }
    public bool EndsWith { get; set; }
    public string NameContains { get; set; }
    public string NameDoesNotContain { get; set; }
    public string SearchFor { get; set; }
    public string TextThatDoesNotFollow { get; set; }
    public string TextThatFollows { get; set; }
    public string IfDifferentInFolder { get; set; }
}