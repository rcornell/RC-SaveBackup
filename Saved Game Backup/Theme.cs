using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Saved_Game_Backup
{
    public class Theme {

        private static Brush _background;

        public Theme() {
            
        }

        public static Brush ToggleTheme(int theme) {
            var bc = new BrushConverter();
            var darkBrush = (Brush)bc.ConvertFrom("#FF2D2D30");
            if (darkBrush != null)
                darkBrush.Freeze();
            _background = (theme == 0) ? Brushes.DeepSkyBlue : darkBrush;
            return _background;
        }


    }
}
