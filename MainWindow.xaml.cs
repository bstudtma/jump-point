using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Interop;
using WinForms = System.Windows.Forms; // Alias Windows Forms to avoid conflicts

namespace JumpPoint
{
    public partial class MainWindow : Window
    {
        // We'll store all found shortcuts here
        private List<ShortcutItem> _allShortcuts;

        // System Tray icon
        private WinForms.NotifyIcon _notifyIcon;

        // Hotkey (ALT + SPACE) constants
        private const int MOD_ALT = 0x1;
        private const int VK_SPACE = 0x20;
        private const int WM_HOTKEY = 0x0312;
        private const int HOTKEY_ID = 9000;  // Arbitrary ID, just be unique

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public MainWindow()
        {
            InitializeComponent();


            

            // Initialize tray icon
            _notifyIcon = new WinForms.NotifyIcon
            {
                Icon = System.Drawing.SystemIcons.Application,
                Visible = false,
                Text = "JumpPoint"
            };

            // Create a context menu for the tray icon
            var contextMenu = new WinForms.ContextMenuStrip();
            contextMenu.Items.Add("Open JumpPoint", null, (s, e) => ShowWindow());
            contextMenu.Items.Add("Exit", null, OnExitClick);

            _notifyIcon.ContextMenuStrip = contextMenu;

            // Double-clicking tray icon also opens the window
            _notifyIcon.DoubleClick += (s, e) => ShowWindow();

            // Index all shortcuts in C:\shortcuts
            string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "JumpPoint");
            _allShortcuts = LoadShortcuts(appDataPath);

            // Display all shortcuts initially
            ShortcutsListBox.ItemsSource = _allShortcuts;

        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            SearchTextBox.Focus();
        }

        private void ShortcutsListBox_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {

            var selectedItem = ShortcutsListBox.SelectedItem as ShortcutItem;
            if (selectedItem != null)
            {
                SearchTextBox.Text = selectedItem.DisplayName;
            }
        }

        private void ShortcutsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // var selectedItem = ShortcutsListBox.SelectedItem as ShortcutItem;
            // if (selectedItem != null)
            // {
            // SearchTextBox.Text = selectedItem.DisplayName;
            // SearchTextBox.CaretIndex = SearchTextBox.Text.Length;
            // }
        }

        private void ShortcutsListBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var selectedItem = ShortcutsListBox.SelectedItem as ShortcutItem;
                if (selectedItem != null)
                {
                    SearchTextBox.Text = selectedItem.DisplayName;
                    SearchTextBox.CaretIndex = SearchTextBox.Text.Length;
                    SearchTextBox.Focus();
                    LaunchSelectedShortcut();
                    e.Handled = true;
                }
            }

            if (e.Key == Key.Escape)
            {
                SearchTextBox.Focus();
                //Hide();
                e.Handled = true;
            }

            if (e.Key == Key.Back)
            {
                SearchTextBox.Focus();
                SearchTextBox.CaretIndex = SearchTextBox.Text.Length;
                e.Handled = false; // Allow the backspace to be processed by the TextBox
            }

            if (e.Key == Key.Up)
            {
                if (ShortcutsListBox.SelectedIndex == 0)
                {
                    SearchTextBox.Focus();
                    e.Handled = true;
                }
            }


            // else if (e.Key == Key.Up)
            // {
            // int selectedIndex = ShortcutsListBox.SelectedIndex;
            // if (selectedIndex > 0)
            // {
            //     ShortcutsListBox.SelectedIndex = selectedIndex - 1;
            //     e.Handled = true;
            // }
            // }
            // else if (e.Key == Key.Down)
            // {
            // int selectedIndex = ShortcutsListBox.SelectedIndex;
            // if (selectedIndex < ShortcutsListBox.Items.Count - 1)
            // {
            //     ShortcutsListBox.SelectedIndex = selectedIndex + 1;
            //     e.Handled = true;
            // }
            // }
            if (e.Key == Key.Tab)
            {
                var selectedItem = ShortcutsListBox.SelectedItem as ShortcutItem;
                if (selectedItem != null)
                {
                    SearchTextBox.Text = selectedItem.DisplayName + " > ";
                    SearchTextBox.CaretIndex = SearchTextBox.Text.Length;
                    SearchTextBox.Focus();
                    e.Handled = true;
                }
            }
        }

        private void SearchTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Tab || e.Key == Key.Enter)
            {
                var textBox = sender as TextBox;
                if (textBox != null && !textBox.Text.Contains(" > "))
                {
                    if (ShortcutsListBox.Items.Count > 0)
                    {
                        var topItem = ShortcutsListBox.Items[0] as ShortcutItem;
                        if (topItem != null)
                        {
                            SearchTextBox.Text = topItem.DisplayName;
                            SearchTextBox.CaretIndex = SearchTextBox.Text.Length;
                            ShortcutsListBox.SelectedItem = topItem;
                            e.Handled = true;
                        }
                    }
                }
            }

            if (e.Key == Key.F1)
            {
                string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "JumpPoint");
                if (!Directory.Exists(appDataPath))
                {
                    Directory.CreateDirectory(appDataPath);
                }
                Process.Start(new ProcessStartInfo
                {
                    FileName = appDataPath,
                    UseShellExecute = true,
                    Verb = "open"
                });
                e.Handled = true;
            }

            if (e.Key == Key.Escape)
            {
                Hide();
                e.Handled = true;
            }

            if (e.Key == Key.Tab)
            {
                var textBox = sender as TextBox;
                if (textBox != null)
                {
                    int caretIndex = textBox.CaretIndex;
                    textBox.Text = textBox.Text.Insert(caretIndex, " > ");
                    textBox.CaretIndex = caretIndex + 3;
                    e.Handled = true;
                }
            }

            if (e.Key == Key.Enter)
            {
                LaunchSelectedShortcut();
            }

            if (e.Key == Key.Down)
            {
                ShortcutsListBox.Focus();

                if (ShortcutsListBox.Items.Count > 0)
                {
                    ShortcutsListBox.SelectedIndex = 0;
                    // ShortcutsListBox.ScrollIntoView(ShortcutsListBox.Items[0]);
                    // // f((ListBoxItem)ShortcutsListBox.SelectedItem).Focus();
                    // var container = ShortcutsListBox.ItemContainerGenerator.ContainerFromItem(ShortcutsListBox.Items[0]) as ListViewItem;
                    
                    // if (container != null)
                    // {
                    //     container.Focus();
                    //     container.IsSelected=true;
                    //     Keyboard.Focus(container);
                    //     //e.Handled = true;
                    // }                    
                    e.Handled = true;

                    FocusListBoxItem(ShortcutsListBox, ShortcutsListBox.SelectedIndex);
                }
            }

            if (e.Key == Key.Back)
            {
                var textBox = sender as TextBox;
                if (textBox != null && textBox.Text.EndsWith(" > "))
                {
                    textBox.Text = textBox.Text.Substring(0, textBox.Text.Length - 3);
                    textBox.CaretIndex = textBox.Text.Length;
                    e.Handled = true;
                }
            }
        }
        public static void FocusListBoxItem(ListBox listBox, int index)
        {
            if (index < 0 || index >= listBox.Items.Count)
                return;

            // Scroll the item into view to ensure it is realized
            listBox.ScrollIntoView(listBox.Items[index]);

            // Use a dispatcher to delay the focus logic until the UI updates
            listBox.Dispatcher.BeginInvoke(new Action(() =>
            {
                var item = listBox.Items[index];
                var container = listBox.ItemContainerGenerator.ContainerFromItem(item) as ListBoxItem;

                if (container != null)
                {
                    // Bring the item into view again (just in case)
                    container.BringIntoView();

                    // Focus the item
                    container.Focus();

                    // Optionally select it
                    container.IsSelected = true;

                    // Ensure the keyboard focus is set to the ListBoxItem
                    Keyboard.Focus(container);
                }
            }));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Once the window is fully loaded, register the global hotkey
            var helper = new WindowInteropHelper(this);
            RegisterHotKey(helper.Handle, HOTKEY_ID, MOD_ALT, VK_SPACE);

            // Attach WndProc hook
            HwndSource source = HwndSource.FromHwnd(helper.Handle);
            if (source != null)
            {
                source.AddHook(WndProc);
            }
        }

        private void ShortcutsListBox_GotFocus(object sender, RoutedEventArgs e)
        {
            
            // var container = ShortcutsListBox.ItemContainerGenerator.ContainerFromItem(ShortcutsListBox.Items[0]) as ListViewItem;
            // if (container != null)
            // {
            //     container.Focus();
            //     container.IsSelected=true;
            //     Keyboard.Focus(container);
            // }
        }



        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Hide to tray instead of truly closing
            //e.Cancel = true;
            //Hide();

            //_notifyIcon.Visible = true;

            // Unregister hotkeys
            //UnregisterHotKey(new WindowInteropHelper(this).Handle, HOTKEY_ID);

            // Dispose of the tray icon
            //_notifyIcon.Dispose();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            // Dispose the tray icon when the window is truly closed
            _notifyIcon.Dispose();

            // Unregister the hotkey
            var helper = new WindowInteropHelper(this);
            UnregisterHotKey(helper.Handle, HOTKEY_ID);
        }

        // Show (restore) the main window
        private void ShowWindow()
        {
            Show();
            Activate();
            SearchTextBox.Focus();
        }

        // "Exit" context menu item click handler
        private void OnExitClick(object sender, EventArgs e)
        {
            // Close the application and clean up
            _notifyIcon.Dispose();
            Application.Current.Shutdown();
        }

        // Capture the HOTKEY message
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY)
            {
                int hotkeyId = wParam.ToInt32();
                if (hotkeyId == HOTKEY_ID)
                {
                    // Toggle window visibility
                    if (this.Visibility == Visibility.Visible)
                    {
                        Hide();
                    }
                    else
                    {
                        ShowWindow();
                    }
                    handled = true;
                }
            }

            const int WM_SYSCOMMAND = 0x0112;
            const int SC_KEYMENU = 0xF100;

            if (msg == WM_SYSCOMMAND && (wParam.ToInt32() & 0xFFF0) == SC_KEYMENU)
            {
                handled = true;
            }

            return IntPtr.Zero;
        }

        // Load shortcuts from a folder (only .lnk files)
        private List<ShortcutItem> LoadShortcuts(string folderPath)
        {
            var list = new List<ShortcutItem>();

            if (Directory.Exists(folderPath))
            {
                var files = Directory.GetFiles(folderPath, "*.lnk", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    list.Add(new ShortcutItem
                    {
                        DisplayName = Path.GetFileNameWithoutExtension(file),
                        FullPath = file,
                        Arguments = ""
                    });
                }

                // Load .ini files
                var iniFiles = Directory.GetFiles(folderPath, "*.ini", SearchOption.AllDirectories);
                foreach (var iniFile in iniFiles)
                {
                    var lines = File.ReadAllLines(iniFile);
                    string displayName = Path.GetFileNameWithoutExtension(iniFile);
                    string fullPath = null;
                    string arguments = "";

                    foreach (var line in lines)
                    {
                        if (line.StartsWith("name=", StringComparison.OrdinalIgnoreCase))
                        {
                            displayName = line.Substring("name=".Length).Trim();
                        }
                        else if (line.StartsWith("path=", StringComparison.OrdinalIgnoreCase))
                        {
                            fullPath = line.Substring("path=".Length).Trim();
                        }
                        else if (line.StartsWith("arguments=", StringComparison.OrdinalIgnoreCase))
                        {
                            arguments = line.Substring("arguments=".Length).Trim();
                        }
                    }

                    if (!string.IsNullOrEmpty(displayName) && !string.IsNullOrEmpty(fullPath))
                    {
                        list.Add(new ShortcutItem
                        {
                            DisplayName = displayName,
                            FullPath = fullPath,
                            Arguments = arguments
                        });
                    }
                }
            }



            return list.OrderBy(s => s.DisplayName).ToList();
        }

        // Filter the list based on user input
        private void SearchTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            var searchText = SearchTextBox.Text.Split(new[] { " > " }, StringSplitOptions.None).FirstOrDefault();
            FilterShortcuts(searchText);
        }

        private void FilterShortcuts(string search)
        {
            if (string.IsNullOrWhiteSpace(search))
            {
                ShortcutsListBox.ItemsSource = _allShortcuts;
            }
            else
            {
                var searchWords = search.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var filtered = _allShortcuts
                    .Where(s => searchWords.All(word => s.DisplayName.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0))
                    .ToList();

                ShortcutsListBox.ItemsSource = filtered;
            }
        }

        // Handle Enter in the TextBox
        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                LaunchSelectedShortcut();
            }
        }

        // Handle double-click on a shortcut
        private void ShortcutsListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            LaunchSelectedShortcut();
        }

        // Launch the highlighted or first item if none highlighted
        private void LaunchSelectedShortcut()
        {
            var selectedItem = ShortcutsListBox.SelectedItem as ShortcutItem;
            var searchTextSplit = SearchTextBox.Text.Split(new[] { " > " }, StringSplitOptions.None);
            var searchParam = searchTextSplit.Length > 1 ? searchTextSplit[1] : string.Empty;

            if (selectedItem == null && ShortcutsListBox.Items.Count > 0)
            {
                selectedItem = ShortcutsListBox.Items[0] as ShortcutItem;
            }

            if (selectedItem != null)
            {
                try
                {
                    var path = selectedItem.FullPath;
                    if (path.Contains("$$"))
                    {
                        path = path.Replace("$$", searchParam);
                    }
                    var arguments = selectedItem.Arguments;
                    if (arguments.Contains("$$"))
                    {
                        arguments = arguments.Replace("$$", searchParam);
                    }

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = path,
                        Arguments = arguments,
                        UseShellExecute = true,
                        WorkingDirectory = System.IO.Path.GetDirectoryName(path)
                    });

                    //Process.Start(path, arguments);

                    // Hide the window after launching
                    Hide();
                    SearchTextBox.Clear();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error launching shortcut:\n" + ex.Message);
                }
            }
        }
    }
}
