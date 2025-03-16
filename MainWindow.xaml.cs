using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Input;

namespace notZvokarna
{
    public partial class MainWindow : Window
    {
        private const int Rows = 24;
        private const int Columns = 32;
        private ToggleButton[,] buttons = new ToggleButton[Rows, Columns];
        private static readonly int[] BlackKeys = { 1, 3, 6, 8, 10 };
        private DispatcherTimer timer;
        private int currentColumn = 0;
        private Border highlightBorder;
        private List<MediaPlayer> activePlayers = new List<MediaPlayer>();
        private bool isMouseDown = false;
        private ToggleButton lastToggledButton = null;

        private static readonly string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        private static readonly string projectRoot = Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\"));
        private static readonly string soundFolder = Path.Combine(projectRoot, "sounds", "piano_keys_wav");


        public MainWindow()
        {
            InitializeComponent();
            GeneratePianoRoll();
            InitializeTimer();
        }

        private void GeneratePianoRoll()
        {
            PianoRollGrid.RowDefinitions.Clear();
            PianoRollGrid.ColumnDefinitions.Clear();

            for (int i = 0; i < Rows; i++)
            {
                PianoRollGrid.RowDefinitions.Add(new RowDefinition());
            }
            for (int j = 0; j < Columns; j++)
            {
                PianoRollGrid.ColumnDefinitions.Add(new ColumnDefinition());
            }

            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Columns; j++)
                {
                    bool isBlackKey = BlackKeys.Contains(i % 12);
                    ToggleButton btn = new ToggleButton
                    {
                        Background = isBlackKey ? Brushes.Black : Brushes.White,
                        BorderBrush = Brushes.Gray,
                        BorderThickness = new Thickness(1),
                        Margin = new Thickness(0),
                        FocusVisualStyle = null
                    };

                    int noteNumber = i;

                    btn.Checked += (s, e) =>
                    {
                        btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2c63ad"));
                        play_one_note(noteNumber);
                    };
                    btn.Unchecked += (s, e) =>
                    {
                        btn.Background = isBlackKey ? Brushes.Black : Brushes.White;
                    };

                    btn.PreviewMouseDown += Button_MouseDown;
                    btn.PreviewMouseMove += Button_MouseMove;
                    btn.PreviewMouseUp += Button_MouseUp;

           
                    //Grid.SetRow(btn, i);

                    int flippedRow = Rows - 1 - i; // Obrnjen kalavid (nizke note levo (spodaj) 
                    Grid.SetRow(btn, flippedRow);
                    Grid.SetColumn(btn, j);
                    PianoRollGrid.Children.Add(btn);
                    buttons[i, j] = btn;
                }
            }

            highlightBorder = new Border
            {
                Background = new SolidColorBrush(Colors.Blue) { Opacity = 0.2 },
                IsHitTestVisible = false
            };
            PianoRollGrid.Children.Add(highlightBorder);
            Grid.SetRowSpan(highlightBorder, Rows);
            Grid.SetColumn(highlightBorder, 0);
        }
        private void Button_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.RightButton == MouseButtonState.Pressed)
            {
                isMouseDown = true;
                ToggleButton btn = sender as ToggleButton;
                if (btn != null)
                {
                    btn.IsChecked = !btn.IsChecked;
                    lastToggledButton = btn;
                }
                e.Handled = true; 
            }
        }

        private void Button_MouseMove(object sender, MouseEventArgs e)
        {
            //ce hocemo namesto desnega gumba uporabiti mod. key 
            //if (e.LeftButton == MouseButtonState.Pressed && Keyboard.Modifiers == ModifierKeys.Shift)

            if (e.RightButton == MouseButtonState.Pressed && isMouseDown)
            {
                ToggleButton btn = sender as ToggleButton;
                if (btn != null && btn != lastToggledButton)
                {
                    btn.IsChecked = lastToggledButton?.IsChecked ?? false;
                    lastToggledButton = btn;
                }
            }
        }

        private void Button_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.RightButton == MouseButtonState.Released)
            {
                isMouseDown = false;
                lastToggledButton = null;
            }
        }

        private void InitializeTimer()
        {
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(250);
            timer.Tick += Timer_Tick;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            currentColumn = (currentColumn + 1) % Columns;
            Grid.SetColumn(highlightBorder, currentColumn);

            for (int note = 0; note < Rows; note++)
            {
                if (buttons[note, currentColumn].IsChecked == true)
                {
                    play_one_note(note);
                }
            }
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            if (timer.IsEnabled)
            {
                timer.Stop();
                PlayButton.Content = "Play";
            }
            else
            {
                currentColumn = 0;
                Grid.SetColumn(highlightBorder, currentColumn);
                timer.Start();
                PlayButton.Content = "Stop";
            }
        }

        private void play_one_note(int note)
        {
            string fileName = $"key{(note + 1).ToString("D2")}.wav";
            string filePath = Path.Combine(soundFolder, fileName);

            Console.WriteLine($"Trying to load: {filePath}");

            if (!File.Exists(filePath))
            {
                MessageBox.Show($"Sound file not found:\n{filePath}");
                return;
            }

            try
            {
                var mediaPlayer = new MediaPlayer();
                activePlayers.Add(mediaPlayer);

                mediaPlayer.Open(new Uri(filePath));
                mediaPlayer.Play();
                mediaPlayer.MediaEnded += (s, e) =>
                {
                    mediaPlayer.Close();
                    activePlayers.Remove(mediaPlayer);
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error playing {fileName}:\n{ex.Message}");
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Columns; j++)
                {
                    ToggleButton btn = buttons[i, j];
                    btn.IsChecked = false;

                    bool isBlackKey = BlackKeys.Contains(i % 12);
                    btn.Background = isBlackKey ? Brushes.Black : Brushes.White;
                }
            }
        }
    }
}