using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Protocols.Protocols
{
    //基本的接口
    internal interface IComm
    {
        byte[] Send(byte[] sendData);
    }

    //为减少代码重写的抽象类
    internal abstract class AComm : IComm
    {
        private int bufferSize = 1024;
        protected static SemaphoreSlim sem = new SemaphoreSlim(1, 1);
        private static int _minSemaphore = 1;
        private static int _maxSemaphore = 1;
        protected abstract Stream GetStream();


        //发送和接收数据
        public byte[] Send(byte[] sendData)
        {
            byte[] ret = new byte[bufferSize];//单次读写最多480字对应960字节，加上固定的报文头，1024字节以内 

            //限制并发连接数
            sem.Wait();
            var s = GetStream();
            s.Write(sendData, 0, sendData.Length);
            int n = s.Read(ret, 0, ret.Length);
            sem.Release();
            Array.Resize(ref ret, n);

            return ret;
        }
    }

    //通讯层父类-可选串口或以太网
    internal class CommNet : AComm, IComm
    {
        private string _ip;
        private int _port;
        private int _timeOut = 1000;

        private TcpClient client = new TcpClient();
        readonly object lckObj = new Object();


        //带IP，端口号设置的构造函数
        public CommNet(string ip, int port)
        {
            _ip = ip;
            _port = port;
        }

        //带IP，端口号，超时时间设置的构造函数
        public CommNet(string ip, int port, int timeOut)
        {
            _ip = ip;
            _port = port;
            _timeOut = timeOut;
        }

        //带IP，端口号，超时时间，以及信号量最小值最大值设置的构造函数
        public CommNet(string ip, int port, int timeOut, int minSemaphore, int maxSemaphore)
        {
            _ip = ip;
            _port = port;
            _timeOut = timeOut;
            sem = new SemaphoreSlim(minSemaphore, maxSemaphore);
        }

        ~CommNet()
        {
            client.Close();
            client.Dispose();
        }

        //获取Tcp连接的Stream
        protected override Stream GetStream()
        {
            lock (lckObj)
            {
                if (client == null) client = new TcpClient();
                client.ReceiveTimeout = _timeOut;
                if (!client.Connected) client.Connect(_ip, _port);
                if (client.Connected) return client.GetStream();
                return null;
            }
        }
    }

    //通讯层父类-可选串口或以太网
    internal class CommSerialPort : AComm, IComm
    {
        private string _portName;
        private int _baudRate;
        private int _dataBits;
        private Parity _parity;
        private StopBits _stopBits;
        private int _timeOut = 1000;

        private SerialPort sp = new SerialPort();
        readonly object lckObj = new Object();

        //最简构造函数
        public CommSerialPort(string portName, int baudRate, int dataBits, Parity parity, StopBits stopBits)
        {
            _portName = portName;
            _baudRate = baudRate;
            _parity = parity;
            _dataBits = dataBits;
            _stopBits = stopBits;
        }

        //不带信号量初始的构造函数
        public CommSerialPort(string portName, int baudRate, int dataBits, Parity parity, StopBits stopBits, int timeOut)
        {
            _portName = portName;
            _baudRate = baudRate;
            _parity = parity;
            _dataBits = dataBits;
            _stopBits = stopBits;
            _timeOut = timeOut;
        }

        //全参构造函数
        public CommSerialPort(string portName, int baudRate, int dataBits, Parity parity, StopBits stopBits, int timeOut, int minSemaphore, int maxSemaphore)
        {
            _portName = portName;
            _baudRate = baudRate;
            _parity = parity;
            _dataBits = dataBits;
            _stopBits = stopBits;
            _timeOut = timeOut;
            sem = new SemaphoreSlim(minSemaphore, maxSemaphore);
        }

        ~CommSerialPort()
        {
            sp.Close();
            sp.Dispose();
        }

        //获取Tcp连接的Stream
        protected override Stream GetStream()
        {
            lock (lckObj)
            {
                //判断串口是否存在
                var ports = SerialPort.GetPortNames();
                if (!ports.Any(p => p.Contains(_portName))) throw new InvalidDataException($"串口:{_portName}不存在");
                if (sp == null)
                {
                    sp = new SerialPort();
                    sp.PortName = _portName;
                    sp.BaudRate = _baudRate;
                    sp.DataBits = _dataBits;
                    sp.Parity = _parity;
                    sp.StopBits = _stopBits;
                    sp.WriteTimeout = _timeOut;
                    sp.ReadTimeout = _timeOut;

                }
                if (!sp.IsOpen) sp.Open();
                return sp.BaseStream;
            }
        }
    }




}
