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
        private MyTask _draggedItem;
        private AdornerLayer _adornerLayer;
        private DropInsertionAdorner _dropAdorner;
        private (ListViewItem container, bool isAbove, int index) _currentDropTarget = (null, false, -1);

        public MainWindow()
        {
            InitializeComponent();
            LB_Seq.ItemsSource = _sequence;
            LB_Seq.PreviewDragLeave += LB_Seq_PreviewDragLeave;
        }

        #region Functionallity
        /// <summary>
        /// Executor of the given task sequence
        /// </summary>
        /// <param name="currendSeq">The current sequence</param>
        /// <param name="token">Non canceled Token</param>
        /// <param name="repeats">Number of repeats</param>
        /// <author>CC-7956</author>
        private async Task RunTasks(List<MyTask> currendSeq, CancellationToken token, int repeats)
        {
            AutoClicker.Topmost = true;
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
            AutoClicker.Topmost = false;
        }

        /// <summary>
        /// Executor of a click task
        /// </summary>
        /// <param name="inputs">Input sequence</param>
        /// <param name="repeats">Number of repeats</param>
        /// <param name="delay">Delay in ms between every click</param>
        /// <param name="token">Non canceled Token</param>
        /// <author>CC-7956</author>
        private async Task DoClick(INPUT[] inputs, int repeats, int delay, CancellationToken token)
        {
            for (int i = 0; i < repeats; i++)
            {
                SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
                await DoDelay(delay, token);
            }
            await Task.Delay(0, token);
        }

        /// <summary>
        /// Executor of a delay task
        /// </summary>
        /// <param name="delay">Delay in ms</param>
        /// <param name="token">Non canceled Token</param>
        /// <author>CC-7956</author>
        private async Task DoDelay(int delay, CancellationToken token)
        {
            await Task.Delay(delay, token);
        }

        /// <summary>
        /// Executor of a move task
        /// </summary>
        /// <param name="x">Relativ X coordinate</param>
        /// <param name="y">Relativ Y coordinate</param>
        /// <param name="token">Non canceled Token</param>
        /// <author>CC-7956</author>
        private async Task DoMove(int x, int y, CancellationToken token)
        {
            SetCursorPos(x, y);
            await Task.Delay(0, token);
        }
        /// <summary>
        /// Stops the current running sequence
        /// </summary>
        /// <author>CC-7956 / ChatGPT</author>
        public void StopClickTask()
        {
            _cts?.Cancel();
            _isRunning = false;
            Status.Text = "Autoclicker was canceled.";
            AutoClicker.Topmost = false;

        }
        #endregion

        #region Buttons

        #region Add
        /// <summary>
        /// Adds a Click Task to the sequence
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event</param>
        /// <author>CC-7956</author>
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

        /// <summary>
        /// Adds a Delay Task to the sequence
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event</param>
        /// <author>CC-7956</author>
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

        /// <summary>
        /// Adds a Move Task to the sequence
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event</param>
        /// <author>CC-7956</author>
        private void btn_AddMove(object sender, RoutedEventArgs e)
        {
            MoveTask t = new MoveTask(int.Parse(TB_X.Text.Trim()), int.Parse(TB_Y.Text.Trim()));
            _sequence.Add(t);
            Status.Text = $"{t} was added";
            Update();
            _saved = false;
        }

        /// <summary>
        /// Adds a Timed Task to the sequence
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event</param>
        /// <author>CC-7956</author>
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
        /// <summary>
        /// Deletes the selected task from the sequence
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event</param>
        /// <author>CC-7956</author>
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

        /// <summary>
        /// Resets the current sequence
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event</param>
        /// <author>CC-7956</author>
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

        /// <summary>
        /// Starts execution of the current sequence
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event</param>
        /// <author>CC-7956 / ChatGPT</author>
        private void btn_Start_Click(object sender, RoutedEventArgs e)
        {
            if (!_isRunning)
            {
                _isRunning = true;
                _cts = new CancellationTokenSource();
                _ = RunTasks(new List<MyTask>(_sequence), _cts.Token, int.Parse(TB_Repeats.Text.Trim()));
            }
        }

        /// <summary>
        /// Toggles the amount of repeats between infinite and a given number
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event</param>
        /// <author>CC-7956</author>
        private void Tog_Inf_Checked(object sender, RoutedEventArgs e)
        {
            if (Tog_Inf.IsChecked == true)
            {
                TB_Repeats.IsEnabled = false;
                TB_Repeats.Text = "-1";
            }
            else
            {
                TB_Repeats.IsEnabled = true;
            }
            Update();
        }

        /// <summary>
        /// Moves the cursor to the given position
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event</param>
        /// <author>CC-7956</author>
        private void btn_Test_Click(object sender, RoutedEventArgs e)
        {
            SetCursorPos(int.Parse(TB_X.Text), int.Parse(TB_Y.Text));
        }

        /// <summary>
        /// Gets the current cursor position and sets the X and Y textboxes
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event</param>
        /// <author>CC-7956 / ChatGPT</author>
        private async void btn_Auto_Click(object sender, RoutedEventArgs e)
        {
            Status.Text = "Move your cursor to the wanted location and confirm the position by pressing \"Spacebar\". Press \"Backspace\" to cancel.";
            btn_Auto.Background = new SolidColorBrush(Colors.Lime);
            Keyboard.ClearFocus();
            bool _continue = await WaitForSpace();
            if (!_continue)
            {
                Status.Text = "Setting cursor position was canceled.";
                btn_Auto.Background = new SolidColorBrush(Colors.LightGray);
                return;
            }
            POINT p;
            GetCursorPos(out p);
            TB_X.Text = p.X.ToString();
            TB_Y.Text = p.Y.ToString();
            btn_Auto.Background = new SolidColorBrush(Colors.LightGray);
            Status.Text = "Cursor position was set.";
        }

        /// <summary>
        /// Saves the current sequence to a .seq file
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event</param>
        /// <author>CC-7956</author>
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

        /// <summary>
        /// Loads a .seq file to the current sequence
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event</param>
        /// <author>CC-7956</author>
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
        /// <summary>
        /// Checks if the input for the Click Task is valid and enables/disables the add button
        /// </summary>
        /// <author>CC-7956</author>
        private void Check_Click()
        {
            btn_Click.IsEnabled = ClickTask.ValidInput(TB_Click.Text, btn_Delay.IsEnabled);
        }

        /// <summary>
        /// Checks if the input for the Delay Task is valid and enables/disables the add button
        /// </summary>
        /// <author>CC-7956</author>
        private void Check_Delay()
        {
            btn_Delay.IsEnabled = DelayTask.ValidInput(tb_dmin.Text, tb_dsec.Text);
        }
        /// <summary>
        /// Checks if the input for the Move Task is valid and enables/disables the add and test button
        /// </summary>
        /// <author>CC-7956</author>
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

        /// <summary>
        /// Checks if the input for the Repeat count is valid and enables/disables the start button
        /// </summary>
        /// <author>CC-7956</author>
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
        /// <summary>
        /// Ckecks if the input for the Timed Task is valid and enables/disables the add button
        /// </summary>
        /// <author>CC-7956</author>
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
        /// <summary>
        /// Checks if the input is a positive decimal number
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event</param>
        /// <author>CC-7956</author>
        private void PosDeci(object sender, TextCompositionEventArgs e)
        {
            RegexTextControl.PosDeci(sender, e);
        }

        /// <summary>
        /// Checks if the input is a positive integer number
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event</param>
        /// <author>CC-7956</author>
        private void Pos(object sender, TextCompositionEventArgs e)
        {
            RegexTextControl.Pos(sender, e);
        }

        /// <summary>
        /// Checks if the input is a integer number
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event</param>
        /// <author>CC-7956</author>
        private void PosNeg(object sender, TextCompositionEventArgs e)
        {
            RegexTextControl.PosNeg(sender, e);
        }
        #endregion

        #region UI updater
        /// <summary>
        /// Helper function to update the UI after a change
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event</param>
        /// <author>CC-7956</author>
        private void Update(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            Update();
        }

        /// <summary>
        /// Method to update the UI after a change
        /// </summary>
        /// <author>CC-7956</author>
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
        #endregion

        #region Key detectors
        /// <summary>
        /// Registers the hotkeys and hooks into the WndProc
        /// </summary>
        /// <param name="e">Event</param>
        /// <author>ChatGPT</author>
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

        /// <summary>
        /// Handles the hotkey messages
        /// </summary>
        /// <param name="hwnd">hwnd</param>
        /// <param name="msg">Message</param>
        /// <param name="wParam">wParam</param>
        /// <param name="lParam">lParam</param>
        /// <param name="handled">handled</param>
        /// <author>ChatGPT</author>
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
        /// <summary>
        /// Unregisters the hotkeys
        /// </summary>
        /// <param name="e">Event</param>
        /// <author>ChatGPT</author>
        protected override void OnClosed(EventArgs e)
        {
            // Cleanup
            var helper = new WindowInteropHelper(this);
            UnregisterHotKey(helper.Handle, F6STRGID);
            UnregisterHotKey(helper.Handle, F7STRGID);
            UnregisterHotKey(helper.Handle, F7ALTID);
            base.OnClosed(e);
        }

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
        #endregion

        #region ListBox handler
        /// <summary>
        /// Selection changed event to enable/disable the delete button
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event</param>
        /// <author>CC-7956</author>
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

        /// <summary>
        /// Double click event to open the edit window for the selected task
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event</param>
        /// <author>ChatGPT</author>
        private void LB_Seq_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            MyTask item = (sender as ListViewItem)?.DataContext as MyTask;
            if (item != null && !_editRunning)
            {
                string old = item.ToString();
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

        /// <summary>
        /// Drag and drop initialization. <br/>
        /// Mouse down event.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event</param>
        /// <author>ChatGPT</author>
        private void LB_Seq_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
            _draggedItem = null;

            // find item under mouse
            var element = e.OriginalSource as DependencyObject;
            var container = ItemsControl.ContainerFromElement(LB_Seq, element) as ListViewItem;
            if (container != null)
            {
                _draggedItem = container.DataContext as MyTask;
            }
        }

        /// <summary>
        /// Drag and drop inprogress. <br/>
        /// Mouse move event.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event</param>
        /// <author>ChatGPT</author>
        private void LB_Seq_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || _draggedItem == null)
                return;

            var currentPos = e.GetPosition(null);
            Vector diff = currentPos - _dragStartPoint;
            if (Math.Abs(diff.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(diff.Y) < SystemParameters.MinimumVerticalDragDistance)
                return;

            // Pack the drag data using the base type so derived types match
            var data = new DataObject(typeof(MyTask), _draggedItem);

            // Make sure we can remove any leftover adorner
            RemoveDropAdorner();

            // Start drag - DoDragDrop blocks until drop completes
            DragDrop.DoDragDrop(LB_Seq, data, DragDropEffects.Move);

            // Clear dragged item and adorner after drag ends
            _draggedItem = null;
            RemoveDropAdorner();
        }

        /// <summary>
        /// Drag and drop processing. <br/>
        /// Drag over event.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event</param>
        /// <author>ChatGPT</author>
        private void LB_Seq_DragOver(object sender, DragEventArgs e)
        {
            // only accept MyTask (or derived)
            if (!e.Data.GetDataPresent(typeof(MyTask)))
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                RemoveDropAdorner();
                return;
            }

            e.Effects = DragDropEffects.Move;
            e.Handled = true;

            // find the item under the mouse (may be null)
            var element = e.OriginalSource as DependencyObject;
            var container = ItemsControl.ContainerFromElement(LB_Seq, element) as ListViewItem;

            bool isAbove = true;
            int index = -1;
            UIElement adornerTarget = null; // can be a ListViewItem or the ListView itself

            if (container != null)
            {
                // hovering over an item
                index = LB_Seq.ItemContainerGenerator.IndexFromContainer(container);
                Point posInItem = e.GetPosition(container);
                isAbove = posInItem.Y < container.ActualHeight / 2;
                adornerTarget = container;
            }
            else
            {
                // not over an item -> either end-of-list or empty list
                if (_sequence.Count > 0)
                {
                    // attach under last item (drop to end)
                    var lastContainer = LB_Seq.ItemContainerGenerator.ContainerFromIndex(_sequence.Count - 1) as ListViewItem;
                    if (lastContainer != null)
                    {
                        container = lastContainer; // keep null-check semantics for currentDropTarget.container
                        isAbove = false; // below last item
                        index = _sequence.Count;
                        adornerTarget = lastContainer;
                    }
                    else
                    {
                        // fall back to attaching to ListView
                        index = _sequence.Count;
                        adornerTarget = LB_Seq;
                        container = null;
                        isAbove = false;
                    }
                }
                else
                {
                    // empty list -> draw at top of ListView
                    index = 0;
                    adornerTarget = LB_Seq;
                    container = null;
                    isAbove = true;
                }
            }

            // Only update adorner if target changed
            if (_currentDropTarget.container != container || _currentDropTarget.isAbove != isAbove)
            {
                RemoveDropAdorner();

                if (adornerTarget != null)
                {
                    _adornerLayer = AdornerLayer.GetAdornerLayer(adornerTarget);
                    if (_adornerLayer != null)
                    {
                        _dropAdorner = new DropInsertionAdorner(adornerTarget, isAbove);
                        _adornerLayer.Add(_dropAdorner);
                    }
                }

                _currentDropTarget = (container, isAbove, index);
            }
        }

        /// <summary>
        /// Drag and Drop finalization. <br/>
        /// Drop event.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event</param>
        private void LB_Seq_Drop(object sender, DragEventArgs e)
        {
            RemoveDropAdorner();

            if (!e.Data.GetDataPresent(typeof(MyTask)))
                return;

            var droppedData = e.Data.GetData(typeof(MyTask)) as MyTask;
            if (droppedData == null)
                return;

            // Calculate intended insertion index (using the last known _currentDropTarget if available)
            int targetIndex;
            if (_currentDropTarget.container == null)
            {
                // fallback: compute from mouse position
                var pt = e.GetPosition(LB_Seq);
                var maybeContainer = ItemsControl.ContainerFromElement(LB_Seq, LB_Seq.InputHitTest(pt) as DependencyObject) as ListViewItem;
                if (maybeContainer == null)
                    targetIndex = _sequence.Count;
                else
                {
                    int idx = LB_Seq.ItemContainerGenerator.IndexFromContainer(maybeContainer);
                    var pIn = e.GetPosition(maybeContainer);
                    targetIndex = pIn.Y < maybeContainer.ActualHeight / 2 ? idx : idx + 1;
                }
            }
            else
            {
                // use previously computed index and above/below
                targetIndex = _currentDropTarget.index;
                if (!_currentDropTarget.isAbove)
                    targetIndex = Math.Min(_sequence.Count, targetIndex + 1);
            }

            int oldIndex = _sequence.IndexOf(droppedData);
            if (oldIndex == -1)
                return;

            // If inserting after the item and the old index is before the target, the final index shifts left by 1
            if (targetIndex > oldIndex) targetIndex--;

            // If targetIndex equals Count -> move to end by remove+add, else use Move
            if (targetIndex < 0) targetIndex = 0;
            if (targetIndex >= _sequence.Count)
            {
                _sequence.RemoveAt(oldIndex);
                _sequence.Add(droppedData);
            }
            else
            {
                if (oldIndex != targetIndex)
                    _sequence.Move(oldIndex, targetIndex);
            }

            // Select moved item
            LB_Seq.SelectedItem = droppedData;

            // mark unsaved and update UI if needed
            _saved = false;
            Update();
        }

        /// <summary>
        /// Drag and Drop leave. <br/>
        /// Preview Drag Leave event.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event</param>
        /// <author>ChatGPT</author>
        private void LB_Seq_PreviewDragLeave(object sender, DragEventArgs e)
        {
            RemoveDropAdorner();
        }

        /// <summary>
        /// Drag and Drop clean up. <br/>
        /// Removes the drop adorner if present.
        /// </summary>
        /// <author>ChatGPT</author>
        private void RemoveDropAdorner()
        {
            if (_dropAdorner != null && _adornerLayer != null)
            {
                _adornerLayer.Remove(_dropAdorner);
                _dropAdorner = null;
            }
            _currentDropTarget = (null, false, -1);
        }
        #endregion
    }
}