namespace Aspenlaub.Net.GitHub.CSharp.PureSearch.Interfaces {
    public interface ISearchArguments {
        string NameContains { get; }
        string NameDoesNotContain { get; }
        string SearchFor { get; }
        string TextThatFollows { get; }
        string TextThatDoesNotFollow { get; }
        bool MatchCase { get; }
        bool EndsWith { get; }
        string IfDifferentInFolder { get; }
    }
}
