using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Vishizhukel.Interfaces.Application;

namespace Aspenlaub.Net.GitHub.CSharp.PureSearch.Test;

public class FakeApplicationCommandExecutionContext : IApplicationCommandExecutionContext {
    public List<IFeedbackToApplication> Feedback { get; }
    public List<string> Messages { get; }
    public bool Success { get; private set; }

    public FakeApplicationCommandExecutionContext() {
        Feedback = new List<IFeedbackToApplication>();
        Messages = new List<string>();
        Success = false;
    }

    public async Task ReportAsync(IFeedbackToApplication feedback) {
        Feedback.Add(feedback);
        await Task.CompletedTask;
    }

    public async Task ReportAsync(string message, bool ofNoImportance) {
        Messages.Add(message);
        await Task.CompletedTask;
    }

    public async Task ReportExecutionResultAsync(Type commandType, bool success, string errorMessage) {
        Success = success;
        await Task.CompletedTask;
    }
}