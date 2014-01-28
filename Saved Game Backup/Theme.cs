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
        private static ObservableCollection<Brush> _brushes; 

        public Theme() {
            
        }

        public static ObservableCollection<Brush> ToggleTheme(int theme) {
            var bc = new BrushConverter();
            var darkBrush = (Brush)bc.ConvertFrom("#FF2D2D30");
            if (darkBrush != null)
                darkBrush.Freeze();
            _background = (theme == 0) ? Brushes.DeepSkyBlue : darkBrush;
            _listBoxBackground = (theme == 0) ? Brushes.White : Brushes.DimGray;
            
            _brushes = new ObservableCollection<Brush>(){_background, _listBoxBackground};
            return _brushes;
        }


    }
}

