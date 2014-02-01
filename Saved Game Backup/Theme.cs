using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Saved_Game_Backup
{
    public class Theme {

        private static ImageBrush _backgroundImage;
        private static Brush _listBoxBackground;
        private static Brush _text;
        private static ObservableCollection<ImageBrush> _backgroundImages;
        private static ObservableCollection<object> _brushes;
        //private static Uri _bluePath = new Uri(@"pack://application:Saved_Game_Backup/YourAssembly;component/Assets/bluewhite.jpg", UriKind.Absolute);
        //private static Uri _darkPath = new Uri(@"pack://application:Saved_Game_Backup/YourAssembly;component/Assets/metro.jpg", UriKind.Absolute);
        private static Uri _bluePath = new Uri(@"C:\Users\Rob\Source\Repos\RC-SaveBackup2\Saved Game Backup\Assets\bluewhite.jpg", UriKind.Absolute);
        private static Uri _darkPath = new Uri(@"C:\Users\Rob\Source\Repos\RC-SaveBackup2\Saved Game Backup\Assets\metro.jpg", UriKind.Absolute);
        

        static Theme() {
            var blue = new ImageBrush() {ImageSource = new BitmapImage(_bluePath)};
            var dark = new ImageBrush() {ImageSource = new BitmapImage(_darkPath)};
            _backgroundImages= new ObservableCollection<ImageBrush>() {blue, dark};
        }

        public static ObservableCollection<object> ToggleTheme(int theme) {
            var bc = new BrushConverter();
            var darkBackgroundBrush = (Brush)bc.ConvertFrom("#4C575757");
            if (darkBackgroundBrush != null)
                darkBackgroundBrush.Freeze();
            _backgroundImage = theme == 0 ? _backgroundImages[0] : _backgroundImages[1];
            //_listBoxBackground = (theme == 0) ? Brushes.White : darkBackgroundBrush;
            _listBoxBackground = darkBackgroundBrush;
            
            //Currently setting _text to white in both background cases
            _text = (theme == 0) ? Brushes.White : Brushes.White;
            
            _brushes = new ObservableCollection<object>(){_backgroundImage, _listBoxBackground, _text};
            return _brushes;
        }


    }
}

