using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Saved_Game_Backup.Assets
{
    /// <summary>
    /// Follow steps 1a or 1b and then 2 to use this custom control in a XAML file.
    ///
    /// Step 1a) Using this custom control in a XAML file that exists in the current project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:Saved_Game_Backup.Assets"
    ///
    ///
    /// Step 1b) Using this custom control in a XAML file that exists in a different project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:Saved_Game_Backup.Assets;assembly=Saved_Game_Backup.Assets"
    ///
    /// You will also need to add a project reference from the project where the XAML file lives
    /// to this project and Rebuild to avoid compilation errors:
    ///
    ///     Right click on the target project in the Solution Explorer and
    ///     "Add Reference"->"Projects"->[Browse to and select this project]
    ///
    ///
    /// Step 2)
    /// Go ahead and use your control in the XAML file.
    ///
    ///     <MyNamespace:GameIconControl/>
    ///
    /// </summary>
    public class GameIconControl : Control
    {
        static GameIconControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(GameIconControl), new FrameworkPropertyMetadata(typeof(GameIconControl)));
        }

        public GameIconControl(string name, BitmapImage icon)
        {
            //DefaultStyleKeyProperty.OverrideMetadata(typeof(GameIconControl), new FrameworkPropertyMetadata(typeof(GameIconControl)));
            Icon = icon;
            GameName = name;
        }

        public const string IconPropertyName = "Icon";
        public const string GameNamePropertyName = "GameName";



        public string GameName
        {
            get { return (string)GetValue(GameNameProperty); }
            set { SetValue(GameNameProperty, value); }
        }

        // Using a DependencyProperty as the backing store for GameName.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty GameNameProperty =
            DependencyProperty.Register(GameNamePropertyName, typeof(string), typeof(GameIconControl), new UIPropertyMetadata(default(string)));

        

        public BitmapImage Icon
        {
            get { return (BitmapImage)GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Icon.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(IconPropertyName, typeof(BitmapImage), typeof(GameIconControl), new PropertyMetadata(default(FrameworkElement)));

        
        
    }
}
