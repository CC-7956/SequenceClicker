using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SequenceClicker
{
    public static class RegexTextControl
    {
        private static readonly Regex _regexDeci = new Regex("[^0-9,]+");
        private static readonly Regex _regexPos = new Regex("[^0-9]+");
        private static readonly Regex _regexPosNeg = new Regex("[^0-9+-]+");

        public static void PosDeci(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as System.Windows.Controls.TextBox;
            // Block invalid characters
            if (_regexDeci.IsMatch(e.Text))
            {
                e.Handled = true;
                return;
            }

            // Allow comma only once
            if (e.Text == "," && textBox.Text.Contains(","))
            {
                e.Handled = true;
            }
        }
        public static void Pos(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as System.Windows.Controls.TextBox;
            // Block invalid characters
            if (_regexPos.IsMatch(e.Text))
            {
                e.Handled = true;
                return;
            }
        }
        public static void PosNeg(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as System.Windows.Controls.TextBox;

            // Block anything that's not digit, +, or -
            if (_regexPosNeg.IsMatch(e.Text))
            {
                e.Handled = true;
                return;
            }

            // Only allow + or - at the beginning, and only once
            if ((e.Text == "+" || e.Text == "-"))
            {
                // Not at the beginning? reject
                if (textBox.SelectionStart != 0)
                {
                    e.Handled = true;
                    return;
                }

                // Already has + or -? reject
                if (textBox.Text.StartsWith("+") || textBox.Text.StartsWith("-"))
                {
                    e.Handled = true;
                    return;
                }
            }
        }
    }
}
