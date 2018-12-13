﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Main.Logger
{
    public delegate void NewProcessLoggerMessageDelegate(string newMessage);

    public interface ILastMessageProcessLogger : IProcessLogger
    {
        string LastMessage
        {
            get;
        }

        event NewProcessLoggerMessageDelegate NewProcessLoggerMessageEvent;

        void ResetCounter();
    }

    public class LastMessageProcessLogger : ILastMessageProcessLogger
    {
        private readonly int _discret;

        private long _messageIndex = 0L;

        public string LastMessage
        {
            get;
            private set;
        }

        public event NewProcessLoggerMessageDelegate NewProcessLoggerMessageEvent;


        public LastMessageProcessLogger(
            int discret
            )
        {
            _discret = discret;
        }

        public void ShowProcessMessage(string message, params object[] args)
        {
            LastMessage = string.Format(message, args);

            InvokeIfNeccessary();
        }

        public void ShowProcessMessage(string message)
        {
            LastMessage = message;

            InvokeIfNeccessary();
        }

        public void ResetCounter()
        {
            Interlocked.Exchange(ref _messageIndex, 0L);
        }

        private void InvokeIfNeccessary()
        {
            var incrementedIndex = Interlocked.Increment(ref _messageIndex);
            if (((incrementedIndex % _discret) == 1)) //1 (instead of 0) is for showing first message
            {
                NewProcessLoggerMessageInvoke();
            }
        }

        private void NewProcessLoggerMessageInvoke()
        {
            var e = NewProcessLoggerMessageEvent;

            if(e==null)
            {
                return;
            }

            e(LastMessage);
        }

    }
}
