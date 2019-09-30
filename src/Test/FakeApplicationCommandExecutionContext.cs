using System;
using System.Collections.Generic;
using Aspenlaub.Net.GitHub.CSharp.Vishizhukel.Interfaces.Application;

namespace Aspenlaub.Net.GitHub.CSharp.PureSearch.Test {
    public class FakeApplicationCommandExecutionContext : IApplicationCommandExecutionContext {
        public List<IFeedbackToApplication> Feedback { get; }
        public List<string> Messages { get; }
        public bool Success { get; private set; }

        public FakeApplicationCommandExecutionContext() {
            Feedback = new List<IFeedbackToApplication>();
            Messages = new List<string>();
            Success = false;
        }

        public void Report(IFeedbackToApplication feedback) {
            Feedback.Add(feedback);
        }

        public void Report(string message, bool ofNoImportance) {
            Messages.Add(message);
        }

        public void ReportExecutionResult(Type commandType, bool success, string errorMessage) {
            Success = success;
        }
    }
}
