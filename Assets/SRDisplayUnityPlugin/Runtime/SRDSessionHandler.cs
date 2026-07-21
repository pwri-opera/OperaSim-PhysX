/*
 * Copyright 2019,2020,2023,2024 Sony Corporation
 */

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SRD.Core
{
    internal class SRDSessionHandler
    {
        private static SRDSessionHandler _instance;
        public static SRDSessionHandler Instance
        {
            get
            {
                if(_instance == null)
                {
                    _instance = new SRDSessionHandler();
                }
                return _instance;
            }
        }

        public static IntPtr SessionHandle
        {
            get { return IntPtr.Zero; }
        }

        private bool _active = true;
        public bool Active
        {
            get { return _active; }
            set { _active = value; }
        }

        private SRDSessionHandler()
        {
            Application.quitting += () =>
            {
                SRDSession.DisposeAll();
            };
        }

        ~SRDSessionHandler()
        {
        }

        internal SRDSession AllocateRunningSession()
        {
            if (!this.Active)
            {
                return null;
            }

            var session = SRDSession.CreateSession();
            if (session == null)
            {
                this.Active = false;
                return null;
            }

            if (!session.Start())
            {
                session.DestroySession();
                return null;
            }

            return session;
        }

        internal SRDSession AllocateSession()
        {
            if (!this.Active)
            {
                return null;
            }

            var session = SRDSession.CreateSession();
            if (session == null)
            {
                this.Active = false;
                return null;
            }

            if (!session.StartAsync())
            {
                session.DestroySession();
                return null;
            }

            return session;
        }

        internal List<SRDSession> AllocateMultiSession(int max)
        {
            if (!this.Active)
            {
                return null;
            }

            var sessions = SRDSession.CreateMultiSession(max);
            if (sessions == null)
            {
                this.Active = false;
                return null;
            }

            foreach(var session in sessions)
            {
                if (!session.StartAsync())
                {
                    session.DestroySession();
                    return null;
                }
            }
            return sessions;
        }

    }
}
