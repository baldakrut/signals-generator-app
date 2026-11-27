using Microsoft.Win32;
using SignalProcessing.Core;
using ScottPlot.Plottables;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static SkiaSharp.HarfBuzz.SKShaper;

namespace SignalGenerator.App
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public SingleChannelSignal sig;
        public List<SingleChannelSignal> signalList = new List<SingleChannelSignal>();
        public List<OutOfRangeEventArgs> markup = new List<OutOfRangeEventArgs>();
        public List<VerticalLine> markerList = new List<VerticalLine>();
        public float markupFreq;
        public MainWindow()
        {
            InitializeComponent();

            SignalTypeComboBox.ItemsSource = Enum.GetValues(typeof(SignalType));
            SignalTypeComboBox.SelectedIndex = 0;
        }
        public void addSignal(SingleChannelSignal obj)
        {

            signalList.Add(obj);
            WpfPlot1.Plot.Add.ScatterLine(obj.TimeTick, obj.Data);
            WpfPlot1.Plot.Axes.AutoScale();
            WpfPlot1.Refresh();
        }
        public void addMarkers(OutOfRangeEventArgs obj, float frequency)
        {
            VerticalLine start = WpfPlot1.Plot.Add.VerticalLine(obj.Start / frequency);
            VerticalLine end = WpfPlot1.Plot.Add.VerticalLine(obj.End / frequency);
            start.Color = ScottPlot.Colors.Red;
            end.Color = ScottPlot.Colors.Red;
            markerList.Add(start);
            markerList.Add(end);
        }
        public void clearMarkers()
        {
            foreach (VerticalLine marker in markerList)
            {
                WpfPlot1.Plot.Remove(marker);
            }
            markerList.Clear();
            WpfPlot1.Refresh();
        }
        public void Button_Clear(object sender, RoutedEventArgs e)
        {
            signalList.Clear();
            markup.Clear();
            markerList.Clear();
            WpfPlot1.Plot.Clear();
            WpfPlot1.Refresh();
        }
        public void Button_Write(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "csv-файл (*.csv)|*.csv";
            saveFileDialog.FileName = "MySignal";
            saveFileDialog.DefaultExt = "csv";
            if (saveFileDialog.ShowDialog() == true && signalList.Count() != 0)
            {
                string filePath = saveFileDialog.FileName;
                SingleChannelSignalWriter.Write(signalList.Last(), filePath);
            }


        }
        public void Button_Load(object sender, RoutedEventArgs e)
        {
            OpenFileDialog myDialog = new OpenFileDialog();
            myDialog.Filter = "csv-файл(*.csv)|*.csv";
            myDialog.CheckFileExists = true;
            myDialog.Multiselect = false;
            if (myDialog.ShowDialog() == true)
            {
                string filePath = myDialog.FileName;
                addSignal(SingleChannelSignalReader.Read(filePath));
                
            }
            

        }
        public void Button_Click_Remove(object sender, RoutedEventArgs e)
        {
            if (signalList.Count == 0)
            {
                MessageBox.Show("Нет сигналов для удаления");
            }
            else
            {
                clearMarkers();
                WpfPlot1.Plot.Remove(WpfPlot1.Plot.PlottableList.Last());
                signalList.Remove(signalList.Last());
                WpfPlot1.Refresh();
            }
        }
        public void Button_Click_Summ(object sender, RoutedEventArgs e)
        {
            try
            {
                SingleChannelSignal sig_summ = SignalOperations.Sum(signalList);
                addSignal(sig_summ);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка суммирования", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        public void Button_Click_Concat(object sender, RoutedEventArgs e)
        {
            try
            {
                SingleChannelSignal sig_con = SignalOperations.Concat(signalList);
                addSignal(sig_con);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка склеивания", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        public void Button_Click_Offline(object sender, RoutedEventArgs e)
        {
            if (signalList.Count == 0)
            {
                MessageBox.Show("Нет сигналов");
                return;
            }
            float window_size = (float.Parse(Window_textBox.Text, CultureInfo.InvariantCulture));
            int intWindowSize = (int)(window_size * signalList.Last().Frequency + 0.5);
            if (intWindowSize % 2 == 0)
            {
                intWindowSize++;
            }
            float[] result = OfflineFilter.Filter(signalList.Last().Data, intWindowSize);
            SingleChannelSignal newData = new SingleChannelSignal(result, signalList.Last().Frequency);
            addSignal(newData);
        }

        public void Button_Click_Online(object sender, RoutedEventArgs e)
        {
            if (signalList.Count == 0)
            {
                MessageBox.Show("Нет сигналов");
                return;
            }
            float window_size = (float.Parse(Window_textBox.Text, CultureInfo.InvariantCulture));
            int intWindowSize = (int)(window_size * signalList.Last().Frequency + 0.5);
            float[] data = signalList.Last().Data;
            int chunkSize = 7;
            OnlineFilter filter = new OnlineFilter(intWindowSize);
            List<float> result = new List<float>();

            // Подаём сигнал на фильтр фрагментами фиксированного размера
            for (int i = 0; i < data.Length; i += chunkSize)
            {
                float[] chunk = data.Skip(i).Take(chunkSize).ToArray();
                result.AddRange(filter.Filter(chunk));
            }

            SingleChannelSignal newData = new SingleChannelSignal(result.ToArray(), signalList.Last().Frequency);
            addSignal(newData);
        }
        public void Button_Click_Analyze(object sender, RoutedEventArgs e)
        {
            if (signalList.Count == 0)
            {
                MessageBox.Show("Нет сигналов");
                return;
            }
            float min = (float.Parse(min_textBox.Text, CultureInfo.InvariantCulture));
            float max = (float.Parse(max_textBox.Text, CultureInfo.InvariantCulture));
            float duration = (float.Parse(duration_textBox.Text, CultureInfo.InvariantCulture));
            SingleChannelSignal sig = signalList.Last();
            int minLength = (int)(duration * sig.Frequency + 0.5);

            clearMarkers();
            markup.Clear();
            markupFreq = sig.Frequency;

            SingleChannelRangeAnalyzer analyzer = new SingleChannelRangeAnalyzer(min, max, minLength);
            analyzer.OutOfRange += Analyzer_OutOfRange;
            analyzer.Analyze(sig.Data);
            WpfPlot1.Refresh();
        }
        public void Analyzer_OutOfRange(object sender, OutOfRangeEventArgs e)
        {
            markup.Add(e);
            addMarkers(e, markupFreq);
        }
        public void Button_Write_Markup(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "csv-файл (*.csv)|*.csv";
            saveFileDialog.FileName = "MyMarkup";
            saveFileDialog.DefaultExt = "csv";
            if (saveFileDialog.ShowDialog() == true && markup.Count() != 0)
            {
                string filePath = saveFileDialog.FileName;
                MarkupWriter.Write(markupFreq, markup, filePath);
            }
        }
        public void Button_Load_Markup(object sender, RoutedEventArgs e)
        {
            OpenFileDialog myDialog = new OpenFileDialog();
            myDialog.Filter = "csv-файл(*.csv)|*.csv";
            myDialog.CheckFileExists = true;
            myDialog.Multiselect = false;
            if (myDialog.ShowDialog() == true)
            {
                string filePath = myDialog.FileName;
                clearMarkers();
                markup = MarkupLoader.Load(filePath, out markupFreq);
                foreach (OutOfRangeEventArgs element in markup)
                {
                    addMarkers(element, markupFreq);
                }
                WpfPlot1.Refresh();
            }
        }
        public void Button_Click_CutUp(object sender, RoutedEventArgs e)
        {
            if (signalList.Count == 0)
            {
                MessageBox.Show("Нет сигналов");
                return;
            }
            float upCut = (float.Parse(upCut_textBox.Text, CultureInfo.InvariantCulture));
            SingleChannelSignal sig = signalList.Last();
            WpfPlot1.Plot.Clear();
            WpfPlot1.Refresh();
            ThresholdCutter thre = new ThresholdCutter();
            thre.SetSignal(sig);
            thre.CutUp(upCut);
            addSignal(sig);




        }
        public void Button_Click_CutDown(object sender, RoutedEventArgs e)
        {
            if (signalList.Count == 0)
            {
                MessageBox.Show("Нет сигналов");
                return;
            }
            float downCut = (float.Parse(downCut_textBox.Text, CultureInfo.InvariantCulture));
            SingleChannelSignal sig = signalList.Last();
            WpfPlot1.Plot.Clear();
            WpfPlot1.Refresh();
            ThresholdCutter thre = new ThresholdCutter();
            thre.SetSignal(sig);
            thre.CutDown(downCut);
            addSignal(sig);
        }
        public void Button_Click_CutUpAndDown(object sender, RoutedEventArgs e)
        {
            if (signalList.Count == 0)
            {
                MessageBox.Show("Нет сигналов");
                return;
            }
            float upCut = (float.Parse(upCut_textBox.Text, CultureInfo.InvariantCulture));
            float downCut = (float.Parse(downCut_textBox.Text, CultureInfo.InvariantCulture));
            SingleChannelSignal sig = signalList.Last();
            WpfPlot1.Plot.Clear();
            WpfPlot1.Refresh();
            ThresholdCutter thre = new ThresholdCutter();
            thre.SetSignal(sig);
            thre.CutUp(upCut);
            thre.CutDown(downCut);
            addSignal(sig);
        }
        public void Button_Click_Resample(object sender, RoutedEventArgs e)
        {
            if (signalList.Count == 0)
            {
                MessageBox.Show("Нет сигналов для изменения частоты", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                float newFreq = float.Parse(newFreq_textBox.Text, CultureInfo.InvariantCulture);
                SingleChannelSignal lastSignal = signalList.Last();
                SingleChannelSignal resampled = Resampler.Resample(lastSignal, newFreq);

                clearMarkers();
                WpfPlot1.Plot.Remove(WpfPlot1.Plot.PlottableList.Last());
                signalList.RemoveAt(signalList.Count - 1);

                addSignal(resampled);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка изменения частоты", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        private void SignalTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(SignalTypeComboBox.SelectedItem is SignalType type) || SinePanel == null)
                return;

            SinePanel.Visibility = Visibility.Collapsed;
            LinePanel.Visibility = Visibility.Collapsed;
            NoisePanel.Visibility = Visibility.Collapsed;

            switch (type)
            {
                case SignalType.Sine:
                    SinePanel.Visibility = Visibility.Visible;
                    break;
                case SignalType.Line:
                    LinePanel.Visibility = Visibility.Visible;
                    break;
                case SignalType.Noise:
                    NoisePanel.Visibility = Visibility.Visible;
                    break;
            }
        }

        private bool TryParseFloat(TextBox textBox, string fieldName, out float value)
        {
            string text = textBox.Text.Replace(',', '.');
            if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            {
                MessageBox.Show(
                    $"Некорректное числовое значение в поле «{fieldName}».",
                    "Ошибка ввода",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private GeneratorParameters ReadParameters(SignalType type)
        {
            switch (type)
            {
                case SignalType.Sine:
                    if (!TryParseFloat(period_textBox, "Период", out float period) ||
                        !TryParseFloat(phase_textBox, "Фаза", out float phase) ||
                        !TryParseFloat(scale_textBox, "Амплитуда", out float amplitude))
                        return null;

                    return new SineParameters
                    {
                        Period = period,
                        Phase = phase,
                        Amplitude = amplitude
                    };

                case SignalType.Line:
                    if (!TryParseFloat(k_textBox, "k", out float k) ||
                        !TryParseFloat(b_textBox, "b", out float b))
                        return null;

                    return new LineParameters { K = k, B = b };

                case SignalType.Noise:
                    if (!TryParseFloat(range_textBox, "Размах", out float range))
                        return null;

                    return new NoiseParameters { Range = range };

                default:
                    return null;
            }
        }

        private void Button_Click_Generate(object sender, RoutedEventArgs e)
        {
            if (!(SignalTypeComboBox.SelectedItem is SignalType selectedType))
                return;

            if (!TryParseFloat(freq_textBox, "Частота", out float samplingRate) ||
                !TryParseFloat(len_textBox, "Длина", out float duration))
                return;

            GeneratorParameters parameters = ReadParameters(selectedType);
            if (parameters == null)
                return;

            try
            {
                var generator = SignalGeneratorFactory.Create(parameters);
                var signal = generator.Generate(new SamplingParameters(samplingRate, duration));
                addSignal(signal);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка генерации", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
