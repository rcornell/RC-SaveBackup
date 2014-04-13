using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Saved_Game_Backup {
    [Serializable]
    public class WrapPanelGame : Control {
        static WrapPanelGame() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof (WrapPanelGame),
                new FrameworkPropertyMetadata(typeof (WrapPanelGame)));
        }

        public string Title {
            get { return (string) GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof (string), typeof (WrapPanelGame),
                new PropertyMetadata(default(string)));

        public Game Game {
            get { return (Game) GetValue(GameProperty); }
            set { SetValue(GameProperty, value); }
        }

        public static readonly DependencyProperty GameProperty =
            DependencyProperty.Register("Game", typeof (Game), typeof (WrapPanelGame),
                new PropertyMetadata(default(Game)));


        public ImageSource ThumbnailSource {
            get { return (ImageSource) GetValue(ThumbnailSourceProperty); }
            set { SetValue(ThumbnailSourceProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ThumbnailSource.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ThumbnailSourceProperty =
            DependencyProperty.Register("ThumbnailSource", typeof (ImageSource), typeof (WrapPanelGame),
                new PropertyMetadata(default(ImageSource)));


        public string ThumbnailPath {
            get { return (string) GetValue(ThumbnailPathProperty); }
            set { SetValue(ThumbnailPathProperty, value); }
        }

        public static readonly DependencyProperty ThumbnailPathProperty =
            DependencyProperty.Register("ThumbnailPath", typeof (string), typeof (WrapPanelGame),
                new PropertyMetadata(default(string)));


        public double ThumbnailHeight {
            get { return (double) GetValue(ThumbnailHeightProperty); }
            set { SetValue(ThumbnailHeightProperty, value); }
        }

        public static readonly DependencyProperty ThumbnailHeightProperty =
            DependencyProperty.Register("ThumbnailHeight", typeof (double), typeof (WrapPanelGame),
                new PropertyMetadata(default(double)));


        public double ThumbnailWidth {
            get { return (double) GetValue(ThumbnailWidthProperty); }
            set { SetValue(ThumbnailWidthProperty, value); }
        }

        public static readonly DependencyProperty ThumbnailWidthProperty =
            DependencyProperty.Register("ThumbnailWidth", typeof (double), typeof (WrapPanelGame),
                new PropertyMetadata(default(double)));


        public Stretch Stretch {
            get { return (Stretch) GetValue(StretchProperty); }
            set { SetValue(StretchProperty, value); }
        }

        public static readonly DependencyProperty StretchProperty =
            DependencyProperty.Register("Stretch", typeof (Stretch), typeof (WrapPanelGame),
                new PropertyMetadata(default(Stretch)));


        public StretchDirection StretchDirection {
            get { return (StretchDirection) GetValue(StretchDirectionProperty); }
            set { SetValue(StretchDirectionProperty, value); }
        }

        public static readonly DependencyProperty StretchDirectionProperty =
            DependencyProperty.Register("StretchDirection", typeof (StretchDirection), typeof (WrapPanelGame),
                new PropertyMetadata(default(StretchDirection)));


        public new bool IsMouseOver {
            get { return (bool) GetValue(IsMouseOverProperty); }
            set { SetValue(IsMouseOverProperty, value); }
        }

        public new static readonly DependencyProperty IsMouseOverProperty =
            DependencyProperty.Register("IsMouseOver", typeof (bool), typeof (WrapPanelGame),
                new PropertyMetadata(default(bool)));


        public bool IsSelected {
            get { return (bool) GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }

        public static readonly DependencyProperty IsSelectedProperty =
            DependencyProperty.Register("IsSelected", typeof (bool), typeof (WrapPanelGame),
                new PropertyMetadata(default(bool)));


        public double BorderHeight {
            get { return (double) GetValue(BorderHeightProperty); }
            set { SetValue(BorderHeightProperty, value); }
        }

        public static readonly DependencyProperty BorderHeightProperty =
            DependencyProperty.Register("BorderHeight", typeof (double), typeof (WrapPanelGame),
                new PropertyMetadata(default(double)));


        public double BorderWidth {
            get { return (double) GetValue(BorderWidthProperty); }
            set { SetValue(BorderWidthProperty, value); }
        }


        public static readonly DependencyProperty BorderWidthProperty =
            DependencyProperty.Register("BorderWidth", typeof (double), typeof (WrapPanelGame),
                new PropertyMetadata(default(double)));
    }
}