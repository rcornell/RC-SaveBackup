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

        private static Brush _background;
        private static Brush _listBoxBackground;
        private static Brush _text;
        private static ObservableCollection<ImageBrush> _backgroundImages;
        private static ObservableCollection<Brush> _brushes;
        private static Uri _bluePath = new Uri("@Assets\\bluewhite.jpg", UriKind.Relative);
        private static Uri _darkPath = new Uri("@Assets\\metro.jpg", UriKind.Relative);
        

        static Theme() {
            var blue = new ImageBrush() {ImageSource = new BitmapImage(_bluePath)};
            var dark = new ImageBrush() {ImageSource = new BitmapImage(_darkPath)};
            _backgroundImages= new ObservableCollection<ImageBrush>() {blue, dark};
        }

        public static ObservableCollection<Brush> ToggleTheme(int theme) {
            var bc = new BrushConverter();
            var darkBackgroundBrush = (Brush)bc.ConvertFrom("#4C575757");
            if (darkBackgroundBrush != null)
                darkBackgroundBrush.Freeze();
            
            //_background currently doesn't matter
            _background = (theme == 0) ? Brushes.DeepSkyBlue : darkBackgroundBrush;
            //_listBoxBackground = (theme == 0) ? Brushes.White : darkBackgroundBrush;
            _listBoxBackground = darkBackgroundBrush;
            _text = (theme == 0) ? Brushes.Black : Brushes.White;
            
            _brushes = new ObservableCollection<Brush>(){_background, _listBoxBackground, _text};
            return _brushes;
        }


    }
}

