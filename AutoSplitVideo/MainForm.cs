﻿using MaterialSkin;
using MaterialSkin.Controls;
using MediaToolkit;
using MediaToolkit.Model;
using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AutoSplitVideo
{
	public partial class MainForm : MaterialForm
	{
		public MainForm()
		{
			InitializeComponent();

			var materialSkinManager = MaterialSkinManager.Instance;
			materialSkinManager.AddFormToManage(this);
			materialSkinManager.ROBOTO_MEDIUM_10 = new Font(@"SimHei", 11f, FontStyle.Bold);
			materialSkinManager.Theme = MaterialSkinManager.Themes.LIGHT;
			materialSkinManager.ColorScheme = new ColorScheme(Primary.Indigo500, Primary.Indigo700, Primary.Indigo100, Accent.Pink200, TextShade.WHITE);
		}

		private delegate void VoidMethod_Delegate();

		private TimeSpan Duration => TimeSpan.FromMinutes(Convert.ToDouble(numericUpDown1.Value));
		private ulong Limit => Convert.ToUInt64(numericUpDown2.Value * 1024 * 1024 * 8);

		private void materialRaisedButton1_Click(object sender, EventArgs e)
		{
			try
			{
				SetControlEnable(false);
				var inputVideoPath = InputVideoPath.Text;
				var outputDirectoryPath = OutputVideoPath.Text;
				var runtask = new Task(() =>
				{
					ShowVideoInfo(inputVideoPath);
					SetprogressBar(0);
					var inputFile = new MediaFile(inputVideoPath);
					var outputFile = new MediaFile();
					var mp4File = new MediaFile($@"{outputDirectoryPath}{Path.GetFileNameWithoutExtension(inputVideoPath)}.mp4");
					using (var engine = new Engine())
					{
						//flv转封装成MP4
						if (mp4File.Filename != inputFile.Filename)
						{
							engine.CustomCommand($@"-i {inputFile.Filename} -c copy -copyts {mp4File.Filename}");
						}
						SetprogressBar(50);

						//分段
						ShowVideoInfo(mp4File.Filename);
						engine.GetMetadata(mp4File);
						var vb = mp4File.Metadata.VideoData.BitRateKbs ?? 0;
						var ab = mp4File.Metadata.AudioData.BitRateKbs;
						var maxDuration = TimeSpan.FromSeconds(Convert.ToDouble(Limit) / (vb + ab));
						var duration = mp4File.Metadata.Duration;
						var now = TimeSpan.Zero;
						for (var i = 0; now < duration; ++i)
						{
							var t = Duration;
							if (now + maxDuration >= duration)
							{
								t = duration - now;
							}

							outputFile.Filename = $@"{outputDirectoryPath}{Path.GetFileNameWithoutExtension(mp4File.Filename)}_{i + 1}.mp4";

							engine.CustomCommand($@"-ss {now} -t {t} -accurate_seek -i {mp4File.Filename} -codec copy -avoid_negative_ts 1 {outputFile.Filename}");

							engine.GetMetadata(outputFile);
							now += outputFile.Metadata.Duration;

							SetprogressBar(50 + Convert.ToInt32(Convert.ToDouble(now.Ticks) / duration.Ticks * 50));
						}
					}

					MessageBox.Show(@"完成！", @"提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
				});
				runtask.Start();
				runtask.ContinueWith(task =>
				{
					BeginInvoke(new VoidMethod_Delegate(() =>
					{
						SetControlEnable(true);
					}));
				});
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, @"错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void InputVideoPath_DragDrop(object sender, DragEventArgs e)
		{
			var path = ((Array)e.Data.GetData(DataFormats.FileDrop)).GetValue(0).ToString();
			if (Directory.Exists(path))
			{
				OutputVideoPath.Text = $@"{Path.GetFullPath(path)}\";
			}
			else if (File.Exists(path))
			{
				InputVideoPath.Text = Path.GetFullPath(path);
				OutputVideoPath.Text = $@"{Path.GetDirectoryName(path)}\";
			}
		}

		private void FilePath_DragEnter(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				e.Effect = DragDropEffects.Link;
			}
			else
			{
				e.Effect = DragDropEffects.None;
			}
		}

		private void MainForm_Load(object sender, EventArgs e)
		{

		}

		private void ShowVideoInfo(string path)
		{
			var inputFile = new MediaFile { Filename = path };

			using (var engine = new Engine())
			{
				engine.GetMetadata(inputFile);
			}

			var sb = new StringBuilder();
			sb.AppendLine($@"时长：{inputFile.Metadata.Duration}");

			sb.AppendLine($@"音频码率：{inputFile.Metadata.AudioData.BitRateKbs}");
			sb.AppendLine($@"音频格式：{inputFile.Metadata.AudioData.Format}");
			sb.AppendLine($@"声道：{inputFile.Metadata.AudioData.ChannelOutput}");
			sb.AppendLine($@"采样率：{inputFile.Metadata.AudioData.SampleRate}");

			sb.AppendLine($@"视频码率：{inputFile.Metadata.VideoData.BitRateKbs}");
			sb.AppendLine($@"视频格式：{inputFile.Metadata.VideoData.Format}");
			sb.AppendLine($@"颜色模型：{inputFile.Metadata.VideoData.ColorModel}");
			sb.AppendLine($@"帧率：{inputFile.Metadata.VideoData.Fps}");
			sb.AppendLine($@"分辨率：{inputFile.Metadata.VideoData.FrameSize}");

			infoTextBox.BeginInvoke(new VoidMethod_Delegate(() =>
			{
				infoTextBox.Text = sb.ToString();
			}));
		}

		private void infoTextBox_DragDrop(object sender, DragEventArgs e)
		{
			var path = ((Array)e.Data.GetData(DataFormats.FileDrop)).GetValue(0).ToString();
			ShowVideoInfo(path);
		}

		private void SetControlEnable(bool b)
		{
			materialRaisedButton1.Enabled = b;
			InputVideoPath.Enabled = b;
			OutputVideoPath.Enabled = b;
			numericUpDown1.Enabled = b;
			numericUpDown2.Enabled = b;
		}

		private void SetprogressBar(int i)
		{
			progressBar.BeginInvoke(new VoidMethod_Delegate(() =>
			{
				progressBar.Value = i;
			}));
		}
	}
}