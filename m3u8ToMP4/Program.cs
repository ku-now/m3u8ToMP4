using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.IO;

namespace m3u8ToMP4
{
    internal static class Program
    {
        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            //Application.Run(new Form1());

            if (!File.Exists("ffmpeg.exe"))
            {
                MessageBox.Show("FFmpegが存在しません。m3u8ToMP4を再度ダウンロードしてください。", "m3u8 -> mp4 | mp3 | m4a");
                return;
            }

            IDataObject data = Clipboard.GetDataObject();

            string clip = "";

            if (data.GetDataPresent(DataFormats.Text))
            {
                clip = data.GetData(DataFormats.Text) as string;
            }

            if ((Path.GetExtension(clip) != ".m3u8") ||
                (clip.IndexOf("http", StringComparison.OrdinalIgnoreCase) < 0))
            {
                MessageBox.Show("クリップボードにコピーされたURLがm3u8ファイルのものでないか、URLではありません。\n" +
                                "m3u8ファイルを含んだURLをコピーしてから、もう一度やり直してください。\n" +
                                "\n" +
                                "クリップボードの内容: " + clip, "m3u8 -> mp4 | mp3 | m4a");
                return;
            }
            
            SaveFileDialog sfd = new SaveFileDialog();

            sfd.Filter = "MP4ファイル(*.mp4)|*.mp4|MP3ファイル(*.mp3)|*.mp3|M4Aファイル(*.m4a)|*.m4a|すべてのファイル(*.*)|*.*";
            sfd.FilterIndex = 1;
            sfd.RestoreDirectory = true;

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                string text = Path.GetExtension(sfd.FileName);

                ProcessStartInfo app = new ProcessStartInfo();

                app.FileName = "ffmpeg.exe";

                if (text == ".mp3")
                {
                    app.Arguments = "-i \"" + clip + "\" -write_xing 0 \"" + sfd.FileName + "\"";
                }
                else if (text == ".m4a")
                {
                    app.Arguments = "-i \"" + clip + "\" -c copy \"" + sfd.FileName + "\"";
                }
                else
                {
                    app.Arguments = "-protocol_whitelist file,http,https,tcp,tls,crypto -i \"" + clip + "\" " +
                                    "-headers \'User-Agent:\"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/83.0.4103.116 Safari/537.36\"\' " +
                                    "-movflags faststart -c copy -bsf:a aac_adtstoasc \"" + sfd.FileName + "\"";
                }

                app.UseShellExecute = true;

                Process.Start(app);
            }
        }
    }
}
