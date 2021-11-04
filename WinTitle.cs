using System;
using System.Runtime.InteropServices;
using Dalamud.Plugin;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Logging;

namespace WinTitle
{
    public class WinTitle : IDalamudPlugin
    {
        [DllImport("user32.dll")]
        static extern int SetWindowText(IntPtr hWnd, string text);

        private readonly string OriginalTitle;
        private readonly IntPtr Handle;

        public string Name => "Window Title Changer";

        [PluginService] private DalamudPluginInterface PluginInterface { get; set; }
        private CommandManager commandManager { get; init; }

        public WinTitle(CommandManager commandManager)
        {
            using Process process = Process.GetCurrentProcess();
            this.Handle = process.MainWindowHandle;
            this.OriginalTitle = process.MainWindowTitle;
            this.commandManager = commandManager;

            this.commandManager.AddHandler("/wintitle", new CommandInfo(WintitleCommand)
            {
                ShowInHelp = true,
                HelpMessage = "改变窗口标题",
            });
        }

        static bool ContainChinese(string input)
        {
            string pattern = "[\u4e00-\u9fbb]";
            return Regex.IsMatch(input, pattern);
        }

        public static string GetHanNumFromString(string str)
        {
            int count = 0;
            int addNum = 0;
            string add = "";
            Regex regex = new Regex(@"^[\u4E00-\u9FA5]{0,}$");

            for (int i = 0; i < str.Length; i++)
            {
                if (regex.IsMatch(str[i].ToString()))
                {
                    count++;
                }
            }

            // 只要存在汉字，则执行
            if (count > 0)
            {
                addNum = 2 * count - 1;
            }

            for (int i = 0; i < addNum; i++)
            {
                add = add + "1";
            }

            return add;
        }

        // user32dll的SetWindowText函数有问题
        // 一个汉字会吞掉1个结尾字符
        // 2个汉字吞掉3个
        // 3个吞掉5个
        // 4个吞掉7个
        // 可知字符串中有n个汉字，会吞掉2n-1个字符
        public void SetTitle(string title)
        {
            //if (string.IsNullOrWhiteSpace(title)) title = this.OriginalTitle;
            if (string.IsNullOrWhiteSpace(title)) title = "最终幻想XIV";
            try
            {
                if (this.OriginalTitle.Equals("最终幻想XIV"))
                {
                    SetWindowText(this.Handle, title);
                }
                else
                {

                    SetWindowText(this.Handle, title + GetHanNumFromString(title));
                }
            }
            catch (Exception ex)
            {
                PluginLog.LogError(ex, $"不能将标题设置为{title}");
            }
        }

        private void WintitleCommand(string _, string title) => this.SetTitle(title);

        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            if (this.IsDisposed) return;
            this.IsDisposed = true;

            this.SetTitle(null);
            commandManager.RemoveHandler("/wintitle");
            this.PluginInterface.Dispose();

            GC.SuppressFinalize(this);
        }

        ~WinTitle() => this.Dispose();
    }
}