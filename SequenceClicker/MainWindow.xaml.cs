using Microsoft.Win32;
using SequenceClicker.Tasks;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace SequenceClicker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region DLL imports
        #region HotKey
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        #endregion
        #region Cursor
        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int X, int Y);
        [DllImport("user32.dll", SetLastError = true)]
        static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);
        [DllImport("user32.dll")]
        static extern bool GetCursorPos(out POINT lpPoint);
        #endregion
        #endregion

        #region HotKey Variables
        // Modifier keys
        private const uint MOD_NONE = 0x0000;  // No modifier
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint MOD_WIN = 0x0008;

        private const int F6STRGID = 9000; // any unique ID
        private const int F7STRGID = 9001; // any unique ID
        private const int F7ALTID = 9002; // any unique ID
        // --- Structures for SendInput ---
        [StructLayout(LayoutKind.Sequential)]
        struct INPUT
        {
            public uint type;
            public MOUSEINPUT mi;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }
        #endregion

        #region Cursor Variables
        // --- Constants ---
        const uint INPUT_MOUSE = 0;
        // Left button
        const uint MOUSEEVENTF_LEFTDOWN = 0x02;
        const uint MOUSEEVENTF_LEFTUP = 0x04;
        // Right button
        const uint MOUSEEVENTF_RIGHTDOWN = 0x08;
        const uint MOUSEEVENTF_RIGHTUP = 0x10;

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }
        #endregion

        #region Functionallity Variables
        private bool _isRunning = false;
        private bool _editRunning = false;
        private bool _saved = false;
        private bool _timed = false;

        private CancellationTokenSource _cts;

        private ObservableCollection<MyTask> _savedSequence = new ObservableCollection<MyTask>();
        private ObservableCollection<MyTask> _sequence = new ObservableCollection<MyTask>();

        private Window _editWindow;
        #endregion

        private Point _dragStartPoint;
        private AdornerLayer _adornerLayer;
        private DropInsertionAdorner _dropAdorner;

        public MainWindow()
        {
            InitializeComponent();
            LB_Seq.ItemsSource = _sequence;
        }

        #region Functionallity
        private async Task RunTasks(List<MyTask> currendSeq, CancellationToken token, int repeats)
        {
            Status.Text = "Autoclicker started...";
            for (int i = 0; i != repeats; i++)
            {
                if (i == int.MaxValue) i = 1;
                foreach (MyTask task in currendSeq)
                {
                    if (token.IsCancellationRequested)
                        break;

                    if (task is ClickTask click)
                    {
                        INPUT[] Input = new INPUT[2];
                        if (click.IsLeftclick)
                        {
                            Input[0].type = INPUT_MOUSE;
                            Input[0].mi.dwFlags = MOUSEEVENTF_LEFTDOWN;

                            Input[1].type = INPUT_MOUSE;
                            Input[1].mi.dwFlags = MOUSEEVENTF_LEFTUP;
                        }
                        else
                        {
                            Input[0].type = INPUT_MOUSE;
                            Input[0].mi.dwFlags = MOUSEEVENTF_RIGHTDOWN;

                            Input[1].type = INPUT_MOUSE;
                            Input[1].mi.dwFlags = MOUSEEVENTF_RIGHTUP;
                        }
                        await DoClick(Input, click.Repeats, click.Delay, token);
                    }
                    if (task is DelayTask delay)
                    {
                        await DoDelay(delay.Delay, token);
                    }
                    if (task is MoveTask move)
                    {
                        await DoMove(move.XPos, move.YPos, token);
                    }
                }

                if (token.IsCancellationRequested)
                    break;
            }
            _isRunning = false;
            Status.Text = "Autoclicker stoped.";
        }
        private async Task DoClick(INPUT[] inputs, int repeats, double delay, CancellationToken token)
        {
            for (int i = 0; i < repeats; i++)
            {
                SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
                await DoDelay(delay, token);
            }
            await Task.Delay(0, token);
        }
        private async Task DoDelay(double delay, CancellationToken token)
        {
            int calculatedDelay = (int)(1000 * delay);
            await Task.Delay(calculatedDelay, token);
        }
        private async Task DoMove(int x, int y, CancellationToken token)
        {
            SetCursorPos(x, y);
            await Task.Delay(0, token);
        }
        public void StopClickTask()
        {
            _cts?.Cancel();
            _isRunning = false;
            Status.Text = "Autoclicker was canceled.";
        }
        #endregion

        #region Buttons

        #region Add
        private void btn_AddClick(object sender, RoutedEventArgs e)
        {
            ClickTask t;
            bool left = Tog_Click.IsChecked == true ? false : true;
            int delay = -1;

            if (int.TryParse(TB_Click.Text.Trim(), out int rep) && rep > 1)
            {
                double min = -1;
                try
                {
                    min = double.Parse(tb_dmin.Text.Trim());
                }
                catch (FormatException)
                {
                    min = 0;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Screenshot this error\n" + ex.Message, "Error while adding Click Task", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                double sec = -1;
                try
                {
                    sec = double.Parse(tb_dsec.Text.Trim());
                }
                catch (FormatException)
                {
                    sec = 0;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Screenshot this error\n" + ex.Message, "Error while adding Click Task", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                delay = (int)((min * 60 + sec) * 1000);
                t = new ClickTask(left, rep, delay);
            }
            else
            {
                t = new ClickTask(left);
            }
            _sequence.Add(t);
            Status.Text = $"{t} was added";
            Update();
            _saved = false;
        }
        private void btn_AddDelay(object sender, RoutedEventArgs e)
        {
            double min = -1;
            try
            {
                min = double.Parse(tb_dmin.Text.Trim());
            }
            catch (FormatException)
            {
                min = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Screenshot this error\n" + ex.Message, "Error while adding Click Task", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            double sec = -1;
            try
            {
                sec = double.Parse(tb_dsec.Text.Trim());
            }
            catch (FormatException)
            {
                sec = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Screenshot this error\n" + ex.Message, "Error while adding Click Task", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            DelayTask t = new DelayTask((int)((min * 60d + sec) * 1000));
            _sequence.Add(t);
            Status.Text = $"{t} was added";
            Update();
            _saved = false;
        }
        private void btn_AddMove(object sender, RoutedEventArgs e)
        {
            MoveTask t = new MoveTask(int.Parse(TB_X.Text.Trim()), int.Parse(TB_Y.Text.Trim()));
            _sequence.Add(t);
            Status.Text = $"{t} was added";
            Update();
            _saved = false;
        }
        private void btn_AddTimed(object sender, RoutedEventArgs e)
        {
            if (_timed)
            {
                _timed = false;
                btn_Timed.Background = new SolidColorBrush(Colors.LightGray);
                tb_min.IsEnabled = true;
                tb_sec.IsEnabled = true;
            }
            else
            {
                _timed = true;
                btn_Timed.Background = new SolidColorBrush(Colors.Lime);
                tb_min.IsEnabled = false;
                tb_sec.IsEnabled = false;
                double min;
                try
                {
                    min = double.Parse(tb_min.Text.Trim());
                }
                catch
                {
                    min = 0;
                }

                double sec;
                try
                {
                    sec = double.Parse(tb_sec.Text.Trim());
                }
                catch
                {
                    sec = 0;
                }
                _sequence.Add(new TimedTask(min * 60 + sec, true));
            }
        }
        #endregion

        #region Funtionallity
        private void btn_Delete_Click(object sender, RoutedEventArgs e)
        {
            if (_sequence.Count > 0 && LB_Seq.SelectedItem != null)
            {
                MyTask t = LB_Seq.SelectedItem as MyTask;
                _sequence.Remove(t);
                Status.Text = $"{t} was deleted";
                Update();
                _saved = false;
            }
        }
        private void btn_Reset_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult res = MessageBox.Show("Are you sure you want to reset the sequence", "Reset warning", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
            if (res == MessageBoxResult.Yes)
            {
                _sequence.Clear();
                Status.Text = $"sequence was reset";
            }
            Update();
            _saved = false;
        }
        private void btn_Start_Click(object sender, RoutedEventArgs e)
        {
            if (!_isRunning)
            {
                _isRunning = true;
                _cts = new CancellationTokenSource();
                _ = RunTasks(new List<MyTask>(_sequence), _cts.Token, int.Parse(TB_Repeats.Text.Trim()));
            }
        }
        private void Tog_Inf_Checked(object sender, RoutedEventArgs e)
        {
            if (Tog_Inf.IsChecked == true)
            {
                TB_Repeats.IsEnabled = false;
                TB_Repeats.Text = "-1";
                Tog_Inf.ToolTip = "Numbered repeats?";
            }
            else
            {
                TB_Repeats.IsEnabled = true;
                Tog_Inf.ToolTip = "Infinite repeats?";
            }
            Update();
        }
        private void btn_Test_Click(object sender, RoutedEventArgs e)
        {
            SetCursorPos(int.Parse(TB_X.Text), int.Parse(TB_Y.Text));
        }
        private async void btn_Auto_Click(object sender, RoutedEventArgs e)
        {
            Status.Text = "Move your cursor to the wanted location and confirm the position by pressing \"Spacebar\".";
            btn_Auto.Background = new SolidColorBrush(Colors.Lime);
            Keyboard.ClearFocus();
            await WaitForSpace();
            POINT p;
            GetCursorPos(out p);
            TB_X.Text = p.X.ToString();
            TB_Y.Text = p.Y.ToString();
            btn_Auto.Background = new SolidColorBrush(Colors.LightGray);
            Status.Text = "Cursor position was set.";
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
        private void btn_Save_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Seq files (*.seq)|*.seq|All files (*.*)|*.*";
            bool? result = saveFileDialog.ShowDialog();
            if (result == true)
            {
                string path = saveFileDialog.FileName;
                StreamWriter sw = new StreamWriter(path);

                StringBuilder sb = new StringBuilder();
                foreach (MyTask t in _sequence)
                {
                    sb.Append(t.GetSave());
                }
                try
                {
                    sw.WriteLine(sb.ToString());
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Saveing Failed.\n{ex}", "Error while saving", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                _saved = true;
                _savedSequence = _sequence;
                sw.Close();
                Update();
            }
        }
        private void btn_Load_Click(object sender, RoutedEventArgs e)
        {
            if (_sequence.Count > 0 && !_saved)
            {
                if (MessageBoxResult.Yes == MessageBox.Show("You have an unsaved sequence. Do you want to save it?", "Loading", MessageBoxButton.YesNo))
                {
                    btn_Save_Click(sender, e);
                }
            }
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Seq files (*.seq)|*.seq|All files (*.*)|*.*";
            bool? result = openFileDialog.ShowDialog();
            if (result == true)
            {
                string filePath = openFileDialog.FileName;
                StreamReader sr = new StreamReader(filePath);
                _sequence.Clear();
                try
                {
                    string saveTxt = sr.ReadToEnd().Trim();
                    string[] tasks = saveTxt.Split("\n");
                    foreach (string task in tasks)
                    {
                        if (!string.IsNullOrEmpty(task))
                        {
                            MyTask t = MyTask.LoadSave(task);
                            if (t == null)
                            {
                                throw new Exception("Save file has wrong values. Loading failed.");
                            }
                            _sequence.Add(t);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Loading Failed.\n{ex}", "Error while loading", MessageBoxButton.OK, MessageBoxImage.Error);
                    _sequence.Clear();
                }
                _saved = true;
                _savedSequence = _sequence;
                Update();
                sr.Close();

            }
        }
        #endregion

        #region Activation handler
        private void Check_Click()
        {
            btn_Click.IsEnabled = ClickTask.ValidInput(TB_Click.Text, btn_Delay.IsEnabled);
        }
        private void Check_Delay()
        {
            btn_Delay.IsEnabled = DelayTask.ValidInput(tb_dmin.Text, tb_dsec.Text);
        }
        private void Check_Move()
        {
            try
            {
                if (int.TryParse(TB_X.Text.Trim(), out int x) && int.TryParse(TB_Y.Text.Trim(), out int y))
                {
                    btn_Move.IsEnabled = true;
                    btn_Test.IsEnabled = true;
                }
                else
                {
                    throw new Exception("False");
                }
            }
            catch
            {
                btn_Move.IsEnabled = false;
                btn_Test.IsEnabled = false;
            }
        }
        private void Check_Repeat()
        {
            try
            {
                if ((int.Parse(TB_Repeats.Text.Trim()) > 0 || Tog_Inf.IsChecked == true) && _sequence.Count > 0)
                {
                    btn_Start.IsEnabled = true;
                }
                else
                {
                    throw new Exception("False");
                }
            }
            catch
            {
                btn_Start.IsEnabled = false;
            }
        }
        private void Check_Timed()
        {
            try
            {
                if ((double.TryParse(tb_min.Text.Trim(), out double d) && d > 0) || (double.TryParse(tb_sec.Text.Trim(), out double q) && q > 0))
                {
                    btn_Timed.IsEnabled = true;
                }
                else
                {
                    throw new Exception("False");
                }
            }
            catch
            {
                btn_Timed.IsEnabled = false;
            }
        }
        #endregion

        #endregion

        #region Input Regex checker
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

        #region UI updater
        private void Update(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            Update();
        }
        private void Update()
        {
            if (_sequence.Count >= 1 && !_saved)
            {
                btn_Save.IsEnabled = true;
            }
            else
            {
                btn_Save.IsEnabled = false;
            }
            Check_Move();
            Check_Delay();
            Check_Click();
            Check_Repeat();
            Check_Timed();
        }
        private void TB_Delay_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            Check_Delay();
            Update();
        }
        private void TB_Move_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            Check_Move();
            Update();
        }
        private void TB_Repeats_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            Check_Repeat();
            Update();
        }
        #endregion

        #region Key detectors
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // Get window handle
            var helper = new WindowInteropHelper(this);

            // Example: register F6 without modifiers
            RegisterHotKey(helper.Handle, F6STRGID, MOD_CONTROL, 0x75);
            RegisterHotKey(helper.Handle, F7STRGID, MOD_CONTROL, 0x76);
            RegisterHotKey(helper.Handle, F7ALTID, MOD_ALT, 0x76);

            // Hook into WndProc
            HwndSource source = HwndSource.FromHwnd(helper.Handle);
            source.AddHook(HwndHook);
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;

            if (msg == WM_HOTKEY && wParam.ToInt32() == F7ALTID)
            {
                // Your event here
                Application.Current.Shutdown();
                handled = true;
            }
            if (msg == WM_HOTKEY && wParam.ToInt32() == F6STRGID)
            {
                // Your event here
                if (!_isRunning && btn_Start.IsEnabled)
                {
                    _isRunning = true;
                    _cts = new CancellationTokenSource();
                    _ = RunTasks(new List<MyTask>(_sequence), _cts.Token, int.Parse(TB_Repeats.Text.Trim()));
                }
                handled = true;
            }
            if (msg == WM_HOTKEY && wParam.ToInt32() == F7STRGID)
            {
                // Your event here
                StopClickTask();
                handled = true;
            }

            return IntPtr.Zero;
        }
        protected override void OnClosed(EventArgs e)
        {
            // Cleanup
            var helper = new WindowInteropHelper(this);
            UnregisterHotKey(helper.Handle, F6STRGID);
            UnregisterHotKey(helper.Handle, F7STRGID);
            UnregisterHotKey(helper.Handle, F7ALTID);
            base.OnClosed(e);
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
        #endregion

        #region ListBox handler
        private void LB_Seq_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (LB_Seq.SelectedItem != null)
            {
                btn_Delete.IsEnabled = true;
            }
            else
            {
                btn_Delete.IsEnabled = false;
            }
        }

        private void LB_Seq_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
        }

        private void LB_Seq_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point position = e.GetPosition(null);
                if (System.Math.Abs(position.X - _dragStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    System.Math.Abs(position.Y - _dragStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    if (sender is ListView listView && listView.SelectedItem != null)
                    {
                        DragDrop.DoDragDrop(listView, listView.SelectedItem, DragDropEffects.Move);
                    }
                }
            }
        }

        private void LB_Seq_Drop(object sender, DragEventArgs e)
        {
            RemoveDropAdorner();

            if (e.Data.GetDataPresent(typeof(MyTask)))
            {
                var droppedData = e.Data.GetData(typeof(MyTask)) as MyTask;
                var listView = sender as ListView;
                Point position = e.GetPosition(listView);
                var targetItem = GetItemAt(listView, position);

                if (targetItem == null || droppedData == targetItem)
                    return;

                int oldIndex = _sequence.IndexOf(droppedData);
                int newIndex = _sequence.IndexOf(targetItem);

                // Decide if above or below
                var container = listView.ItemContainerGenerator.ContainerFromItem(targetItem) as ListViewItem;
                if (container != null)
                {
                    Point itemPos = e.GetPosition(container);
                    if (itemPos.Y > container.ActualHeight / 2)
                        newIndex++;
                }

                if (oldIndex != newIndex)
                {
                    if (newIndex >= _sequence.Count) newIndex = _sequence.Count - 1;
                    _sequence.Move(oldIndex, newIndex);

                    _saved = false;
                    Update();
                }
            }
        }
        private MyTask GetItemAt(ListView listView, Point position)
        {
            var element = listView.InputHitTest(position) as FrameworkElement;
            return element?.DataContext as MyTask;
        }

        private void LB_Seq_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(MyTask)))
            {
                var listView = sender as ListView;
                Point position = e.GetPosition(listView);
                var targetItem = GetItemAt(listView, position);

                RemoveDropAdorner();

                if (targetItem != null)
                {
                    var container = listView.ItemContainerGenerator.ContainerFromItem(targetItem) as ListViewItem;
                    if (container != null)
                    {
                        Point itemPos = e.GetPosition(container);
                        bool isAbove = itemPos.Y < container.ActualHeight / 2;

                        _adornerLayer = AdornerLayer.GetAdornerLayer(container);
                        _dropAdorner = new DropInsertionAdorner(container, isAbove);
                        _adornerLayer.Add(_dropAdorner);
                    }
                }
            }
        }
        private void RemoveDropAdorner()
        {
            if (_dropAdorner != null && _adornerLayer != null)
            {
                _adornerLayer.Remove(_dropAdorner);
                _dropAdorner = null;
            }
        }
        private void LB_Seq_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (LB_Seq.SelectedItem != null && !_editRunning)
            {
                string old = LB_Seq.SelectedItem.ToString();
                _editRunning = true;
                _editWindow = new EditWindow(LB_Seq.SelectedItem as MyTask);
                AutoClicker.Topmost = false;
                _editWindow.ShowDialog();
                _editRunning = false;
                AutoClicker.Topmost = true;
                if (old != LB_Seq.SelectedItem.ToString())
                {
                    _saved = false;
                    Update();
                }
            }
            else
            {
                _editWindow.Focus();
            }
        }
        #endregion
    }
}