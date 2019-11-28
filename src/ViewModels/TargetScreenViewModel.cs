using spiw.Mvvm;

namespace spiw.ViewModels
{
    internal class TargetScreenViewModel : BindableBase
    {
        private string friendlyName;
        public string FriendlyName
        {
            get { return friendlyName; }
            set { SetProperty(ref friendlyName, value); }
        }

        private int left;
        public int Left
        {
            get { return left; }
            set { SetProperty(ref left, value); }
        }

        private int top;
        public int Top
        {
            get { return top; }
            set { SetProperty(ref top, value); }
        }

        private int width;
        public int Width
        {
            get { return width; }
            set { SetProperty(ref width, value); }
        }

        private int height;
        public int Height
        {
            get { return height; }
            set { SetProperty(ref height, value); }
        }
    }
}
