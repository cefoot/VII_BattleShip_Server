using DE.Cefoot.BattleShips.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DE.Cefoot.BattleShips.Client
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class ClientWindow : Window
    {
        public NetworkStream ServerStream { get; private set; }

        public ClientWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            ConnectToServer();
        }

        private void ConnectToServer()
        {
            var client = new TcpClient();
            try
            {
                client.Connect(Properties.Settings.Default.ServerAddress, Properties.Settings.Default.ServerPort);
                ServerStream = client.GetStream();
                ServerStream.SendName(Environment.GetEnvironmentVariable("USERNAME"));
                ThreadPool.QueueUserWorkItem(ReceiveData);
                lblServer.Content = Properties.Settings.Default.ServerAddress;
                if (Environment.GetEnvironmentVariables().Contains("ADMIN"))
                {
                    grdBack.RowDefinitions.First(row => row.Name == "AdminRow").Height = GridLength.Auto;
                }
            }
            catch (SocketException)
            {
                MessageBox.Show(String.Format("Server ({0}) nicht erreichbar.\r\nAnwendung wird beendet.", Properties.Settings.Default.ServerAddress), "Verbindungsfehler", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (ServerStream != null)
            {
                try
                {
                    ServerStream.Close();
                }
                catch (Exception)
                {
                }
            }
            ConnectToServer();
        }

        private void ReceiveData(object state)
        {
            if (!this.IsVisible) return;
            if (ServerStream.DataAvailable)
            {
                var data = ServerStream.ReadData();
                switch (data.DataKind)
                {
                    case DataKind.Info:
                        this.Dispatcher.BeginInvoke(new Dummy(() =>
                        {
                            UpdateGameData(data as GameInfo);
                        }));
                        break;
                    case DataKind.Field:
                        HandleFieldData(data as Field);
                        break;
                    case DataKind.Leaderboard:
                        HandleLeaderboard(data as Leaderboard);
                        break;
                    case DataKind.TipAnswer:
                        HandleTipAnswer(data as TipAnswer);
                        break;
                    case DataKind.Error:
                        MessageBox.Show((data as Error).Exc.ToString(), "Fehler");
                        break;
                }
            }
            Thread.Sleep(100);
            ThreadPool.QueueUserWorkItem(ReceiveData);
        }

        private void HandleLeaderboard(Leaderboard leaderboard)
        {
            var current = from ppl in leaderboard.PlayerList
                          where ppl.TipCount > 0
                          orderby ppl.TipCount
                          select String.Format("{0}\t|{1}", ppl.Name, ppl.TipCount);

            var week = from ppl in leaderboard.PlayerList
                       where ppl.Points > 0
                       orderby ppl.Points descending
                       select String.Format("{0}\t|{1}", ppl.Name, ppl.Points);

            this.Dispatcher.BeginInvoke(new Dummy(() =>
            {
                lbCurrent.Items.Clear();
                current.ToList().ForEach(i => lbCurrent.Items.Add(i));
                lbWeek.Items.Clear();
                week.ToList().ForEach(i => lbWeek.Items.Add(i));
            }));

        }

        private void HandleTipAnswer(TipAnswer tipAnswer)
        {
            this.Dispatcher.BeginInvoke(new Dummy(() =>
            {
                UpdateGameData(tipAnswer);
                var box = grdField.Children.Cast<CheckBox>().FirstOrDefault(e => Grid.GetColumn(e) == tipAnswer.OriginalTip.PosX && Grid.GetRow(e) == tipAnswer.OriginalTip.PosY);
                box.IsEnabled = false;
                box.IsChecked = tipAnswer.Hit;
                if (tipAnswer.GameFinished)
                {
                    MessageBox.Show("Spiel beendet", "Ende");
                    grdField.IsEnabled = false;
                }
            }));
        }

        private delegate void Dummy();

        private void UpdateGameData(GameInfo info)
        {
            lblTipsCnt.Content = info.UsedTipCount;
            lblTipsRemain.Content = info.TipCount;
            if (grdField.Children.Count > 0)
                if (info.TipCount == 0 && ((CheckBox)grdField.Children[0]).Cursor == Cursors.Hand)
                {
                    grdField.Children.Cast<CheckBox>().ToList().ForEach(c => c.Cursor = Cursors.No);
                }
                else if (info.TipCount > 0 && ((CheckBox)grdField.Children[0]).Cursor == Cursors.No)
                {
                    grdField.Children.Cast<CheckBox>().ToList().ForEach(c => c.Cursor = Cursors.Hand);
                }

            lblTime.Content = String.Format("{0}h {1}m {2}s", info.TipNewTime.Hours, info.TipNewTime.Minutes, info.TipNewTime.Seconds);
        }

        private void HandleFieldData(Field field)
        {
            this.Dispatcher.BeginInvoke(new Dummy(() =>
            {
                grdField.IsEnabled = true;
                UpdateGameData(field);
                UpdateAdminData(field);
                lblShips.Content = field.ShipCount;
                grdField.Children.Clear();
                grdField.ColumnDefinitions.Clear();
                for (int x = 0; x < field.FieldWidth; x++)
                {
                    grdField.ColumnDefinitions.Add(new ColumnDefinition());
                }
                grdField.RowDefinitions.Clear();
                for (int y = 0; y < field.FieldHeight; y++)
                {
                    grdField.RowDefinitions.Add(new RowDefinition());
                }
                for (int x = 0; x < field.FieldWidth; x++)
                    for (int y = 0; y < field.FieldHeight; y++)
                    {
                        var check = new CheckBox();
                        Grid.SetColumn(check, x);
                        Grid.SetRow(check, y);
                        check.HorizontalAlignment = HorizontalAlignment.Center;
                        check.VerticalAlignment = VerticalAlignment.Center;
                        check.Cursor = Cursors.Hand;
                        check.Checked += Check_Checked;
                        grdField.Children.Add(check);
                    }
                MessageBox.Show("Ein neus Spiel startet.", "Start");
            }));
        }

        private bool adminUpdated = false;

        private void UpdateAdminData(Field field)
        {
            if (adminUpdated) return;
            adminUpdated = true;
            tbHeight.Text = field.FieldHeight.ToString();
            tbWidth.Text = field.FieldWidth.ToString();
            tbShipCnt.Text = field.ShipCount.ToString();
            tbTipCnt.Text = field.TipCount.ToString();
        }

        private void Check_Checked(object sender, RoutedEventArgs e)
        {
            var box = sender as CheckBox;
            if (!box.IsEnabled) return;
            var tip = new Tip
            {
                PosX = Grid.GetColumn(box),
                PosY = Grid.GetRow(box)
            };
            box.IsChecked = false;
            ServerStream.SendData(tip);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var newRound = new Field();
            int num;
            if (!int.TryParse(tbHeight.Text, out num)) return;
            newRound.FieldHeight = num;
            if (!int.TryParse(tbWidth.Text, out num)) return;
            newRound.FieldWidth = num;
            if (!int.TryParse(tbShipCnt.Text, out num)) return;
            newRound.ShipCount = num;
            if (!int.TryParse(tbTipCnt.Text, out num)) return;
            newRound.TipCount = num;
            ServerStream.SendData(newRound);
        }

        private void TB_PreviewText(object sender, TextCompositionEventArgs e)
        {
            var regex = new Regex("[0-9]+"); //regex that matches disallowed text
            e.Handled = !regex.IsMatch(e.Text);
        }

        private void tb_GotFocus(object sender, RoutedEventArgs e)
        {
            var tb = sender as TextBox;
            tb.SelectAll();
        }
    }
}
