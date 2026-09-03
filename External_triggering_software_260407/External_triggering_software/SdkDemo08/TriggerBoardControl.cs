using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Windows.Forms;

namespace SdkDemo08
{
    internal sealed class TriggerBoardControl : UserControl
    {
        private const int LeftPanelWidth = 300;
        private const int RightPanelMinimumWidth = 250;

        private enum CommandKind
        {
            Connect,
            Disconnect,
            Configure,
            Start,
            Stop,
            PulseOnce,
            Refresh,
            ClearStats
        }

        private sealed class BoardCommand
        {
            public CommandKind Kind;
            public string PortName;
            public TriggerBoardParameters Parameters;
            public bool Manual;
        }

        private delegate void UiAction();

        private readonly ComboBox portCombo = new ComboBox();
        private readonly Button scanButton = new Button();
        private readonly Button connectButton = new Button();
        private readonly Button disconnectButton = new Button();
        private readonly Label connectionLabel = new Label();
        private readonly NumericUpDown periodInput = new NumericUpDown();
        private readonly NumericUpDown widthInput = new NumericUpDown();
        private readonly ComboBox outputPolarity = new ComboBox();
        private readonly ComboBox inputPolarity = new ComboBox();
        private readonly NumericUpDown countInput = new NumericUpDown();
        private readonly CheckBox continuousCheck = new CheckBox();
        private readonly Button applyButton = new Button();
        private readonly Button startButton = new Button();
        private readonly Button stopButton = new Button();
        private readonly Button pulseButton = new Button();
        private readonly Label stateLabel = new Label();
        private readonly NumericUpDown pollInput = new NumericUpDown();
        private readonly Button refreshButton = new Button();
        private readonly Button clearStatsButton = new Button();
        private readonly Label statsLabel = new Label();
        private readonly TriggerWaveformPanel waveform = new TriggerWaveformPanel();
        private readonly RichTextBox logBox = new RichTextBox();
        private readonly Button clearLogButton = new Button();
        private readonly SplitContainer mainSplit = new SplitContainer();

        private readonly Queue<BoardCommand> commandQueue = new Queue<BoardCommand>();
        private readonly AutoResetEvent commandEvent = new AutoResetEvent(false);
        private readonly ManualResetEvent workerStopped = new ManualResetEvent(false);
        private readonly Queue<string> logLines = new Queue<string>();
        private readonly Thread worker;
        private volatile bool shutdownRequested;
        private volatile int pollingIntervalMs = 500;

        private SerialPort boardPort;
        private TriggerBoardClient client;
        private bool workerConnected;
        private UInt32 lastEventCount;
        private bool hasLastStats;
        private string lastStatusSignature = String.Empty;
        private DateTime nextPollUtc = DateTime.MaxValue;

        public TriggerBoardControl()
        {
            Name = "triggerBoardControl";
            Dock = DockStyle.Fill;
            BackColor = Color.White;
            BuildInterface();
            LoadSettings();
            ScanPorts();
            SetConnectedUi(false);

            worker = new Thread(WorkerMain);
            worker.Name = "Trigger board serial worker";
            worker.IsBackground = true;
            worker.Start();
        }

        private void BuildInterface()
        {
            mainSplit.Dock = DockStyle.Fill;
            mainSplit.FixedPanel = FixedPanel.Panel1;
            mainSplit.IsSplitterFixed = true;
            mainSplit.SizeChanged += delegate { ApplyMainSplitLayout(); };
            Controls.Add(mainSplit);

            Panel left = mainSplit.Panel1;
            left.AutoScroll = true;
            BuildConnectionGroup(left);
            BuildOutputGroup(left);
            BuildStateGroup(left);
            BuildRightPanel(mainSplit.Panel2);
            ApplyMainSplitLayout();
        }

        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            ApplyMainSplitLayout();
        }

        private void ApplyMainSplitLayout()
        {
            int requiredWidth = LeftPanelWidth + RightPanelMinimumWidth + mainSplit.SplitterWidth;
            if (mainSplit.Width < requiredWidth)
                return;

            mainSplit.Panel1MinSize = LeftPanelWidth;
            mainSplit.Panel2MinSize = RightPanelMinimumWidth;
            if (mainSplit.SplitterDistance != LeftPanelWidth)
                mainSplit.SplitterDistance = LeftPanelWidth;
        }

        private void BuildConnectionGroup(Control parent)
        {
            GroupBox box = NewGroup("小板串口（115200 8N1）", 4, 4, 288, 82);
            parent.Controls.Add(box);
            portCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            portCombo.SetBounds(8, 21, 92, 22);
            scanButton.Text = "扫描";
            scanButton.SetBounds(104, 20, 52, 24);
            connectButton.Text = "连接";
            connectButton.SetBounds(160, 20, 56, 24);
            disconnectButton.Text = "断开";
            disconnectButton.SetBounds(220, 20, 56, 24);
            connectionLabel.Text = "未连接";
            connectionLabel.ForeColor = Color.DarkRed;
            connectionLabel.SetBounds(8, 51, 268, 20);
            box.Controls.AddRange(new Control[] { portCombo, scanButton, connectButton, disconnectButton, connectionLabel });
            scanButton.Click += delegate { ScanPorts(); };
            connectButton.Click += delegate { ConnectSelectedPort(); };
            disconnectButton.Click += delegate { EnqueueDisconnect(); };
        }

        private void BuildOutputGroup(Control parent)
        {
            GroupBox box = NewGroup("TrigIn 输出（GPIO2 → 相机 Trigger In）", 4, 90, 288, 224);
            parent.Controls.Add(box);

            ConfigureNumeric(periodInput, 20M, 42949672950M, 10M);
            ConfigureNumeric(widthInput, 10M, 42949672950M, 10M);
            ConfigureNumeric(countInput, 1M, 4294967295M, 1M);
            AddField(box, "周期 (ns)", periodInput, 22);
            AddField(box, "有效脉宽 (ns)", widthInput, 50);

            outputPolarity.DropDownStyle = ComboBoxStyle.DropDownList;
            outputPolarity.Items.AddRange(new object[] { "高有效", "低有效" });
            AddField(box, "输出极性", outputPolarity, 78);
            inputPolarity.DropDownStyle = ComboBoxStyle.DropDownList;
            inputPolarity.Items.AddRange(new object[] { "低有效", "高有效" });
            AddField(box, "TrigOut 捕获极性", inputPolarity, 106);
            AddField(box, "输出数量", countInput, 134);

            continuousCheck.Text = "连续输出 (count=0)";
            continuousCheck.SetBounds(121, 159, 155, 21);
            box.Controls.Add(continuousCheck);

            applyButton.Text = "应用参数";
            startButton.Text = "开始";
            stopButton.Text = "停止";
            pulseButton.Text = "单脉冲";
            applyButton.SetBounds(7, 187, 68, 27);
            startButton.SetBounds(78, 187, 61, 27);
            stopButton.SetBounds(142, 187, 61, 27);
            pulseButton.SetBounds(206, 187, 68, 27);
            box.Controls.AddRange(new Control[] { applyButton, startButton, stopButton, pulseButton });

            continuousCheck.CheckedChanged += delegate
            {
                countInput.Enabled = !continuousCheck.Checked;
                SaveSettings();
            };
            applyButton.Click += delegate { QueueConfiguredCommand(CommandKind.Configure); };
            startButton.Click += delegate { QueueConfiguredCommand(CommandKind.Start); };
            stopButton.Click += delegate { Enqueue(new BoardCommand { Kind = CommandKind.Stop }); };
            pulseButton.Click += delegate { QueueConfiguredCommand(CommandKind.PulseOnce); };
        }

        private void BuildStateGroup(Control parent)
        {
            GroupBox box = NewGroup("运行状态", 4, 318, 288, 126);
            parent.Controls.Add(box);
            stateLabel.Text = "等待连接";
            stateLabel.AutoEllipsis = true;
            stateLabel.SetBounds(8, 20, 270, 96);
            box.Controls.Add(stateLabel);
        }

        private void BuildRightPanel(Control parent)
        {
            waveform.SetBounds(4, 4, 332, 122);
            waveform.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            parent.Controls.Add(waveform);

            GroupBox statsBox = NewGroup("TrigOut 捕获（GPIO1 ← 相机 Trigger Out）", 4, 130, 332, 135);
            statsBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            parent.Controls.Add(statsBox);
            Label pollLabel = new Label();
            pollLabel.Text = "轮询(ms)";
            pollLabel.SetBounds(8, 22, 56, 20);
            ConfigureNumeric(pollInput, 100M, 5000M, 100M);
            pollInput.SetBounds(66, 19, 62, 22);
            refreshButton.Text = "立即刷新";
            refreshButton.SetBounds(134, 18, 72, 25);
            clearStatsButton.Text = "清除统计";
            clearStatsButton.SetBounds(210, 18, 72, 25);
            statsLabel.Text = "累计: 0    增量: 0\r\n周期: --    脉宽: --\r\n频率: --    占空比: --\r\n过窄: 0    超时: 否    溢出: 否";
            statsLabel.SetBounds(8, 49, 314, 78);
            statsBox.Controls.AddRange(new Control[] { pollLabel, pollInput, refreshButton, clearStatsButton, statsLabel });
            pollInput.ValueChanged += delegate
            {
                pollingIntervalMs = Decimal.ToInt32(pollInput.Value);
                SaveSettings();
                commandEvent.Set();
            };
            refreshButton.Click += delegate { Enqueue(new BoardCommand { Kind = CommandKind.Refresh, Manual = true }); };
            clearStatsButton.Click += delegate { Enqueue(new BoardCommand { Kind = CommandKind.ClearStats }); };

            Label logTitle = new Label();
            logTitle.Text = "外触发小板日志";
            logTitle.SetBounds(5, 270, 110, 20);
            clearLogButton.Text = "清空日志";
            clearLogButton.SetBounds(258, 266, 78, 25);
            clearLogButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            logBox.ReadOnly = true;
            logBox.BackColor = Color.White;
            logBox.Font = new Font("Consolas", 8.25F);
            logBox.WordWrap = false;
            logBox.SetBounds(4, 294, 332, 150);
            logBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            parent.Controls.AddRange(new Control[] { logTitle, clearLogButton, logBox });
            clearLogButton.Click += delegate
            {
                logLines.Clear();
                logBox.Clear();
            };
        }

        private static GroupBox NewGroup(string text, int x, int y, int width, int height)
        {
            GroupBox box = new GroupBox();
            box.Text = text;
            box.SetBounds(x, y, width, height);
            return box;
        }

        private static void ConfigureNumeric(NumericUpDown input, decimal minimum, decimal maximum, decimal increment)
        {
            input.Minimum = minimum;
            input.Maximum = maximum;
            input.Increment = increment;
            input.DecimalPlaces = 0;
            input.ThousandsSeparator = true;
        }

        private static void AddField(Control parent, string text, Control input, int y)
        {
            Label label = new Label();
            label.Text = text;
            label.SetBounds(8, y + 3, 112, 20);
            input.SetBounds(121, y, 155, 22);
            parent.Controls.Add(label);
            parent.Controls.Add(input);
        }

        private void ScanPorts()
        {
            string selected = portCombo.SelectedItem as string;
            if (String.IsNullOrEmpty(selected))
                selected = Properties.Settings.Default.TriggerBoardPort;
            string[] ports = SerialPort.GetPortNames();
            Array.Sort(ports);
            portCombo.Items.Clear();
            portCombo.Items.AddRange(ports);
            if (!String.IsNullOrEmpty(selected) && portCombo.Items.Contains(selected))
                portCombo.SelectedItem = selected;
            else if (ports.Length == 1)
                portCombo.SelectedIndex = 0;
            else if (ports.Length > 0)
                portCombo.SelectedIndex = 0;
            AppendLog("扫描串口：" + (ports.Length == 0 ? "未发现串口" : String.Join(", ", ports)));
        }

        private void ConnectSelectedPort()
        {
            string name = portCombo.SelectedItem as string;
            if (String.IsNullOrEmpty(name))
            {
                MessageBox.Show("请先选择外触发小板串口。", "Trig Board", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            Properties.Settings.Default.TriggerBoardPort = name;
            SaveSettings();
            SetConnectingUi();
            Enqueue(new BoardCommand { Kind = CommandKind.Connect, PortName = name });
        }

        private void EnqueueDisconnect()
        {
            lock (commandQueue)
            {
                commandQueue.Clear();
                commandQueue.Enqueue(new BoardCommand { Kind = CommandKind.Disconnect });
            }
            commandEvent.Set();
        }

        private void QueueConfiguredCommand(CommandKind kind)
        {
            try
            {
                UInt32 count = continuousCheck.Checked ? 0U : Decimal.ToUInt32(countInput.Value);
                TriggerBoardParameters parameters = TriggerBoardParameters.FromNanoseconds(
                    Decimal.ToInt64(periodInput.Value), Decimal.ToInt64(widthInput.Value), count,
                    outputPolarity.SelectedIndex == 1, inputPolarity.SelectedIndex == 0);
                SaveSettings();
                Enqueue(new BoardCommand { Kind = kind, Parameters = parameters });
            }
            catch (Exception ex)
            {
                AppendLog("参数错误：" + ex.Message);
                MessageBox.Show(ex.Message, "Trig Board 参数错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void Enqueue(BoardCommand command)
        {
            lock (commandQueue)
                commandQueue.Enqueue(command);
            commandEvent.Set();
        }

        private void WorkerMain()
        {
            try
            {
                while (!shutdownRequested)
                {
                    BoardCommand command = null;
                    lock (commandQueue)
                    {
                        if (commandQueue.Count > 0)
                            command = commandQueue.Dequeue();
                    }
                    if (command != null)
                    {
                        ProcessCommand(command);
                        continue;
                    }

                    if (workerConnected && DateTime.UtcNow >= nextPollUtc)
                    {
                        PollBoard(false);
                        nextPollUtc = DateTime.UtcNow.AddMilliseconds(pollingIntervalMs);
                        continue;
                    }

                    int wait = 250;
                    if (workerConnected)
                    {
                        double remaining = (nextPollUtc - DateTime.UtcNow).TotalMilliseconds;
                        wait = (int)Math.Max(20, Math.Min(remaining, 250));
                    }
                    commandEvent.WaitOne(wait, false);
                }
            }
            finally
            {
                StopAndClose(true);
                workerStopped.Set();
            }
        }

        private void ProcessCommand(BoardCommand command)
        {
            try
            {
                if (command.Kind == CommandKind.Connect)
                {
                    ConnectWorker(command.PortName);
                    return;
                }
                if (command.Kind == CommandKind.Disconnect)
                {
                    StopAndClose(false);
                    return;
                }
                if (!workerConnected || client == null)
                    throw new TriggerBoardException("外触发小板尚未连接。");

                switch (command.Kind)
                {
                    case CommandKind.Configure:
                        client.Configure(command.Parameters);
                        WorkerLog("参数已应用：周期=" + (command.Parameters.PeriodTicks * 10UL) +
                            " ns，脉宽=" + (command.Parameters.WidthTicks * 10UL) + " ns，输出=" +
                            (command.Parameters.OutputActiveLow ? "低有效" : "高有效") + "。");
                        PollBoard(true);
                        break;
                    case CommandKind.Start:
                        client.Configure(command.Parameters);
                        client.Start(command.Parameters.Count);
                        WorkerLog(command.Parameters.Count == 0 ? "开始连续输出。" :
                            "开始输出，目标数量=" + command.Parameters.Count + "。");
                        PollBoard(true);
                        break;
                    case CommandKind.Stop:
                        client.Stop();
                        WorkerLog("已发送 STOP，输出回到低电平。");
                        PollBoard(true);
                        break;
                    case CommandKind.PulseOnce:
                        client.Configure(command.Parameters);
                        client.PulseOnce();
                        WorkerLog("已发送单脉冲命令。");
                        PollBoard(true);
                        break;
                    case CommandKind.Refresh:
                        PollBoard(true);
                        break;
                    case CommandKind.ClearStats:
                        client.ClearStats();
                        hasLastStats = false;
                        lastEventCount = 0;
                        WorkerLog("TrigOut 捕获统计已清除。");
                        PollBoard(true);
                        break;
                }
            }
            catch (Exception ex)
            {
                WorkerLog("操作失败：" + ex.Message);
                if (IsConnectionFailure(ex))
                    ConnectionLost(ex.Message);
            }
        }

        private void ConnectWorker(string portName)
        {
            StopAndClose(false);
            try
            {
                boardPort = new SerialPort(portName, 115200, Parity.None, 8, StopBits.One);
                boardPort.Handshake = Handshake.None;
                boardPort.ReadTimeout = 600;
                boardPort.WriteTimeout = 600;
                boardPort.DtrEnable = false;
                boardPort.RtsEnable = false;
                boardPort.Open();
                boardPort.DiscardInBuffer();
                boardPort.DiscardOutBuffer();
                client = new TriggerBoardClient(boardPort);
                TriggerBoardInfo info = client.Ping();
                if (info.ProtocolVersion != TriggerBoardClient.Version)
                    throw new TriggerBoardException("小板协议版本不是 v1。");
                workerConnected = true;
                hasLastStats = false;
                lastStatusSignature = String.Empty;
                nextPollUtc = DateTime.UtcNow;
                WorkerLog("连接成功 " + portName + "；固件 " + info.FirmwareVersion +
                    "；时钟 " + info.ClockHz + " Hz。");
                Ui(delegate { SetConnectedUi(true); });
            }
            catch (Exception ex)
            {
                ClosePortOnly();
                WorkerLog("连接失败：" + ex.Message);
                Ui(delegate { SetConnectedUi(false); });
            }
        }

        private void PollBoard(bool manual)
        {
            if (!workerConnected || client == null)
                return;
            try
            {
                TriggerBoardStatus status = client.ReadStatus();
                TriggerBoardStats stats = client.ReadStats();
                UInt32 delta = 0;
                if (hasLastStats)
                    delta = stats.EventCount >= lastEventCount ? stats.EventCount - lastEventCount : stats.EventCount;
                bool countChanged = hasLastStats && stats.EventCount != lastEventCount;
                string signature = StatusSignature(status, stats);
                bool statusChanged = signature != lastStatusSignature;
                lastEventCount = stats.EventCount;
                hasLastStats = true;
                lastStatusSignature = signature;

                Ui(delegate { UpdateStatusUi(status, stats, delta); });
                if (manual || countChanged || statusChanged)
                {
                    string detail = "TrigOut：总数=" + stats.EventCount + "，增量=" + delta +
                        "，最近周期=" + (stats.LastPeriodTicks * 10UL) + " ns，最近脉宽=" +
                        (stats.LastWidthTicks * 10UL) + " ns，过窄=" + stats.TooNarrowCount;
                    if (stats.Timeout) detail += "，超时";
                    if (stats.Overflow) detail += "，溢出";
                    WorkerLog(detail + "。");
                }
            }
            catch (Exception ex)
            {
                WorkerLog("自动读取失败：" + ex.Message);
                ConnectionLost(ex.Message);
            }
        }

        private static string StatusSignature(TriggerBoardStatus status, TriggerBoardStats stats)
        {
            return status.Running + "/" + status.Configured + "/" + status.PendingUpdate + "/" +
                status.Precharge + "/" + status.Remaining + "/" + status.LastError + "/" +
                stats.Timeout + "/" + stats.Overflow + "/" + stats.TooNarrowCount;
        }

        private void UpdateStatusUi(TriggerBoardStatus status, TriggerBoardStats stats, UInt32 delta)
        {
            stateLabel.Text = "运行: " + YesNo(status.Running) + "    已配置: " + YesNo(status.Configured) +
                "\r\n待周期更新: " + YesNo(status.PendingUpdate) + "    低有效预充: " + YesNo(status.Precharge) +
                "\r\n剩余数量: " + status.Remaining +
                "\r\n当前周期/脉宽: " + (status.PeriodTicks * 10UL) + " / " +
                (status.WidthTicks * 10UL) + " ns\r\n最近错误: " + TriggerBoardClient.StatusText(status.LastError);

            string frequency = "--";
            string duty = "--";
            if (stats.LastPeriodTicks > 0)
            {
                frequency = (100000000.0 / stats.LastPeriodTicks).ToString("0.###") + " Hz";
                duty = (100.0 * stats.LastWidthTicks / stats.LastPeriodTicks).ToString("0.###") + "%";
            }
            statsLabel.Text = "累计: " + stats.EventCount + "    增量: " + delta +
                "\r\n周期: " + FormatNs(stats.LastPeriodTicks) + "    脉宽: " + FormatNs(stats.LastWidthTicks) +
                "\r\n频率: " + frequency + "    占空比: " + duty +
                "\r\n过窄: " + stats.TooNarrowCount + "    超时: " + YesNo(stats.Timeout) +
                "    溢出: " + YesNo(stats.Overflow);
            waveform.UpdateWaveform(stats.LastPeriodTicks, stats.LastWidthTicks, status.InputActiveLow);
        }

        private static string FormatNs(UInt32 ticks)
        {
            return ticks == 0 ? "--" : (ticks * 10UL).ToString() + " ns";
        }

        private static string YesNo(bool value) { return value ? "是" : "否"; }

        private bool IsConnectionFailure(Exception ex)
        {
            return ex is TimeoutException || ex is IOException || ex is InvalidOperationException || ex is UnauthorizedAccessException ||
                (ex.InnerException != null && IsConnectionFailure(ex.InnerException));
        }

        private void ConnectionLost(string reason)
        {
            ClosePortOnly();
            WorkerLog("连接已断开：" + reason);
            Ui(delegate { SetConnectedUi(false); });
        }

        private void StopAndClose(bool applicationExit)
        {
            if (client != null && boardPort != null && boardPort.IsOpen)
            {
                try
                {
                    client.Stop();
                    WorkerLog(applicationExit ? "关闭软件前已确认 STOP。" : "断开前已确认 STOP。");
                }
                catch (Exception ex)
                {
                    WorkerLog("警告：STOP 未确认，仍将关闭串口：" + ex.Message);
                }
            }
            ClosePortOnly();
            Ui(delegate { SetConnectedUi(false); });
        }

        private void ClosePortOnly()
        {
            workerConnected = false;
            nextPollUtc = DateTime.MaxValue;
            client = null;
            if (boardPort != null)
            {
                try { if (boardPort.IsOpen) boardPort.Close(); }
                catch { }
                try { boardPort.Dispose(); }
                catch { }
                boardPort = null;
            }
        }

        private void SetConnectingUi()
        {
            connectionLabel.Text = "正在连接...";
            connectionLabel.ForeColor = Color.DarkOrange;
            connectButton.Enabled = false;
            scanButton.Enabled = false;
            portCombo.Enabled = false;
        }

        private void SetConnectedUi(bool connected)
        {
            connectionLabel.Text = connected ? "已连接：" + (portCombo.SelectedItem as string) : "未连接";
            connectionLabel.ForeColor = connected ? Color.DarkGreen : Color.DarkRed;
            connectButton.Enabled = !connected;
            scanButton.Enabled = !connected;
            portCombo.Enabled = !connected;
            disconnectButton.Enabled = connected;
            applyButton.Enabled = connected;
            startButton.Enabled = connected;
            stopButton.Enabled = connected;
            pulseButton.Enabled = connected;
            refreshButton.Enabled = connected;
            clearStatsButton.Enabled = connected;
            if (!connected)
                stateLabel.Text = "等待连接";
        }

        private void Ui(UiAction action)
        {
            if (IsDisposed || Disposing)
                return;
            try
            {
                if (InvokeRequired)
                    BeginInvoke(action);
                else
                    action();
            }
            catch (InvalidOperationException) { }
        }

        private void WorkerLog(string message)
        {
            Ui(delegate { AppendLog(message); });
        }

        private void AppendLog(string message)
        {
            string line = DateTime.Now.ToString("HH:mm:ss.fff") + "  " + message;
            logLines.Enqueue(line);
            logBox.AppendText(line + Environment.NewLine);
            if (logLines.Count > 5000)
            {
                for (int i = 0; i < 500 && logLines.Count > 0; i++)
                    logLines.Dequeue();
                logBox.Lines = logLines.ToArray();
            }
            logBox.SelectionStart = logBox.TextLength;
            logBox.ScrollToCaret();
        }

        private void LoadSettings()
        {
            try
            {
                Properties.Settings settings = Properties.Settings.Default;
                periodInput.Value = Clamp(settings.TriggerPeriodNs, periodInput.Minimum, periodInput.Maximum);
                widthInput.Value = Clamp(settings.TriggerWidthNs, widthInput.Minimum, widthInput.Maximum);
                countInput.Value = Clamp(settings.TriggerCount, countInput.Minimum, countInput.Maximum);
                outputPolarity.SelectedIndex = settings.TriggerOutputActiveLow ? 1 : 0;
                inputPolarity.SelectedIndex = settings.TriggerInputActiveLow ? 0 : 1;
                continuousCheck.Checked = settings.TriggerContinuous;
                pollInput.Value = Clamp(settings.TriggerPollIntervalMs, pollInput.Minimum, pollInput.Maximum);
                pollingIntervalMs = Decimal.ToInt32(pollInput.Value);
                countInput.Enabled = !continuousCheck.Checked;
            }
            catch
            {
                periodInput.Value = 1000000M;
                widthInput.Value = 200M;
                countInput.Value = 1000M;
                outputPolarity.SelectedIndex = 0;
                inputPolarity.SelectedIndex = 0;
                continuousCheck.Checked = false;
                pollInput.Value = 500M;
            }
        }

        private static decimal Clamp(Int64 value, decimal minimum, decimal maximum)
        {
            decimal converted = value;
            if (converted < minimum) return minimum;
            if (converted > maximum) return maximum;
            return converted;
        }

        private void SaveSettings()
        {
            try
            {
                Properties.Settings settings = Properties.Settings.Default;
                settings.TriggerPeriodNs = Decimal.ToInt64(periodInput.Value);
                settings.TriggerWidthNs = Decimal.ToInt64(widthInput.Value);
                settings.TriggerCount = Decimal.ToInt64(countInput.Value);
                settings.TriggerOutputActiveLow = outputPolarity.SelectedIndex == 1;
                settings.TriggerInputActiveLow = inputPolarity.SelectedIndex == 0;
                settings.TriggerContinuous = continuousCheck.Checked;
                settings.TriggerPollIntervalMs = Decimal.ToInt32(pollInput.Value);
                settings.Save();
            }
            catch (Exception ex)
            {
                AppendLog("保存设置失败：" + ex.Message);
            }
        }

        public void Shutdown()
        {
            if (shutdownRequested)
                return;
            SaveSettings();
            shutdownRequested = true;
            commandEvent.Set();
            if (!workerStopped.WaitOne(1700, false))
            {
                try { if (boardPort != null && boardPort.IsOpen) boardPort.Close(); }
                catch { }
                commandEvent.Set();
                workerStopped.WaitOne(500, false);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Shutdown();
                if (workerStopped.WaitOne(0, false))
                {
                    commandEvent.Close();
                    workerStopped.Close();
                }
            }
            base.Dispose(disposing);
        }
    }

    internal sealed class TriggerWaveformPanel : Panel
    {
        private UInt32 periodTicks;
        private UInt32 widthTicks;
        private bool activeLow;

        public TriggerWaveformPanel()
        {
            DoubleBuffered = true;
            BackColor = Color.WhiteSmoke;
            BorderStyle = BorderStyle.FixedSingle;
        }

        public void UpdateWaveform(UInt32 period, UInt32 width, bool isActiveLow)
        {
            periodTicks = period;
            widthTicks = width;
            activeLow = isActiveLow;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            using (Brush textBrush = new SolidBrush(Color.Black))
            {
                g.DrawString("TrigOut 最近捕获波形", Font, textBrush, 6, 5);
                using (Font noteFont = new Font(Font, FontStyle.Italic))
                    g.DrawString("统计重建，非示波器采样", noteFont, Brushes.DimGray, 165, 5);
            }
            if (periodTicks == 0 || widthTicks == 0 || widthTicks >= periodTicks)
            {
                g.DrawString("暂无有效捕获", Font, Brushes.Gray, 106, 56);
                return;
            }

            int left = 12;
            int right = Math.Max(left + 40, ClientSize.Width - 12);
            int highY = 43;
            int lowY = 83;
            int cycleWidth = (right - left) / 2;
            int pulseWidth = Math.Max(2, (int)(cycleWidth * ((double)widthTicks / periodTicks)));
            int activeY = activeLow ? lowY : highY;
            int idleY = activeLow ? highY : lowY;
            using (Pen axis = new Pen(Color.LightGray))
            using (Pen wave = new Pen(Color.RoyalBlue, 2F))
            {
                g.DrawLine(axis, left, highY, right, highY);
                g.DrawLine(axis, left, lowY, right, lowY);
                int x = left;
                for (int cycle = 0; cycle < 2; cycle++)
                {
                    g.DrawLine(wave, x, idleY, x, activeY);
                    g.DrawLine(wave, x, activeY, x + pulseWidth, activeY);
                    g.DrawLine(wave, x + pulseWidth, activeY, x + pulseWidth, idleY);
                    g.DrawLine(wave, x + pulseWidth, idleY, x + cycleWidth, idleY);
                    x += cycleWidth;
                }
            }
            string detail = "周期 " + (periodTicks * 10UL) + " ns    脉宽 " +
                (widthTicks * 10UL) + " ns    " + (activeLow ? "低有效" : "高有效");
            g.DrawString(detail, Font, Brushes.Black, 8, 96);
        }
    }
}
