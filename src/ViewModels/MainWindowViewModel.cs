using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Media;
using spiw.Mvvm;

namespace spiw.ViewModels
{
    internal class MainWindowViewModel : BindableBase
    {
        public MainWindowViewModel()
        {
            WindowTitle = "Screen Preview in The Window";
            WindowWidthMin = 800;
            WindowHeightMin = 600;
            WindowWidth = 800;
            WindowHeight = 600;

            IsImageUpdateTimerEnabled = false;
            IsSettingPanelVisible = true;

            TargetScreens = new ObservableCollection<TargetScreenViewModel>();
            UpdateTargetScreenList();

            ScreenPreviewRefreshRateMin = 1;
            ScreenPreviewRefreshRateMax = 30;
            ScreenPreviewRefreshRate = 30;

            OpenSettingPanelCommand = new RelayCommand(OpenSettingPanel);
            SettingPanelCancelCommand = new RelayCommand(CancelSettingPanel);
            UpdateTargetScreenListCommand = new RelayCommand(UpdateTargetScreenList);
            SettingPanelOkCommand = new RelayCommand(ApplySettings);
            ImageUpdateTimerCommand = new RelayCommand(UpdateScreenImage);
        }

        private string windowTitle;
        public string WindowTitle
        {
            get { return windowTitle; }
            set { SetProperty(ref windowTitle, value); }
        }

        private double windowWidthMin;
        public double WindowWidthMin
        {
            get { return windowWidthMin; }
            set { SetProperty(ref windowWidthMin, value); }
        }

        private double windowHeightMin;
        public double WindowHeightMin
        {
            get { return windowHeightMin; }
            set { SetProperty(ref windowHeightMin, value); }
        }

        private double windowWidth;
        public double WindowWidth
        {
            get { return windowWidth; }
            set { SetProperty(ref windowWidth, value); }
        }

        private double windowHeight;
        public double WindowHeight
        {
            get { return windowHeight; }
            set { SetProperty(ref windowHeight, value); }
        }

        private ImageSource screenPreviewImage;
        public ImageSource ScreenPreviewImage
        {
            get { return screenPreviewImage; }
            set { SetProperty(ref screenPreviewImage, value); }
        }

        private ObservableCollection<TargetScreenViewModel> targetScreens;
        public ObservableCollection<TargetScreenViewModel> TargetScreens
        {
            get { return targetScreens; }
            set { SetProperty(ref targetScreens, value); }
        }

        private TargetScreenViewModel selectedTargetScreen;
        public TargetScreenViewModel SelectedTargetScreen
        {
            get { return selectedTargetScreen; }
            set { SetProperty(ref selectedTargetScreen, value); }
        }

        private int screenPreviewRefreshRateMin;
        public int ScreenPreviewRefreshRateMin
        {
            get { return screenPreviewRefreshRateMin; }
            set { SetProperty(ref screenPreviewRefreshRateMin, value); }
        }

        private int screenPreviewRefreshRateMax;
        public int ScreenPreviewRefreshRateMax
        {
            get { return screenPreviewRefreshRateMax; }
            set { SetProperty(ref screenPreviewRefreshRateMax, value); }
        }

        private int screenPreviewRefreshRate;
        public int ScreenPreviewRefreshRate
        {
            get { return screenPreviewRefreshRate; }
            set
            {
                if (value >= ScreenPreviewRefreshRateMin && value <= ScreenPreviewRefreshRateMax)
                {
                    SetProperty(ref screenPreviewRefreshRate, value);
                }
            }
        }

        private bool isSettingPanelVisible;
        public bool IsSettingPanelVisible
        {
            get { return isSettingPanelVisible; }
            set { SetProperty(ref isSettingPanelVisible, value); }
        }

        private bool isImageUpdateTimerEnabled;
        public bool IsImageUpdateTimerEnabled
        {
            get { return isImageUpdateTimerEnabled; }
            set { SetProperty(ref isImageUpdateTimerEnabled, value); }
        }

        public RelayCommand OpenSettingPanelCommand { get; private set; }

        private void OpenSettingPanel()
        {
            IsImageUpdateTimerEnabled = false;
            IsSettingPanelVisible = true;
        }

        public RelayCommand SettingPanelOkCommand { get; private set; }

        private void ApplySettings()
        {
            IsImageUpdateTimerEnabled = true;
            IsSettingPanelVisible = false;
        }

        public RelayCommand SettingPanelCancelCommand { get; private set; }

        private void CancelSettingPanel()
        {
            IsImageUpdateTimerEnabled = true;
            IsSettingPanelVisible = false;
        }

        public RelayCommand UpdateTargetScreenListCommand { get; private set; }

        private void UpdateTargetScreenList()
        {
            var displayConfigInfoList = DisplayConfig.GetDisplayConfigInfo();

            this.TargetScreens.Clear();
            foreach (var configInfo in displayConfigInfoList)
            {
                this.TargetScreens.Add(new TargetScreenViewModel()
                {
                    FriendlyName = configInfo.FriendlyName,
                    Left = configInfo.LeftPosition,
                    Top = configInfo.TopPosition,
                    Width = (int)configInfo.HorizontalResolution,
                    Height = (int)configInfo.VerticalResolution,
                });
            }

            this.SelectedTargetScreen = this.TargetScreens[0];
        }

        public RelayCommand ImageUpdateTimerCommand { get; private set; }

        private async void UpdateScreenImage()
        {
            var bitmapSource = await Task.Run(() =>
            {
                return ScreenCapture.GetScreenImage(SelectedTargetScreen.Left, SelectedTargetScreen.Top, SelectedTargetScreen.Width, SelectedTargetScreen.Height);
            });
            ScreenPreviewImage = bitmapSource;
            //GC.Collect();
        }
    }
}
