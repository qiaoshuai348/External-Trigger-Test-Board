using System;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace SdkDemo08
{
    internal static class TriggerBoardUiSelfTest
    {
        private static int passed;

        private static void Check(bool condition, string name)
        {
            if (!condition)
                throw new Exception("FAILED: " + name);
            passed++;
        }

        private static Control FindByText(Control root, string text)
        {
            foreach (Control child in root.Controls)
            {
                if (child.Text == text)
                    return child;
                Control nested = FindByText(child, text);
                if (nested != null)
                    return nested;
            }
            return null;
        }

        private static SplitContainer FindSplitContainer(Control root)
        {
            SplitContainer split = root as SplitContainer;
            if (split != null)
                return split;
            foreach (Control child in root.Controls)
            {
                split = FindSplitContainer(child);
                if (split != null)
                    return split;
            }
            return null;
        }

        [STAThread]
        public static int Main()
        {
            Application.EnableVisualStyles();
            Console.WriteLine("Assembly: " + typeof(Form1).Assembly.Location);
            Form1 form = new Form1();
            try
            {
                FieldInfo field = typeof(Form1).GetField("tabLVDS", BindingFlags.Instance | BindingFlags.NonPublic);
                TabControl tabs = (TabControl)field.GetValue(form);
                int qhyIndex = -1;
                int boardIndex = -1;
                int testIndex = -1;
                for (int i = 0; i < tabs.TabPages.Count; i++)
                {
                    Console.WriteLine(i + ": [" + tabs.TabPages[i].Text + "]");
                    if (tabs.TabPages[i].Text.Trim() == "QHY461_lite") qhyIndex = i;
                    if (tabs.TabPages[i].Text.Trim() == "Trig Board") boardIndex = i;
                    if (tabs.TabPages[i].Text.Trim() == "TEST") testIndex = i;
                }
                Check(qhyIndex >= 0 && boardIndex == qhyIndex + 1, "tab follows QHY461_lite");
                Check(testIndex < 0 || boardIndex < testIndex, "tab precedes TEST");
                TabPage page = tabs.TabPages[boardIndex];
                Check(page.Controls.Count == 1 && page.Controls[0].Name == "triggerBoardControl", "dedicated user control");
                Check(page.Controls[0].Dock == DockStyle.Fill, "control fills page");
                form.StartPosition = FormStartPosition.Manual;
                form.Left = -2000;
                form.Top = -2000;
                tabs.SelectedIndex = boardIndex;
                form.Show();
                Application.DoEvents();
                form.PerformLayout();
                page.PerformLayout();
                page.Controls[0].PerformLayout();
                SplitContainer split = FindSplitContainer(page);
                Console.WriteLine("Trigger page width=" + page.ClientSize.Width +
                    ", split width=" + (split == null ? -1 : split.Width) +
                    ", left width=" + (split == null ? -1 : split.Panel1.ClientSize.Width) +
                    ", distance=" + (split == null ? -1 : split.SplitterDistance));
                Check(split != null && split.SplitterDistance == 300, "left panel keeps 300 px after final layout");
                Check(split != null && split.Panel1.ClientSize.Width >= 296, "left controls have usable width");
                Check(FindByText(page, "应用参数") != null, "apply button");
                Check(FindByText(page, "开始") != null, "start button");
                Check(FindByText(page, "停止") != null, "stop button");
                Check(FindByText(page, "单脉冲") != null, "pulse button");
                Check(FindByText(page, "立即刷新") != null, "refresh button");
                Check(FindByText(page, "清除统计") != null, "clear stats button");
                Check(FindByText(page, "统计重建，非示波器采样") == null, "waveform note is custom drawn");
                Console.WriteLine("TriggerBoardUiSelfTest passed: " + passed);
                return 0;
            }
            finally
            {
                form.Dispose();
            }
        }
    }
}
