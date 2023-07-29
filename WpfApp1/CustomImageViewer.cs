using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows.Media.Imaging;

namespace WpfApp1 {

    internal class CustomImageViewer : FrameworkElement {

        public static readonly DependencyProperty ImageSourceProperty =
            DependencyProperty.Register(nameof(ImageSource), typeof(BitmapImage), typeof(CustomImageViewer),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public BitmapImage ImageSource {
            get => (BitmapImage)GetValue(ImageSourceProperty);
            set => SetValue(ImageSourceProperty, value);
        }

        protected override void OnRender(DrawingContext drawingContext) {
            base.OnRender(drawingContext);

            // 在此处绘制图像
            drawingContext.DrawImage(ImageSource, new Rect(0, 0, ImageSource.PixelWidth, ImageSource.PixelHeight));
        }
    }
}