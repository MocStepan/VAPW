using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;

namespace SP
{
    public enum DoorState
    {
        OPEN,
        OPENNING,
        CLOSED,
        CLOSSING
    }

    public enum CarState
    {
        READY,
        WASHING,
        DONE
    }

    public class Moc_mycka : IDisposable
    {
        public GarageDoor LeftGarageDoor { get; private set; }
        public GarageDoor CarWashProcessBar { get; private set; }
        public GarageDoor RightGarageDoor { get; private set; }
        private CarState _CarWashState;
        public CarState CarWashState 
        {
            get
            {
                return _CarWashState;
            }
            set
            {
                bool flag = value != _CarWashState;
                _CarWashState = value;
                if (flag)
                {
                    this.onCarWashStateChange?.Invoke(this, CarWashState);
                }

            }
        }
        public bool IsRunning { get; private set; }
        private int WorkingCycleMs { get; set; }

        public event ChangedCarWashStateHandler onCarWashStateChange;
        public delegate void ChangedCarWashStateHandler(object sender, CarState carWashState);
        private Thread _thread = new Thread(new ParameterizedThreadStart(ThreadProcedure));

        public void startCarProccess()
        {
            this.LeftGarageDoor.close();
            this.RightGarageDoor.close();
            this.CarWashState = CarState.WASHING;
        }

        private static void ThreadProcedure(object obj)
        {
            var carWash = (Moc_mycka)obj;
            var processStopwatch = Stopwatch.StartNew();

            while (carWash.IsRunning)
            {
                Stopwatch timingStopwatch = Stopwatch.StartNew();

                processStopwatch.Stop();
                var totalSeconds = processStopwatch.Elapsed.TotalSeconds;
                if (totalSeconds > 0)
                {
                    if (carWash.LeftGarageDoor.doorState == DoorState.CLOSED && carWash.CarWashProcessBar.doorState == DoorState.OPEN && carWash.RightGarageDoor.doorState == DoorState.CLOSED)
                    {
                        carWash.CarWashProcessBar.close();
                    } 
                    else if (carWash.LeftGarageDoor.doorState == DoorState.CLOSED && carWash.CarWashProcessBar.doorState == DoorState.CLOSED && carWash.RightGarageDoor.doorState == DoorState.CLOSED) 
                    {
                        carWash.RightGarageDoor.open();
                    }
                    else if (carWash.LeftGarageDoor.doorState == DoorState.CLOSED && carWash.CarWashProcessBar.doorState == DoorState.CLOSED && carWash.RightGarageDoor.doorState == DoorState.OPEN) 
                    {
                        carWash.LeftGarageDoor.open();
                        carWash.CarWashProcessBar.open();
                        carWash.RightGarageDoor.close();
                        carWash.CarWashState = CarState.DONE;
                    }
                    else if (carWash.LeftGarageDoor.doorState == DoorState.OPEN && carWash.CarWashProcessBar.doorState == DoorState.OPEN && carWash.RightGarageDoor.doorState == DoorState.CLOSED)
                    {
                        carWash.CarWashState = CarState.READY;
                    }
                }

                processStopwatch.Restart();
                timingStopwatch.Stop();

                var toWaitMs = carWash.WorkingCycleMs - (int)timingStopwatch.ElapsedMilliseconds;
                try
                {
                    Thread.Sleep(toWaitMs < 1 ? 1 : toWaitMs);
                }
                catch (ThreadInterruptedException e)
                {
                    carWash.IsRunning = false;
                }
            }
        }

        public Moc_mycka()
        {
            this.LeftGarageDoor = new GarageDoor(5000);
            this.CarWashProcessBar = new GarageDoor(5000);
            this.RightGarageDoor = new GarageDoor(5000);
            this.CarWashState = CarState.READY;

            IsRunning = true;
            WorkingCycleMs = (int)(250 + 1 * 1000);
            _thread.Start(this);

        }

        public void Dispose()
        {
            try
            {
                LeftGarageDoor?.Dispose();
                RightGarageDoor?.Dispose();
                CarWashProcessBar?.Dispose();
                _thread.Interrupt();
                _thread.Join();
            }
            catch (Exception e)
            {

            }
        }
    }

    public class GarageDoor : IDisposable
    {
        public DoorState doorState { get; private set; }
        public double GarageDoorPosition { get; private set; }
        private double Height { get; set; }
        private int closeDoorSpeed { get; set; }
        private int openDoorSpeed { get; set; }
        private bool Running { get; set; }
        private int WorkingCycleMs { get; set; }
        private double _actualHeight {  get; set; }

        private double ActualHeight { 
            get
            {
                return _actualHeight;
            }
            set
            {
                bool flag = value != _actualHeight;
                _actualHeight = value;
                if (flag)
                {
                    this.onDoorHeightStateChange?.Invoke(this, GarageDoorPosition);
                }
            }     
        }

        public event ChangedDoorStateHandler onDoorHeightStateChange;
        public delegate void ChangedDoorStateHandler(object sender, double garageDoorPosition);
        private Thread _thread = new Thread(new ParameterizedThreadStart(ThreadProcedure));

        public void open()
        {
            if (this.doorState == DoorState.CLOSED)
            {
                this.doorState = DoorState.OPENNING;
            }
        }

        public void close()
        {
            if (this.doorState == DoorState.OPEN)
            {
                this.doorState = DoorState.CLOSSING;
            }
        }

        private static void ThreadProcedure(object obj)
        {
            var door = (GarageDoor)obj;
            var processStopwatch = Stopwatch.StartNew();

            while (door.Running)
            {
                Stopwatch timingStopwatch = Stopwatch.StartNew();

                processStopwatch.Stop();
                var totalSeconds = processStopwatch.Elapsed.TotalSeconds;
                if (totalSeconds > 0)
                {
                    if (door.doorState == DoorState.CLOSSING)
                    {
                        door.ActualHeight += door.closeDoorSpeed * totalSeconds;
                        door.GarageDoorPosition = 1 - door.ActualHeight / door.Height;
                        if (door.ActualHeight >= door.Height)
                        {
                            door.doorState = DoorState.CLOSED;
                        }
                    }
                    else if (door.doorState == DoorState.OPENNING)
                    {
                        door.ActualHeight -= door.openDoorSpeed * totalSeconds;
                        door.GarageDoorPosition = 1 - door.ActualHeight / door.Height;
                        if (door.ActualHeight <= 0)
                        {
                            door.doorState = DoorState.OPEN;
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

        public GarageDoor(int height)
        {
            if (height < 5000 || height > 100000)
                throw new InvalidOperationException("Capacity out of range!");

            this.Height = height;

            closeDoorSpeed = (int)(500 + 1 * 200);
            openDoorSpeed = (int)(500 + 1 * 200);

            this.doorState = DoorState.OPEN;
            this.GarageDoorPosition = 1;

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
