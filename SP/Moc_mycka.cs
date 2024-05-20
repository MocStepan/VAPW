using Sim1;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Sim1.TankValves;

namespace SP
{
    public enum DoorState
    {
        Open,
        Openning,
        Closed,
        Clossing
    }

    public class Moc_mycka : IDisposable
    {

        public double Height { get; private set; }

        private int closeDoorSpeed { get; set; }
        private int openDoorSpeed { get; set; }

        private bool Running { get; set; }
        private int WorkingCycleMs { get; set; }

        public event ChangedDoorStateHandler onDoorHeightStateChange;

        public delegate void ChangedDoorStateHandler(object sender, double actualHeight, double height);

        public DoorState doorState {  get; private set; }

        private double _actualHeight;

        public double ActualHeight 
        {
            get
            {
                return _actualHeight;
            }
            private set
            {
                bool flag = value != _actualHeight;
                _actualHeight = value;
                if (flag)
                {
                    this.onDoorHeightStateChange?.Invoke(this, _actualHeight, Height);
                }
            }
        }


        private Thread _thread = new Thread(new ParameterizedThreadStart(ThreadProcedure));

        public void open()
        {
            if (this.doorState == DoorState.Closed)
            {
                this.doorState = DoorState.Openning;
            }
        }

        public void close()
        {
            if (this.doorState == DoorState.Open)
            {
                this.doorState = DoorState.Clossing;
            }
        }

        private static void ThreadProcedure(object obj)
        {
            var door = (Moc_mycka)obj;
            var processStopwatch = Stopwatch.StartNew();

            while (door.Running)
            {
                Stopwatch timingStopwatch = Stopwatch.StartNew();

                processStopwatch.Stop();
                var totalSeconds = processStopwatch.Elapsed.TotalSeconds;
                if (totalSeconds > 0)
                {
                    if (door.doorState == DoorState.Clossing)
                    {
                        door.ActualHeight += door.closeDoorSpeed * totalSeconds;
                        if (door.ActualHeight >= door.Height)
                        {
                            door.doorState = DoorState.Closed;
                        }
                    } else if (door.doorState == DoorState.Openning)
                    {
                        door.ActualHeight -= door.openDoorSpeed * totalSeconds;
                        if (door.ActualHeight <= 0)
                        {
                            door.doorState = DoorState.Open;
                        }
                    }
                }

                processStopwatch.Restart();
                timingStopwatch.Stop();

                var toWaitMs = door.WorkingCycleMs - (int)timingStopwatch.ElapsedMilliseconds;
                try
                {
                    Thread.Sleep(toWaitMs < 1 ? 1 : toWaitMs);
                }
                catch (ThreadInterruptedException e)
                {
                    door.Running = false;
                }
            }
        }

        public Moc_mycka(int height)
        {
            if (height < 5000 || height > 100000)
                throw new InvalidOperationException("Capacity out of range!");

            this.Height = height;

            closeDoorSpeed = (int)(500 + 1 * 200);
            openDoorSpeed = (int)(500 + 1 * 200);

            this.doorState = DoorState.Open;

            Running = true;
            WorkingCycleMs = (int)(250 + 1 * 1000);
            _thread.Start(this);
        }

        public void Dispose()
        {
            try
            {
                _thread.Interrupt();
                _thread.Join();
            }
            catch (Exception e)
            {

            }
        }
    }
}
