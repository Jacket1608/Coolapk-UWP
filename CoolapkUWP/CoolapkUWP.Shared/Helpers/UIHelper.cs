﻿using Microsoft.Toolkit.Uwp.UI.Extensions;
using System;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CoolapkUWP.Pages;
using Windows.ApplicationModel.Core;
using Windows.Foundation.Metadata;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using System.Net.Http;
using Windows.ApplicationModel.Resources;
using Windows.UI.Notifications;
using Windows.Data.Xml.Dom;

namespace CoolapkUWP.Helpers
{
    internal static partial class UIHelper
    {
        public const int Duration = 3000;
        public static bool IsShowingProgressBar, IsShowingMessage;
        public static bool HasTitleBar => !CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar;
        public static bool HasStatusBar => ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar");
        private static readonly ObservableCollection<string> MessageList = new ObservableCollection<string>();

        private static CoreDispatcher shellDispatcher;
        public static CoreDispatcher ShellDispatcher
        {
            get => shellDispatcher;
            set
            {
                if (shellDispatcher != value)
                {
                    shellDispatcher = value;
                }
            }
        }

        public static async void ShowProgressBar()
        {
            IsShowingProgressBar = true;
            if (HasStatusBar)
            {
                MainPage?.HideProgressBar();
                StatusBar.GetForCurrentView().ProgressIndicator.ProgressValue = null;
                await StatusBar.GetForCurrentView().ProgressIndicator.ShowAsync();
            }
            else
            {
                MainPage?.ShowProgressBar();
            }
        }

        public static async void ShowProgressBar(double value = 0)
        {
            IsShowingProgressBar = true;
            if (HasStatusBar)
            {
                MainPage?.HideProgressBar();
                StatusBar.GetForCurrentView().ProgressIndicator.ProgressValue = value * 0.01;
                await StatusBar.GetForCurrentView().ProgressIndicator.ShowAsync();
            }
            else
            {
                MainPage?.ShowProgressBar(value);
            }
        }

        public static async void PausedProgressBar()
        {
            IsShowingProgressBar = true;
            if (HasStatusBar)
            {
                await StatusBar.GetForCurrentView().ProgressIndicator.HideAsync();
            }
            MainPage?.PausedProgressBar();
        }

        public static async void ErrorProgressBar()
        {
            IsShowingProgressBar = true;
            if (HasStatusBar)
            {
                await StatusBar.GetForCurrentView().ProgressIndicator.HideAsync();
            }
            MainPage?.ErrorProgressBar();
        }

        public static async void HideProgressBar()
        {
            IsShowingProgressBar = false;
            if (HasStatusBar)
            {
                await StatusBar.GetForCurrentView().ProgressIndicator.HideAsync();
            }
            MainPage?.HideProgressBar();
        }

        public static async void ShowMessage(string message)
        {
            MessageList.Add(message);
            if (!IsShowingMessage)
            {
                IsShowingMessage = true;
                while (MessageList.Count > 0)
                {
                    if (HasStatusBar)
                    {
                        StatusBar statusBar = StatusBar.GetForCurrentView();
                        if (!string.IsNullOrEmpty(MessageList[0]))
                        {
                            statusBar.ProgressIndicator.Text = $"[{MessageList.Count}] {MessageList[0].Replace("\n", " ")}";
                            statusBar.ProgressIndicator.ProgressValue = IsShowingProgressBar ? null : (double?)0;
                            await statusBar.ProgressIndicator.ShowAsync();
                            await Task.Delay(Duration);
                        }
                        MessageList.RemoveAt(0);
                        if (MessageList.Count == 0 && !IsShowingProgressBar) { await statusBar.ProgressIndicator.HideAsync(); }
                        statusBar.ProgressIndicator.Text = string.Empty;
                    }
                    else if (MainPage != null)
                    {
                        if (!string.IsNullOrEmpty(MessageList[0]))
                        {
                            string messages = $"[{MessageList.Count}] {MessageList[0].Replace("\n", " ")}";
                            MainPage.ShowMessage(messages);
                            await Task.Delay(Duration);
                        }
                        MessageList.RemoveAt(0);
                        if (MessageList.Count == 0)
                        {
                            MainPage.ShowMessage();
                        }
                    }
                }
                IsShowingMessage = false;
            }
        }

        public static void ShowInAppMessage(MessageType type, string message = null)
        {
            switch (type)
            {
                case MessageType.Message:
                    ShowMessage(message);
                    break;
                default:
                    ShowMessage(type.ConvertMessageTypeToMessage());
                    break;
            }
        }

        public static void ShowHttpExceptionMessage(HttpRequestException e)
        {
            if (e.Message.IndexOfAny(new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' }) != -1)
            { ShowInAppMessage(MessageType.Message, $"服务器错误： {e.Message.Replace("Response status code does not indicate success: ", string.Empty)}"); }
            else if (e.Message == "An error occurred while sending the request.") { ShowInAppMessage(MessageType.Message, "无法连接网络。"); }
            else { ShowInAppMessage(MessageType.Message, $"请检查网络连接。 {e.Message}"); }
        }

        public static bool IsOriginSource(object source, object originalSource)
        {
            bool r = false;
            DependencyObject DependencyObject = originalSource as DependencyObject;
            if (DependencyObject.FindAscendant<Button>() == null && DependencyObject.FindAscendant<AppBarButton>() == null && originalSource.GetType() != typeof(Button) && originalSource.GetType() != typeof(AppBarButton) && originalSource.GetType() != typeof(RichEditBox))
            {
                if (source is FrameworkElement FrameworkElement)
                {
                    r = source == DependencyObject.FindAscendantByName(FrameworkElement.Name);
                }
            }
            return source == originalSource || r;
        }

        public static string ConvertMessageTypeToMessage(this MessageType type)
        {
            switch (type)
            {
                case MessageType.NoMore:
                    return ResourceLoader.GetForViewIndependentUse("NotificationsPage").GetString("NoMore");

                case MessageType.NoMoreShare:
                    return ResourceLoader.GetForViewIndependentUse("NotificationsPage").GetString("NoMoreShare");

                case MessageType.NoMoreReply:
                    return ResourceLoader.GetForViewIndependentUse("NotificationsPage").GetString("NoMoreReply");

                case MessageType.NoMoreHotReply:
                    return ResourceLoader.GetForViewIndependentUse("NotificationsPage").GetString("NoMoreHotReply");

                case MessageType.NoMoreLikeUser:
                    return ResourceLoader.GetForViewIndependentUse("NotificationsPage").GetString("NoMoreLikeUser");

                default: return string.Empty;
            }
        }

        public static void SetBadgeNumber(string badgeGlyphValue)
        {
            // Get the blank badge XML payload for a badge number
            XmlDocument badgeXml =
                BadgeUpdateManager.GetTemplateContent(BadgeTemplateType.BadgeNumber);
            // Set the value of the badge in the XML to our number
            XmlElement badgeElement = badgeXml.SelectSingleNode("/badge") as XmlElement;
            badgeElement.SetAttribute("value", badgeGlyphValue);
            // Create the badge notification
            BadgeNotification badge = new BadgeNotification(badgeXml);
            // Create the badge updater for the application
            BadgeUpdater badgeUpdater =
                BadgeUpdateManager.CreateBadgeUpdaterForApplication();
            // And update the badge
            badgeUpdater.Update(badge);
        }
    }

    public enum NavigationThemeTransition
    {
        Default,
        Entrance,
        DrillIn,
        Suppress
    }

    internal static partial class UIHelper
    {
        public static MainPage MainPage;

        public static void Navigate(Type pageType, object e = null, NavigationThemeTransition Type = NavigationThemeTransition.Default)
        {
            switch (Type)
            {
                case NavigationThemeTransition.DrillIn:
                    _ = (MainPage?.NavigationViewFrame.Navigate(pageType, e, new DrillInNavigationTransitionInfo()));
                    break;
                case NavigationThemeTransition.Entrance:
                    _ = (MainPage?.NavigationViewFrame.Navigate(pageType, e, new EntranceNavigationTransitionInfo()));
                    break;
                case NavigationThemeTransition.Suppress:
                    _ = (MainPage?.NavigationViewFrame.Navigate(pageType, e, new SuppressNavigationTransitionInfo()));
                    break;
                case NavigationThemeTransition.Default:
                    _ = (MainPage?.NavigationViewFrame.Navigate(pageType, e));
                    break;
                default:
                    _ = (MainPage?.NavigationViewFrame.Navigate(pageType, e));
                    break;
            }
        }

        private static readonly ImmutableArray<string> routes = new string[]
        {
            "/u/",
            "/feed/",
            "/picture/",
            "/t/",
            "t/",
            "http://image.coolapk.com/",
            "https",
            "http",
            "www.coolapk.com",
        }.ToImmutableArray();

        private static bool IsFirst(this string str, int i) => str.IndexOf(routes[i], StringComparison.Ordinal) == 0;

        private static string Replace(this string str, int oldText)
        {
            return oldText == -1
                ? str.Replace("https://www.coolapk.com", string.Empty)
                : oldText == -2
                    ? str.Replace("http://www.coolapk.com", string.Empty)
                    : oldText == -3
                        ? str.Replace("www.coolapk.com", string.Empty)
                        : oldText < 0
                            ? throw new Exception($"i = {oldText}")
                            : str.Replace(routes[oldText], string.Empty);
        }

        public static async void OpenLinkAsync(string str)
        {
            string rawstr = str;
            if (string.IsNullOrWhiteSpace(str)) { return; }
            int i = 0;
            if (str.IsFirst(i++))
            {
                string u = str.Replace(i - 1);
                string uid = int.TryParse(u, out _) ? u : (await NetworkHelper.GetUserInfoByNameAsync(u)).UID;
                //FeedListViewModel f = FeedListViewModel.GetProvider(FeedListType.UserPageList, uid);
                //if (f != null)
                //{
                //    Navigate(typeof(FeedListPage), f);
                //}
            }
            else if (str.IsFirst(i++) || str.IsFirst(i++))
            {
                if (str == "/feed/writer") { ShowMessage("暂不支持"); }
                //else { Navigate(typeof(FeedShellPage), new FeedDetailViewModel(str.Replace(i - 1))); }
            }
            else if (str.IsFirst(i++) || str.IsFirst(i++))
            {
                string u = str.Replace(i - 1);
                if (u.Contains("?type=")) { u = u.Substring(0, u.IndexOf('?')); }
                //FeedListViewModel f = FeedListViewModel.GetProvider(FeedListType.TagPageList, u);
                //if (f != null)
                //{
                //    Navigate(typeof(FeedListPage), f);
                //}
            }
            else if (str.IsFirst(i++))
            {
                //ShowImage(new ImageModel(str, ImageType.SmallImage));
            }
            else if (str.IsFirst(i++))
            {
                if (str.Contains("coolapk.com"))
                {
                    OpenLinkAsync(str.Replace(-1));
                }
                else
                {
                    //Navigate(typeof(BrowserPage), new object[] { false, str });
                }
            }
            else if (str.IsFirst(i++))
            {
                if (str.Contains("coolapk.com"))
                {
                    OpenLinkAsync(str.Replace(-2));
                }
                else
                {
                    //Navigate(typeof(BrowserPage), new object[] { false, str });
                }
            }
            else if (str.IsFirst(i++))
            {
                OpenLinkAsync(str.Replace(-3));
            }
        }
    }

    public enum MessageType
    {
        Message,
        NoMore,
        NoMoreReply,
        NoMoreLikeUser,
        NoMoreShare,
        NoMoreHotReply,
    }
}