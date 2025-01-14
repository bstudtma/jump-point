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

namespace JumpPoint
{
    public partial class MainWindow : Window
    {
        // We'll store all found shortcuts here
        private List<ShortcutItem> _allShortcuts;

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



            // Index all shortcuts in C:\shortcuts
            string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "JumpPoint");
            _allShortcuts = LoadShortcuts(appDataPath);

            // Display all shortcuts initially
            ShortcutsListBox.ItemsSource = _allShortcuts;

            this.Deactivated += MainWindow_Deactivated;
            this.ShowInTaskbar = false;

        }

        private void MainWindow_Deactivated(object sender, EventArgs e)
        {
            this.Hide();
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

            if (char.IsLetterOrDigit((char)KeyInterop.VirtualKeyFromKey(e.Key)))
            {
                SearchTextBox.Focus();
                SearchTextBox.CaretIndex = SearchTextBox.Text.Length;
                e.Handled = false; // Allow the key press to be processed by the TextBox
            }

            if (e.Key == Key.Up)
            {
                if (ShortcutsListBox.SelectedIndex == 0)
                {
                    SearchTextBox.Focus();
                    e.Handled = true;
                }
            }

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

            if (e.Key == Key.F2)
            {
                string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "JumpPoint");
                _allShortcuts = LoadShortcuts(appDataPath);
                FilterShortcuts(SearchTextBox.Text);
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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Unregister the hotkey when the window is closing
            var helper = new WindowInteropHelper(this);
            UnregisterHotKey(helper.Handle, HOTKEY_ID);
        }

        // Show (restore) the main window
        private void ShowWindow()
        {
            Show();
            Activate();
            SearchTextBox.Focus();
            SearchTextBox.CaretIndex = SearchTextBox.Text.Length;
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

            // Handle ALT to prevent the system menu from showing unexpectedly
            const int WM_SYSCOMMAND = 0x0112;
            const int SC_KEYMENU = 0xF100;

            if (msg == WM_SYSCOMMAND && (wParam.ToInt32() & 0xFFF0) == SC_KEYMENU)
            {
                handled = true;
            }

            return IntPtr.Zero;
        }

        // Load shortcuts from a folder
        private List<ShortcutItem> LoadShortcuts(string folderPath)
        {
            var list = new List<ShortcutItem>();

            if (Directory.Exists(folderPath))
            {
                var files = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
                                    .Where(file => file.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".url", StringComparison.OrdinalIgnoreCase))
                                    .ToArray();
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
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
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
                e.Handled = true;
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