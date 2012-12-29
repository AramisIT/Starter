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
using AramisStarter.FilesDownloading;
using AramisStarter.SolutionSelecting;
using AramisStarter.Utils;
using Windows;

namespace AramisStarter
    {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class SolutionSelectingWindow : Window
        {
        private System.Collections.ObjectModel.ObservableCollection<SolutionInfo> solutions;

        public static readonly DependencyProperty IsEditListModeProperty;

        static SolutionSelectingWindow()
            {
            FrameworkPropertyMetadata metaData = null;

            #region IsEditListMode property registration

            metaData = new FrameworkPropertyMetadata( false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnModeChanged );

            IsEditListModeProperty = DependencyProperty.Register( "IsEditListMode", typeof( bool ), typeof( SolutionSelectingWindow ), metaData,
                new ValidateValueCallback( value =>
                {
                    return value != null;
                } ) );

            #endregion
            }

        private static void OnModeChanged( DependencyObject d, DependencyPropertyChangedEventArgs e )
            {
            SolutionSelectingWindow solutionSelectingWindow = ( SolutionSelectingWindow )d;
            // solutionSelectingWindow.goButton.Content = solutionSelectingWindow.IsEditListMode ? "OK" : "Старт";
            solutionSelectingWindow.goButton.SetValue( KeyStateChecker.OkModeProperty, solutionSelectingWindow.IsEditListMode );
            solutionSelectingWindow.goButton.Width = solutionSelectingWindow.IsEditListMode ? 50 : 220;

            solutionSelectingWindow.UpdateListBoxMode();
            }

        public bool IsEditListMode
            {
            set
                {
                SetValue( IsEditListModeProperty, value );
                }
            get
                {
                return ( bool )GetValue( IsEditListModeProperty );
                }
            }

        private SolutionSelectingWindow()
            {
            InitializeComponent();
            Icon = EmbededResourcesConverter.BitmapSourceFromBitmap( Properties.Resources.Transparent );
            IsEditListMode = false;
            }

        internal SolutionSelectingWindow( System.Collections.ObjectModel.ObservableCollection<SolutionInfo> solutions )
            : this()
            {
            this.solutions = solutions;
            listBox.ItemsSource = solutions;
            solutions.CollectionChanged += solutions_CollectionChanged;
            if ( solutions.Count > 0 )
                {
                listBox.SelectedIndex = 0;
                }

            UpdateGoButton();
            }

        void solutions_CollectionChanged( object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e )
            {
            UpdateGoButton();
            }

        private void UpdateGoButton()
            {
            goButton.IsEnabled = solutions.Count > 0;
            }

        private void Window_Loaded( object sender, RoutedEventArgs e )
            {
            if ( !VistaGlassHelper.ExtendGlass( this, -1, -1, -1, -1 ) )
                {
                UseAlternativeStyle();
                }
            listBox.Focus();
            }

        private void UseAlternativeStyle()
            {
            //backGroungImage.Source = EmbededResourcesConverter.BitmapSourceFromBitmap( Properties.Resources.BBB );
            //ImageSource imgSource = new ImageSource();
            //this.Background = new ImageBrush(imgSource);

            }

        private void ListBoxItemMouseEnter( object sender, MouseEventArgs e )
            {
            FrameworkElement listBoxItem = sender as FrameworkElement;
            FrameworkElement child = VisualTreeHelperAdditional.GetVisualChild<Image>( listBoxItem );//.FindName( "image" ) as FrameworkElement;
            if ( child != null )
                {
                child.Opacity = 1;
                }
            }

        private void ListBoxItemMouseLeave( object sender, MouseEventArgs e )
            {
            if ( e.LeftButton == MouseButtonState.Released )
                {
                droppedData = null;
                }
            FrameworkElement listBoxItem = sender as FrameworkElement;
            FrameworkElement child = VisualTreeHelperAdditional.GetVisualChild<Image>( listBoxItem );//.FindName( "image" ) as FrameworkElement;
            if ( child != null && droppedData == null )
                {
                child.Opacity = 0;
                }
            }

        private void UpdateListBoxMode()
            {
            listBox.ItemTemplate = FindResource( IsEditListMode ? "EditSolutionsList" : "SelectingSolution" ) as DataTemplate;
            }

        private void GoButton_Click( object sender, RoutedEventArgs e )
            {
            if ( ( bool )goButton.GetValue( KeyStateChecker.IsKeyPressedProperty ) )
                {
                IsEditListMode = true;
                return;
                }

            if ( IsEditListMode )
                {
                App.SaveSolutionsList( solutions );
                }
            else
                {
                SelectSolution();
                }
            IsEditListMode = !IsEditListMode;
            }

        private void SelectSolution()
            {
            if ( listBox.SelectedItem != null )
                {
                SelectedSolution = listBox.SelectedItem as SolutionInfo;
                }
            else if ( solutions.Count > 0 )
                {
                SelectedSolution = solutions[ 0 ];
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
            SolutionInfo newSolutionInfo = AddNewSystemWindow.AddNewSolution();
            if ( newSolutionInfo != null )
                {
                solutions.Add( newSolutionInfo );
                }
            }

        private SolutionInfo droppedData = null;
        private Image currentImage = null;

        private void BeginSelectByDown( object sender, MouseButtonEventArgs e )
            {
            if ( !IsEditListMode )
                {
                return;
                }

            if ( sender is ListBoxItem )
                {
                ( sender as ListBoxItem ).IsSelected = true;
                }
            currentImage = e.OriginalSource as Image;
            if ( currentImage != null && currentImage.Name != "buttonImage" )
                {
                if ( droppedData == null )
                    {
                    FrameworkElement fd = e.OriginalSource as FrameworkElement;
                    if ( fd != null )
                        {
                        SolutionInfo item = fd.DataContext as SolutionInfo;
                        if ( item != null && solutions.Contains( item ) )
                            {
                            droppedData = item;
                            }
                        }
                    }
                if ( droppedData != null )
                    {
                    try
                        {
                        currentImage.Opacity = 1;
                        DragDrop.DoDragDrop( currentImage, droppedData, DragDropEffects.Move );
                        }
                    catch ( Exception ex )
                        {
                        Console.WriteLine( ex.ToString() );
                        }
                    }
                }
            }

        private void TextBlock_MouseDown_1( object sender, MouseButtonEventArgs e )
            {
            if ( e.ClickCount > 1 )
                {
                SelectSolution();
                }
            }

        private void Button_Click_1( object sender, RoutedEventArgs e )
            {
            object fd = e.OriginalSource;
            }

        private void RemoveButton_Click( object sender, RoutedEventArgs e )
            {
            FrameworkElement frameworkElement = e.OriginalSource as FrameworkElement;

            if ( frameworkElement != null )
                {
                SolutionInfo item = frameworkElement.DataContext as SolutionInfo;
                if ( item != null && solutions.Contains( item ) )
                    {
                    solutions.Remove( item );
                    }
                }
            }



        private void ItemDrop( object sender, DragEventArgs e )
            {
            if ( droppedData == null )
                {
                return;
                }
            SolutionInfo target = ( ( FrameworkElement )( sender ) ).DataContext as SolutionInfo;
            SolutionInfo dataConnected = e.Data.GetData( typeof( SolutionInfo ) ) as SolutionInfo;
            if ( dataConnected == null )
                {
                return;
                }
            if ( currentImage != null )
                {
                currentImage.Opacity = 0;
                }
            int removedIdx = solutions.IndexOf( droppedData );
            int targetIdx = solutions.IndexOf( target );

            if ( removedIdx < targetIdx )
                {
                solutions.Insert( targetIdx + 1, droppedData );
                solutions.RemoveAt( removedIdx );
                }
            else
                {
                int remIdx = removedIdx + 1;
                if ( solutions.Count + 1 > remIdx )
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
            ListBoxItem item = ( ListBoxItem )sender;
            item.IsSelected = true;
            }
        }

    //AramisWPFComonents.Reports
    public static class VisualTreeHelperAdditional
        {
        public static T GetVisualChild<T>( DependencyObject parent ) where T : Visual
            {
            T child = default( T );
            int numVisuals = VisualTreeHelper.GetChildrenCount( parent );
            for ( int i = 0; i < numVisuals; i++ )
                {
                Visual v = ( Visual )VisualTreeHelper.GetChild( parent, i );
                child = v as T;
                if ( child == null )
                    {
                    child = GetVisualChild<T>( v );
                    }
                if ( child != null )
                    {
                    break;
                    }
                }
            return child;
            }
        }
    }
