using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Sanford.Multimedia.Midi;
using Sanford.Multimedia.Midi.UI;

namespace SequencerDemo
{
    public partial class Form1 : Form
    {
        #region origin


        private bool scrolling = false;
        private bool playing = false;
        private bool closing = false;
        private OutputDevice outDevice;
        private int outDeviceID = 0;
        private OutputDeviceDialog outDialog = new OutputDeviceDialog();
        public Form1()
        {
            InitializeComponent();            
        }
        protected override void OnLoad(EventArgs e)
        {
            if(OutputDevice.DeviceCount == 0)
            {
                MessageBox.Show("No MIDI output devices available.", "Error!",
                    MessageBoxButtons.OK, MessageBoxIcon.Stop);

                Close();
            }
            else
            {
                try
                {
                    outDevice = new OutputDevice(outDeviceID);

                    sequence1.LoadProgressChanged += HandleLoadProgressChanged;
                    sequence1.LoadCompleted += HandleLoadCompleted;
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error!",
                        MessageBoxButtons.OK, MessageBoxIcon.Stop);

                    Close();
                }
            }

            base.OnLoad(e);
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            pianoControl1.PressPianoKey(e.KeyCode);

            base.OnKeyDown(e);
        }
        protected override void OnKeyUp(KeyEventArgs e)
        {
            pianoControl1.ReleasePianoKey(e.KeyCode);

            base.OnKeyUp(e);
        }
        protected override void OnClosing(CancelEventArgs e)
        {
            closing = true;

            base.OnClosing(e);
        }
        protected override void OnClosed(EventArgs e)
        {
            sequence1.Dispose();

            if(outDevice != null)
            {
                outDevice.Dispose();
            }

            outDialog.Dispose();

            base.OnClosed(e);
        }
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(openMidiFileDialog.ShowDialog() == DialogResult.OK)
            {
                string fileName = openMidiFileDialog.FileName;
                Open(fileName);
            }
        }
        public void Open(string fileName)
        {
            try
            {
                sequencer1.Stop();
                playing = false;
                sequence1.LoadAsync(fileName);
                this.Cursor = Cursors.WaitCursor;
                startButton.Enabled = false;
                continueButton.Enabled = false;
                stopButton.Enabled = false;
                openToolStripMenuItem.Enabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
        }
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }
        private void outputDeviceToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutDialog dlg = new AboutDialog();

            dlg.ShowDialog();
        }
        private void stopButton_Click(object sender, EventArgs e)
        {
            try
            {
                playing = false;
                sequencer1.Stop();
                timer1.Stop();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
        }
        private void startButton_Click(object sender, EventArgs e)
        {
            try
            {
                playing = true;
                sequencer1.Start();
                timer1.Start();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
        }
        private void continueButton_Click(object sender, EventArgs e)
        {
            try
            {
                playing = true;
                sequencer1.Continue();
                timer1.Start();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
        }
        private void positionHScrollBar_Scroll(object sender, ScrollEventArgs e)
        {
            if(e.Type == ScrollEventType.EndScroll)
            {
                sequencer1.Position = e.NewValue;

                scrolling = false;
            }
            else
            {
                scrolling = true;
            }
        }
        private void HandleLoadProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            toolStripProgressBar1.Value = e.ProgressPercentage;
        }
        private void HandleLoadCompleted(object sender, AsyncCompletedEventArgs e)
        {
            this.Cursor = Cursors.Arrow;
            startButton.Enabled = true;
            continueButton.Enabled = true;
            stopButton.Enabled = true;
            openToolStripMenuItem.Enabled = true;
            toolStripProgressBar1.Value = 0;

            if(e.Error == null)
            {
                positionHScrollBar.Value = 0;
                positionHScrollBar.Maximum = sequence1.GetLength();
            }
            else
            {
                MessageBox.Show(e.Error.Message);
            }

            //�����ļ���������������startButton_Click�¼�
            startButton_Click(sender, e);
        }
        private void HandleChannelMessagePlayed(object sender, ChannelMessageEventArgs e)
        {
            if(closing)
            {
                return;
            }

            outDevice.Send(e.Message);
            pianoControl1.Send(e.Message);
        }
        private void HandleChased(object sender, ChasedEventArgs e)
        {
            foreach(ChannelMessage message in e.Messages)
            {
                outDevice.Send(message);
            }
        }
        private void HandleSysExMessagePlayed(object sender, SysExMessageEventArgs e)
        {
       //     outDevice.Send(e.Message); Sometimes causes an exception to be thrown because the output device is overloaded.
        }
        private void HandleStopped(object sender, StoppedEventArgs e)
        {
            foreach(ChannelMessage message in e.Messages)
            {
                outDevice.Send(message);
                pianoControl1.Send(message);
            }
        }
        private void HandlePlayingCompleted(object sender, EventArgs e)
        {
            timer1.Stop();

            //playNext();
            //һ�ײ�����Ϻ�����������һ�ף�����ί�����̵߳��ã������̳߳�ͻ
            this.BeginInvoke(new playNextDG(playNext));
        }
        private void pianoControl1_PianoKeyDown(object sender, PianoKeyEventArgs e)
        {
            #region Guard

            if(playing)
            {
                return;
            }

            #endregion

            outDevice.Send(new ChannelMessage(ChannelCommand.NoteOn, 0, e.NoteID, 127));
        }
        private void pianoControl1_PianoKeyUp(object sender, PianoKeyEventArgs e)
        {
            #region Guard

            if(playing)
            {
                return;
            }

            #endregion

            outDevice.Send(new ChannelMessage(ChannelCommand.NoteOff, 0, e.NoteID, 0));
        }
        private void timer1_Tick(object sender, EventArgs e)
        {
            if(!scrolling)
            {
                positionHScrollBar.Value = Math.Min(sequencer1.Position, positionHScrollBar.Maximum);
            }
        }


        #endregion

        #region MyRegion

        AutoSizeFormClass asc = new AutoSizeFormClass();
        private void Form1_Load(object sender, EventArgs e)
        {
            asc.controllInitializeSize(this);
        }
        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            asc.controlAutoSize(this);
        }
        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.All;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        // https://stackoverflow.com/questions/14430979/why-backgroundworker-always-is-busy
        //����һ
        //while (sequence1.IsBusy)
        //{
        //    Application.DoEvents();
        //    System.Threading.Thread.Sleep(100);
        //}
        //startButton_Click(sender, e);
        //������������LoadCompleted�¼�

        private int count;               //��ǰ���ŵĸ���
        FileInfo[] playlist;             //�������·����Ϣ
        Dictionary<string, int> songid;  //���������playlist���id

        /// <summary>
        /// ��ק�ļ����ţ������һ��·�����������midi�ļ�ȫ�����룬�����һ���ļ���ֱ�Ӷ���
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            //��ȡ�ļ�·��
            string fileName = ((string[])e.Data.GetData(DataFormats.FileDrop))[0];

            if (Directory.Exists(fileName)) //·��
            {
                playlist = new DirectoryInfo(fileName).GetFiles("*.mid");
                songid = new Dictionary<string, int>();
                listBox1.Items.Clear();
                for (int i=0; i<playlist.Length; i++)
                {
                    string name = Path.GetFileNameWithoutExtension(playlist[i].FullName);
                    string fullname = playlist[i].FullName;
                    listBox1.Items.Add(name);
                    songid[name] = i;
                }
            }
            else if (File.Exists(fileName)) //�ļ�
            {
                playlist = new FileInfo[1];
                playlist[0] = new FileInfo(fileName);
                songid = new Dictionary<string, int>();
                string name = Path.GetFileNameWithoutExtension(fileName);
                string fullname = fileName;
                listBox1.Items.Clear();
                listBox1.Items.Add(name);
                songid[name] = 0;
            }

            //������һ��
            count = sequential ? 0 : rd.Next(0, playlist.Length - 1);
            playNext();
        }

        private delegate void playNextDG();
        /// <summary>
        /// ������һ��
        /// </summary>
        private void playNext()
        {
            listBox1.SetSelected(count, true);
            Open(playlist[count].FullName);
            if (sequential)
            {
                count = (count + 1) % playlist.Length;
            }
            else
            {
                count = rd.Next(0, playlist.Length - 1);
            }

            //while (sequence1.IsBusy)
            //{
            //    Application.DoEvents();
            //    System.Threading.Thread.Sleep(100);
            //}
            //startButton_Click(sender, e);
            //���ò����¼�
            //try
            //{
            //    playing = true;
            //    sequencer1.Start();
            //    timer1.Start();
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show(ex.Message, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            //}
        }

        private void listBox1_DoubleClick(object sender, EventArgs e)
        {
            count = songid[listBox1.SelectedItem.ToString()];
            this.BeginInvoke(new playNextDG(playNext));
        }

        /// <summary>
        /// ��һ���ļ��У���ȡ�����midi�ļ�
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void openPlaylistToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // https://www.cnblogs.com/tinaluo/p/6636073.html
            FolderBrowserDialog folderDialog = new FolderBrowserDialog();
            folderDialog.Description = "Open playlist";

            if (folderDialog.ShowDialog() == DialogResult.OK)
            {
                string folderPath = folderDialog.SelectedPath;
                // http://www.cnblogs.com/willingtolove/p/9235353.html
                playlist = new DirectoryInfo(folderPath).GetFiles("*.mid");
                songid = new Dictionary<string, int>();
                listBox1.Items.Clear();
                for (int i = 0; i < playlist.Length; i++)
                {
                    string name = Path.GetFileNameWithoutExtension(playlist[i].FullName);
                    string fullname = playlist[i].FullName;
                    listBox1.Items.Add(name);
                    songid[name] = i;
                }
                count = sequential ? 0 : rd.Next(0, playlist.Length - 1);
                playNext();
            }
        }


        #endregion
        
        private bool sequential=true;        //ȫ�ֱ�����ʾ��ǰ�Ƿ�˳���������
        private Random rd = new Random();
        /// <summary>
        /// �л�˳���������
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tmpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            sequential = !sequential;
            tmpToolStripMenuItem.Text = sequential ? "sequential" : "random";
        }
    }
}