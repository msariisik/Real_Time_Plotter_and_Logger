using Microsoft.Research.DynamicDataDisplay;
using Microsoft.Research.DynamicDataDisplay.Common;
using Microsoft.Research.DynamicDataDisplay.DataSources;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;

namespace Real_Time_Plotter_and_Logger
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        SerialPort serial = new SerialPort();

        string data_string = Convert.ToString(0);
        List<string> _parsed = new List<string>();

        double data;

        bool plotflag = false;

        string mydoctimeforpath;

        public VoltagePointCollection voltagePointCollection;
        public VoltagePointCollection2 dataSeriesCollection;
        DispatcherTimer updateCollectionTimer;
        DispatcherTimer updateCollectionTimer2;
        private int i = 0;
        private int i2 = 0;

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;

            Comm_Port_Names.Items.Clear();
            string[] ports = SerialPort.GetPortNames();
            foreach (string port in ports)
            {
                Comm_Port_Names.Items.Add(port);
            }
            Connect_btn.Content = "Connect";

            updateCollectionTimer = new DispatcherTimer();
            updateCollectionTimer.Interval = TimeSpan.FromMilliseconds(0.0001);
            updateCollectionTimer.Tick += new EventHandler(updateCollectionTimer_Tick);

            updateCollectionTimer2 = new DispatcherTimer();
            updateCollectionTimer2.Interval = TimeSpan.FromMilliseconds(1);
            updateCollectionTimer2.Tick += new EventHandler(updateCollectionTimer2_Tick);
            voltagePointCollection = new VoltagePointCollection();
            var ds1 = new EnumerableDataSource<VoltagePoint>(voltagePointCollection);
            ds1.SetXMapping(x => x.time);
            ds1.SetYMapping(y => y.Voltage);

            plotter.AddLineGraph(ds1, Colors.Green, 2, "Raw  Signal");

        }

        private void Connect_Comms(object sender, RoutedEventArgs e)
        {
            if (Connect_btn.Content.ToString() == "Connect")
            {
                //Sets up serial port
                try
                {
                    serial = new SerialPort(Convert.ToString(Comm_Port_Names.SelectedItem), 115200, Parity.None, 8, StopBits.One);
                    serial.DataReceived += new SerialDataReceivedEventHandler(Receive);
                    serial.ReadTimeout = 0;
                    serial.RtsEnable = true;
                    serial.Open();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                //Sets button State and Creates function call on data recieved
                if (serial.IsOpen)
                {
                    Connect_btn.Content = "Disconnect";
                    serial.DataReceived += new SerialDataReceivedEventHandler(Receive);
                    updateCollectionTimer.Interval = new TimeSpan(Convert.ToUInt32(textBoxTimer.Text));
                    updateCollectionTimer.Start();
                    plotflag = true;
                }
            }
            else if (Connect_btn.Content.ToString() == "Disconnect")
            {
                try // just in case serial port is not open could also be acheved using if(serial.IsOpen)
                {
                    serial.Close();
                    Connect_btn.Content = "Connect";
                    updateCollectionTimer.Stop();
                }
                catch
                {
                }
            }
            else
            {
                try // just in case serial port is not open could also be acheved using if(serial.IsOpen)
                {
                    serial.Close();
                    Connect_btn.Content = "Connect";
                }
                catch
                {
                }
            }
        }

        #region Receiving

        private delegate void UpdateUiTextDelegate(string text);
        private void Receive(object sender, SerialDataReceivedEventArgs e)
        {
            string received_string = serial.ReadLine();
            received_string = received_string.Remove(received_string.Length - 1);
            data_string = received_string;
            //data = double.Parse(data_string, System.Globalization.CultureInfo.InvariantCulture);

            Dispatcher.Invoke(DispatcherPriority.Send, new UpdateUiTextDelegate(LineReceived), data_string);
        }

        private void LineReceived(string line)
        {
            data_textbox.Text = line;
            data = double.Parse(line, System.Globalization.CultureInfo.InvariantCulture);
            voltagePointCollection.Add(new VoltagePoint(data, i));
            i++;
        }


        //For richtext box
        void updateCollectionTimer_Tick(object sender, EventArgs e)
        {
            //if (plotflag == true)
            //{
            //    voltagePointCollection.Add(new VoltagePoint(data, i));
            //    i++;
            //}
            //data_textbox.Text = data_string;
            //data.ScrollToEnd();
            //data.AppendText(data_string + '\r');
        }


        //private void Clear_btn_Click(object sender, RoutedEventArgs e)
        //{
        //    //data.Document.Blocks.Clear();
        //    data_string = "";
        //}

        private void Record_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                switch (Record_btn.Content.ToString())
                {
                    case "Record":
                        recordTimer.Text = "0";
                        updateCollectionTimer2.Interval = new TimeSpan(Convert.ToInt32(textBoxTimer.Text));
                        updateCollectionTimer2.Start();
                        i2 = i;
                        Record_btn.Content = "Stop";
                        mydoctimeforpath = Environment.CurrentDirectory + string.Format(@"\Logs\signal-log-{0:yyyy-MM-dd_HH-mm-ss}.txt", DateTime.Now);
                        Start_Logging();
                        break;
                    case "Stop":
                        using (StreamWriter outputFile = File.AppendText(mydoctimeforpath))
                        {
                            outputFile.Write("]");
                            outputFile.WriteLine("");
                        }
                        updateCollectionTimer2.Stop();
                        recordTimer.Text = "0";
                        Record_btn.Content = "Record";
                        break;
                    default:
                        updateCollectionTimer2.Stop();
                        break;
                }
            }
            catch (Exception ex)
            {
                updateCollectionTimer2.Stop();
                MessageBox.Show("Error in starting timer: " + ex.Message);
            }
        }
        private void Show_btn_Click(object sender, RoutedEventArgs e)
        {
            int line_count = 0;
            Microsoft.Win32.OpenFileDialog text_parsing_dialog = new Microsoft.Win32.OpenFileDialog();
            text_parsing_dialog.DefaultExt = ".txt";
            text_parsing_dialog.Filter = "TXT files|*.txt";

            var result = text_parsing_dialog.ShowDialog();

            if (result == true)
            {
                browse_path.Text = text_parsing_dialog.FileName;
                using (StreamReader file = new StreamReader(text_parsing_dialog.FileName))
                {
                    string line;
                    while ((line = file.ReadLine()) != null)
                    {
                        _parsed.Add(line);
                        line_count++;
                    }
                }
                plotter_saved.Children.RemoveAll(typeof(LineGraph));
                dataSeriesCollection = new VoltagePointCollection2();
                var dataSeries = new EnumerableDataSource<VoltagePoint2>(dataSeriesCollection);
                int j = 0;
                for (j = 0; j < line_count - 2; j++)
                {
                    double d__parsed;
                    double.TryParse(_parsed[j + 1], out d__parsed);
                    if (d__parsed != 0)
                        dataSeriesCollection.Add(new VoltagePoint2(d__parsed, j));
                }
                dataSeries.SetXMapping(x => x.time);
                dataSeries.SetYMapping(y => y.Voltage);
                line_count = 0;
                plotter_saved.AddLineGraph(dataSeries, Colors.Blue, 2, "Raw  Signal Recorded");
            }
        }
        private void Sample_btn_Click(object sender, RoutedEventArgs e)
        {

        }
        private void Start_Logging()
        {
            using (StreamWriter outputFile = File.CreateText(mydoctimeforpath))
            {
                outputFile.Write("XYZ = [");
                outputFile.WriteLine("");
            }
        }

        private void Write_Log()
        {
            using (StreamWriter outputFile = File.AppendText(mydoctimeforpath))
            {
                outputFile.Write(data_string);
                outputFile.WriteLine("");
            }
        }
        private void updateCollectionTimer2_Tick(object sender, EventArgs e)
        {
            Write_Log();
            recordTimer.Text = Convert.ToString(i - i2);
        }



        #region INotifyPropertyChanged members
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                this.PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
        #endregion

        public class VoltagePointCollection : RingArray<VoltagePoint>
        {
            private const int TOTAL_POINTS = 100;

            public VoltagePointCollection()
                : base(TOTAL_POINTS) // here i set how much values to show 
            {
            }
        }

        public class VoltagePoint
        {
            public DateTime Date { get; set; }
            public double time { get; set; }

            public double Voltage { get; set; }

            public VoltagePoint(double voltage, double time)
            {
                this.Voltage = voltage;
                this.time = time;
            }
        }

        public class VoltagePointCollection2 : RingArray<VoltagePoint2>
        {
            private const int TOTAL_POINTS = 100000000;

            public VoltagePointCollection2()
                : base(TOTAL_POINTS) // here i set how much values to show 
            {
            }
        }

        public class VoltagePoint2
        {
            public DateTime Date { get; set; }
            public double time { get; set; }

            public double Voltage { get; set; }

            public VoltagePoint2(double voltage, double time)
            {
                this.Voltage = voltage;
                this.time = time;
            }
        }

        #region Window Controls

        private void Window_Closed(object sender, EventArgs e)
        {
            if (serial.IsOpen) serial.Close();
        }

        private void data_textbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            data_textbox.Text = data_string;
        }
    }
}
        #endregion
#endregion