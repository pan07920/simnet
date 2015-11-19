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

            if (! MTOperationMode )
                LoadDailyDataFile();
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

        }
        private void LoadDailyDataFile()
        {
            string prefixDailyReport = "Daily Reports for Case ";
            string casename = listBoxCases.SelectedItem.ToString();
            string dailyreportfile = textBoxFolder.Text + @"\" + prefixDailyReport + casename + ".csv";
            List<string> inputlines = File.ReadAllLines(dailyreportfile).ToList<string>();

            for (int i = 0; i < 4; i++)
                inputlines.RemoveAt(0);

            int _security_count = TotalSecurities;
            int _total_day_count = SimulationLength;
            double[,] price = new double[_total_day_count, _security_count];
            int[,] volume = new int[_total_day_count, _security_count];
            double[,] weightmket = new double[_total_day_count, 2];
            int[] totalvolume = new int[_total_day_count];
            int row = 0;
            foreach (string line in inputlines)
            {
                string workingline = line.Replace("1.#J", "0");
                workingline = line.Replace("1.#IO", "0");
                List<string> linedata = line.Split(',').ToList<string>();
                int day = int.Parse(linedata[0]);

                for (int i = 1; i <= _security_count; i++)
                {
                    price[row, i - 1] = double.Parse(linedata[i]);
                    volume[row, i - 1] = int.Parse(linedata[i + 2 + _security_count]);
                }
                weightmket[row, 0] = double.Parse(linedata[_security_count + 1]);
                weightmket[row, 1] = double.Parse(linedata[_security_count + 2]);
                totalvolume[row] = int.Parse(linedata[3 + 2 * _security_count]);
                row++;
            }

            // Copy the data to appropriate arrays,
            // and find the maximum prices and volumes.
            double slope = (_total_day_count - 1.0) / 99.0;
            double maxPrice = 0;
            double maxVolume = 0;
            int mprv = 0;
            double[,] prcData = new double[100, _security_count];
            int[,] volData = new int[100, _security_count];
            int pCol = 0;//todo taking first security for now
            int[] indexVol = new int[100];
            double[] op = new double[100];
            double[] lo = new double[100];
            double[] hi = new double[100];
            double[] cl = new double[100];
            double P = 0;
            for (int i = 0; i < 100; i++)
            {
                int k = (int)Math.Round(slope * i);
                prcData[i, 0] = price[k, pCol];
                volData[i, 0] = 0;

                op[i] = price[mprv, pCol];
                lo[i] = 1000000;
                hi[i] = -1000000;
                indexVol[i] = 0;
                for (int m = mprv; m <= k; m++)
                {
                    volData[i, 0] = volData[i, 0] + volume[m, pCol];
                    indexVol[i] = indexVol[i] + totalvolume[m];
                    P = price[m, pCol];
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
                mprv = k+1 ;
            }
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
                    val = String.Format("{0:n0}",  amount);
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
                List<string> linedata  = inputlines[i* interval].Split(',').ToList<string>();
                sampleindex[i] = int.Parse(linedata[0]);
                for(int j = 0; j < _security_count + 1; j++)
                    sampledata[i,j] = double.Parse(linedata[j+1]);
            }
        }

        private void CMEReturnEstimateData(string filename, int numberofmonth)
        {
            //For CME cases, Return Chart, all securities, by month
            int _security_count = TotalSecurities;
            List<string> inputlines = File.ReadAllLines(filename).ToList<string>();
            int totalline = inputlines.Count;

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
                    ymin = Math.Max(ymin, pointvalue);
                    chartReturns.Series[s].Points.Add(new DevExpress.XtraCharts.SeriesPoint(m, 100 * estimatereturndata[m, s]));
                }
            }
            ymax = Math.Ceiling(ymax);
            ymin = Math.Floor(ymin);
            //chartReturns.Diagram .AxisY.VisualRange.Auto = false;
            //xyDiagram1.AxisY.VisualRange.MaxValueSerializable = "5.6";
            //xyDiagram1.AxisY.VisualRange.MinValueSerializable = "0";

        }

        private void JLSimOutput_Load(object sender, EventArgs e)
        {
            LastWorkingFolder = @"C:\JLMSim";
            textBoxFolder.Text = LastWorkingFolder;
        }
    }
}

