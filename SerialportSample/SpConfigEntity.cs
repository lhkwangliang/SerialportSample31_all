using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SerialportSample
{
    class SpConfigEntity
    {
        private string portName;

        public string PortName
        {
            get { return portName; }
            set { portName = value; }
        }
        private int baudrate;

        public int Baudrate
        {
            get { return baudrate; }
            set { baudrate = value; }
        }
        private string parityBit;

        public string ParityBit
        {
            get { return parityBit; }
            set { parityBit = value; }
        }
        private int dataBit;

        public int DataBit
        {
            get { return dataBit; }
            set { dataBit = value; }
        }
        private int stopBit;

        public int StopBit
        {
            get { return stopBit; }
            set { stopBit = value; }
        }


    }
}
