using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace SP
{
    public partial class MainForm : Form
    {
        Moc_mycka carWash;

        public MainForm()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            carWash = new Moc_mycka();
            var form = new SettingForm();
            var result = form.ShowDialog();
            if (result == DialogResult.OK)
            {
                if (form.IsEvent)
                {
                    timer1.Start();
                    carWash.LeftGarageDoor.onDoorHeightStateChange -= onLeftDoorStateChange;
                    carWash.CarWashProcessBar.onDoorHeightStateChange -= onLeftDoorStateChange;
                    carWash.RightGarageDoor.onDoorHeightStateChange -= onRightDoorStateChange;
                    carWash.onCarWashStateChange -= onCarWashStateChange;
                }
                else
                {
                    timer1.Stop();
                    carWash.LeftGarageDoor.onDoorHeightStateChange += onLeftDoorStateChange;
                    carWash.CarWashProcessBar.onDoorHeightStateChange += onCarWashProccessStateChange;
                    carWash.RightGarageDoor.onDoorHeightStateChange += onRightDoorStateChange;
                    carWash.onCarWashStateChange += onCarWashStateChange;
                }
                   
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            carWash.Dispose();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            carWash.startCarProccess();
        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            panel2.Height = (int)(panel1.Height * carWash.LeftGarageDoor.GarageDoorPosition);
            panel4.Height = (int)(panel3.Height * carWash.RightGarageDoor.GarageDoorPosition);
            panel6.Width = (int)(panel5.Width * (1 - carWash.CarWashProcessBar.GarageDoorPosition));
            panel7.BackColor = carWash.CarWashState == CarState.READY ? Color.Black : Color.White;
            panel8.BackColor = carWash.CarWashState == CarState.WASHING ? Color.Black : Color.White;
            panel9.BackColor = carWash.CarWashState == CarState.DONE ? Color.Black : Color.White;
        }

        private void onLeftDoorStateChange(object sender, double garageDoorPosition)
        {
            this.Invoke(new Action(() =>
            {
                panel2.Height = (int)(panel1.Height * garageDoorPosition);
            }));
        }
        private void onCarWashProccessStateChange(object sender, double garageDoorPosition)
        {
            this.Invoke(new Action(() =>
            {
                panel6.Width = (int)(panel5.Width * (1 - garageDoorPosition));
            }));
        }

        private void onRightDoorStateChange(object sender, double garageDoorPosition)
        {
            this.Invoke(new Action(() =>
            {
                panel4.Height = (int)(panel3.Height * garageDoorPosition);
            }));
        }

        private void onCarWashStateChange(object sender, CarState carWashState)
        {
            this.Invoke((Action)(() =>
            {
                panel7.BackColor = carWashState == CarState.READY ? Color.Black : Color.White;
                panel8.BackColor = carWashState == CarState.WASHING ? Color.Black : Color.White;
                panel9.BackColor = carWashState == CarState.DONE ? Color.Black : Color.White;
            }));
        }
    }
}
