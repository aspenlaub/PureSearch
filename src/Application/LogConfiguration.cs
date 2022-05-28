using System;
using System.Diagnostics;

namespace Aspenlaub.Net.GitHub.CSharp.PureSearch.Application;

public class LogConfiguration {
    public string LogSubFolder => @"AspenlaubLogs\PureSearch";
    public string LogId => $"{DateTime.Today:yyyy-MM-dd}-{Process.GetCurrentProcess().Id}";
}