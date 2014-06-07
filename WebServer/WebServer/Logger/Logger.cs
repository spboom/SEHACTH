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
        private static string filePath = @"Logger/log.txt";
        private static Logger instance;
        private Semaphore canRead;
        private Semaphore canAdd;
        private Semaphore busy;
        private Semaphore fileBusy;
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
            fileBusy = new Semaphore(1, 1);
            readPos = 0;
            addPos = 0;

            new Thread(writeFile).Start();
        }

        public void writeFile()
        {
            while (true)
            {
                string logEntry = readFromQueue();
                fileBusy.WaitOne();
                using (StreamWriter sw = File.AppendText(filePath))
                {
                    sw.WriteLine(logEntry);
                    sw.Flush();
                }
                fileBusy.Release();
            }
        }

        public string readFile()
        {
            string fileContent = "";
            fileBusy.WaitOne();
            using (StreamReader sr = File.OpenText(filePath))
            {
                while (!sr.EndOfStream)
                {
                    fileContent = sr.ReadLine() + "\n" + fileContent;
                }
            }
            fileBusy.Release();
            return fileContent;
        }

        public void addToQueue(string message)
        {
            canAdd.WaitOne();
            busy.WaitOne();
            buffer[addPos++] = message;

            addPos %= BUFFLENG;
            canRead.Release();
            busy.Release();
        }

        public string readFromQueue()
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
