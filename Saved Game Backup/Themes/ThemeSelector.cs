using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GalaSoft.MvvmLight;

namespace Saved_Game_Backup.Themes
{
    public class ThemeSelector : ObservableObject {

        private ImageBrush _backgroundImage;
        public ImageBrush BackgroundImage
        {
            get { return _backgroundImage; }
            set
            {
                _backgroundImage = value;
                RaisePropertyChanged(() => BackgroundImage);
            }
        }

        private Brush _listBoxBackgroundBrush;
        public Brush ListBoxBackgroundBrush
        {
            get { return _listBoxBackgroundBrush; }
            set
            {
                _listBoxBackgroundBrush = value;
                RaisePropertyChanged(() => ListBoxBackgroundBrush);
            }
        }
        private Brush _textBrush;
        public Brush TextBrush
        {
            get { return _textBrush; }
            set
            {
                _textBrush = value;
                RaisePropertyChanged(() => TextBrush);
            }
        }

        private static readonly Uri BluePath = new Uri(@"pack://application:,,,/Assets/bluewhite.jpg", UriKind.Absolute);
        private static readonly Uri DarkPath = new Uri(@"pack://application:,,,/Assets/metro.jpg", UriKind.Absolute);
        private static readonly Uri RapPath = new Uri(@"pack://application:,,,/Assets/rap.jpg", UriKind.Absolute);
        private static readonly ImageBrush _blueBackground = new ImageBrush() { ImageSource = new BitmapImage(BluePath) };
        private static readonly ImageBrush _darkBackground = new ImageBrush() { ImageSource = new BitmapImage(DarkPath) };
        private static readonly ImageBrush _rapBackground = new ImageBrush() { ImageSource = new BitmapImage(RapPath) };
        private static Theme _blueTheme;
        private static Theme _darkTheme;
        private static Theme _rapTheme;
        private readonly ObservableCollection<Theme> _themes;
        //Singleton of this class?
        
        public ThemeSelector() {
            var listDarkBackground = (Color)ColorConverter.ConvertFromString("#99575757");
            var buttonTopDark = (Color)ColorConverter.ConvertFromString("#32FFFFFF");
            var buttonBottomDark = (Color)ColorConverter.ConvertFromString("#3CFFFFFF");
            var buttonTopLight = (Color)ColorConverter.ConvertFromString("#7FFFFFFF");
            var buttonBottomLight = (Color)ColorConverter.ConvertFromString("#7FE8E8E8");
            var mouseEnterDark = (Color) ColorConverter.ConvertFromString("#5AFFFFFF");
            var mouseEnterLight = (Color)ColorConverter.ConvertFromString("#5AFFFFFF"); //FIX THIS
            _blueTheme = new Theme(_blueBackground, listDarkBackground, Colors.White, buttonTopDark, buttonBottomDark, mouseEnterDark);
            _darkTheme = new Theme(_darkBackground, listDarkBackground, Colors.White, buttonTopDark, buttonBottomDark, mouseEnterDark);
            _rapTheme = new Theme(_rapBackground, listDarkBackground, Colors.Black, buttonTopLight, buttonBottomLight, mouseEnterLight);
            _themes = new ObservableCollection<Theme>(){_blueTheme, _darkTheme, _rapTheme};
        }

        public Theme ToggleTheme(int theme) {
            return _themes[theme];
        }
    }
}

