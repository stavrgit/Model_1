using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace Model__
{
    public partial class Form1 : Form
    {
        private int[] sampleMethod1;
        private int[] sampleMethod2;
        private Random random = new Random();
        private const double a = 3.0; // Математическое ожидание
        private int sampleSize = 1000; // Размер выборки
        public Form1()
        {
            InitializeComponent();
            InitializeChart();
            InitializeSampleSizeComboBox();
        }


        private void InitializeChart()
        {
            chart1.ChartAreas.Clear();
            chart1.Series.Clear();

            var chartArea = new ChartArea();
            chartArea.AxisX.Title = "Значение";
            chartArea.AxisY.Title = "Частота";
            chart1.ChartAreas.Add(chartArea);
        }

        private void InitializeSampleSizeComboBox()
        {
            cmbSampleSize.Items.Add("1,000 элементов");
            cmbSampleSize.Items.Add("10,000 элементов");
            cmbSampleSize.SelectedIndex = 0;
        }

        // Метод 1: На основе теоремы Пуассона (биномиальная аппроксимация)
        private int GeneratePoissonMethod1()
        {
            int n = 1000; // Большое число испытаний
            double p = a / n; // Вероятность успеха
            int successes = 0;

            for (int i = 0; i < n; i++)
            {
                if (random.NextDouble() < p)
                    successes++;
            }

            return successes;
        }

        // Метод 2: Произведение равномерных случайных чисел
        private int GeneratePoissonMethod2()
        {
            double threshold = Math.Exp(-a);
            double product = 1.0;
            int k = 0;

            while (product >= threshold)
            {
                product *= random.NextDouble();
                k++;
            }

            return k - 1;
        }

        // Генерация выборки
        private int[] GenerateSample(Func<int> generator)
        {
            int[] sample = new int[sampleSize];
            for (int i = 0; i < sampleSize; i++)
            {
                sample[i] = generator();
            }
            return sample;
        }

        // Построение гистограмм
        private void PlotHistograms()
        {
            chart1.Series.Clear();

            var freq1 = sampleMethod1.GroupBy(x => x)
                           .ToDictionary(g => g.Key, g => g.Count());
            var freq2 = sampleMethod2.GroupBy(x => x)
                           .ToDictionary(g => g.Key, g => g.Count());

            int minValue = Math.Min(freq1.Keys.Min(), freq2.Keys.Min());
            int maxValue = Math.Max(freq1.Keys.Max(), freq2.Keys.Max());

            var series1 = new Series
            {
                Name = "Метод 1 (Биномиальная аппроксимация)",
                ChartType = SeriesChartType.Column,
                Color = System.Drawing.Color.Blue
            };

            var series2 = new Series
            {
                Name = "Метод 2 (Произведение случайных чисел)",
                ChartType = SeriesChartType.Column,
                Color = System.Drawing.Color.Red
            };

            for (int x = minValue; x <= maxValue; x++)
            {
                series1.Points.AddXY(x, freq1.ContainsKey(x) ? freq1[x] : 0);
                series2.Points.AddXY(x, freq2.ContainsKey(x) ? freq2[x] : 0);
            }

            chart1.Series.Add(series1);
            chart1.Series.Add(series2);

            chart1.Legends.Clear();
            var legend = new Legend();
            chart1.Legends.Add(legend);
        }

        private double KolmogorovSmirnovTest() // Критерий Колмогорова - Смирнова
        {
            var sorted1 = sampleMethod1.OrderBy(x => x).ToArray();
            var sorted2 = sampleMethod2.OrderBy(x => x).ToArray();

            double maxDiff = 0;
            int n1 = sorted1.Length, n2 = sorted2.Length;
            int i = 0, j = 0;
            double cum1 = 0, cum2 = 0;

            while (i < n1 || j < n2)
            {
                if (i < n1 && (j >= n2 || sorted1[i] <= sorted2[j]))
                {
                    cum1 = (i + 1.0) / n1;
                    i++;
                }
                else
                {
                    cum2 = (j + 1.0) / n2;
                    j++;
                }
                maxDiff = Math.Max(maxDiff, Math.Abs(cum1 - cum2));
            }

            return maxDiff;
        }
        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void chart1_Click(object sender, EventArgs e)
        {

        }
        private (double ksStatistic, double timeMethod1, double timeMethod2) PerformTest()
        {
            // Измеряем время генерации
            var watch1 = System.Diagnostics.Stopwatch.StartNew();
            sampleMethod1 = GenerateSample(GeneratePoissonMethod1);
            watch1.Stop();

            var watch2 = System.Diagnostics.Stopwatch.StartNew();
            sampleMethod2 = GenerateSample(GeneratePoissonMethod2);
            watch2.Stop();

            // Сортируем для KS-теста
            var sorted1 = sampleMethod1.OrderBy(x => x).ToArray();
            var sorted2 = sampleMethod2.OrderBy(x => x).ToArray();

            double maxDiff = 0;
            int n1 = sorted1.Length, n2 = sorted2.Length;
            int i = 0, j = 0;
            double cum1 = 0, cum2 = 0;

            while (i < n1 || j < n2)
            {
                if (i < n1 && (j >= n2 || sorted1[i] <= sorted2[j]))
                {
                    cum1 = (i + 1.0) / n1;
                    i++;
                }
                else
                {
                    cum2 = (j + 1.0) / n2;
                    j++;
                }
                maxDiff = Math.Max(maxDiff, Math.Abs(cum1 - cum2));
            }

            return (maxDiff, watch1.Elapsed.TotalMilliseconds, watch2.Elapsed.TotalMilliseconds);
        }


        private void btnGenerate_Click(object sender, EventArgs e)
        {
            sampleSize = cmbSampleSize.SelectedIndex == 0 ? 1000 : 10000;

            var (ksStatistic, time1, time2) = PerformTest();
            PlotHistograms();

            double criticalValue = 1.36 / Math.Sqrt(sampleSize);

            string result = $"Размер выборки: {sampleSize:N0}\n" +
                          $"Время генерации (Метод 1): {time1:F2} мс\n" +
                          $"Время генерации (Метод 2): {time2:F2} мс\n\n" +
                          $"KS-статистика: {ksStatistic:F4}\n" +
                          $"Критическое значение: {criticalValue:F4}\n\n" +
                          (ksStatistic < criticalValue
                              ? "Выборки принадлежат одной генеральной совокупности"
                              : "Выборки из разных распределений");

            lblResult.Text = result;
        }

        private void btnTest_Click(object sender, EventArgs e)
        {
            if (sampleMethod1 == null || sampleMethod2 == null)
            {
                MessageBox.Show("Сначала сгенерируйте выборки!", "Ошибка",
                               MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            double ksStatistic = KolmogorovSmirnovTest();
            double criticalValue = 1.36 / Math.Sqrt(sampleSize); // Для α=0.05

            string result = $"Результаты теста Колмогорова-Смирнова:\n\n" +
                           $"KS-статистика: {ksStatistic:F4}\n" +
                           $"Критическое значение (α=0.05): {criticalValue:F4}\n\n" +
                           (ksStatistic < criticalValue
                               ? "Выборки принадлежат одной генеральной совокупности (H0 не отвергается)"
                               : "Выборки из разных распределений (H0 отвергается)");

            lblResult.Text = result;
        }

        private void lblResult_Click(object sender, EventArgs e)
        {

        }
    }
}
