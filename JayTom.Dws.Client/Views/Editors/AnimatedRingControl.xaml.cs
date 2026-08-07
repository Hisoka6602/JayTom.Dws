using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Navigation;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;

namespace JayTom.Dws.Client.Views.Editors
{

    /// <summary>
    /// AnimatedRingControl.xaml 的交互逻辑
    /// </summary>
    public partial class AnimatedRingControl : UserControl
    {
        private Storyboard _storyboard;
        private const int SegmentCount = 10; // 段数
        private const double StrokeThickness = 15; // 线条粗细
        private const double AnimationDuration = 1.0; // 动画持续时间（秒）
        private const double MaxOpacity = 0.6; // 最大不透明度

        public AnimatedRingControl()
        {
            InitializeComponent();
            Loaded += AnimatedRingControl_Loaded;
            SizeChanged += AnimatedRingControl_SizeChanged;
        }

        private void AnimatedRingControl_Loaded(object sender, RoutedEventArgs e)
        {
            CreateSegments();
            if (IsAnimationEnabled)
            {
                StartAnimations();
            }
        }

        private void AnimatedRingControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateSegments();
        }

        #region Dependency Properties

        public static readonly DependencyProperty IsAnimationEnabledProperty =
            DependencyProperty.Register(nameof(IsAnimationEnabled), typeof(bool), typeof(AnimatedRingControl),
                new PropertyMetadata(true, OnIsAnimationEnabledChanged));

        public bool IsAnimationEnabled
        {
            get => (bool)GetValue(IsAnimationEnabledProperty);
            set => SetValue(IsAnimationEnabledProperty, value);
        }

        private static void OnIsAnimationEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AnimatedRingControl control)
            {
                if ((bool)e.NewValue)
                {
                    control.StartAnimations();
                }
                else
                {
                    control.StopAnimations();
                }
            }
        }

        public static readonly DependencyProperty ControlOpacityProperty =
            DependencyProperty.Register(nameof(ControlOpacity), typeof(double), typeof(AnimatedRingControl),
                new PropertyMetadata(1.0));

        public double ControlOpacity
        {
            get => (double)GetValue(ControlOpacityProperty);
            set => SetValue(ControlOpacityProperty, value);
        }

        #endregion Dependency Properties

        #region Segment Creation and Update

        private void CreateSegments()
        {
            MainCanvas.Children.Clear();
            _storyboard = new Storyboard();

            var radius = Math.Min(ActualWidth, ActualHeight) / 2 - StrokeThickness;
            var center = new Point(ActualWidth / 2, ActualHeight / 2);
            var angleStep = 360.0 / SegmentCount;

            for (var i = 0; i < SegmentCount; i++)
            {
                var segmentPath = CreateSegmentPath(radius, center, i * angleStep);
                if (segmentPath is not null)
                {
                    MainCanvas.Children.Add(segmentPath);

                    var opacityAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = MaxOpacity,
                        Duration = TimeSpan.FromSeconds(AnimationDuration),
                        AutoReverse = true,
                        RepeatBehavior = RepeatBehavior.Forever,
                        BeginTime = TimeSpan.FromSeconds(i * (AnimationDuration / SegmentCount))
                    };

                    Storyboard.SetTarget(opacityAnimation, segmentPath);
                    Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath("Opacity"));
                    _storyboard.Children.Add(opacityAnimation);
                }
            }
        }

        private Path? CreateSegmentPath(double radius, Point center, double angle)
        {
            if (radius <= 0) return null;
            var startAngle = angle - 5; // 每个段的角度范围，可以根据需要调整
            var endAngle = angle + 5;

            var startPoint = ComputeCartesianCoordinate(startAngle, radius, center);
            var endPoint = ComputeCartesianCoordinate(endAngle, radius, center);

            var isLargeArc = Math.Abs(endAngle - startAngle) > 180;

            var pathFigure = new PathFigure
            {
                StartPoint = startPoint,
                Segments = {
                    new ArcSegment
                    {
                        Point = endPoint,
                        Size = new Size(radius, radius),
                        IsLargeArc = isLargeArc,
                        SweepDirection = SweepDirection.Clockwise
                    }
                }
            };

            var pathGeometry = new PathGeometry();
            pathGeometry.Figures.Add(pathFigure);

            return new Path
            {
                Data = pathGeometry,
                Stroke = Brushes.White,
                StrokeThickness = StrokeThickness,
                Opacity = 0,
                RenderTransform = new RotateTransform(0, center.X, center.Y),
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            };
        }

        private Point ComputeCartesianCoordinate(double angle, double radius, Point center)
        {
            // 将角度转换为弧度
            var angleRad = (Math.PI / 180.0) * angle;

            var x = center.X + radius * Math.Cos(angleRad);
            var y = center.Y + radius * Math.Sin(angleRad);

            return new Point(x, y);
        }

        private void UpdateSegments()
        {
            if (MainCanvas.Children.Count == 0)
                return;

            var radius = Math.Min(ActualWidth, ActualHeight) / 2 - StrokeThickness;
            var center = new Point(ActualWidth / 2, ActualHeight / 2);

            for (var i = 0; i < SegmentCount; i++)
            {
                var angle = i * (360.0 / SegmentCount);
                var segmentPath = (Path)MainCanvas.Children[i];

                segmentPath.Data = CreateSegmentPath(radius, center, angle)?.Data;

                segmentPath.RenderTransform = new RotateTransform(0, center.X, center.Y);
            }
        }

        #endregion Segment Creation and Update

        #region Animation Control

        private void StartAnimations()
        {
            _storyboard?.Begin(this, true);
        }

        private void StopAnimations()
        {
            _storyboard?.Stop(this);
        }

        #endregion Animation Control
    }
}