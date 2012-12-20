using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace AramisStarter.Utils
    {
    static class EmbededResourcesConverter
        {

        [DllImport( "gdi32.dll" )]
        public static extern bool DeleteObject( IntPtr hObject );

        public static BitmapSource BitmapSourceFromBitmap( Bitmap bitmap )
            {
            IntPtr hBitmap = bitmap.GetHbitmap();
            BitmapSource bitMapSource = Imaging.CreateBitmapSourceFromHBitmap( hBitmap,
                                          IntPtr.Zero, Int32Rect.Empty,
                                          BitmapSizeOptions.FromEmptyOptions() );
            bitMapSource.Freeze();
            DeleteObject( hBitmap );

            return bitMapSource;
            }
        }
    }
