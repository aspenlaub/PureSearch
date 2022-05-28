using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Components;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.PureSearch.Application;
using Aspenlaub.Net.GitHub.CSharp.PureSearch.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Vishizhukel.Application;
using Aspenlaub.Net.GitHub.CSharp.Vishizhukel.Interfaces.Application;
using Autofac;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Ookii.Dialogs.Wpf;

namespace Aspenlaub.Net.GitHub.CSharp.PureSearch;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
// ReSharper disable once UnusedMember.Global
public partial class PureSearchWindow : ISearchFolder, ISearchArguments, ISearchFolderChanger {
    protected ApplicationCommandController Controller;
    protected SearchApplication SearchApplication;
    protected SynchronizationContext SynchronizationContext;
    protected ISimpleLogger SimpleLogger;
    protected string LogId;

    private const string RegPath = @"Software\PureSearch\";

    public PureSearchWindow() {
        var container = new ContainerBuilder().UsePegh(new DummyCsArgumentPrompter()).Build();
        var logConfiguration = new LogConfiguration();
        SimpleLogger = container.Resolve<ISimpleLogger>();
        SimpleLogger.LogSubFolder = logConfiguration.LogSubFolder;
        LogId = logConfiguration.LogId;
        SynchronizationContext = SynchronizationContext.Current;
        Controller = new ApplicationCommandController(HandleFeedbackToApplicationAsync);
        SearchApplication = new SearchApplication(Controller, this, this, this);
        InitializeComponent();
        GetRegistry();
    }

    protected void SetRegistry() {
        var key = Registry.CurrentUser.CreateSubKey(RegPath);
        // ReSharper disable once UseNullPropagationWhenPossible
        if (key == null) { return; }

        key.SetValue("LastSelectedFolder", Folder.Text);
        key.SetValue("LastNameContent", FileNameContains.Text);
        key.SetValue("LastNameDoesNotContain", FileNameDoesNotContain.Text);
        key.SetValue("LastSearchString", TextToSearchFor.Text);
        key.SetValue("LastSearchFollowString", FollowingLinesContain.Text);
        key.SetValue("LastSearchNotFollowString", FollowingLinesDoNotContain.Text);
        key.SetValue("LastMatchCase", CaseSensitive.IsChecked == true ? "TRUE" : "FALSE");
        key.SetValue("LastEndsWith", FileNameEndsWith.IsChecked == true ? "TRUE" : "FALSE");
        key.SetValue("IfDifferentInFolder", IfDifferentInWhichFolder.Text);
    }

    protected void GetRegistry() {
        var key = Registry.CurrentUser.OpenSubKey(RegPath);
        if (key == null) {
            return;
        }

        Folder.Text = (string)key.GetValue("LastSelectedFolder") ?? "";
        FileNameContains.Text = (string)key.GetValue("LastNameContent") ?? "";
        FileNameDoesNotContain.Text = (string)key.GetValue("LastNameDoesNotContain") ?? "";
        TextToSearchFor.Text = (string)key.GetValue("LastSearchString") ?? "";
        FollowingLinesContain.Text = (string)key.GetValue("LastSearchFollowString") ?? "";
        FollowingLinesDoNotContain.Text = (string)key.GetValue("LastSearchNotFollowString") ?? "";
        CaseSensitive.IsChecked = "TRUE" == ((string)key.GetValue("LastMatchCase") ?? "");
        FileNameEndsWith.IsChecked = "TRUE" == ((string)key.GetValue("LastEndsWith") ?? "");
        IfDifferentInWhichFolder.Text = (string)key.GetValue("IfDifferentInFolder") ?? "";
    }

    private async void OnBrowseFolderClickAsync(object sender, RoutedEventArgs e) {
        await Controller.DisableCommandAsync(typeof(SearchCommand));
        await Controller.ExecuteAsync(typeof(SelectFolderCommand));
    }

    public string ChangeFolderFromThisOneToWhat(string oldFolder) {
        var folderBrowserDialog = new VistaFolderBrowserDialog {
            SelectedPath = Folder.Text,
            ShowNewFolderButton = true
        };
        return folderBrowserDialog.ShowDialog() != true ? oldFolder : folderBrowserDialog.SelectedPath;
    }

    private async void OnSearchClickAsync(object sender, RoutedEventArgs e) {
        Cursor = Cursors.Wait;
        Results.Items.Clear();
        await Controller.DisableCommandAsync(typeof(SelectFolderCommand));
        await Controller.ExecuteAsync(typeof(SearchCommand));
    }

    private void Results_OnMouseDoubleClick(object sender, MouseButtonEventArgs e) {
        if (Results.SelectedIndex < 0) { return; }

        var fileName = Folder.Text + '\\' + (string)Results.Items[Results.SelectedIndex];
        if (!File.Exists(fileName)) { return; }

        try {
            var process = new Process {StartInfo = new ProcessStartInfo(fileName)};
            process.Start();
        } catch {
            MessageBox.Show(Properties.Resources.DoNotKnowHowToOpenThisFileType, Properties.Resources.PureSearch, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void PureSearchWindow_OnClosing(object sender, CancelEventArgs e) {
        SetRegistry();
    }

    private async void OnPureSearchWindowActivated(object sender, EventArgs e) {
        await CommandsEnabledOrDisabledHandlerAsync();
    }

    public async Task HandleFeedbackToApplicationAsync(IFeedbackToApplication feedback) {
        using (SimpleLogger.BeginScope(SimpleLoggingScopeId.Create("Scope", LogId))) {

            switch (feedback.Type) {
                case FeedbackType.CommandExecutionCompleted:
                case FeedbackType.CommandExecutionCompletedWithMessage: {
                    await CommandExecutionCompletedHandlerAsync(feedback);
                }
                    break;
                case FeedbackType.CommandsEnabledOrDisabled: {
                    await CommandsEnabledOrDisabledHandlerAsync();
                }
                    break;
                case FeedbackType.LogInformation: {
                    SimpleLogger.LogInformation(feedback.Message);
                }
                    break;
                case FeedbackType.LogWarning: {
                    SimpleLogger.LogWarning(feedback.Message);
                }
                    break;
                case FeedbackType.LogError: {
                    SimpleLogger.LogError(feedback.Message);
                }
                    break;
                case FeedbackType.CommandIsDisabled: {
                    SimpleLogger.LogError("Attempt to run disabled command " + feedback.CommandType);
                }
                    break;
                case FeedbackType.ImportantMessage: {
                    var fileName = feedback.Message;
                    if (File.Exists(fileName)) {
                        var folder = new Folder(Folder.Text);
                        fileName = fileName.Substring(folder.FullName.Length + 1);
                        Results.Items.Add(fileName);
                    }
                }
                    break;
                default: {
                    throw new NotImplementedException();
                }
            }
        }

        await Task.CompletedTask;
    }

    private async Task CommandExecutionCompletedHandlerAsync(IFeedbackToApplication feedback) {
        if (!Controller.IsMainThread()) { return; }

        Cursor = Cursors.Arrow;
        if (feedback.CommandType == typeof(SearchCommand)) {
            await Controller.EnableCommandAsync(typeof(SelectFolderCommand));
        } else if (feedback.CommandType == typeof(SelectFolderCommand)) {
            await Controller.EnableCommandAsync(typeof(SearchCommand));
        }
    }

    public async Task CommandsEnabledOrDisabledHandlerAsync() {
        BrowseFolder.IsEnabled = await Controller.EnabledAsync(typeof(SelectFolderCommand));
        Search.IsEnabled = await Controller.EnabledAsync(typeof(SearchCommand));
    }

    public string SearchInFolder {
        get => Text(Folder);
        set {
            SynchronizationContext.Post(_ => { Folder.Text = value; }, null);
        }
    }

    public string NameContains => Text(FileNameContains);
    public string NameDoesNotContain => Text(FileNameDoesNotContain);
    public string SearchFor => Text(TextToSearchFor);
    public string TextThatFollows => Text(FollowingLinesContain);
    public string TextThatDoesNotFollow => Text(FollowingLinesDoNotContain);
    public bool MatchCase => IsChecked(CaseSensitive);
    public bool EndsWith => IsChecked(FileNameEndsWith);
    public string IfDifferentInFolder => Text(IfDifferentInWhichFolder);

    private string Text(TextBox textBox) {
        var s = "";
        SynchronizationContext.Send(_ => { s = textBox.Text; }, null);
        return s;
    }

    private bool IsChecked(ToggleButton checkBox) {
        var isChecked = false;
        SynchronizationContext.Send(_ => { isChecked = checkBox.IsChecked == true; }, null);
        return isChecked;
    }
}