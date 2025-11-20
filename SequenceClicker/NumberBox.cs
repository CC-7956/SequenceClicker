using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Globalization;

namespace SequenceClicker
{
    public enum NumberMode
    {
        None,
        PositiveInteger,
        PositiveDecimal,
        SignedInteger
    }

    public static class NumberBox
    {
        public static NumberMode GetMode(DependencyObject obj) =>
            (NumberMode)obj.GetValue(ModeProperty);

        public static void SetMode(DependencyObject obj, NumberMode value) =>
            obj.SetValue(ModeProperty, value);

        public static readonly DependencyProperty ModeProperty =
            DependencyProperty.RegisterAttached(
                "Mode", typeof(NumberMode), typeof(NumberBox),
                new PropertyMetadata(NumberMode.PositiveInteger, OnModeChanged));

        private static void OnModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox tb)
            {
                tb.PreviewTextInput -= OnPreviewTextInput;
                tb.PreviewKeyDown -= OnPreviewKeyDown;
                tb.TextChanged -= OnTextChanged;

                DataObject.RemovePastingHandler(tb, OnPaste);
                tb.PreviewDragOver -= OnDragOver;
                tb.PreviewDrop -= OnDrop;

                tb.PreviewTextInput += OnPreviewTextInput;
                tb.PreviewKeyDown += OnPreviewKeyDown;
                tb.TextChanged += OnTextChanged;

                DataObject.AddPastingHandler(tb, OnPaste);
                tb.PreviewDragOver += OnDragOver;
                tb.PreviewDrop += OnDrop;
            }
        }

        // Helper: ascii-only digit check
        private static bool IsAsciiDigit(char c) => c >= '0' && c <= '9';

        // Typing validation
        private static void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var tb = (TextBox)sender;
            var mode = GetMode(tb);

            var sep = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            string input = e.Text;

            // Normalize '.' to culture separator when decimal mode
            if (input == "." && sep == ",") input = ",";

            // Clamp insertion point to safe range
            int insertPos = Math.Max(0, Math.Min(tb.SelectionStart, tb.Text.Length));

            string newText = tb.Text.Insert(insertPos, input);

            e.Handled = !IsValid(newText, mode);
        }

        private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
                e.Handled = true;

            if (e.Key == Key.V && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                e.Handled = true;
        }

        private static void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (!e.DataObject.GetDataPresent(DataFormats.Text))
            {
                e.CancelCommand();
                return;
            }

            var tb = (TextBox)sender;
            var mode = GetMode(tb);

            string text = (string)e.DataObject.GetData(DataFormats.Text);
            string newText = tb.Text.Insert(tb.SelectionStart, text);

            if (!IsValid(newText, mode))
                e.CancelCommand();
        }

        private static void OnDragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        private static void OnDrop(object sender, DragEventArgs e)
        {
            e.Handled = true;
        }

        // Handles IME, emoji picker, Win+., programmatic insertions, etc.
        private static void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            var tb = (TextBox)sender;
            var mode = GetMode(tb);

            if (IsValid(tb.Text, mode))
                return;

            string filtered = Filter(tb.Text, mode);

            // Clamp caret safely
            int caret = tb.CaretIndex;
            caret = Math.Min(caret, filtered.Length); // prevent out of range
            caret = Math.Max(0, caret);               // ensure non-negative

            tb.Text = filtered;
            tb.CaretIndex = caret;
        }

        // Filters invalid chars out (ASCII-digit strict for PositiveInteger)
        private static string Filter(string text, NumberMode mode)
        {
            var sep = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;

            switch (mode)
            {
                case NumberMode.PositiveInteger:
                    // Only ASCII digits 0-9 allowed
                    return new string(text.Where(IsAsciiDigit).ToArray());

                case NumberMode.PositiveDecimal:
                    bool foundSep = false;
                    // Build result allowing digits and one separator (culture-aware)
                    var resultChars = text.Where(ch =>
                    {
                        if (IsAsciiDigit(ch)) return true;

                        string s = ch.ToString();
                        if (s == sep && !foundSep)
                        {
                            foundSep = true;
                            return true;
                        }

                        // also accept decimal separator characters typed as '.' when culture is ','
                        if (s == "." && sep == "," && !foundSep)
                        {
                            foundSep = true;
                            return true;
                        }

                        return false;
                    }).ToArray();

                    // If '.' was kept while culture sep=="," convert to ','
                    if (sep == ",")
                    {
                        for (int i = 0; i < resultChars.Length; i++)
                            if (resultChars[i] == '.') resultChars[i] = ',';
                    }

                    // Prevent leading separator like ",5" (remove leading sep)
                    if (resultChars.Length > 0 && resultChars[0].ToString() == sep)
                        resultChars = resultChars.Skip(1).ToArray();

                    return new string(resultChars);

                case NumberMode.SignedInteger:
                    // Keep ASCII digits, and only one leading '-'
                    var digitsOnly = new string(text.Where(IsAsciiDigit).ToArray());
                    if (text.StartsWith("-"))
                        return "-" + digitsOnly;
                    return digitsOnly;

                default:
                    return text;
            }
        }

        private static bool IsValid(string text, NumberMode mode)
        {
            var sep = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;

            if (string.IsNullOrEmpty(text))
                return true;

            switch (mode)
            {
                case NumberMode.PositiveInteger:
                    // Every char must be ASCII digit 0-9
                    return text.All(IsAsciiDigit);

                case NumberMode.PositiveDecimal:
                    // Prevent leading separator
                    if (text.StartsWith(sep)) return false;

                    // count separators (either culture sep or '.' typed on other cultures)
                    int sepCount = text.Count(ch => ch.ToString() == sep || (ch == '.' && sep == ","));
                    if (sepCount > 1) return false;

                    // Remove separator(s) then check remaining are ascii digits
                    string cleaned = text.Replace(sep, "");
                    if (sep == ",") cleaned = cleaned.Replace(".", ""); // dispose of stray dots
                    return cleaned.All(IsAsciiDigit);

                case NumberMode.SignedInteger:
                    if (text == "-") return true; // allow single leading '-'
                    if (text.StartsWith("-"))
                        return text.Length > 1 && text[1..].All(IsAsciiDigit);
                    return text.All(IsAsciiDigit);

                default:
                    return false;
            }
        }
        public static void AttachHandlers(TextBox tb)
        {
            tb.PreviewTextInput -= OnPreviewTextInput;
            tb.PreviewTextInput += OnPreviewTextInput;

            tb.PreviewKeyDown -= OnPreviewKeyDown;
            tb.PreviewKeyDown += OnPreviewKeyDown;

            tb.TextChanged -= OnTextChanged;
            tb.TextChanged += OnTextChanged;

            DataObject.RemovePastingHandler(tb, OnPaste);
            DataObject.AddPastingHandler(tb, OnPaste);

            tb.PreviewDragOver -= OnDragOver;
            tb.PreviewDragOver += OnDragOver;
            tb.PreviewDrop -= OnDrop;
            tb.PreviewDrop += OnDrop;
        }
    }
}
