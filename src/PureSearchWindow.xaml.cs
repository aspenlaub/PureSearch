using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.PureSearch.Application;
using Aspenlaub.Net.GitHub.CSharp.PureSearch.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Vishizhukel.Application;
using Aspenlaub.Net.GitHub.CSharp.Vishizhukel.Entities.Application;
using Aspenlaub.Net.GitHub.CSharp.Vishizhukel.Interfaces.Application;
using Aspenlaub.Net.GitHub.CSharp.Vishizhukel.Interfaces.Basic.Application;
using Microsoft.Win32;
using Ookii.Dialogs.Wpf;

namespace Aspenlaub.Net.GitHub.CSharp.PureSearch {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public partial class PureSearchWindow : ISearchFolder, ISearchArguments, ISearchFolderChanger {
        protected ApplicationCommandController Controller;
        protected SearchApplication SearchApplication;
        protected SynchronizationContext SynchronizationContext;

        private const string RegPath = @"Software\PureSearch\";

        public PureSearchWindow() {
            SynchronizationContext = SynchronizationContext.Current;
            Controller = new ApplicationCommandController(ApplicationFeedbackHandler);
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

            Folder.Text = (string)key.GetValue("LastSelectedFolder");
            FileNameContains.Text = (string)key.GetValue("LastNameContent");
            FileNameDoesNotContain.Text = (string)key.GetValue("LastNameDoesNotContain");
            TextToSearchFor.Text = (string)key.GetValue("LastSearchString");
            FollowingLinesContain.Text = (string)key.GetValue("LastSearchFollowString");
            FollowingLinesDoNotContain.Text = (string)key.GetValue("LastSearchNotFollowString");
            CaseSensitive.IsChecked = "TRUE" == (string)key.GetValue("LastMatchCase");
            FileNameEndsWith.IsChecked = "TRUE" == (string)key.GetValue("LastEndsWith");
            IfDifferentInWhichFolder.Text = (string)key.GetValue("IfDifferentInFolder");
        }

        private void BrowseFolder_OnClick(object sender, RoutedEventArgs e) {
            Controller.DisableCommand(typeof(SearchCommand));
            Controller.Execute(typeof(SelectFolderCommand));
        }

        public string ChangeFolderFromThisOneToWhat(string oldFolder) {
            var folderBrowserDialog = new VistaFolderBrowserDialog {
                SelectedPath = Folder.Text,
                ShowNewFolderButton = true
            };
            return folderBrowserDialog.ShowDialog() != true ? oldFolder : folderBrowserDialog.SelectedPath;
        }

        private void Search_OnClick(object sender, RoutedEventArgs e) {
            Cursor = Cursors.Wait;
            Results.Items.Clear();
            Controller.DisableCommand(typeof(SelectFolderCommand));
            Controller.Execute(typeof(SearchCommand));
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

        private void PureSearchWindow_OnActivated(object sender, EventArgs e) {
            CommandsEnabledOrDisabledHandler();
        }

        public void ApplicationFeedbackHandler(IFeedbackToApplication feedback) {
            switch (feedback.Type) {
                case FeedbackType.CommandExecutionCompleted:
                case FeedbackType.CommandExecutionCompletedWithMessage: {
                    CommandExecutionCompletedHandler(feedback);
                } break;
                case FeedbackType.CommandsEnabledOrDisabled: {
                    CommandsEnabledOrDisabledHandler();
                } break;
                case FeedbackType.LogInformation: {
                    SearchApplication.Log.Add(new LogEntry() { Message = feedback.Message, CreatedAt = feedback.CreatedAt, SequenceNumber = feedback.SequenceNumber });
                } break;
                case FeedbackType.LogWarning: {
                    SearchApplication.Log.Add(new LogEntry() { Class = LogEntryClass.Warning, Message = feedback.Message, CreatedAt = feedback.CreatedAt, SequenceNumber = feedback.SequenceNumber });
                } break;
                case FeedbackType.LogError: {
                    SearchApplication.Log.Add(new LogEntry() { Class = LogEntryClass.Error, Message = feedback.Message, CreatedAt = feedback.CreatedAt, SequenceNumber = feedback.SequenceNumber });
                } break;
                case FeedbackType.CommandIsDisabled: {
                    SearchApplication.Log.Add(new LogEntry() { Class = LogEntryClass.Error, Message = "Attempt to run disabled command " + feedback.CommandType, CreatedAt = feedback.CreatedAt, SequenceNumber = feedback.SequenceNumber });
                } break;
                case FeedbackType.ImportantMessage: {
                    var fileName = feedback.Message;
                    if (File.Exists(fileName)) {
                        var folder = new Folder(Folder.Text);
                        fileName = fileName.Substring(folder.FullName.Length + 1);
                        Results.Items.Add(fileName);
                    }
                } break;
                default: {
                    throw new NotImplementedException();
                }
            }
        }

        private void CommandExecutionCompletedHandler(IFeedbackToApplication feedback) {
            if (!Controller.IsMainThread()) { return; }

            Cursor = Cursors.Arrow;
            if (feedback.CommandType == typeof(SearchCommand)) {
                Controller.EnableCommand(typeof(SelectFolderCommand));
            } else if (feedback.CommandType == typeof(SelectFolderCommand)) {
                Controller.EnableCommand(typeof(SearchCommand));
            }
        }

        public void CommandsEnabledOrDisabledHandler() {
            BrowseFolder.IsEnabled = Controller.Enabled(typeof(SelectFolderCommand));
            Search.IsEnabled = Controller.Enabled(typeof(SearchCommand));
        }

        public string SearchInFolder {
            get => Text(Folder);
            set {
                SynchronizationContext.Post(o => { Folder.Text = value; }, null);
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
            SynchronizationContext.Send(x => { s = textBox.Text; }, null);
            return s;
        }

        private bool IsChecked(ToggleButton checkBox) {
            var isChecked = false;
            SynchronizationContext.Send(x => { isChecked = checkBox.IsChecked == true; }, null);
            return isChecked;
        }
    }
}
