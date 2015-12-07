#define DEBUG_AGENT

using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Media;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using SqlServerVersionsWP.ViewModels;
using Microsoft.Phone.Tasks;
using Microsoft.Phone.UserData;
using Microsoft.Phone.Scheduler;
using RepositoryCaller;

namespace SqlServerVersionsWP
{
    public partial class MainPage : PhoneApplicationPage
    {
        private DeviceIntegrator _deviceIntegrator = new DeviceIntegrator();

        private enum MessageType
        {
            Information,
            Error
        }

        private PeriodicTask _periodicTask;
        private string _periodicTaskName = "Notifier";

        private int _topRecentCount = 10;
        private ObservableCollection<RecentVersionInfoViewModel> _recentReleaseVersionInfo = new ObservableCollection<RecentVersionInfoViewModel>();
        private IEnumerable<VersionInfoConsumer> _majorMinorReleasesBase;
        private IEnumerable<VersionInfoConsumer> _oldestAndRecentSupportedVersions;
        private VersionInfoConsumer _newestSupportedVersion;
        private VersionInfoConsumer _oldestSupportedVersion;

        // Constructor
        public MainPage()
        {
            InitializeComponent();

            _periodicTask = ScheduledActionService.Find(_periodicTaskName) as PeriodicTask;

            if (_periodicTask != null)
                ScheduledActionService.Remove(_periodicTaskName);

            _periodicTask = new PeriodicTask(_periodicTaskName);
            _periodicTask.Description = "testing";

            try
            {
                ScheduledActionService.Add(_periodicTask);

#if DEBUG_AGENT
                ScheduledActionService.LaunchForTest(_periodicTaskName, TimeSpan.FromSeconds(10));
#endif
            }
            catch (InvalidOperationException exception)
            {
                if (exception.Message.Contains("BNS Error: The action is disabled"))
                {
                    MessageBox.Show("Background agents for this application have been disabled by the user.");
                }

                if (exception.Message.Contains("BNS Error: The maximum number of ScheduledActions of this type have already been added."))
                {
                    // No user action required. The system prompts the user when the hard limit of periodic tasks has been reached.
                }
            }
            catch (SchedulerServiceException)
            {
                // No user action required.  
            }

            // Set the data context of the listbox control to the sample data
            //DataContext = App.ViewModel;

            //RecentReleaseList.ItemsSource = new List<string>();
            //RecentReleaseList.ItemsSource = _recentReleaseVersionInfo;
        }

        // Load data for the ViewModel Items
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            //if (!App.ViewModel.IsDataLoaded)
            //{
            //    App.ViewModel.LoadData();
            //    await FillRecentReleaseList();
            //}

            if (RecentReleaseList.ItemsSource == null)
                await FillRecentReleaseList();

            if (_majorMinorReleasesBase == null)
            { 
                _majorMinorReleasesBase = await GetMajorReleases();

                if (_oldestAndRecentSupportedVersions == null)
                    _oldestAndRecentSupportedVersions = await GetOldestAndRecentSupportedReleases();

                SetMajorReleaseSelector();
            }
        }

        private async void GoSearch_Click(object sender, RoutedEventArgs e)
        {
            // if the data is not valid or there then short circuit and 
            // show an error message
            //
            if (!IsAllDataAvailableAndValid())
            {
                HideResultData();
                ClearResultData();
                DisplayMessage("invalid data!", MessageType.Error);
                return;
            }

            // turn on the progress bar
            //
            ProgressIndicator.Visibility = Visibility.Visible;
            
            VersionInfoConsumer ResultVersion = await DataRetriever.GetVersionInfoAsync(
                Convert.ToInt32(MajorInput.Text),
                Convert.ToInt32(MinorInput.Text),
                Convert.ToInt32(BuildInput.Text),
                Convert.ToInt32(RevisionInput.Text)
                );

            if (ResultVersion != null)
                ShowResultData(ResultVersion);
            else
            {
                DisplayMessage("not found", MessageType.Information);
                HideResultData();
                ClearResultData();
            }

            // turn off the progress bar
            //
            ProgressIndicator.Visibility = Visibility.Collapsed;
        }

        private bool IsAllDataAvailableAndValid()
        {
            int Output;

            if (string.IsNullOrWhiteSpace(MajorInput.Text) ||
                string.IsNullOrWhiteSpace(MinorInput.Text) ||
                string.IsNullOrWhiteSpace(BuildInput.Text) ||
                string.IsNullOrWhiteSpace(RevisionInput.Text))
                return false;

            if (!int.TryParse(MajorInput.Text, out Output) ||
                !int.TryParse(MinorInput.Text, out Output) ||
                !int.TryParse(BuildInput.Text, out Output) ||
                !int.TryParse(RevisionInput.Text, out Output))
                return false;

            // if we pass all of the validations then return success
            //
            return true;
        }

        private void DisplayMessage(string message, MessageType messageType)
        {
            MessageDisplay.Visibility = Visibility.Visible;

            MessageDisplay.Text = message;
            if (messageType == MessageType.Information)
                MessageDisplay.Foreground = new SolidColorBrush(Colors.Yellow);
            else if (messageType == MessageType.Error)
                MessageDisplay.Foreground = new SolidColorBrush(Colors.Red);
        }
        private void HideMessage()
        {
            MessageDisplay.Visibility = Visibility.Collapsed;
            MessageDisplay.Text = "";
        }

        private void ClearResultData()
        {
            FriendlyNameShortDisplay.Text = "";
            FriendlyNameLongDisplay.Text = "";
            ReleaseDateDisplay.Text = "";

            // delete all of the hyperlink buttons in the stack panel
            //
            LinkVersionContent.Children.Clear();

            // disable the app bar button
            //
            //CopyToClipboard.IsEnabled = false;
            ApplicationBar.IsVisible = false;
        }
        private void HideResultData()
        {
            MainVersionContent.Visibility = Visibility.Collapsed;
            LinkVersionContent.Visibility = Visibility.Collapsed;
        }
        private void ShowResultData(VersionInfoConsumer resultData)
        {
            // it is assumed that whenever there is data to show, there 
            // should be no message
            //
            HideMessage();

            // display all of the data fields on their appropriate controls
            //
            FriendlyNameShortDisplay.Text = resultData.FriendlyNameShort;
            FriendlyNameLongDisplay.Text = resultData.FriendlyNameLong;
            ReleaseDateDisplay.Text = resultData.ReleaseDate.ToString("M/d/yyyy");
            IsSupportedDisplay.Text = string.Format("Is Supported: {0}", resultData.IsSupported);

            // first make sure there are no remaining items for links
            // and then add the new ones
            //
            LinkVersionContent.Children.Clear();
            foreach (string RefLink in resultData.ReferenceLinks)
            {
                HyperlinkButton NewHyperLinkButton = new HyperlinkButton();
                NewHyperLinkButton.Click += RefLink_Click;
                NewHyperLinkButton.Content = RefLink;
                NewHyperLinkButton.FontSize = 20;
                LinkVersionContent.Children.Add(NewHyperLinkButton);
            }

            MainVersionContent.Visibility = Visibility.Visible;
            LinkVersionContent.Visibility = Visibility.Visible;

            // enable the copy to clipboard app bar button
            //
            //CopyToClipboard.IsEnabled = true;
            ApplicationBar.IsVisible = true;
        }

        private async Task FillRecentReleaseList()
        {
            bool IsCheckedCached;

            //RecentReleaseList.ItemsSource = new ObservableCollection<string>();
            RecentReleaseList.ItemsSource = _recentReleaseVersionInfo;

            //IEnumerable<VersionInfoConsumer> AllVersionInfo = await DataRetriever.GetRecentReleaseVersionInfo(_topRecentCount);
            foreach (VersionInfoConsumer versionInfoConsumer in await DataRetriever.GetRecentReleaseVersionInfoAsync(_topRecentCount))
            {
                IsCheckedCached = _deviceIntegrator.IsCheckedNewVersion(versionInfoConsumer);

                RecentReleaseList.ItemsSource.Add(new RecentVersionInfoViewModel() 
                    { 
                        Major = versionInfoConsumer.Major,
                        Minor = versionInfoConsumer.Minor,
                        Build = versionInfoConsumer.Build,
                        Revision = versionInfoConsumer.Revision,
                        FriendlyNameShort = versionInfoConsumer.FriendlyNameShort,
                        FriendlyNameLong = versionInfoConsumer.FriendlyNameLong,
                        ReleaseDate = versionInfoConsumer.ReleaseDate,
                        IsChecked = IsCheckedCached
                    });

                // if the version info wasn't checked before, it now needs to be 
                // considered as "checked", so let's cache the data
                //
                if (!IsCheckedCached)
                    _deviceIntegrator.AddLocallyCheckedVersion(versionInfoConsumer);
            }

            // clear out the "new release" counter 
            //
            ShellTile ActiveTile = ShellTile.ActiveTiles.First();
            if (ActiveTile != null)
            {
                IconicTileData NewTileData = new IconicTileData();
                NewTileData.Count = 0;
                ActiveTile.Update(NewTileData);
            }
        }

        private void Copy_Click(object sender, EventArgs e)
        {
            //ShareStatusTask ShareVersionData = new ShareStatusTask();
            ShareLinkTask ShareVersionData = new ShareLinkTask();

            string TextForClipboard = string.Format(
                "{0}.{1}.{2}.{3}\r\n{4}\r\n{5}\r\nReleased on {6}",
                MajorInput.Text, MinorInput.Text, BuildInput.Text, RevisionInput.Text,
                FriendlyNameShortDisplay.Text, FriendlyNameLongDisplay.Text,
                ReleaseDateDisplay.Text
                );

            // loop through all of the links and add those to 
            // our buffer string
            //
            foreach (UIElement Child in LinkVersionContent.Children)
                if (Child is HyperlinkButton)
                    TextForClipboard += string.Format("\r\n{0}", ((HyperlinkButton)Child).Content);

            //ShareVersionData.Status = TextForClipboard;
            ShareVersionData.Title = "SQL Version";
            ShareVersionData.Message = TextForClipboard;
            ShareVersionData.LinkUri = new Uri("http://microsoft.com");

            ShareVersionData.Show();
            // push the buffer string out to the clipboard for later 
            // use by the user
            //
            //Clipboard.SetText(TextForClipboard);
        }
        private void Schedule_Click(object sender, EventArgs e)
        {
            SaveAppointmentTask NewAppt = new SaveAppointmentTask();
            string DetailsText = string.Format(
                "{0}.{1}.{2}.{3}\r\n{4}\r\n{5}\r\nReleased on {6}",
                MajorInput.Text, MinorInput.Text, BuildInput.Text, RevisionInput.Text,
                FriendlyNameShortDisplay.Text, FriendlyNameLongDisplay.Text,
                ReleaseDateDisplay.Text
                );
            // loop through all of the links and add those to 
            // our buffer string
            //
            foreach (UIElement Child in LinkVersionContent.Children)
                if (Child is HyperlinkButton)
                    DetailsText += string.Format("\r\n{0}", ((HyperlinkButton)Child).Content);

            //NewAppt.StartTime = DateTime.Now.AddDays(1);
            NewAppt.StartTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, 0, 0).AddDays(1);
            NewAppt.EndTime = NewAppt.StartTime.Value.AddHours(1);

            NewAppt.Subject = string.Format("Apply {0}", FriendlyNameShortDisplay.Text);
            NewAppt.Details = DetailsText;

            NewAppt.Reminder = Microsoft.Phone.Tasks.Reminder.FifteenMinutes;
            NewAppt.AppointmentStatus = AppointmentStatus.Busy;

            NewAppt.Show();
        }

        private void RecentReleaseList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0)
                return;

            RecentVersionInfoViewModel SelectedItem = (RecentVersionInfoViewModel)e.AddedItems[0];

            if (SelectedItem != null)
            {
                MajorInput.Text = SelectedItem.Major.ToString();
                MinorInput.Text = SelectedItem.Minor.ToString();
                BuildInput.Text = SelectedItem.Build.ToString();
                RevisionInput.Text = SelectedItem.Revision.ToString();

                // automate the "go" search button
                //
                GoSearch_Click(null, null);

                // shift back to the first panorama view to see the newly 
                // and automatically searched item
                //
                MainPan.DefaultItem = MainPan.Items[0];
            }
        }
        
        private void RefLink_Click(object sender, RoutedEventArgs e)
        {
            WebBrowserTask NewBrowserTask = new WebBrowserTask();
            NewBrowserTask.Uri = new Uri(((HyperlinkButton)sender).Content.ToString(), UriKind.Absolute);
            NewBrowserTask.Show();
        }

        private async Task<IEnumerable<VersionInfoConsumer>> GetMajorReleases()
        {
            return await DataRetriever.GetMajorReleases();
        }
        private async Task<IEnumerable<VersionInfoConsumer>> GetOldestAndRecentSupportedReleases()
        {
            return await DataRetriever.GetRecentAndOldestSupported();
        }
        private void SetMajorReleaseSelector()
        {
            if (_majorMinorReleasesBase == null || _majorMinorReleasesBase.Count() == 0)
                return;

            VersionInfoConsumer FirstToDisplay = _majorMinorReleasesBase.ToList<VersionInfoConsumer>()[0];
            MajorReleaseSelector.Content = FirstToDisplay.FriendlyNameLong;

            SetOldestSupportedRelease(GetOldestSupportedRelease(FirstToDisplay.Major, FirstToDisplay.Minor));
            SetNewestSupportedRelease(GetNewestSupportedRelease(FirstToDisplay.Major, FirstToDisplay.Minor));
        }
        private void MajorReleaseSelector_Click(object sender, RoutedEventArgs e)
        {
            // do a sanity check to make sure we have items and it's instantiated
            //
            if (_majorMinorReleasesBase == null || _majorMinorReleasesBase.Count() == 0)
                return;

            // cache the count of items
            //
            int MajorReleasesCount = _majorMinorReleasesBase.Count();

            // retrieve the current item (button text)
            //
            string CurrentMajorRelease = MajorReleaseSelector.Content.ToString();
            // look for this in the collection
            //
            int CurrentReleaseIdx = _majorMinorReleasesBase.ToList<VersionInfoConsumer>().FindIndex(m => m.FriendlyNameLong == CurrentMajorRelease);
            int NewReleaseIdx;

            // take the appropriate action to set the new index depending
            // on where the current index is and the legitimacy for the 
            // next item
            //
            if (CurrentReleaseIdx < 0)
                return;
            else if (CurrentReleaseIdx == MajorReleasesCount - 1)
                NewReleaseIdx = 0;
            else
                NewReleaseIdx = CurrentReleaseIdx + 1;

            // cache the next major release to show
            //
            VersionInfoConsumer NextReleaseToShow = _majorMinorReleasesBase.ToList<VersionInfoConsumer>()[NewReleaseIdx];

            // set the button's text to now reflect the *next* item
            //
            MajorReleaseSelector.Content = NextReleaseToShow.FriendlyNameLong;

            // retrieve the data and display accordingly
            //
            SetOldestSupportedRelease(GetOldestSupportedRelease(NextReleaseToShow.Major, NextReleaseToShow.Minor));
            SetNewestSupportedRelease(GetNewestSupportedRelease(NextReleaseToShow.Major, NextReleaseToShow.Minor));
        }

        private VersionInfoConsumer GetOldestSupportedRelease(int major, int minor)
        {
            if (_oldestAndRecentSupportedVersions == null)
                return null;

            try
            {
                return _oldestAndRecentSupportedVersions.
                    Where(m => m.Major == major && m.Minor == minor).
                    OrderBy(m => m.Build).OrderBy(m => m.Revision).
                    First();
            }
            catch
            {
                return null;
            }
        }
        private VersionInfoConsumer GetNewestSupportedRelease(int major, int minor)
        {
            if (_oldestAndRecentSupportedVersions == null)
                return null;

            try
            {
                return _oldestAndRecentSupportedVersions.
                    Where(m => m.Major == major && m.Minor == minor).
                    OrderByDescending(m => m.Build).OrderByDescending(m => m.Revision).
                    First();
            }
            catch
            {
                return null;
            }
        }

        private void SetOldestSupportedRelease(VersionInfoConsumer versionInfoConsumer)
        {
            if (versionInfoConsumer == null)
            {
                _oldestSupportedVersion = null;
                OldestName.Content = "none";
                OldestDesc.Text = "";
                return;
            }

            _oldestSupportedVersion = versionInfoConsumer;
            OldestName.Content =
                string.Format("{0} ({1}.{2}.{3}.{4})",
                    versionInfoConsumer.FriendlyNameShort,
                    versionInfoConsumer.Major,
                    versionInfoConsumer.Minor,
                    versionInfoConsumer.Build,
                    versionInfoConsumer.Revision);

            OldestDesc.Text = versionInfoConsumer.FriendlyNameLong;
        }
        private void SetNewestSupportedRelease(VersionInfoConsumer versionInfoConsumer)
        {
            if (versionInfoConsumer == null)
            {
                _newestSupportedVersion = null;
                MostRecentName.Content = "none";
                MostRecentDesc.Text = "";
                return;
            }

            _newestSupportedVersion = versionInfoConsumer;
            MostRecentName.Content =
                string.Format("{0} ({1}.{2}.{3}.{4})",
                    versionInfoConsumer.FriendlyNameShort,
                    versionInfoConsumer.Major,
                    versionInfoConsumer.Minor,
                    versionInfoConsumer.Build,
                    versionInfoConsumer.Revision);

            MostRecentDesc.Text = versionInfoConsumer.FriendlyNameLong;
        }

        private void MostRecentName_Click(object sender, RoutedEventArgs e)
        {
            if (_newestSupportedVersion != null)
            {
                MajorInput.Text = _newestSupportedVersion.Major.ToString();
                MinorInput.Text = _newestSupportedVersion.Minor.ToString();
                BuildInput.Text = _newestSupportedVersion.Build.ToString();
                RevisionInput.Text = _newestSupportedVersion.Revision.ToString();

                // automate the "go" search button
                //
                GoSearch_Click(null, null);

                // shift back to the first panorama view to see the newly 
                // and automatically searched item
                //
                MainPan.DefaultItem = MainPan.Items[0];
            }
        }

        private void OldestName_Click(object sender, RoutedEventArgs e)
        {
            if (_oldestSupportedVersion != null)
            {
                MajorInput.Text = _oldestSupportedVersion.Major.ToString();
                MinorInput.Text = _oldestSupportedVersion.Minor.ToString();
                BuildInput.Text = _oldestSupportedVersion.Build.ToString();
                RevisionInput.Text = _oldestSupportedVersion.Revision.ToString();

                // automate the "go" search button
                //
                GoSearch_Click(null, null);

                // shift back to the first panorama view to see the newly 
                // and automatically searched item
                //
                MainPan.DefaultItem = MainPan.Items[0];
            }
        }
    }
}