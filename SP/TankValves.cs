using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sim1
{
    public class TankValves : IDisposable
    {
        public int CapacityLitres { get; private set; }
        private ValveState _inletValveState;
        public ValveState InletValveState { get { return _inletValveState; } private set { var changed = value != _inletValveState; _inletValveState = value; if (changed) OnInletValveStateChange?.Invoke(this, _inletValveState); } }
        private ValveState _outletValveState;
        public ValveState OutletValveState { get { return _outletValveState; } private set { var changed = value != _outletValveState; _outletValveState = value; if (changed) OnOutletValveStateChange?.Invoke(this, _outletValveState); } }
        public int InletValveFlowRate { get; private set; }
        public int OutletValveFlowRate { get; private set; }
        private double _stateLitres;
        public double StateLitres { get { return _stateLitres; } private set { var changed = value != _stateLitres; _stateLitres = value; if (changed) OnTankStateChange?.Invoke(this, _stateLitres,  (100 * _stateLitres / CapacityLitres)); } }

        public delegate void ChangedValveStateHandler(object sender, ValveState valveState);
        public event ChangedValveStateHandler OnInletValveStateChange;
        public event ChangedValveStateHandler OnOutletValveStateChange;

        public delegate void ChangedTankStateHandler(object sender, double stateLitres, double statePercent);
        public event ChangedTankStateHandler OnTankStateChange;

        private bool Running { get; set; }
        private int WorkingCycleMs { get; set; }


        private Thread _thread = new Thread(new ParameterizedThreadStart(ThreadProcedure));


        private static void ThreadProcedure(object obj)
        {
            var tankValves = (TankValves)obj;
            var processStopwatch = Stopwatch.StartNew();

            while (tankValves.Running)
            {
                Stopwatch timingStopwatch = Stopwatch.StartNew();

                processStopwatch.Stop();
                var elapsedTimeSeconds = processStopwatch.Elapsed.TotalSeconds;
                if(elapsedTimeSeconds > 0)
                {
                    var intake = (tankValves.InletValveState == ValveState.On ? tankValves.InletValveFlowRate : 0) * elapsedTimeSeconds;
                    var outlet = (tankValves.OutletValveState == ValveState.On ? tankValves.OutletValveFlowRate : 0) * elapsedTimeSeconds;
                    tankValves.StateLitres += intake - outlet;

                    if(tankValves.StateLitres > 0.9 * tankValves.CapacityLitres)
                    {
                        tankValves.InletValveState = ValveState.Off;
                        tankValves.OutletValveState = ValveState.On;
                    }
                    if(tankValves.StateLitres < 0.1 * tankValves.CapacityLitres)
                    {
                        tankValves.InletValveState = ValveState.On;
                        tankValves.OutletValveState = ValveState.Off;
                    }
                }
                processStopwatch.Restart();

                timingStopwatch.Stop();

                var toWaitMs = tankValves.WorkingCycleMs - (int)timingStopwatch.ElapsedMilliseconds;
                toWaitMs = toWaitMs < 1 ? 1 : toWaitMs;

                try
                {
                    Thread.Sleep(toWaitMs);
                }
                catch(ThreadInterruptedException e)
                {
                    tankValves.Running = false;
                }
            }
        }

        public TankValves(int capacityLitres)
        {
            if (capacityLitres < 5000 || capacityLitres > 100000)
                throw new InvalidOperationException("Capacity out of range!");

            CapacityLitres = capacityLitres;

            var random = new Random(Environment.TickCount);
            InletValveFlowRate = (int)(500 + random.NextDouble() * 1500);
            OutletValveFlowRate = (int)(500 + random.NextDouble() * 1500);

            InletValveState = ValveState.Unknown;
            OutletValveState = ValveState.Unknown;

            Running = true;
            WorkingCycleMs = (int)(250 + random.NextDouble() * 1000);
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
