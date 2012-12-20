using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AramisStarter;


namespace WpfApplication1
    {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class SolutionSelectingWindow : Window
        {
        private System.Collections.ObjectModel.ObservableCollection<SolutionInfo> solutions;
        private bool editListMode;

        public SolutionSelectingWindow()
            {
            InitializeComponent();
            start( new System.Collections.ObjectModel.ObservableCollection<SolutionInfo>(
                new List<SolutionInfo>() { new SolutionInfo() { SolutionFriendlyName = "Aramis" }
                
                ,new SolutionInfo() { SolutionFriendlyName = "111" }
                ,new SolutionInfo() { SolutionFriendlyName = "222" }
                ,new SolutionInfo() { SolutionFriendlyName = "333333333333333333" }
                ,new SolutionInfo() { SolutionFriendlyName = "4444444" }} ) );
            }

        void start( System.Collections.ObjectModel.ObservableCollection<SolutionInfo> solutions )
            {
            this.solutions = solutions;
            listBox.ItemsSource = solutions;
            solutions.CollectionChanged += solutions_CollectionChanged;
            if (solutions.Count > 0)
                {
                listBox.SelectedIndex = 0;
                }

            UpdateGoButton();
            listBox.ItemTemplate = FindResource( "EditSolutionsList" ) as DataTemplate;

            }

        void solutions_CollectionChanged( object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e )
            {
            UpdateGoButton();
            }

        private void UpdateGoButton()
            {
            goButton.IsEnabled = !editListMode && solutions.Count > 0;
            }

        private void Window_Loaded( object sender, RoutedEventArgs e )
            {


            }

        private void UseAlternativeStyle()
            {
            //backGroungImage.Source = EmbededResourcesConverter.BitmapSourceFromBitmap( Properties.Resources.BBB );
            //ImageSource imgSource = new ImageSource();
            //this.Background = new ImageBrush(imgSource);

            }

        private void GoButton_Click( object sender, RoutedEventArgs e )
            {
            var currentMode = (bool)goButton.GetValue( KeyStateChecker.OkModeProperty );
           // goButton.SetValue( KeyStateChecker.AlternativeContentProperty, "test" );
            goButton.SetValue( KeyStateChecker.OkModeProperty, !currentMode );
            //if (editListMode)
            //    {
            //    }
            //else
            //    {
            //    SelectSolution();
            //    }

            }

        private void SelectSolution()
            {
            if (listBox.SelectedItem != null)
                {
                SelectedSolution = listBox.SelectedItem as SolutionInfo;
                }
            else if (solutions.Count > 0)
                {
                SelectedSolution = solutions[0];
                }
            Close();
            }





        private void ComboBox_SelectionChanged_1( object sender, SelectionChangedEventArgs e )
            {

            }

        internal SolutionInfo SelectedSolution
            {
            get;
            private set;
            }

        private void AddButton_Click( object sender, RoutedEventArgs e )
            {

            }

        private void RemoveButton_Click( object sender, RoutedEventArgs e )
            {
            if (listBox.SelectedItem != null)
                {
                solutions.RemoveAt( listBox.SelectedIndex );
                }
            }



        private void TextBlock_MouseDown_1( object sender, MouseButtonEventArgs e )
            {
            BeginSelectByDown( sender, e );
            if (e.ClickCount >= 2)
                {
                SelectSolution();
                }
            }


        private void Button_Click_1( object sender, RoutedEventArgs e )
            {
            FrameworkElement fd = e.OriginalSource as FrameworkElement;
            if (fd != null)
                {
                SolutionInfo item = fd.DataContext as SolutionInfo;
                if (item != null && solutions.Contains( item ))
                    {
                    solutions.Remove( item );
                    }
                }
            }

        SolutionInfo droppedData = null;
        Image currentImage = null;

        private void BeginSelectByDown( object sender, MouseButtonEventArgs e )
            {
            if (sender is ListBoxItem)
                {
                (sender as ListBoxItem).IsSelected = true;
                }
            currentImage = e.OriginalSource as Image;
            if (currentImage != null)
                {
                if (droppedData == null)
                    {
                    FrameworkElement fd = e.OriginalSource as FrameworkElement;
                    if (fd != null)
                        {
                        SolutionInfo item = fd.DataContext as SolutionInfo;
                        if (item != null && solutions.Contains( item ))
                            {
                            droppedData = item;
                            }
                        }
                    }
                if (droppedData != null)
                    {
                    try
                        {
                        currentImage.Opacity = 1;
                        DragDrop.DoDragDrop( currentImage, droppedData, DragDropEffects.Move );
                        }
                    catch (Exception ex)
                        {
                        Console.WriteLine( ex.ToString() );
                        }
                    }
                }
            }

        void ItemDrop( object sender, DragEventArgs e )
            {
            if (droppedData == null)
                {
                return;
                }
            SolutionInfo target = ((FrameworkElement)(sender)).DataContext as SolutionInfo;
            SolutionInfo dataConnected = e.Data.GetData( typeof( SolutionInfo ) ) as SolutionInfo;
            if (dataConnected == null)
                {
                return;
                }
            if (currentImage != null)
                {
                currentImage.Opacity = 0;
                }
            int removedIdx = solutions.IndexOf( droppedData );
            int targetIdx = solutions.IndexOf( target );

            if (removedIdx < targetIdx)
                {
                solutions.Insert( targetIdx + 1, droppedData );
                solutions.RemoveAt( removedIdx );
                }
            else
                {
                int remIdx = removedIdx + 1;
                if (solutions.Count + 1 > remIdx)
                    {
                    solutions.Insert( targetIdx, droppedData );
                    solutions.RemoveAt( remIdx );
                    }
                }
            droppedData = null;
            }

        private void TextBoxItemDragEnter( object sender, DragEventArgs e )
            {
            e.Effects = DragDropEffects.Move;
            e.Handled = true;
            }

        protected void SelectCurrentItem( object sender, KeyboardFocusChangedEventArgs e )
            {
            ListBoxItem item = (ListBoxItem)sender;
            item.IsSelected = true;
            }

        private void ListBoxItemMouseEnter( object sender, MouseEventArgs e )
            {
            FrameworkElement listBoxItem = sender as FrameworkElement;
            FrameworkElement child = VisualTreeHelperAdditional.GetVisualChild<Image>( listBoxItem );//.FindName( "image" ) as FrameworkElement;
            if (child != null)
                {
                child.Opacity = 1;
                }
            }

        private void ListBoxItemMouseLeave( object sender, MouseEventArgs e )
            {
            if (e.LeftButton == MouseButtonState.Released)
                {
                droppedData = null;
                }
            FrameworkElement listBoxItem = sender as FrameworkElement;
            FrameworkElement child = VisualTreeHelperAdditional.GetVisualChild<Image>( listBoxItem );//.FindName( "image" ) as FrameworkElement;
            if (child != null && droppedData == null)
                {
                child.Opacity = 0;
                }
            }
        }
    //AramisWPFComonents.Reports
    public static class VisualTreeHelperAdditional
        {
        public static T GetVisualChild<T>( DependencyObject parent ) where T : Visual
            {
            T child = default( T );
            int numVisuals = VisualTreeHelper.GetChildrenCount( parent );
            for (int i = 0; i < numVisuals; i++)
                {
                Visual v = (Visual)VisualTreeHelper.GetChild( parent, i );
                child = v as T;
                if (child == null)
                    {
                    child = GetVisualChild<T>( v );
                    }
                if (child != null)
                    {
                    break;
                    }
                }
            return child;
            }
        }

    public class KeyStateChecker
        {

        #region AlternativeContent

        public static string GetAlternativeContent( DependencyObject obj )
            {
            return (string)obj.GetValue( AlternativeContentProperty );
            }

        public static void SetAlternativeContent( DependencyObject obj, string value )
            {
            obj.SetValue( AlternativeContentProperty, value );
            }

        public static readonly DependencyProperty AlternativeContentProperty =
            DependencyProperty.RegisterAttached( "AlternativeContent", typeof( string ), typeof( KeyStateChecker ), new PropertyMetadata( "test" ) );

        #endregion

        #region IsKeyPressed

        public static bool GetIsKeyPressed( DependencyObject obj )
            {
            return (bool)obj.GetValue( IsKeyPressedProperty );
            }

        public static void SetIsKeyPressed( DependencyObject obj, bool value )
            {
            obj.SetValue( IsKeyPressedProperty, value );
            }

        public static readonly DependencyProperty IsKeyPressedProperty =
            DependencyProperty.RegisterAttached( "IsKeyPressed", typeof( bool ), typeof( KeyStateChecker ), new PropertyMetadata( false ) );

        #endregion

        #region OkMode

        public static bool GetOkMode( DependencyObject obj )
            {
            return (bool)obj.GetValue( OkModeProperty );
            }

        public static void SetOkMode( DependencyObject obj, bool value )
            {
            obj.SetValue( OkModeProperty, value );
            }

        public static readonly DependencyProperty OkModeProperty =
            DependencyProperty.RegisterAttached( "OkMode", typeof( bool ), typeof( KeyStateChecker ), new PropertyMetadata( false ) );

        #endregion

        #region AlternativeCheckingKey

        public static Key GetAlternativeCheckingKey( DependencyObject obj )
            {
            return (Key)obj.GetValue( AlternativeCheckingKeyProperty );
            }

        public static void SetAlternativeCheckingKey( DependencyObject obj, Key value )
            {
            obj.SetValue( AlternativeCheckingKeyProperty, value );
            }

        public static readonly DependencyProperty AlternativeCheckingKeyProperty =
            DependencyProperty.RegisterAttached( "AlternativeCheckingKey", typeof( Key ), typeof( KeyStateChecker ) );

        #endregion

        #region CheckingKey
        public static Key GetCheckingKey( DependencyObject obj )
            {
            return (Key)obj.GetValue( CheckingKeyProperty );
            }

        public static void SetCheckingKey( DependencyObject target, Key value )
            {
            target.GetValue( CheckingKeyProperty );
            }

        public static readonly DependencyProperty CheckingKeyProperty =
            DependencyProperty.RegisterAttached( "CheckingKey", typeof( Key ), typeof( KeyStateChecker )
            , new PropertyMetadata( OnCheckingKeyChanged ) );


        private static void OnCheckingKeyChanged( DependencyObject target, DependencyPropertyChangedEventArgs e )
            {
            Key CheckingKey = (Key)e.NewValue;
            onCheckingKeyChanged( target, CheckingKey );
            }

        private static void onCheckingKeyChanged( DependencyObject target, Key value )
            {
            FrameworkElement element = target as FrameworkElement;
            if (element == null)
                {
                return;
                }
            element.PreviewMouseMove += element_PreviewMouseMove;
            element.Focusable = true;
            element.AddHandler( FrameworkElement.PreviewKeyDownEvent, new KeyEventHandler( checkKey ), true );
            element.AddHandler( FrameworkElement.PreviewKeyUpEvent, new KeyEventHandler( checkKey ), true );
            }
        #endregion

        #region Methods,handlers

        private static void checkKey( object sender, KeyEventArgs e )
            {
            checkKey( sender );
            }

        private static void element_PreviewMouseMove( object sender, MouseEventArgs e )
            {
            (sender as FrameworkElement).Focus();
            checkKey( sender );
            }

        private static void checkKey( object sender )
            {
            FrameworkElement target = sender as FrameworkElement;
            if (target == null)
                {
                return;
                }
            Key checkKey = GetCheckingKey( target );
            Key alternativeKey = GetAlternativeCheckingKey( target );
            if (!(bool)target.GetValue( OkModeProperty ) && (checkKey != Key.None && Keyboard.IsKeyDown( checkKey )) || (alternativeKey != Key.None && Keyboard.IsKeyDown( alternativeKey )))
                {
                SetIsKeyPressed( target, true );
                }
            else
                {
                SetIsKeyPressed( target, false );
                }
            }
        #endregion
    
        }
    }
