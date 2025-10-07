using Microsoft.Win32;
using SequenceClicker.Tasks;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SequenceClicker
{
    /// <summary>
    /// Interaction logic for EditTimedWindow.xaml
    /// </summary>
    public partial class EditTimedWindow : Window
    {
        #region DLL imports
        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int X, int Y);
        
        [DllImport("user32.dll")]
        static extern bool GetCursorPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }
        #endregion

        private ObservableCollection<MyTask> _sequence;
        private TimedTask timedTask;
        private bool _editRunning = false;
        private EditWindow _editWindow;

        private Point _dragStartPoint;
        private MyTask _draggedItem;
        private AdornerLayer _adornerLayer;
        private DropInsertionAdorner _dropAdorner;
        private (ListViewItem container, bool isAbove, int index) _currentDropTarget = (null, false, -1);
        internal EditTimedWindow(TimedTask tt)
        {
            InitializeComponent();
            timedTask = tt;
            _sequence = tt.SubSeq;
            LB_Seq.ItemsSource = _sequence;
            LB_Seq.PreviewDragLeave += LB_Seq_PreviewDragLeave;
            tb_min.Text = Math.Floor(tt.Time / 60).ToString();
            tb_sec.Text = (tt.Time % 60).ToString("0.###");
            Update();
        }

        #region Buttons

        #region Add
        /// <summary>
        /// Adds a Click Task to the sub sequence
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
        }

        /// <summary>
        /// Adds a Delay Task to the sub sequence
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
        }

        /// <summary>
        /// Adds a Move Task to the sub sequence
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
        }
        #endregion

        #region Funtionallity
        /// <summary>
        /// Deletes the selected task from the sub sequence
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
            }
        }

        /// <summary>
        /// Resets the current sub sequence
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
        /// Applies the changes and closes the window
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event</param>
        /// <author>CC-7956</author>
        private void btn_Ok_Click(object sender, RoutedEventArgs e)
        {
            timedTask.Time = (double.Parse(tb_min.Text.Trim()) * 60d + double.Parse(tb_sec.Text.Trim()));
            timedTask.SubSeq = _sequence;
            this.Close();
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
        /// Ckecks if the input for the Timed Task is valid and enables/disables the add button
        /// </summary>
        /// <author>CC-7956</author>
        private bool Check_Timed()
        {
            try
            {
                if ((double.TryParse(tb_min.Text.Trim(), out double d) && d > 0) || (double.TryParse(tb_sec.Text.Trim(), out double q) && q > 0))
                {
                    return true;
                }
                else
                {
                    throw new Exception("False");
                }
            }
            catch
            {
                return false;
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
           
            Check_Move();
            Check_Delay();
            Check_Click();
            if(TimedTask.ValidInput(tb_min.Text, tb_sec.Text) && _sequence.Count != 0)
            {
                btn_Ok.IsEnabled = true;
            }
            else
            {
                btn_Ok.IsEnabled = false;
            }
        }
        #endregion

        #region Key detectors
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
                EditTimedWin.Topmost = false;
                _editWindow.ShowDialog();
                _editRunning = false;
                EditTimedWin.Topmost = true;
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
