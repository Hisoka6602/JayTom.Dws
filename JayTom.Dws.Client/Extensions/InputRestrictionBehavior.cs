using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Extensions
{
    public class InputRestrictionBehavior
    {

        public static readonly DependencyProperty AllowedInputProperty =
            DependencyProperty.RegisterAttached("AllowedInput", typeof(string), typeof(InputRestrictionBehavior), new PropertyMetadata("", OnAllowedInputChanged));

        public static string GetAllowedInput(DependencyObject obj)
        {
            return (string)obj.GetValue(AllowedInputProperty);
        }

        public static void SetAllowedInput(DependencyObject obj, string value)
        {
            obj.SetValue(AllowedInputProperty, value);
        }

        private static void OnAllowedInputChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox)
            {
                textBox.PreviewTextInput -= TextBox_PreviewTextInput;
                textBox.PreviewTextInput += TextBox_PreviewTextInput;
            }
        }

        private static void TextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            var textBox = (TextBox)sender;
            var allowedInput = GetAllowedInput(textBox);

            // 只允许输入AllowedInput中指定的字符
            if (!allowedInput.Contains(e.Text))
            {
                e.Handled = true;
            }

            // 判断是否已经存在小数点
            var hasDecimalSeparator = textBox.Text.Contains(".");

            // 如果输入的是小数点，并且已经存在小数点，则禁止输入
            if (e.Text == "." && hasDecimalSeparator)
            {
                e.Handled = true;
            }

            // 如果输入的是小数点，并且小数点位于第一个位置，则禁止输入
            if (e.Text == "." && textBox.SelectionStart == 0)
            {
                e.Handled = true;
            }

            // 如果输入的是小数点，并且已经存在其他选中内容，则禁止输入
            if (e.Text == "." && textBox.SelectionLength > 0 && textBox.SelectionLength != textBox.Text.Length)
            {
                e.Handled = true;
            }

            if (e.Text == ".")
            {
                textBox.Text += ".";
            }
        }
    }
}