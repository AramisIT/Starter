using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace AramisStarter.SolutionSelecting
    {
    public class KeyStateChecker
        {

        #region AlternativeContent

        public static string GetAlternativeContent( DependencyObject obj )
            {
            return ( string )obj.GetValue( AlternativeContentProperty );
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
            return ( bool )obj.GetValue( IsKeyPressedProperty );
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
            return ( bool )obj.GetValue( OkModeProperty );
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
            return ( Key )obj.GetValue( AlternativeCheckingKeyProperty );
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
            return ( Key )obj.GetValue( CheckingKeyProperty );
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
            Key CheckingKey = ( Key )e.NewValue;
            onCheckingKeyChanged( target, CheckingKey );
            }

        private static void onCheckingKeyChanged( DependencyObject target, Key value )
            {
            FrameworkElement element = target as FrameworkElement;
            if ( element == null )
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
            ( sender as FrameworkElement ).Focus();
            checkKey( sender );
            }

        private static void checkKey( object sender )
            {
            FrameworkElement target = sender as FrameworkElement;
            if ( target == null )
                {
                return;
                }
            Key checkKey = GetCheckingKey( target );
            Key alternativeKey = GetAlternativeCheckingKey( target );
            if ( !( bool )target.GetValue( OkModeProperty ) && ( checkKey != Key.None && Keyboard.IsKeyDown( checkKey ) ) || ( alternativeKey != Key.None && Keyboard.IsKeyDown( alternativeKey ) ) )
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
