using SequenceClicker.Tasks;
using System.Runtime.InteropServices;
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
            ClickVis();
            DelayVis();
            MoveVis();
            TimedVis();

            prep(task);
            Update();
        }
        #region UI updater
        #region Visibilitys
        private void ClickVis()
        {
            if (lb_EditClick.Visibility == Visibility.Visible)
            {
                lb_EditClick.Visibility = Visibility.Collapsed;
                lb_Click.Visibility = Visibility.Collapsed;
                TB_Click.Visibility = Visibility.Collapsed;
                Tog_Click.Visibility = Visibility.Collapsed;
            }
            else
            {
                lb_EditClick.Visibility = Visibility.Visible;
                lb_Click.Visibility = Visibility.Visible;
                TB_Click.Visibility = Visibility.Visible;
                Tog_Click.Visibility = Visibility.Visible;
            }
        }
        private void DelayVis()
        {
            if (lb_dmin.Visibility == Visibility.Visible)
            {
                lb_EditDelay.Visibility = Visibility.Collapsed;
                lb_dmin.Visibility = Visibility.Collapsed;
                lb_dsec.Visibility = Visibility.Collapsed;
                tb_dmin.Visibility = Visibility.Collapsed;
                tb_dsec.Visibility = Visibility.Collapsed;
            }
            else if (lb_EditClick.Visibility == Visibility.Visible)
            {
                lb_dmin.Visibility = Visibility.Visible;
                lb_dsec.Visibility = Visibility.Visible;
                tb_dmin.Visibility = Visibility.Visible;
                tb_dsec.Visibility = Visibility.Visible;
            }
            else
            {
                lb_EditDelay.Visibility = Visibility.Visible;
                lb_dmin.Visibility = Visibility.Visible;
                lb_dsec.Visibility = Visibility.Visible;
                tb_dmin.Visibility = Visibility.Visible;
                tb_dsec.Visibility = Visibility.Visible;
            }
        }
        private void MoveVis()
        {
            if (lb_EditMove.Visibility == Visibility.Visible)
            {
                lb_EditMove.Visibility = Visibility.Collapsed;
                lb_X.Visibility = Visibility.Collapsed;
                lb_Y.Visibility = Visibility.Collapsed;
                TB_X.Visibility = Visibility.Collapsed;
                TB_Y.Visibility = Visibility.Collapsed;
                btn_Auto.Visibility = Visibility.Collapsed;
                btn_Test.Visibility = Visibility.Collapsed;
            }
            else
            {
                lb_EditMove.Visibility = Visibility.Visible;
                lb_X.Visibility = Visibility.Visible;
                lb_Y.Visibility = Visibility.Visible;
                TB_X.Visibility = Visibility.Visible;
                TB_Y.Visibility = Visibility.Visible;
                btn_Auto.Visibility = Visibility.Visible;
                btn_Test.Visibility = Visibility.Visible;
            }
        }
        private void TimedVis()
        {
            if (lb_TimedDelay.Visibility == Visibility.Visible)
            {
                lb_TimedDelay.Visibility = Visibility.Collapsed;
                lb_tmin.Visibility = Visibility.Collapsed;
                lb_tsec.Visibility = Visibility.Collapsed;
                tb_tmin.Visibility = Visibility.Collapsed;
                tb_tsec.Visibility = Visibility.Collapsed;
            }
            else
            {
                lb_TimedDelay.Visibility = Visibility.Visible;
                lb_tmin.Visibility = Visibility.Visible;
                lb_tsec.Visibility = Visibility.Visible;
                tb_tmin.Visibility = Visibility.Visible;
                tb_tsec.Visibility = Visibility.Visible;
            }
        }
        #endregion
        #region UI setter
        private void Click(ClickTask task)
        {
            ClickVis();
            TB_Click.IsEnabled = true;
            Tog_Click.IsEnabled = true;
            TB_Click.Text = task.Repeats.ToString();

            if (task.Delay > 0)
            {
                DelayVis();
                tb_dmin.Margin = new Thickness(45, 71, 0, 0);
                tb_dsec.Margin = new Thickness(135, 71, 0, 0);
                lb_dmin.Margin = new Thickness(10, 67, 0, 0);
                lb_dsec.Margin = new Thickness(100, 71, 0, 0);
                tb_dmin.IsEnabled = true;
                tb_dsec.IsEnabled = true;
                double time = (double)task.Delay / 1000d;
                double min = 0;
                if (time > 60)
                    min = (time / 60d);
                double sec = (time % 60d);
                tb_dmin.Text = min.ToString();
                tb_dsec.Text = sec.ToString();
            }
            if (!task.IsLeftclick)
            {
                Tog_Click.IsChecked = true;
            }
        }
        private void Delay(DelayTask task)
        {
            DelayVis();

            tb_dmin.IsEnabled = true;
            tb_dsec.IsEnabled = true;
            double time = (double)task.Delay / 1000d;
            double min = 0;
            if (time > 60)
                min = (time / 60d);
            double sec = (time % 60d);
            tb_dmin.Text = min.ToString();
            tb_dsec.Text = sec.ToString();
        }
        private void Move(MoveTask task)
        {
            MoveVis();

            TB_X.IsEnabled = true;
            TB_Y.IsEnabled = true;
            btn_Auto.IsEnabled = true;
            btn_Test.IsEnabled = true;
            TB_X.Text = task.XPos.ToString();
            TB_Y.Text = task.YPos.ToString();
        }
        private void Timed(TimedTask task)
        {

        }
        #endregion
        private void prep(MyTask task)
        {
            switch (task)
            {
                case ClickTask click: Click(task as ClickTask); break;
                case DelayTask delay: Delay(task as DelayTask); break;
                case MoveTask move: Move(task as MoveTask); break;
                case TimedTask Time: Timed(task as TimedTask); break;
                default: MessageBox.Show($"Unknown Task.\n{task.GetType()} does not exist.", "Error while executing", MessageBoxButton.OK, MessageBoxImage.Error); break;
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
            if (uint.TryParse(TB_Click.Text.Trim(), out uint rep) && rep > 1)
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
                delay = DelayTask.ValidInput(tb_dmin.Text, tb_dsec.Text);
                click = ClickTask.ValidInput(TB_Click.Text, delay);
            }
            else
            {
                lb_dmin.Visibility = Visibility.Collapsed;
                lb_dsec.Visibility = Visibility.Collapsed;
                tb_dmin.Visibility = Visibility.Collapsed;
                tb_dsec.Visibility = Visibility.Collapsed;
                delay = true;
                click = ClickTask.ValidInput(TB_Click.Text, delay);
            }
        }

        private void Check_Delay()
        {
            delay = DelayTask.ValidInput(tb_dmin.Text, tb_dsec.Text);
        }
        private void Check_Move()
        {
            move = MoveTask.ValidInput(TB_X.Text, TB_Y.Text);
        }
        #endregion
        #region Regex Input Handler
        private void PosDeci(object sender, TextCompositionEventArgs e)
        {
            RegexTextControl.PosDeci(sender, e);
        }
        private void Pos(object sender, TextCompositionEventArgs e)
        {
            RegexTextControl.Pos(sender, e);
        }
        private void PosNeg(object sender, TextCompositionEventArgs e)
        {
            RegexTextControl.PosNeg(sender, e);
        }
        #endregion
        #region Button
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


        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (task is ClickTask click)
            {
                click.IsLeftclick = Tog_Click.IsChecked == true ? false : true;
                if(int.TryParse(TB_Click.Text.Trim(), out int rep) && rep > 1)
                {
                    click.Repeats = rep;
                    try
                    {
                        click.Delay = (int)((double.Parse(tb_dmin.Text.Trim()) * 60 + double.Parse(tb_dsec.Text.Trim())) * 1000);
                    }
                    catch (OverflowException ex)
                    {
                        click.Delay = int.MaxValue;
                    }
                }
            }

            if (task is DelayTask delay)
            {
                try
                {
                    delay.Delay = (int)((double.Parse(tb_dmin.Text.Trim()) * 60 + double.Parse(tb_dsec.Text.Trim())) * 1000);
                }
                catch (OverflowException ex)
                {
                    delay.Delay = int.MaxValue;
                }
            }

            if (task is MoveTask move)
            {
                move.XPos = int.Parse(TB_X.Text.Trim());
                move.YPos = int.Parse(TB_Y.Text.Trim());
            }
            EditingWindow.Close();
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
        #endregion
        /// <summary>
        /// Waits until the spacebar is pressed
        /// </summary>
        /// <author>CC-7956</author>
        private async Task<bool> WaitForSpace()
        {
            while (true)
            {
                if (Keyboard.IsKeyDown(Key.Space))
                {
                    return true;
                }
                if (Keyboard.IsKeyDown(Key.Back))
                {
                    return false;
                }
                await Task.Delay(10);
            }
        }
    }
}
