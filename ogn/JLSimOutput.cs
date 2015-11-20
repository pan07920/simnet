using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Data.OleDb;

namespace ogn
{

    public partial class JLSimOutput : Form
    {
        [DllImport("DataParsingDLL.dll", EntryPoint = "Process")]
        public static extern int ParsingProcessData(string SimInput, string TraceFile, string output);
        public string LastWorkingFolder { get; set; }
        public int TotalSecurities { get; set; }
        public int SimulationLength { get; set; }
        public bool MTOperationMode { get; set; }
        private List<string[]> GetCaseInputParameters()
        {
            List<string[]> list = new List<string[]>();
            string[] SimLength = { "Length of Simulation (in days)", "Simulation Length" };
            list.Add(SimLength);
            string[] Securities = { "Securities (other than cash and borrowing)", "Number of Securities" };
            list.Add(Securities);
            string[] Statisticians = { "   Statisticians", "Number of Statisticians" };
            list.Add(Statisticians);
            string[] Analysts = { "   Portfolio Analysts", "Number of Analysts" };
            list.Add(Analysts);
            string[] Investors = { "Investor Templates (types of investors)", "Types of Investors" };
            list.Add(Investors);
            string[] Trader = { "Trader Templates (types of traders)", "Types of Traders" };
            list.Add(Trader);
            string[] Factor = { "price series before the start of simulation", "Number of Factors" };
            list.Add(Factor);
            string[] DayKept = { "Nr. of Days Data Kept for statisticians", "Number of Days Kept" };
            list.Add(DayKept);
            string[] nMonthsKept = { "Nr. of Months Data Kept for statisticians", "Number of Months Kept" };
            list.Add(nMonthsKept);
            string[] mILS = { "Max Initial Sum of Long + |Short|", "Max Initial L+abs(S)" };
            list.Add(mILS);
            string[] mMLS = { "Max Mark-to-Market Sum of Long + |Short|", "Max Maintenance L+abs(S)" };
            list.Add(mMLS);
            string[] ds = { "(statisticians use simulation's own history)", "Data Source" };//"Endogenous", "Exogenous"
            list.Add(ds);
            string[] uptickRule = { "Can only short on uptick?  Y or N", "Uptick Rule" };// N --No else Yes
            list.Add(uptickRule);
            string[] rebateFraction = { "Short rebate fraction", "Rebate Fraction" };
            list.Add(rebateFraction);
            return list;
        }
        private List<string[]> GetCaseMessageParameters()
        {
            List<string[]> list = new List<string[]>();
            string[] TD = { "Total Deposits", "Total Deposits" };
            list.Add(TD);
            string[] NoD = { "Nr. Deposits", "Number of Deposits" };
            list.Add(NoD);
            string[] TW = { "Total Withdrawals", "Total Withdrawals" };
            list.Add(TW);
            string[] NOW = { "Nr. Withdrawals", "Number of Withdrawals" };
            list.Add(NOW);
            string[] LRS = { "Last random number seed", "Last Random Seed" };
            list.Add(LRS);
            return list;
        }

        double[,] _price;
        int[,] _volume;
        double[,] _weightmarket;
        int[] _totalvolume;

        public JLSimOutput()
        {

            InitializeComponent();
        }

        private void buttonFolder_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                textBoxFolder.Text = folderBrowserDialog1.SelectedPath;
            }

        }

        private void textBoxFolder_TextChanged(object sender, EventArgs e)
        {
            LoadCaseFiles();
        }
        private void LoadCaseFiles()
        {
            string prefixcase = "JLMSimInput for Case ";
            try
            {
                var files = from file in Directory.EnumerateFiles(textBoxFolder.Text, "*.txt", SearchOption.AllDirectories)
                            where file.Contains(prefixcase)
                            select new
                            {
                                File = (file.Replace(".txt", "")).Replace(textBoxFolder.Text + @"\" + prefixcase, "")
                            };

                foreach (var f in files)
                {
                    listBoxCases.Items.Add(f.File);
                }

            }
            catch (UnauthorizedAccessException UAEx)
            {
                Console.WriteLine(UAEx.Message);
            }
            catch (PathTooLongException PathEx)
            {
                Console.WriteLine(PathEx.Message);
            }
        }

        private void buttonLoad_Click(object sender, EventArgs e)
        {
            if (listBoxCases.SelectedIndex < 0)
                return;
            LoadSummary();

            if (!MTOperationMode)
            {
                LoadDailyDataFile();
                RefreshIndicesChart();
            }
            else
            {
                string prefixInput = "JLMSimInput for Case ";

                string casename = listBoxCases.SelectedItem.ToString();
                string caseinputfile = textBoxFolder.Text + @"\" + prefixInput + casename + ".txt";

                string tracefile = textBoxFolder.Text + @"\" + "Trace file for Case " + casename + ".txt";
                string outfile = textBoxFolder.Text + @"\" + "ParsedSecurityWts.csv";
                int rtn = ParsingProcessData(caseinputfile, tracefile, outfile);

                SamplingParsedWeightData(outfile, 20);

                string returnfile = textBoxFolder.Text + @"\" + "Estimates During Case " + casename + ".csv";
                CMEReturnEstimateData(returnfile, 100);
            }

            for (int i = 0; i < TotalSecurities; i++)
                checkedlbxSecurities.Items.Add("Security " + i.ToString());

        }
        private void LoadDailyDataFile()
        {
            string prefixDailyReport = "Daily Reports for Case ";
            string casename = listBoxCases.SelectedItem.ToString();
            string dailyreportfile = textBoxFolder.Text + @"\" + prefixDailyReport + casename + ".csv";
            List<string> inputlines = File.ReadAllLines(dailyreportfile).ToList<string>();

            for (int i = 0; i < 4; i++)
                inputlines.RemoveAt(0);

            int security_count = TotalSecurities;
            int total_day_count = SimulationLength;
            _price = new double[total_day_count, security_count];
            _volume = new int[total_day_count, security_count];
            _weightmarket = new double[total_day_count, 2];
            _totalvolume = new int[total_day_count];
            int row = 0;
            foreach (string line in inputlines)
            {
                string workingline = line.Replace("1.#J", "0");
                workingline = line.Replace("1.#IO", "0");
                List<string> linedata = line.Split(',').ToList<string>();
                int day = int.Parse(linedata[0]);

                for (int i = 1; i <= security_count; i++)
                {
                    _price[row, i - 1] = double.Parse(linedata[i]);
                    _volume[row, i - 1] = int.Parse(linedata[i + 2 + security_count]);
                }
                _weightmarket[row, 0] = double.Parse(linedata[security_count + 1]);
                _weightmarket[row, 1] = double.Parse(linedata[security_count + 2]);
                _totalvolume[row] = int.Parse(linedata[3 + 2 * security_count]);
                row++;
            }

        }
        private void RefreshIndicesChart()
        {
            int security_count = TotalSecurities;
            int total_day_count = SimulationLength;

            // Copy the data to appropriate arrays,
            // and find the maximum prices and volumes.
            double slope = (total_day_count - 1.0) / 99.0;
            double maxPrice = 0;
            double maxVolume = 0;
            int mprv = 0;
            double[,] prcData = new double[100, security_count];
            int[,] volData = new int[100, security_count];
            int pCol = 0;//todo taking first security for now
            int[] indexVol = new int[100];
            double[,] indexPrice = new double[100, 2];
            double[] op = new double[100];
            double[] lo = new double[100];
            double[] hi = new double[100];
            double[] cl = new double[100];
            double P = 0;
            for (int i = 0; i < 100; i++)
            {
                int k = (int)Math.Round(slope * i);
                prcData[i, 0] = _price[k, pCol];
                volData[i, 0] = 0;

                op[i] = _price[mprv, pCol];
                lo[i] = 1000000;
                hi[i] = -1000000;
                indexVol[i] = 0;
                indexPrice[i, 0] = _weightmarket[k, 0];
                indexPrice[i, 1] = _weightmarket[k, 1];
                for (int m = mprv; m <= k; m++)
                {
                    volData[i, 0] = volData[i, 0] + _volume[m, pCol];
                    indexVol[i] = indexVol[i] + _totalvolume[m];
                    P = _price[m, pCol];
                    if (P > hi[i])
                        hi[i] = P;

                    if (P < lo[i])
                        lo[i] = P;
                }
                cl[i] = P;

                if (hi[i] > maxPrice)
                    maxPrice = hi[i];

                if (volData[i, 0] > maxVolume)
                    maxVolume = volData[i, 0];
                mprv = k + 1;
            }

            ///////////////////
            chartIndicesPrice.Series.Clear();
            chartIndicesPrice.Series.Add("Equally Weighted", DevExpress.XtraCharts.ViewType.Line);
            chartIndicesPrice.Series.Add("Capitalization Weighted", DevExpress.XtraCharts.ViewType.Line);

            double ymax = 10, ymin = 10;
            double ewvalue;
            double cwvalue;

            for (int i = 0; i < 100; i++)
            {
                ewvalue = indexPrice[i, 0];
                cwvalue = indexPrice[i, 1];
                ymax = Math.Max(ymax, ewvalue);
                ymin = Math.Min(ymin, ewvalue);
                ymax = Math.Max(ymax, cwvalue);
                ymin = Math.Min(ymin, cwvalue);

                chartIndicesPrice.Series[0].Points.Add(new DevExpress.XtraCharts.SeriesPoint(i, ewvalue));
                chartIndicesPrice.Series[1].Points.Add(new DevExpress.XtraCharts.SeriesPoint(i, cwvalue));
            }

            ymax = Math.Ceiling(ymax);
            ymin = Math.Floor(ymin);
            DevExpress.XtraCharts.XYDiagram diagram = (DevExpress.XtraCharts.XYDiagram)chartIndicesPrice.Diagram;
            // Enable the diagram's scrolling.
            diagram.EnableAxisXScrolling = true;
            diagram.EnableAxisYScrolling = true;


            // Define the whole range for the Y-axis. 
            diagram.AxisY.WholeRange.Auto = false;
            diagram.AxisY.WholeRange.SetMinMaxValues(ymin, ymax);

            diagram.AxisX.VisualRange.AutoSideMargins = false;
            diagram.AxisY.VisualRange.AutoSideMargins = false;


            /////////////////// Chart Indices Tab (Price Indices and  Volume)
            chartIndicesVolume.Series.Clear();
            chartIndicesVolume.Series.Add("Volume", DevExpress.XtraCharts.ViewType.Bar);

            double ymaxv = indexVol[0] / 1000000, yminv = indexVol[0] / 1000000;
            int volumedata;

            for (int i = 0; i < indexVol.Length; i++)
            {
                volumedata = indexVol[i] / 1000000;

                ymaxv = Math.Max(ymaxv, volumedata);
                yminv = Math.Min(yminv, volumedata);

                chartIndicesVolume.Series[0].Points.Add(new DevExpress.XtraCharts.SeriesPoint(i * 40, volumedata));
            }

            ymax = Math.Ceiling(ymax);
            ymin = Math.Floor(ymin);
            DevExpress.XtraCharts.XYDiagram diagramvol = (DevExpress.XtraCharts.XYDiagram)chartIndicesVolume.Diagram;
            // Enable the diagram's scrolling.
            diagramvol.EnableAxisXScrolling = false;
            diagramvol.EnableAxisYScrolling = true;


            // Define the whole range for the Y-axis. 
            //diagramvol.AxisY.WholeRange.Auto = false;
            //diagramvol.AxisY.WholeRange.SetMinMaxValues(ymin, ymax);

            diagramvol.AxisX.VisualRange.AutoSideMargins = false;
            diagramvol.AxisY.VisualRange.AutoSideMargins = false;
            diagramvol.AxisX.VisualRange.SideMarginsValue = 0;

        }



        private void LoadSummary()
        {
            listViewCaseSummary.Items.Clear();

            if (listBoxCases.SelectedIndex < 0)
                return;
            string prefixInput = "JLMSimInput for Case ";
            string prefixMessage = "Messages from Case ";
            string casename = listBoxCases.SelectedItem.ToString();
            string caseinputfile = textBoxFolder.Text + @"\" + prefixInput + casename + ".txt";
            string casemessagefile = textBoxFolder.Text + @"\" + prefixMessage + casename + ".csv";

            string[] rowfirst = { "Case Name", casename };
            var listviewitemfirst = new ListViewItem(rowfirst);
            listViewCaseSummary.Items.Add(listviewitemfirst);

            List<string> inputlines = File.ReadAllLines(caseinputfile).ToList<string>();
            List<string> linemessagelines = File.ReadAllLines(casemessagefile).ToList<string>();

            List<string[]> frominput = GetCaseInputParameters();
            List<string[]> fromMessage = GetCaseMessageParameters();

            foreach (string[] param in frominput)
            {
                string val = inputlines.Find(delegate (string s) { return s.Contains(param[0]); });
                if (val == null)
                    val = "";
                else
                {
                    val = val.Split(':').ToList<string>()[1].Trim();
                }
                string[] row = { param[1], val };
                var listviewitem = new ListViewItem(row);
                if (param[1] == "Number of Securities")
                    TotalSecurities = int.Parse(val);
                if (param[1] == "Simulation Length")
                    SimulationLength = int.Parse(val);

                listViewCaseSummary.Items.Add(listviewitem);
            }

            foreach (string[] param in fromMessage)
            {
                string val = linemessagelines.Find(delegate (string s) { return s.Contains(param[0]); });
                if (val == null)
                    val = "";
                else
                {
                    val = val.Split(new char[] { ':', ',' }).ToList<string>().Last<string>().Trim();
                }
                if (param[0].Contains("Total"))
                {
                    Decimal amount = Decimal.Parse(val);
                    val = String.Format(new CultureInfo("en-US"), "{0:C0}", amount); //"{0:C}" for decimal/cents
                }

                if (param[0].Contains("Nr."))
                {
                    int amount = int.Parse(val);
                    val = String.Format("{0:n0}", amount);
                }

                string[] row = { param[1], val };
                var listviewitem = new ListViewItem(row);
                listViewCaseSummary.Items.Add(listviewitem);
            }
            MTOperationMode = true; //show Weight and Returns
            string mode = inputlines.Find(delegate (string s) { return s.Contains("or that specific specs follow"); });
            if (mode == null)
                mode = "";
            else
            {
                mode = mode.Split(new char[] { ':', ',' }).ToList<string>().Last<string>().Trim();
            }
            if (mode == "X" || mode == "N")
                MTOperationMode = false;
            if (MTOperationMode)
            {
                tabControl1.TabPages.Remove(tabPage2);
                tabControl1.TabPages.Remove(tabPage3);
            }
            else
            {
                tabControl1.TabPages.Remove(tabPage4);
                tabControl1.TabPages.Remove(tabPage5);
            }


        }
        private void SamplingParsedWeightData(string filename, int interval)
        {
            int _security_count = TotalSecurities;
            List<string> inputlines = File.ReadAllLines(filename).ToList<string>();
            int totalline = inputlines.Count;

            int samplecount = totalline / interval;
            int[] sampleindex = new int[samplecount];
            double[,] sampledata = new double[samplecount, _security_count + 1];
            for (int i = 0; i < samplecount; i++)
            {
                List<string> linedata = inputlines[i * interval].Split(',').ToList<string>();
                sampleindex[i] = int.Parse(linedata[0]);
                for (int j = 0; j < _security_count + 1; j++)
                    sampledata[i, j] = double.Parse(linedata[j + 1]);
            }

            chartWeights.Series.Clear();
            for (int i = 0; i < TotalSecurities; i++)
            {
                chartWeights.Series.Add("S" + i.ToString(), DevExpress.XtraCharts.ViewType.Line);
            }
            chartWeights.Series.Add("Cash", DevExpress.XtraCharts.ViewType.Line);

            double ymax = 10, ymin = 10;
            double pointvalue;
            for (int m = 0; m < 40; m++)
            {
                for (int s = 0; s < TotalSecurities + 1; s++)
                {
                    pointvalue = 100 * sampledata[m, s];
                    ymax = Math.Max(ymax, pointvalue);
                    ymin = Math.Min(ymin, pointvalue);
                    chartWeights.Series[s].Points.Add(new DevExpress.XtraCharts.SeriesPoint(sampleindex[m], 100 * sampledata[m, s]));
                }
            }
            ymax = Math.Ceiling(ymax);
            ymin = Math.Floor(ymin);
            DevExpress.XtraCharts.XYDiagram diagram = (DevExpress.XtraCharts.XYDiagram)chartWeights.Diagram;
            // Enable the diagram's scrolling.
            diagram.EnableAxisXScrolling = true;
            diagram.EnableAxisYScrolling = true;


            // Define the whole range for the Y-axis. 
            diagram.AxisY.WholeRange.Auto = false;
            diagram.AxisY.WholeRange.SetMinMaxValues(ymin, ymax);

            diagram.AxisX.VisualRange.AutoSideMargins = false;
            diagram.AxisY.VisualRange.AutoSideMargins = false;

        }

        private void CMEReturnEstimateData(string filename, int numberofmonth)
        {
            //For CME cases, Return Chart, all securities, by month
            int _security_count = TotalSecurities;
            List<string> inputlines = File.ReadAllLines(filename).ToList<string>();
            int totalline = inputlines.Count;
            totalline = 800;
            double[,] estimatereturndata = new double[numberofmonth, _security_count + 1];
            for (int i = 0; i < numberofmonth; i++)
            {
                List<string> linedata = inputlines[i + 2].Split(',').ToList<string>();
                for (int j = 0; j < _security_count + 1; j++)
                    estimatereturndata[i, j] = double.Parse(linedata[j + 1]);
            }

            chartReturns.Series.Clear();
            for (int i = 0; i < TotalSecurities; i++)
            {
                chartReturns.Series.Add("S" + i.ToString(), DevExpress.XtraCharts.ViewType.Line);
            }
            double ymax = 10, ymin = 10;
            double pointvalue;
            for (int m = 0; m < numberofmonth; m++)
            {
                for (int s = 0; s < TotalSecurities; s++)
                {
                    pointvalue = 100 * estimatereturndata[m, s];
                    ymax = Math.Max(ymax, pointvalue);
                    ymin = Math.Min(ymin, pointvalue);
                    chartReturns.Series[s].Points.Add(new DevExpress.XtraCharts.SeriesPoint(m, 100 * estimatereturndata[m, s]));
                }
            }
            ymax = Math.Ceiling(ymax);
            ymin = Math.Floor(ymin);
            DevExpress.XtraCharts.XYDiagram diagram = (DevExpress.XtraCharts.XYDiagram)chartReturns.Diagram;
            // Enable the diagram's scrolling.
            diagram.EnableAxisXScrolling = true;
            diagram.EnableAxisYScrolling = true;

            //// Define the whole range for the X-axis. 
            //diagram.AxisX.WholeRange.Auto = false;
            //diagram.AxisX.WholeRange.SetMinMaxValues("A", "D");

            //// Disable the side margins 
            //// (this has an effect only for certain view types).
            //diagram.AxisX.VisualRange.AutoSideMargins = false;

            //// Limit the visible range for the X-axis.
            //diagram.AxisX.VisualRange.Auto = false;
            //diagram.AxisX.VisualRange.SetMinMaxValues("B", "C");

            // Define the whole range for the Y-axis. 
            diagram.AxisY.WholeRange.Auto = false;
            diagram.AxisY.WholeRange.SetMinMaxValues(ymin, ymax);

            diagram.AxisX.VisualRange.AutoSideMargins = false;
            diagram.AxisY.VisualRange.AutoSideMargins = false;

        }

        private void JLSimOutput_Load(object sender, EventArgs e)
        {
            LastWorkingFolder = @"C:\JLMSim";
            textBoxFolder.Text = LastWorkingFolder;
        }

        private void checkedlbxSecurities_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            List<string> checkedItems = new List<string>();
            foreach (var item in checkedlbxSecurities.CheckedItems)
                checkedItems.Add(item.ToString());

            if (e.NewValue == CheckState.Checked)
                checkedItems.Add(checkedlbxSecurities.Items[e.Index].ToString());
            chartSecurityPrice.Series.Clear();
            foreach (string item in checkedItems)
            {
                if (checkedItems.Count == 1)
                    RefreshSecurityPriceVolumeChart(item);
                else
                    AddSecurityPriceChart(item);
            }

            if (checkedlbxSecurities.CheckedItems.Count == 1 && e.NewValue == CheckState.Unchecked)
            {
                bool check = true;
            }
            // The collection is about to be emptied: there's just one item checked, and it's being unchecked at this moment
            else
            { // The collection will not be empty once this click is handled

            }
        }
        private void RefreshSecurityPriceVolumeChart(string security)
        {
            int security_index = int.Parse(security.Replace("Security", "").Trim());

            int security_count = TotalSecurities;

            if(security_index >= security_count-1)
                return;

            int total_day_count = SimulationLength;

            // Copy the data to appropriate arrays,
            // and find the maximum prices and volumes.
            double slope = (total_day_count - 1.0) / 99.0;
            double maxPrice = 0;
            double maxVolume = 0;
            int mprv = 0;

            int[] volData = new int[100];
            int pCol = security_index;//todo taking first security for now
            int[] indexVol = new int[100];
            double[,] indexPrice = new double[100, 2];
            double[] op = new double[100];
            double[] lo = new double[100];
            double[] hi = new double[100];
            double[] cl = new double[100];
            double P = 0;
            for (int i = 0; i < 100; i++)
            {
                int k = (int)Math.Round(slope * i);
                volData[i] = 0;

                op[i] = _price[mprv, pCol];
                lo[i] = 1000000;
                hi[i] = -1000000;
                indexVol[i] = 0;
                indexPrice[i, 0] = _weightmarket[k, 0];
                indexPrice[i, 1] = _weightmarket[k, 1];
                for (int m = mprv; m <= k; m++)
                {
                    volData[i] = volData[i] + _volume[m, pCol];
                    indexVol[i] = indexVol[i] + _totalvolume[m];
                    P = _price[m, pCol];
                    if (P > hi[i])
                        hi[i] = P;

                    if (P < lo[i])
                        lo[i] = P;
                }
                cl[i] = P;

                if (hi[i] > maxPrice)
                    maxPrice = hi[i];

                if (volData[i] > maxVolume)
                    maxVolume = volData[i];
                mprv = k + 1;
            }

            ///////////////////
            chartSecurityPrice.Series.Clear();
            chartSecurityPrice.Series.Add(security, DevExpress.XtraCharts.ViewType.Stock);
           
  
            for (int i = 0; i < 100; i++)
            {
             

                chartSecurityPrice.Series[0].Points.Add(new DevExpress.XtraCharts.SeriesPoint(i, new object[] { lo[i], hi[i], op[i], cl[i] }));

            }




            ///////////////////// Chart security volume bar chart
            chartSecurityVolume.Series.Clear();
            chartSecurityVolume.Series.Add("Volume", DevExpress.XtraCharts.ViewType.Bar);

            int volumedata;
            for (int i = 0; i < 100; i++)
            {
                volumedata = volData[i] ;

                chartSecurityVolume.Series[0].Points.Add(new DevExpress.XtraCharts.SeriesPoint(i, volumedata));
            }

        }
        private void AddSecurityPriceChart(string security)
        {
        }
    }
}

