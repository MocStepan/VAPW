using Sim1;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SP
{
    public partial class MainForm : Form
    {
        Moc_mycka inputDoor = new Moc_mycka(6000);
        Moc_mycka outputDoor = new Moc_mycka(6000);
        ProgressBar progressBar;
        Thread thread;
        long counter;
        const long MAX = 10000000;
        private long state = 0;

        /// <summary>
        /// Synchronizační objekt, poskytuje zámek kritické sekce
        /// </summary>
        object lockObject = new object();

        public MainForm()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            var form = new SettingForm();
            var result = form.ShowDialog();
            if (result == DialogResult.OK)
            {
                // THREADS
                if (form.IsEvent)
                {
                    timer1.Start();
                }
                // HANDLERS
                else
                {
                    timer2.Start();
                    inputDoor.onDoorHeightStateChange += onInputDoorStateChange;
                    outputDoor.onDoorHeightStateChange += onOutputDoorStateChange;
                }
            }
            button1.Enabled = false;
            label2.BackColor = Color.Red;
            outputDoor.close();
            thread = new Thread(new ParameterizedThreadStart(ThreadProcSyncSleep));
            this.progressBar = new ProgressBar
            {
                Parent = this,
                Left = 180,
                Top = 100,
                Value = 0,
                Height = 50
            };
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            inputDoor?.Dispose();
            outputDoor?.Dispose();
            progressBar?.Dispose();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            state = 1;
            button1.Enabled = false;
            inputDoor.close();
        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            label1.BackColor = inputDoor.doorState == DoorState.Open ? Color.Green : Color.Red;
            switch(state) 
            {
                case 0:
                    panel4.Height = (int)(panel3.Height * (1 - outputDoor.ActualHeight / outputDoor.Height));
                    if (outputDoor.doorState == DoorState.Closed)
                    {
                        label2.BackColor = Color.Green;
                        button1.Enabled = true;
                    }
                    break;
                case 1:
                    panel2.Height = (int)(panel1.Height * (1 - inputDoor.ActualHeight / inputDoor.Height));
                    label2.BackColor = Color.Red;
                    if (inputDoor.doorState == DoorState.Closed)
                    {
                        state = 2;
                    }
                    break;
                case 2:
                    thread.Start();
                    state = 3;
                    break;
                case 3:
                    if (counter == MAX)
                    {
                        outputDoor.open();
                        thread.Abort();
                        state = 4;
                    }
                    progressBar.Value = (int)(100 * counter / MAX);
                    break;
                case 4:
                    panel4.Height = (int)(panel3.Height * (1 - outputDoor.ActualHeight / outputDoor.Height));
                    if (outputDoor.doorState == DoorState.Open)
                    {
                        label2.BackColor = Color.Green;
                        inputDoor.open();
                        outputDoor.close();
                        state = 5;
                    }
                    break;
                case 5:
                    label2.BackColor = Color.Red;
                    panel2.Height = (int)(panel1.Height * (1 - inputDoor.ActualHeight / inputDoor.Height));
                    panel4.Height = (int)(panel3.Height * (1 - outputDoor.ActualHeight / outputDoor.Height));
                    if (inputDoor.doorState == DoorState.Open && outputDoor.doorState == DoorState.Closed)
                    {
                        label2.BackColor = Color.Green;
                        progressBar.Value = 0;
                        button1.Enabled = true;
                        timer1.Stop();
                    }
                    break;
            }
        }

        private void Timer2_Tick(object sender, EventArgs e)
        {
            label1.BackColor = inputDoor.doorState == DoorState.Open ? Color.Green : Color.Red;
            switch (state)
            {
                case 1:
                    label2.BackColor = Color.Red;
                    if (inputDoor.doorState == DoorState.Closed)
                    {
                        state = 2;
                    }
                    break;
                case 2:
                    thread.Start();
                    state = 3;
                    break;
                case 3:
                    if (counter == MAX)
                    {
                        outputDoor.open();
                        thread.Abort();
                        state = 4;
                    }
                    progressBar.Value = (int)(100 * counter / MAX);
                    break;
                case 4:
                    if (outputDoor.doorState == DoorState.Open)
                    {
                        label2.BackColor = Color.Green;
                        inputDoor.open();
                        outputDoor.close();
                        state = 5;
                    }
                    break;
                case 5:
                    label2.BackColor = Color.Red;
                    if (inputDoor.doorState == DoorState.Open && outputDoor.doorState == DoorState.Closed)
                    {
                        label2.BackColor = Color.Green;
                        progressBar.Value = 0;
                        button1.Enabled = true;
                        timer2.Stop();
                    }
                    break;
            }
        }

        private void ThreadProcSyncSleep(object o)
        {
            while (counter < MAX)
            {
                lock (lockObject)
                {
                    for (int j = 0; j < 50000; j++)
                    {
                        counter++;
                    }
                }
                Thread.Sleep(1);
            }
        }

        private void onInputDoorStateChange(object sender, double actualHeight, double height)
        {
            this.Invoke(new Moc_mycka.ChangedDoorStateHandler(updateInputDoorState),
                sender, actualHeight, height);
        }

        private void onOutputDoorStateChange(object sender, double actualHeight, double height)
        {
            this.Invoke(new Moc_mycka.ChangedDoorStateHandler(updateOutputDoorState),
                sender, actualHeight, height);
        }

        void updateInputDoorState(object sender, double actualHeight, double height)
        {
            panel2.Height = (int)(panel1.Height * (1 - actualHeight / height));
        }

        void updateOutputDoorState(object sender, double actualHeight, double height)
        {
            panel4.Height = (int)(panel3.Height * (1 - actualHeight / height));
        }
    }
}
