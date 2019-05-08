/*
 * Copyright by LtFlash 2018
 * 
 * 
 * 
 */


using System;
using System.Collections.Generic;
using Rage;

namespace AssortedCallouts
{
    public class ProcessHost
    {
        public bool IsRunning { get; private set; }
        public bool this[Action proc]
        {
            get => this.proc.Contains(proc); 
            set
            {
                if (value) ActivateProcess(proc);
                else DeactivateProcess(proc);
            }
        }

        private GameFiber fiber;
        private bool canRun = true;
        private List<Action> proc = new List<Action>();

        public ProcessHost()
        {
            fiber = new GameFiber(InternalProcess);
        }

        public void Start()
        {
            IsRunning = true;
            canRun = true;
            fiber.Start();
        }

        public void Stop()
        {
            IsRunning = false;
            canRun = false;
        }

        public void ActivateProcess(Action proc)
        {
            if (!this.proc.Contains(proc)) this.proc.Add(proc);
        }

        public void DeactivateProcess(Action proc)
        {
            if (this.proc.Contains(proc)) this.proc.Remove(proc);
        }

        public void SwapProcesses(Action toDisable, Action toEnable)
        {
            DeactivateProcess(toDisable);
            ActivateProcess(toEnable);
        }

        private void InternalProcess()
        {
            while (canRun)
            {
                Process();
                GameFiber.Yield();
            }
        }

        public void Process()
        {
            for (int i = 0; i < proc.Count; i++)
            {
                proc[i].Invoke();
            }
        }
    }
}
