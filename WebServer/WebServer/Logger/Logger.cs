using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server.Logger
{
    class Logger
    {
        private static int BUFFLENG = 5;
        private static Logger instance;
        private Semaphore canRead;
        private Semaphore canAdd;
        private Semaphore busy;
        private int readPos;
        private int addPos;


        private string[] buffer;

        public static Logger Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new Logger();
                }
                return instance;
            }
            private set
            {
                instance = value;
            }
        }

        private Logger()
        {
            buffer = new string[BUFFLENG];
            canAdd = new Semaphore(BUFFLENG, BUFFLENG);
            canRead = new Semaphore(0, BUFFLENG);
            busy = new Semaphore(1, 1);
            readPos = 0;
            addPos = 0;

            new Thread(run).Start();
        }

        public void run()
        {
            StreamWriter sw = new StreamWriter(@"Logger/log.txt");
            while (true)
            {
                sw.WriteLine(read());
                sw.Flush();
            }
        }

        public void add(string message)
        {
            canAdd.WaitOne();
            busy.WaitOne();
            buffer[addPos++] = message;

            addPos %= BUFFLENG;
            canRead.Release();
            busy.Release();
        }

        public string read()
        {
            canRead.WaitOne();
            busy.WaitOne();
            string message = buffer[readPos++];

            readPos %= BUFFLENG;
            canAdd.Release();
            busy.Release();
            return message;
        }
    }
}
