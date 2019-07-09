using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MediaElementPOC
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private enum VideoState
        {
            Undefined,
            Playing,
            Paused,
            Stopped
        }

        private const double ButtonsOpacity = 0.5;
        private const string TimestampFormat = @"hh\:mm\:ss";

        private Timer _timer;
        private VideoState _mediaElementState = VideoState.Undefined;

        private int _seekInterval = 10;
        public string SeekIntervalText
        {
            get => _seekInterval.ToString();
            set
            {
                if (int.TryParse(value, out int interval))
                {
                    _seekInterval = interval;
                }
            }
        } 

        public MainWindow()
        {
            InitializeComponent();

            DataContext = this;
            SetPlaybackButtonsState(true, false, false);

            _timer = new Timer(TimestampLabelUpdateTimerTicked);

            mediaPanel.MediaOpened += MediaPanelMediaOpened;

            KeyDown += OnMediaElementKeyDown;
            Closing += MainWindowClosing;
        }

        private void MediaPanelMediaOpened(object sender, RoutedEventArgs e)
        {
            if(mediaPanel.NaturalDuration.HasTimeSpan)
            {
                totalTimestampLabel.Content = mediaPanel.NaturalDuration.TimeSpan.ToString(TimestampFormat);
            }

            sizeLabel.Content = $"Size: {mediaPanel.NaturalVideoWidth}x{mediaPanel.NaturalVideoHeight}";
        }

        private void TimestampLabelUpdateTimerTicked(object state)
        {
            Dispatcher.Invoke(() => currentTimestampLabel.Content = mediaPanel.Position.ToString(TimestampFormat));
        }

        private void MainWindowClosing(object sender, CancelEventArgs e)
        {
            KeyDown -= OnMediaElementKeyDown;

            _timer?.Change(Timeout.Infinite, Timeout.Infinite);
            _timer?.Dispose();
        }

        private void ButtonHelpClick(object sender, RoutedEventArgs e)
        {
            var helpWindow = new HelpWindow();
            helpWindow.ShowDialog();
        }

        private void OnMediaElementKeyDown(object sender, KeyEventArgs e)
        {
            if (!(_mediaElementState == VideoState.Playing || _mediaElementState == VideoState.Paused))
                return;

            switch (e.Key)
            {
                case Key.Down: mediaPanel.Volume -= 0.1;
                    break;

                case Key.Up: mediaPanel.Volume += 0.1;
                    break;

                case Key.Right: mediaPanel.Position += TimeSpan.FromSeconds(_seekInterval);
                    break;

                case Key.Left: mediaPanel.Position -= TimeSpan.FromSeconds(_seekInterval);
                    break;

                case Key.M:
                    {
                        mediaPanel.IsMuted = !mediaPanel.IsMuted;
                        muteLabel.Visibility = mediaPanel.IsMuted ? Visibility.Visible : Visibility.Hidden;
                    }
                    break;
            }
        }

        private void PlayVideo()
        {
            if (_mediaElementState != VideoState.Playing)
            {
                mediaPanel.Play();
                _mediaElementState = VideoState.Playing;

                SetPlaybackButtonsState(false, true, true);

                _timer.Change(0, 500);
            }
        }

        private void SetPlaybackButtonsState(bool playEnabled, bool pauseEnabled, bool stopEnabled)
        {
            playButton.IsEnabled = playEnabled;
            pauseButton.IsEnabled = pauseEnabled;
            stopButton.IsEnabled = stopEnabled;

            seekIntervalValue.IsEnabled = playEnabled;
            uri.IsEnabled = playEnabled;
        }

        #region Playback Buttons Click Handlers
        private void ButtonPlayClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(uri.Text))
                {
                    if (_mediaElementState != VideoState.Paused)
                    {
                        mediaPanel.LoadedBehavior = MediaState.Manual;
                        mediaPanel.Source = new Uri(uri.Text);
                    }
                    
                    PlayVideo();
                }
                else
                {
                    MessageBox.Show("Video URI should not be empty", "Invalid URI", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to load the video: {ex.Message}", "Loading Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ButtonPauseClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_mediaElementState != VideoState.Paused)
                {
                    mediaPanel.Pause();
                    _mediaElementState = VideoState.Paused;

                    SetPlaybackButtonsState(true, false, true);

                    _timer.Change(Timeout.Infinite, Timeout.Infinite);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to pause the video: {ex.Message}", "Pausing Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }  
        }

        private void ButtonStopClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_mediaElementState != VideoState.Stopped)
                {
                    mediaPanel.Stop();
                    _mediaElementState = VideoState.Stopped;

                    SetPlaybackButtonsState(true, true, false);

                    _timer.Change(Timeout.Infinite, Timeout.Infinite);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to stop the video: {ex.Message}", "Stopping Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Mouse Enter/Leave Click Handlers
        private void ButtonStartMouseEnter(object sender, MouseEventArgs e)
        {
            playButton.Opacity = 1;
        }

        private void ButtonStartMouseLeave(object sender, MouseEventArgs e)
        {
            playButton.Opacity = ButtonsOpacity;
        }

        private void ButtonPauseMouseEnter(object sender, MouseEventArgs e)
        {
            pauseButton.Opacity = 1;
        }

        private void ButtonPauseMouseLeave(object sender, MouseEventArgs e)
        {
            pauseButton.Opacity = ButtonsOpacity;
        }

        private void ButtonStopMouseEnter(object sender, MouseEventArgs e)
        {
            stopButton.Opacity = 1;
        }

        private void ButtonStopMouseLeave(object sender, MouseEventArgs e)
        {
            stopButton.Opacity = ButtonsOpacity;
        }

        #endregion

        private void SeekIntervalValueKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Escape)
            {
                seekIntervalValue.MoveFocus(new TraversalRequest(FocusNavigationDirection.Down));
            }
        }
    }
}
