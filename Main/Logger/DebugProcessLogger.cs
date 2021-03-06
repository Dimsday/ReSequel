using System;
using System.Diagnostics;

namespace Main.Logger
{
    public class DebugProcessLogger : IProcessLogger
    {
        public DebugProcessLogger()
        {

        }

        public void ShowProcessMessage(
            string message,
            params object[] args
            )
        {
            this.ShowProcessMessage(
                string.Format(message, args)
                );
        }

        public void ShowProcessMessage(
            string message
            )
        {
            message = DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss.fff") + "    " + message + "... ";

            Debug.WriteLine(message);
        }


    }



}
