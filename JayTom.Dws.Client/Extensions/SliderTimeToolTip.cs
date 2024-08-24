using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Diagnostics;
using System.Windows.Data;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Collections.Generic;
using JayTom.Dws.Client.Converters;
using System.Windows.Controls.Primitives;

namespace JayTom.Dws.Client.Extensions {

    public class SliderTimeToolTip {

        public static readonly DependencyProperty AutoTickFrequencyProperty =
            DependencyProperty.RegisterAttached("AutoTickFrequency", typeof(bool), typeof(SliderTimeToolTip), new PropertyMetadata(false, OnAutoTickFrequencyChanged));

        public static bool GetAutoTickFrequency(DependencyObject obj) {
            return (bool)obj.GetValue(AutoTickFrequencyProperty);
        }

        public static void SetAutoTickFrequency(DependencyObject obj, bool value) {
            obj.SetValue(AutoTickFrequencyProperty, value);
        }

        private static void OnAutoTickFrequencyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is Slider slider && e.NewValue is bool isEnabled) {
                if (isEnabled) {
                    // Calculate and set TickFrequency when ActualWidth changes
                    slider.SizeChanged += (s, args) => {
                        UpdateTickFrequency(slider);
                    };

                    // Initial calculation
                    UpdateTickFrequency(slider);
                }
            }
        }

        private static void UpdateTickFrequency(Slider slider) {
            if (slider.ActualWidth > 0 && slider.Maximum > slider.Minimum) {
                var range = slider.Maximum - slider.Minimum;
                slider.TickFrequency = range / (slider.ActualWidth / 20);
            }
        }

        public static readonly DependencyProperty IsTimeToolTipEnabledProperty =
            DependencyProperty.RegisterAttached("IsTimeToolTipEnabled", typeof(bool), typeof(SliderTimeToolTip), new PropertyMetadata(false, OnIsTimeToolTipEnabledChanged));

        public static bool GetIsTimeToolTipEnabled(DependencyObject obj) {
            return (bool)obj.GetValue(IsTimeToolTipEnabledProperty);
        }

        public static void SetIsTimeToolTipEnabled(DependencyObject obj, bool value) {
            obj.SetValue(IsTimeToolTipEnabledProperty, value);
        }

        private static void OnIsTimeToolTipEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is Slider slider && e.NewValue is bool isEnabled) {
                if (isEnabled) {
                    slider.Tag = false;
                    // Set up the ToolTip and event handlers
                    slider.AutoToolTipPlacement = AutoToolTipPlacement.None; // Disable the default AutoToolTipPlacement
                    var toolTip = new ToolTip();
                    slider.ToolTip = toolTip;

                    slider.MouseMove += (sender, args) => {
                        var mousePosition = args.MouseDevice.GetPosition(slider);
                        // 获取Slider的Track
                        if (slider.Template.FindName("PART_Track", slider) is Track track && !track.Thumb.IsDragging) {
                            // 计算鼠标相对于 Track 的位置
                            var relativePosition = (mousePosition.X - (track.Thumb.ActualWidth / 2)) / (track.ActualWidth - track.Thumb.ActualWidth);

                            // 确保 relativePosition 在 [0, 1] 范围内
                            relativePosition = Math.Max(0, Math.Min(1, relativePosition));
                            // 计算滑块在此位置的值
                            var sliderValue = slider.Minimum + (relativePosition * (slider.Maximum - slider.Minimum));

                            toolTip.PlacementTarget = slider;
                            toolTip.Content = new DoubleToTimeToolTipConverter().Convert(sliderValue, typeof(string), null, null);
                            toolTip.HorizontalOffset = mousePosition.X - (slider.ActualWidth / 2);
                            toolTip.HorizontalAlignment = HorizontalAlignment.Left;
                            toolTip.VerticalAlignment = VerticalAlignment.Top;
                            toolTip.VerticalOffset = -70;

                            if (!toolTip.IsOpen) {
                                toolTip.IsOpen = true;
                            }
                        }
                    };
                    slider.MouseLeave += (s, args) => {
                        toolTip.IsOpen = false;
                    };

                    slider.ValueChanged += (s, args) => {
                        if (slider.IsMouseOver) {
                            var mousePosition = Mouse.GetPosition(slider);
                            toolTip.PlacementTarget = slider;
                            toolTip.Content = new DoubleToTimeToolTipConverter().Convert(slider.Value, typeof(string), null, null);
                            toolTip.HorizontalOffset = mousePosition.X - (slider.ActualWidth / 2);
                            toolTip.HorizontalAlignment = HorizontalAlignment.Left;
                            toolTip.VerticalAlignment = VerticalAlignment.Top;
                            toolTip.VerticalOffset = -70;
                            if (!toolTip.IsOpen) {
                                toolTip.IsOpen = true;
                            }
                        }
                    };
                }
                else {
                    // Clean up if the property is set to false
                    slider.ToolTip = null;
                    slider.MouseEnter -= null;
                    slider.MouseLeave -= null;
                    slider.ValueChanged -= null;
                }
            }
        }
    }
}