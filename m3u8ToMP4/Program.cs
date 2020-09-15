using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;

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

            string title = "m3u8 -> mp4 | mp3 | m4a";
            string clip = "";

            // 起動時にFFmpegの存在チェック
            if (!File.Exists("ffmpeg.exe"))
            {
                MessageBox.Show("FFmpegが存在しません。m3u8ToMP4を再度ダウンロードしてください。", title);
                return;
            }

            IDataObject data = Clipboard.GetDataObject();

            // クリップボード文字列を取得
            if (data.GetDataPresent(DataFormats.Text))
            {
                clip = data.GetData(DataFormats.Text) as string;
            }

            // クリップボード文字列が有効かチェック(ファイルの存在チェックはしない)
            if ((clip.IndexOf(".m3u8", StringComparison.OrdinalIgnoreCase) < 0) || (clip.IndexOf(":") < 0))
            {
                MessageBox.Show("クリップボードにコピーされたURL・パスが、*.m3u8ファイルのものではありません。\n" +
                                "*.m3u8ファイルを含んだURL・パスをコピーしてから、もう一度やり直してください。\n" +
                                "\n" +
                                "クリップボードの内容: " + clip, title);
                return;
            }

            SaveFileDialog sfd = new SaveFileDialog();

            sfd.Filter = "MP4ファイル(*.mp4)|*.mp4|MP3ファイル(*.mp3)|*.mp3|M4Aファイル(*.m4a)|*.m4a|すべてのファイル(*.*)|*.*";
            sfd.FilterIndex = 1;
            sfd.RestoreDirectory = true;

            // 保存ダイアログにて保存をキャンセルした場合はそのまま終了
            if (sfd.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            string extension = Path.GetExtension(sfd.FileName);
            string filename = Path.GetFileName(clip);

            ProcessStartInfo app = new ProcessStartInfo();

            // m3u8がローカルファイルだった場合
            if (Regex.IsMatch(Path.GetPathRoot(clip), "[a-zA-Z]"))
            {
                // 念のためパスが有効かチェック(ローカルの場合存在しないとコピー作成時に例外が発生するため)
                if (!File.Exists(clip))
                {
                    MessageBox.Show("クリップボードにコピーされたパスが無効です。", title);
                    return;
                }

                // カレントディレクトリをローカルパスへ移動
                app.WorkingDirectory = Path.GetDirectoryName(clip);

                // m3u8ファイルのコピーを作成
                File.Copy(clip, Path.GetDirectoryName(clip) + "\\_" + filename, true);

                // 読み取った文字列にもコピー先のパスを反映
                clip = Path.GetDirectoryName(clip) + "\\_" + filename;

                // 強制的にm3u8ファイル終端を追記する
                File.AppendAllText(clip, Environment.NewLine + "#EXT-X-ENDLIST");
            }

            // 各種設定
            app.FileName = "cmd.exe";
            // cmdからFFmpeg呼び出し
            app.Arguments = "/c \"\"" + Environment.CurrentDirectory + "\\ffmpeg.exe\" ";
            // FFmpegの引数設定
            app.Arguments += "-protocol_whitelist crypto,file,http,https,tcp,tls -allowed_extensions ALL -analyzeduration 1G -probesize 1G -i \"" + clip + "\" ";

            if (extension == ".mp3")
            {
                app.Arguments += "-write_xing 0";
            }
            else if (extension == ".m4a")
            {
                app.Arguments += "-c copy";
            }
            else
            {
                app.Arguments += "-movflags faststart -c copy -bsf:a aac_adtstoasc";
            }

            app.Arguments += " \"" + sfd.FileName + "\" -y\"";
            app.UseShellExecute = true;

            // 実行直前にもFFmpegの存在チェック
            if (!File.Exists("ffmpeg.exe"))
            {
                MessageBox.Show("FFmpegが存在しません。", title);
                return;
            }

            Process proc = Process.Start(app);

            // 終了待ち処理
            proc.WaitForExit();

            // 作成したコピーの後処理
            if (File.Exists(clip))
            {
                File.Delete(clip);
            }
        }
    }
}
