using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SequenceClicker
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class EditWindow : Window
    {
        #region Cursor
        [DllImport("user32.dll")]
        static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int X, int Y);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }
        #endregion

        bool click = true;
        bool delay = true;
        bool move = true;
        MyTask task;
        public EditWindow(MyTask task)
        {
            InitializeComponent();
            this.task = task;
            lb_EditClick.Visibility = Visibility.Collapsed;
            lb_Click.Visibility = Visibility.Collapsed;
            TB_Click.Visibility = Visibility.Collapsed;
            Tog_Click.Visibility = Visibility.Collapsed;


            lb_EditDelay.Visibility = Visibility.Collapsed;
            lb_dmin.Visibility = Visibility.Collapsed;
            lb_dsec.Visibility = Visibility.Collapsed;
            tb_dmin.Visibility = Visibility.Collapsed;
            tb_dsec.Visibility = Visibility.Collapsed;
            
            lb_EditMove.Visibility = Visibility.Collapsed;
            lb_X.Visibility = Visibility.Collapsed;
            lb_Y.Visibility = Visibility.Collapsed;
            TB_X.Visibility = Visibility.Collapsed;
            TB_Y.Visibility = Visibility.Collapsed;
            btn_Auto.Visibility = Visibility.Collapsed;
            btn_Test.Visibility = Visibility.Collapsed;


            prep(task);
            Update();
        }

        private void prep(MyTask task)
        {
            switch (task)
            {
                case ClickTask click: Click(task as ClickTask); break;
                case DelayTask delay: Delay(task as DelayTask); break;
                case MoveTask move: Move(task as MoveTask); break;
                default: MessageBox.Show($"Unknown Task.\n{task.GetType()} does not exist.", "Error while executing", MessageBoxButton.OK, MessageBoxImage.Error); break;
            }
        }
        private void Click(ClickTask task)
        {
            lb_EditClick.Visibility = Visibility.Visible;
            lb_Click.Visibility = Visibility.Visible;
            TB_Click.Visibility = Visibility.Visible;
            Tog_Click.Visibility = Visibility.Visible;

            TB_Click.IsEnabled = true;
            Tog_Click.IsEnabled = true;
            TB_Click.Text = task.Repeats.ToString();

            if (task._Delay > 0)
            {
                lb_dmin.Visibility = Visibility.Visible;
                lb_dsec.Visibility = Visibility.Visible;
                tb_dmin.Visibility = Visibility.Visible;
                tb_dsec.Visibility = Visibility.Visible;

                tb_dmin.Margin = new Thickness(45, 71, 0, 0);
                tb_dsec.Margin = new Thickness(135, 71, 0, 0);
                lb_dmin.Margin = new Thickness(10, 67, 0, 0);
                lb_dsec.Margin = new Thickness(100, 71, 0, 0);
                tb_dmin.IsEnabled = true;
                tb_dsec.IsEnabled = true;
                int min = (int)(task._Delay / 60);
                int sec = (int)(task._Delay % 60);
                tb_dmin.Text = min.ToString();
                tb_dsec.Text = sec.ToString();
            }
            if (!task.Leftclick)
            {
                Tog_Click.IsChecked = true;
            }
        }
        private void Delay(DelayTask task)
        {
            lb_EditDelay.Visibility = Visibility.Visible;
            lb_dmin.Visibility = Visibility.Visible;
            lb_dsec.Visibility = Visibility.Visible;
            tb_dmin.Visibility = Visibility.Visible;
            tb_dsec.Visibility = Visibility.Visible;

            tb_dmin.IsEnabled = true;
            tb_dsec.IsEnabled = true;
            int min = (int)(task.Delay / 60);
            int sec = (int)(task.Delay % 60);
            tb_dmin.Text = min.ToString();
            tb_dsec.Text = sec.ToString();
        }
        private void Move(MoveTask task)
        {
            lb_EditMove.Visibility = Visibility.Visible;
            lb_X.Visibility = Visibility.Visible;
            lb_Y.Visibility = Visibility.Visible;
            TB_X.Visibility = Visibility.Visible;
            TB_Y.Visibility = Visibility.Visible;
            btn_Auto.Visibility = Visibility.Visible;
            btn_Test.Visibility = Visibility.Visible;

            TB_X.IsEnabled = true;
            TB_Y.IsEnabled = true;
            btn_Auto.IsEnabled = true;
            btn_Test.IsEnabled = true;
            TB_X.Text = task.XPos.ToString();
            TB_Y.Text = task.YPos.ToString();
        }

        private static readonly Regex _regexDeci = new Regex("[^0-9,]+");
        private void PosDeci(object sender, TextCompositionEventArgs e)
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
        private static readonly Regex _regexPos = new Regex("[^0-9]+");
        private void Pos(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as System.Windows.Controls.TextBox;
            // Block invalid characters
            if (_regexPos.IsMatch(e.Text))
            {
                e.Handled = true;
                return;
            }
        }
        private static readonly Regex _regexPosNeg = new Regex("[^0-9+-]+");
        private void PosNeg(object sender, TextCompositionEventArgs e)
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

        private void Tog_Clicked(object sender, RoutedEventArgs e)
        {
            if (Tog_Click.IsChecked == true)
            {
                Tog_Click.ToolTip = "Leftclick?";
            }
            else
            {
                Tog_Click.ToolTip = "Rightclick?";
            }
        }

        private void Update(object sender, TextChangedEventArgs e)
        {
            Update();
        }
        private void Update()
        {

            if (task is DelayTask)
            {
                Check_Delay();
            }
            if (task is ClickTask)
            {
                Check_Click();
            }
            if (task is MoveTask)
            {
                Check_Move();
            }

            OK.IsEnabled = click && delay && move;
        }
        private void Check_Click()
        {
            click = false;
            try
            {
                if (uint.Parse(TB_Click.Text.Trim()) > 1)
                {
                    lb_dmin.Visibility = Visibility.Visible;
                    lb_dsec.Visibility = Visibility.Visible;
                    tb_dmin.Visibility = Visibility.Visible;
                    tb_dsec.Visibility = Visibility.Visible;

                    tb_dmin.Margin = new Thickness(45, 71, 0, 0);
                    tb_dsec.Margin = new Thickness(135, 71, 0, 0);
                    lb_dmin.Margin = new Thickness(10, 67, 0, 0);
                    lb_dsec.Margin = new Thickness(100, 71, 0, 0);
                    tb_dmin.IsEnabled = true;
                    tb_dsec.IsEnabled = true;
                }
                else
                {
                    lb_dmin.Visibility = Visibility.Collapsed;
                    lb_dsec.Visibility = Visibility.Collapsed;
                    tb_dmin.Visibility = Visibility.Collapsed;
                    tb_dsec.Visibility = Visibility.Collapsed;
                }
                if (uint.Parse(TB_Click.Text.Trim()) == 1)
                {
                    click = true;
                }
                else if (uint.Parse(TB_Click.Text.Trim()) > 1 && delay)
                {
                    click = true;
                }
                else
                {
                    throw new Exception("False");
                }
            }
            catch
            {
                click = false;
            }
        }

        private void Check_Delay()
        {
            delay = false;
            try
            {
                if ((double.TryParse(tb_dmin.Text.Trim(), out double d) && d > 0) || (double.TryParse(tb_dsec.Text.Trim(), out double q) && q > 0))
                {
                    delay = true;
                }
                else
                {
                    throw new Exception("False");
                }
            }
            catch
            {
                delay = false;
            }
        }

        private void Check_Move()
        {
            move = false;
            try
            {
                if (int.TryParse(TB_X.Text.Trim(), out int x) && int.TryParse(TB_Y.Text.Trim(), out int y))
                {
                    move = true;
                }
                else
                {
                    throw new Exception("False");
                }
            }
            catch
            {
                move = false;
            }
        }

        private void btn_Test_Click(object sender, RoutedEventArgs e)
        {
            SetCursorPos(int.Parse(TB_X.Text), int.Parse(TB_Y.Text));
        }

        private async void btn_Auto_Click(object sender, RoutedEventArgs e)
        {
            btn_Auto.Background = new SolidColorBrush(Colors.Lime);
            Keyboard.ClearFocus();
            await WaitForSpace();
            POINT p;
            GetCursorPos(out p);
            TB_X.Text = p.X.ToString();
            TB_Y.Text = p.Y.ToString();
            btn_Auto.Background = new SolidColorBrush(Colors.LightGray);
        }
        private async Task WaitForSpace()
        {
            while (true)
            {
                if (Keyboard.IsKeyDown(Key.Space))
                {
                    return;
                }
                await Task.Delay(10);
            }
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (task is ClickTask click)
            {
                click.Repeats = int.Parse(TB_Click.Text.Trim());
                click.Leftclick = Tog_Click.IsChecked == true ? false : true;
            }

            if (task is DelayTask delay)
            {
                delay.Delay = double.Parse(tb_dmin.Text.Trim()) * 60 + double.Parse(tb_dsec.Text.Trim());
            }

            if (task is MoveTask move)
            {
                move.XPos = int.Parse(TB_X.Text.Trim());
                move.YPos = int.Parse(TB_Y.Text.Trim());
            }
            EditingWindow.Close();
        }
    }
}
