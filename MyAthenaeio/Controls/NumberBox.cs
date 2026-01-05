using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MyAthenaeio.Controls
{
    public partial class NumberBox : TextBox
    {
        public NumberBox()
        {
            PreviewTextInput += OnPreviewTextInput;
            DataObject.AddPastingHandler(this, OnPaste);
            TextChanged += OnTextChanged;
        }

        // Source-generated regex - compiled at build time
        [GeneratedRegex("^[0-9]+$")]
        private static partial Regex NumericRegex();

        #region Dependency Properties

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register(nameof(Minimum), typeof(int?), typeof(NumberBox),
                new PropertyMetadata(null, OnValueChanged));

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(nameof(Maximum), typeof(int?), typeof(NumberBox),
                new PropertyMetadata(null, OnValueChanged));

        public static readonly DependencyProperty StepProperty =
            DependencyProperty.Register(nameof(Step), typeof(int), typeof(NumberBox),
                new PropertyMetadata(1));

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(int?), typeof(NumberBox),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnValuePropertyChanged));

        public int? Minimum
        {
            get => (int?)GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        public int? Maximum
        {
            get => (int?)GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        public int Step
        {
            get => (int)GetValue(StepProperty);
            set => SetValue(StepProperty, value);
        }

        public int? Value
        {
            get => (int?)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        #endregion

        private static void OnValuePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is NumberBox numberBox)
            {
                numberBox.UpdateTextFromValue();
            }
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is NumberBox numberBox)
            {
                numberBox.ValidateValue();
            }
        }

        private void UpdateTextFromValue()
        {
            if (Value.HasValue)
            {
                Text = Value.Value.ToString();
            }
            else
            {
                Text = string.Empty;
            }
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(Text))
            {
                Value = null;
                return;
            }

            if (int.TryParse(Text, out int result))
            {
                Value = ClampValue(result);
            }
        }

        private void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Only allow digits
            e.Handled = !IsTextNumeric(e.Text);
        }

        private void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (!IsTextNumeric(text))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        private static bool IsTextNumeric(string text)
        {
            return NumericRegex().IsMatch(text);
        }

        private void ValidateValue()
        {
            if (Value.HasValue)
            {
                Value = ClampValue(Value.Value);
            }
        }

        private int ClampValue(int value)
        {
            if (Minimum.HasValue && value < Minimum.Value)
                return Minimum.Value;

            if (Maximum.HasValue && value > Maximum.Value)
                return Maximum.Value;

            return value;
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);

            if (e.Key == Key.Up)
            {
                IncrementValue();
                e.Handled = true;
            }
            else if (e.Key == Key.Down)
            {
                DecrementValue();
                e.Handled = true;
            }
        }

        private void IncrementValue()
        {
            if (Value.HasValue)
            {
                Value = ClampValue(Value.Value + Step);
            }
            else if (Minimum.HasValue)
            {
                Value = Minimum.Value;
            }
            else
            {
                Value = 0;
            }
        }

        private void DecrementValue()
        {
            if (Value.HasValue)
            {
                Value = ClampValue(Value.Value - Step);
            }
            else if (Maximum.HasValue)
            {
                Value = Maximum.Value;
            }
            else
            {
                Value = 0;
            }
        }
    }
}