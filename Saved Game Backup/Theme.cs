using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using GalaSoft.MvvmLight;

namespace Saved_Game_Backup {
    public class Theme : ObservableObject {

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

        public Theme(ImageBrush imagebrush, Brush listBoxBackgroundBrush, Brush textBrush) {
            BackgroundImage = imagebrush;
            ListBoxBackgroundBrush = listBoxBackgroundBrush;
            TextBrush = textBrush;
        }
    }
}
