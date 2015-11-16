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
namespace ogn
{
    public partial class JLSimOutput : Form
    {
        public string LastWorkingFolder { get; set; }
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
            string[] TD = { "TotalDeposits", "Total Deposits" };
            list.Add(TD);
            string[] NoD = { "Nr.Deposits", "Number of Deposits" };
            list.Add(NoD);
            string[] TW = { "TotalWithdrawals", "Total Withdrawals" };
            list.Add(TW);
            string[] NOW = { "Nr.Withdrawals", "Number of Withdrawals" };
            list.Add(NOW);
            string[] LRS = { "Lastrandomnumberseed", "Last Random Seed" };
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
            LoadSummary();
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
            List<string> linemessagelines = File.ReadAllLines(caseinputfile).ToList<string>();

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
                listViewCaseSummary.Items.Add(listviewitem);
            }

            foreach (string[] param in fromMessage)
            {
                string val = linemessagelines.Find(delegate (string s) { return s.Contains(param[0]); });
                if (val == null)
                    val = "";
                else
                {
                    val = val.Split(':').ToList<string>()[1].Trim();
                }
                string[] row = { param[1], val };
                var listviewitem = new ListViewItem(row);
                listViewCaseSummary.Items.Add(listviewitem);
            }



            //var SimulationLenth = from line in lines
            //                      where line.Contains("Length of Simulation (in days)")
            //                      select (line.Split(':').ToList<string>()[1]).Single();

            //string NumOfSecurities = "";

            //    // Declarations.
            //    short j = 0;
            //    short i = 0;
            //    short k = 0;
            //    string FileName = null;
            //    string FileLine = null;
            //    string SimLength = null;
            //    string FormatStr = null;
            //    string nDaysKept = null;
            //    string nFactors = null;
            //    string nMonthsKept = null;
            //    string ds = null;
            //    string mILS = null;
            //    string mMLS = null;
            //    string uptickRule = null;
            //    string rebateFraction = null;
            //    object FSys = null;
            //    object TStream = null;

            //    object DailyData = null;
            //    object theArray = null;
            //    int LastRandomSeed = 0;
            //    string TotalDeposits = null;
            //    string NumberDeposits = null;
            //    string TotalWithdrawals = null;
            //    string NumberWithdrawals = null;
            //    object title = null;
            //    object msg = null;
            //    object style = null;
            //    object response = null;

            //    const short ForReading = 1;



            //    // Get the user's choice of case.
            //    CaseName = VB6.GetItemString(lbChooseCase, CaseNumber);
            //    FileName = FileNames(CaseNumber);
            //    DailyReportsFilename = "Daily Reports for Case " + CaseName;
            //    //UPGRADE_WARNING: Couldn't resolve default property of object FSys.OpenTextFile. Click for more: 'ms-help://MS.VSExpressCC.v80/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
            //    TStream = FSys.OpenTextFile(FileName, ForReading);

            //    // Get the case's parameters from the carbon copy of the JLMSimInput file.
            //    SimLength = Convert.ToString(0);
            //    //UPGRADE_WARNING: Couldn't resolve default property of object nSecurities. Click for more: 'ms-help://MS.VSExpressCC.v80/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
            //    nSecurities = 0;
            //    nStatisticians = 0;
            //    //UPGRADE_WARNING: Couldn't resolve default property of object nAnalysts. Click for more: 'ms-help://MS.VSExpressCC.v80/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
            //    nAnalysts = 0;
            //    nTraders = 0;
            //    //UPGRADE_WARNING: Couldn't resolve default property of object nInvestors. Click for more: 'ms-help://MS.VSExpressCC.v80/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
            //    nInvestors = 0;
            //    //UPGRADE_WARNING: Couldn't resolve default property of object TStream.AtEndOfStream. Click for more: 'ms-help://MS.VSExpressCC.v80/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
            //    while ((TStream.AtEndOfStream == false))
            //    {
            //        //UPGRADE_WARNING: Couldn't resolve default property of object TStream.readline. Click for more: 'ms-help://MS.VSExpressCC.v80/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
            //        FileLine = TStream.readline;;
            //string[] SimLength = "Length of Simulation (in days)";
            //string[] SECURITIES = "Securities (other than cash and borrowing)";
            //int g_SimulationLen = Convert.ToInt16(SimLength);
            //string[] Securities = "Securities (other than cash and borrowing)";
            //   // int g_NumSecurites = nSecurities;
            //string[] Statisticians = "   Statisticians";
            //int nStatisticians = Convert.ToInt16(Statisticians);
            //string[] Analysts = "   Portfolio Analysts";
            //string[] Investors = "Investor Templates (types of investors)";
            //string[] Trader = "Trader Templates (types of traders)";
            //int nTrader = Convert.ToInt16(Trader);
            //string[] Factor = "price series before the start of simulation";
            //string[] DayKept = "Nr. of Days Data Kept for statisticians";
            //string[] nMonthsKept = "Nr. of Months Data Kept for statisticians";
            //string[] mILS = "Max Initial Sum of Long + |Short|";
            //string[] mMLS = "Max Mark-to-Market Sum of Long + |Short|";
            //string[] ds = "(statisticians use simulation's own history)";//"Endogenous", "Exogenous"
            //string[] uptickRule = "Can only short on uptick?  Y or N";// N --No else Yes
            //string[] rebateFraction = "Short rebate fraction";
            //string[] g_eMTOperationMode = "or that specific specs follow";//: X, or : N =>0, else 1

            //string[] a = "Lastrandomnumberseed";
            //string[] b = "TotalDeposits";
            //string[] c = "TotalWithdrawals";
            //string[] d = "Nr.Deposits";
            //string[] e = "Nr.Withdrawals";


            //    }
            //    //UPGRADE_NOTE: Object TStream may not be destroyed until it is garbage collected. Click for more: 'ms-help://MS.VSExpressCC.v80/dv_commoner/local/redirect.htm?keyword="6E35BFF6-CD74-4B09-9689-3E1A43DF8969"'
            //    TStream = null;

            //    // Construct the name of the case data file.
            //    objRE.Pattern = "JLMSimInput for Case ";
            //    objRE.Global = false;
            //    FileName = objRE.Replace(FileName, "Daily Reports for Case ");
            //    objRE.Pattern = ".txt";
            //    objRE.Global = false;
            //    FileName = objRE.Replace(FileName, ".csv");



            //    // Get the case's parameters.
            //    objRE.Pattern = "Daily Reports for Case ";
            //    objRE.Global = false;
            //    FileName = objRE.Replace(FileName, "Messages from Case ");

            //    flxParameters.Rows = 20;
            //    FormatStr = ";Case Name|";
            //    FormatStr = FormatStr + "Simulation Length|";
            //    FormatStr = FormatStr + "Number of Securities|";
            //    FormatStr = FormatStr + "Number of Statisticians|";
            //    FormatStr = FormatStr + "Number of Analysts|";
            //    FormatStr = FormatStr + "Types of Investors|";
            //    FormatStr = FormatStr + "Types of Traders|";
            //    FormatStr = FormatStr + "Number of Factors|";
            //    FormatStr = FormatStr + "Number of Days Kept|";
            //    FormatStr = FormatStr + "Number of Months Kept|";
            //    FormatStr = FormatStr + "Max Initial L+abs(S)|";
            //    FormatStr = FormatStr + "Max Maintenance L+abs(S)|";
            //    FormatStr = FormatStr + "Data Source|";
            //    FormatStr = FormatStr + "Uptick Rule|";
            //    FormatStr = FormatStr + "Rebate Fraction|";
            //    FormatStr = FormatStr + "Total Deposits|";
            //    FormatStr = FormatStr + "Number of Deposits|";
            //    FormatStr = FormatStr + "Total Withdrawals|";
            //    FormatStr = FormatStr + "Number of Withdrawals|";
            //    FormatStr = FormatStr + "Last Random Seed";
            //    flxParameters.FormatString = FormatStr;



            //    //UPGRADE_WARNING: Couldn't resolve default property of object FSys.OpenTextFile. Click for more: 'ms-help://MS.VSExpressCC.v80/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
            //    TStream = FSys.OpenTextFile(FileName, ForReading);
            //    //UPGRADE_WARNING: Couldn't resolve default property of object TStream.AtEndOfStream. Click for more: 'ms-help://MS.VSExpressCC.v80/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
            //    while ((TStream.AtEndOfStream == false))
            //    {
            //        //UPGRADE_WARNING: Couldn't resolve default property of object TStream.readline. Click for more: 'ms-help://MS.VSExpressCC.v80/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
            //        FileLine = TStream.readline;
            //        FileLine = Strings.Replace(FileLine, " ", "");

            //        i = 0;
            //        i = Strings.InStr(FileLine, "Lastrandomnumberseed");
            //        if ((i > 0))
            //        {
            //            i = Strings.InStr(FileLine, ":");
            //            LastRandomSeed = Convert.ToInt32(Strings.Mid(FileLine, i + 1));
            //            flxParameters.Row = 19;
            //            flxParameters.Col = 1;
            //            flxParameters.Text = Convert.ToString(LastRandomSeed);
            //            flxParameters.CellAlignment = MSFlexGridLib.AlignmentSettings.flexAlignLeftBottom;
            //        }

            //        i = Strings.InStr(FileLine, "TotalDeposits");
            //        if ((i > 0))
            //        {
            //            i = Strings.InStr(FileLine, ",");
            //            TotalDeposits = Strings.Mid(FileLine, i + 2);
            //            TotalDeposits = "$" + InsertCommas(TotalDeposits);
            //            flxParameters.Row = 15;
            //            flxParameters.Col = 1;
            //            flxParameters.CellAlignment = MSFlexGridLib.AlignmentSettings.flexAlignLeftBottom;
            //            flxParameters.Text = TotalDeposits;
            //        }

            //        i = Strings.InStr(FileLine, "TotalWithdrawals");
            //        if ((i > 0))
            //        {
            //            i = Strings.InStr(FileLine, ",");
            //            TotalWithdrawals = Strings.Mid(FileLine, i + 2);
            //            i = Strings.InStr(TotalWithdrawals, "-");
            //            if ((i > 0))
            //            {
            //                TotalWithdrawals = Strings.Mid(TotalWithdrawals, i + i);
            //            }
            //            TotalWithdrawals = "$" + InsertCommas(TotalWithdrawals);
            //            flxParameters.Row = 17;
            //            flxParameters.Col = 1;
            //            flxParameters.CellAlignment = MSFlexGridLib.AlignmentSettings.flexAlignLeftBottom;
            //            flxParameters.Text = TotalWithdrawals;
            //        }

            //        i = Strings.InStr(FileLine, "Nr.Deposits");
            //        if ((i > 0))
            //        {
            //            i = Strings.InStr(FileLine, ",");
            //            NumberDeposits = Strings.Mid(FileLine, i + 2);
            //            NumberDeposits = InsertCommas(NumberDeposits);
            //            flxParameters.Row = 16;
            //            flxParameters.Col = 1;
            //            flxParameters.CellAlignment = MSFlexGridLib.AlignmentSettings.flexAlignLeftBottom;
            //            flxParameters.Text = NumberDeposits;
            //        }

            //        i = Strings.InStr(FileLine, "Nr.Withdrawals");
            //        if ((i > 0))
            //        {
            //            i = Strings.InStr(FileLine, ",");
            //            NumberWithdrawals = Strings.Mid(FileLine, i + 2);
            //            NumberWithdrawals = InsertCommas(NumberWithdrawals);
            //            flxParameters.Row = 18;
            //            flxParameters.Col = 1;
            //            flxParameters.CellAlignment = MSFlexGridLib.AlignmentSettings.flexAlignLeftBottom;
            //            flxParameters.Text = NumberWithdrawals;
            //        }

            //    }


            //    TextNumDays.Text = Convert.ToString(g_SimulationLen / 10);
            //    TextSampleInterval.Text = Convert.ToString(20);

            //    short iMaxMnths = 0;
            //    iMaxMnths = g_SimulationLen / 5;
            //    LabelMaxMnths.Text = "(Max months " + iMaxMnths + ")";
            //    TextNumMnths.Text = Convert.ToString(iMaxMnths / 16);



            //=======================================================
            //Service provided by Telerik (www.telerik.com)
            //Conversion powered by NRefactory.
            //Twitter: @telerik
            //Facebook: facebook.com/telerik
            //=======================================================

        }

        private void JLSimOutput_Load(object sender, EventArgs e)
        {
            LastWorkingFolder = @"C:\JLMSim";
            textBoxFolder.Text = LastWorkingFolder;
        }
    }
}

