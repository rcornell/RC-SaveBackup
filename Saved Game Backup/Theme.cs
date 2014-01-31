using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Saved_Game_Backup
{
    public class Theme {

        private static Brush _background;
        private static Brush _listBoxBackground;
        private static Brush _text;
        private static ObservableCollection<Brush> _brushes; 

        public Theme() {
            
        }

        public static ObservableCollection<Brush> ToggleTheme(int theme) {
            var bc = new BrushConverter();
            var darkBackgroundBrush = (Brush)bc.ConvertFrom("#4C575757");
            if (darkBackgroundBrush != null)
                darkBackgroundBrush.Freeze();
            _background = (theme == 0) ? Brushes.DeepSkyBlue : darkBackgroundBrush;
            _listBoxBackground = (theme == 0) ? Brushes.White : darkBackgroundBrush;
            _text = (theme == 0) ? Brushes.Black : Brushes.White;
            
            _brushes = new ObservableCollection<Brush>(){_background, _listBoxBackground, _text};
            return _brushes;
        }


    }
}

