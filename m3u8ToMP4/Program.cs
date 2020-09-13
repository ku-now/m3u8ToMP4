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

            // 起動時にFFmpegの存在チェック
            if (!File.Exists("ffmpeg.exe"))
            {
                MessageBox.Show("FFmpegが存在しません。m3u8ToMP4を再度ダウンロードしてください。", "m3u8 -> mp4 | mp3 | m4a");
                return;
            }

            IDataObject data = Clipboard.GetDataObject();

            string clip = "";

            // クリップボード文字列を取得
            if (data.GetDataPresent(DataFormats.Text))
            {
                clip = data.GetData(DataFormats.Text) as string;
            }

            // クリップボード文字列が有効かチェック
            if ((clip.IndexOf(".m3u8", StringComparison.OrdinalIgnoreCase) < 0) || (clip.IndexOf(":") < 0))
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

            // 保存ダイアログにて保存を選択した場合の処理
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                string extension = Path.GetExtension(sfd.FileName);
                string filename = Path.GetFileName(clip);

                ProcessStartInfo app = new ProcessStartInfo();

                // m3u8がローカルファイルだった場合
                if (System.Text.RegularExpressions.Regex.IsMatch(Path.GetPathRoot(clip), "[a-zA-Z]"))
                {
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
                app.Arguments += "-protocol_whitelist file,http,https,tcp,tls,crypto -allowed_extensions ALL -analyzeduration 1G -probesize 1G -i \"" + clip + "\" ";

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

                app.Arguments += " \"" + sfd.FileName + "\"\" -y";
                app.UseShellExecute = true;

                // 実行直前にもFFmpegの存在チェック
                if (!File.Exists("ffmpeg.exe"))
                {
                    MessageBox.Show("FFmpegが存在しません。", "m3u8 -> mp4 | mp3 | m4a");
                    return;
                }

                Process.Start(app);

                // 終了待ち処理…のはずですが妥協しました
                MessageBox.Show("<< 注意 >>\n" +
                                "\n" +
                                "このダイアログは、必ず変換処理が終わってから閉じてください。\n" +
                                "\n" +
                                "このダイアログを閉じると一時ファイルが消去されるため、\n" +
                                "変換処理の途中で閉じてしまうと変換に失敗することがあります。", "m3u8 -> mp4 | mp3 | m4a");

                // 作成したコピーの後処理
                if (File.Exists(clip))
                {
                    File.Delete(clip);
                }
            }
            // 保存をキャンセルした場合はそのまま終了
        }
    }
}
