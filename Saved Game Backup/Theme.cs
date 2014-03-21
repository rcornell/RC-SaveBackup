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

        private Color _listBoxBackgroundBrush;
        public Color ListBoxBackgroundBrush
        {
            get { return _listBoxBackgroundBrush; }
            set
            {
                _listBoxBackgroundBrush = value;
                RaisePropertyChanged(() => ListBoxBackgroundBrush);
            }
        }

        private Color _textBrush;
        public Color TextBrush
        {
            get { return _textBrush; }
            set
            {
                _textBrush = value;
                RaisePropertyChanged(() => TextBrush);
            }
        }

        private Color _buttonGradientTop;
        public Color ButtonGradientTop { 
            get { return _buttonGradientTop; }
            set {
                _buttonGradientTop = value;
                RaisePropertyChanged(() => ButtonGradientTop);
            } 
        }

        private Color _buttonGradientBottom;
        public Color ButtonGradientBottom 
            { 
            get { return _buttonGradientBottom; }
            set {
                _buttonGradientBottom = value;
                RaisePropertyChanged(() => ButtonGradientBottom);
            } 
        }

        public SolidColorBrush GradientTopColor {
            get { return new SolidColorBrush(ButtonGradientTop);}
        }

        public Theme(ImageBrush imagebrush, Color listBoxBackgroundBrush, Color textBrush, Color buttonTop, Color buttonBottom) {
            BackgroundImage = imagebrush;
            ListBoxBackgroundBrush = listBoxBackgroundBrush;
            TextBrush = textBrush;
            ButtonGradientTop = buttonTop;
            ButtonGradientBottom = buttonBottom;
        }
    }
}
