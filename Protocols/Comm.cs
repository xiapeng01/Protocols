using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks; 
using System.Data;
using System.Net;
using System.Collections;

namespace Protocols
{
    //基本的接口
    public interface IComm:IDisposable
    {
        string LocalIp { get; set; }
        string RemoteIp { get; set; }
        string Send(string str);//用于收发字符串格式的内容
        byte[] Send(byte[] sendData);//用于收发字节数组格式的内容
        //byte[] Send(Action<Stream> action, byte[] sendData);//用于收发需要额外握手的协议内容
        byte[] Send(Func<bool> handShake, byte[] sendData);//带额外握手的方法
        Task<byte[]> SendAsync(byte[] sendData);//用于异步收发内容-暂未用
        Task<byte[]> SendAsync(byte[] sendData, Func<byte[],bool> ValidationFrame);//用于异步收发内容-暂未用-带帧校验

        bool Open();
        bool Close();
    }

    //为减少代码重写的抽象类
    public abstract class AComm : IComm, IDisposable
    {
        protected int waitReadDelay = 0;
        protected int bufferSize = 2500;//1024;//部分协议有最大读写数量限制
        protected static SemaphoreSlim sem1 = new SemaphoreSlim(1, 1);
        protected static SemaphoreSlim sem2 = new SemaphoreSlim(1, 1);
        protected static int _minSemaphore = 1;
        protected static int _maxSemaphore = 1;

        public string LocalIp { get; set; }
        public string RemoteIp { get; set; }

        //protected abstract Stream GetStream();//普通场景使用的方法
        //protected abstract Stream GetStream(Action<Stream> action);//需要额外握手的场景使用的方法

        ~AComm() 
        {
            Dispose();
        }

        /// <summary>
        /// 经过包装的字符串格式发送接收方法
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public string Send(string str)
        {
            string ret = Encoding.UTF8.GetString(Send(Encoding.UTF8.GetBytes(str)));
            return ret;
        }

        public abstract byte[] Send(byte[] sendData);//由子类实现的方法         
        public abstract byte[] Send(Func<bool> handShake, byte[] sendData);//带额外握手包的方法

        public Task<Byte[]> SendAsync(byte[] sendData)
        {      
            return Task.Run<Byte[]>(() => {
                return Task.FromResult<byte[]>(Send(sendData));            
            });            
        }

        public Task<Byte[]> SendAsync(byte[] sendData, Func<byte[],bool> ValidationFrame)
        {
            return Task.Run<Byte[]>(() => {
                var ret=Send(sendData);
                if(ValidationFrame(ret)) return Task.FromResult<byte[]>(ret);
                return Task.FromResult<Byte[]>(null);
            });
        }
        public abstract bool Open();
        public abstract bool Close();
        public abstract void Dispose();       
    }

    //通讯层父类-可选串口或以太网
    public class CommTCP : AComm, IComm
    {
        private string _ip;
        private int _port;
        private int _timeOut = 1000;

        private TcpClient client =null;
        readonly object lckObj = new Object(); 

        //带IP，端口号设置的构造函数
        public CommTCP(string ip, int port)
        {
            _ip = ip;
            _port = port;
        }

        //带IP，端口号，超时时间设置的构造函数
        public CommTCP(string ip, int port, int timeOut)
        {
            _ip = ip;
            _port = port;
            _timeOut = timeOut;
        }

        //带IP，端口号，超时时间，以及信号量最小值最大值设置的构造函数
        public CommTCP(string ip, int port, int timeOut, int minSemaphore, int maxSemaphore)
        {
            _ip = ip;
            _port = port;
            _timeOut = timeOut;
            sem1 = new SemaphoreSlim(minSemaphore, maxSemaphore);
        }

        ~CommTCP()
        {
            Dispose();
        }

        //获取Tcp连接的Stream
        public override byte[] Send(byte[] sendData)
        {
            //lock (lckObj)
            {
                waitReadDelay = 0;
                if (client == null)
                {
                    client = new TcpClient();
                    client.Connect(_ip, _port);
                } 
                //if (client.Connected) return client.GetStream();
                client.ReceiveTimeout = _timeOut;

                byte[] ret = new byte[bufferSize];//单次读写最多480字对应960字节，加上固定的报文头，1024字节以内             
                sem1.Wait();//限制并发连接数
                var s = client.GetStream();
                s.Write(sendData, 0, sendData.Length);
                Thread.Sleep(waitReadDelay);
                int n = s.Read(ret, 0, ret.Length);
                sem1.Release();
                Array.Resize(ref ret, n);
                return ret;
            }
        }

        /// <summary>
        /// 获取连接的Stream，带委托的版本，适用于需要独立握手的协议
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public override byte[] Send(Func<bool> handShakeDelegate, byte[] sendData)
        {
            //lock (lckObj)
            {
                sem2.Wait();//限制并发连接数
                waitReadDelay = 0;
                if (client == null)
                {
                    client = new TcpClient();
                    client.Connect(_ip, _port);
                    LocalIp = ((IPEndPoint)client.Client.LocalEndPoint).Address.ToString();
                    RemoteIp = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();

                    //应在此时调用子类的方法发送握手包
                    for (int i = 0; i < 3; i++) if (handShakeDelegate?.Invoke() ?? false) break;//握手 
                }
                
                if (!client.Connected) client.Connect(_ip, _port);
                client.ReceiveTimeout = _timeOut;

                byte[] ret = new byte[bufferSize];//单次读写最多480字对应960字节，加上固定的报文头，1024字节以内    

                var s = client.GetStream();
                s.Write(sendData, 0, sendData.Length);
                Thread.Sleep(waitReadDelay);
                int n = s.Read(ret, 0, ret.Length);
                sem2.Release();
                Array.Resize(ref ret, n);
                return ret;
            }
        }

        public override bool Open()
        {
            try
            {
                client = new TcpClient();
                client.Connect(_ip, _port);
                return true;
            }
            catch { }
            return false;
        }


        public override bool Close()
        {
            try
            {
                client?.Close();
                return true;
            }
            catch{}
            return false;
        }

        public override void Dispose()
        {
            try
            {
                client?.Close();
                client?.Dispose();
                client = null;
            }
            catch { }
        }
    }

    //通讯层父类-可选串口或以太网--未完成
    
    public class CommUDP : AComm, IComm
    {
        private string _ip;
        private int _port;
        private int _timeOut = 1000;

        private UdpClient client = null;
        readonly object lckObj = new Object();

        private IPEndPoint localIPE=null;
        private IPEndPoint remoteIPE=null;

        //带IP，端口号设置的构造函数
        public CommUDP(string ip, int port)
        {
            _ip = ip;
            _port = port;
        }

        //带IP，端口号，超时时间设置的构造函数
        public CommUDP(string ip, int port, int timeOut)
        {
            _ip = ip;
            _port = port;
            _timeOut = timeOut;
        }

        //带IP，端口号，超时时间，以及信号量最小值最大值设置的构造函数
        public CommUDP(string ip, int port, int timeOut, int minSemaphore, int maxSemaphore)
        {
            _ip = ip;
            _port = port;
            _timeOut = timeOut;
            sem1 = new SemaphoreSlim(minSemaphore, maxSemaphore);
        }

        ~CommUDP()
        {
            Dispose();
        }


        public override byte[] Send(byte[] sendData)
        {
            sem1.Wait();//限制并发连接数 
            waitReadDelay = 0;
            if (localIPE == null) localIPE = new IPEndPoint(IPAddress.Any, 0);
            if (remoteIPE == null) remoteIPE = new IPEndPoint(IPAddress.Parse(_ip), _port);

            if (client == null)//为空
            {
                client = new UdpClient(localIPE); 
            }
             
            
            client.Send(sendData, sendData.Length, remoteIPE);
            Thread.Sleep(waitReadDelay);
            var ret = client.Receive(ref remoteIPE);
            sem1.Release(); 
            return ret;
        }

        /// <summary>
        /// 带一个回调委托的方法-适用于需要额外握手的协议
        /// </summary>
        /// <param name="sendData"></param>
        /// <returns></returns>
        public override byte[] Send(Func<bool> handShake,byte[] sendData)
        {
            sem2.Wait();//限制并发连接数 
            waitReadDelay = 0; 
            if (localIPE == null) localIPE = new IPEndPoint(IPAddress.Any, 0);
            if (remoteIPE == null) remoteIPE = new IPEndPoint(IPAddress.Parse(_ip), _port);
             
            if (client == null)//为空
            {
                client = new UdpClient(localIPE);
                LocalIp = ((IPEndPoint)client.Client.LocalEndPoint).Address.ToString();
                RemoteIp = _ip.ToString();
                for (int i = 0; i < 3; i++)
                    if (handShake?.Invoke()??false) break ;//执行我手法方法 
            }
            
            client.Send(sendData, sendData.Length, remoteIPE);
            Thread.Sleep(waitReadDelay);
            var ret = client.Receive(ref remoteIPE);
            sem2.Release();
            return ret;
        }

        public override bool Open()
        {
            try
            {
                if (localIPE == null) localIPE = new IPEndPoint(IPAddress.Any, 0);
                if (remoteIPE == null) remoteIPE = new IPEndPoint(IPAddress.Parse(_ip), _port);

                if (client == null) client = new UdpClient(localIPE);
 
                return true;
            }
            catch { }
            return false;
        }

        public override bool Close()
        {
            try
            {
                client?.Close();
                return true;
            }
            catch { }
            return false;
        }

        public override void Dispose()
        {
            try
            {
                client?.Close();
                client?.Dispose();
                client = null;
            }
            catch { }
        }
    }

    //通讯层父类-可选串口或以太网
    public class CommSerialPort : AComm, IComm
    {
        private string _portName;
        private int _baudRate;
        private int _dataBits;
        private Parity _parity;
        private StopBits _stopBits;
        private int _timeOut = 1000;

        private SerialPort sp ;
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
            sem1 = new SemaphoreSlim(minSemaphore, maxSemaphore);
        }

        ~CommSerialPort()
        {
            Dispose();
        }

        //获取Tcp连接的Stream
        public override byte[] Send(byte[] sendData)
        {
            lock (lckObj)
            {
                waitReadDelay = 50;
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
                    sp.Open();
                }
                sp.WriteTimeout = _timeOut;
                sp.ReadTimeout = _timeOut;

                byte[] ret = new byte[bufferSize];//单次读写最多480字对应960字节，加上固定的报文头，1024字节以内             
                sem1.Wait();//限制并发连接数
                var s = sp.BaseStream;
                s.Write(sendData, 0, sendData.Length);
                Thread.Sleep(waitReadDelay);
                int n = s.Read(ret, 0, ret.Length);
                sem1.Release();
                Array.Resize(ref ret, n);
                return ret;
            }
        }        

        public override byte[] Send(Func<bool> handShake, byte[] sendData)
        { 
            lock (lckObj)
            {
                waitReadDelay = 50;
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

                    sp.Open();

                    for (int i = 0; i < 3; i++) if (handShake?.Invoke() ?? false) break;//握手
                }
                sp.WriteTimeout = _timeOut;
                sp.ReadTimeout = _timeOut;

                byte[] ret = new byte[bufferSize];//单次读写最多480字对应960字节，加上固定的报文头，1024字节以内             
                sem1.Wait();//限制并发连接数
                var s = sp.BaseStream;
                s.Write(sendData, 0, sendData.Length);
                Thread.Sleep(waitReadDelay);
                int n = s.Read(ret, 0, ret.Length);
                sem1.Release();
                Array.Resize(ref ret, n);
                return ret;
            }
        }

        public override bool Open()
        {
            try
            {
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

                    sp.Open(); 
                }
                return true;
            }
            catch { }
            return false;
        }

        public override bool Close()
        {
            try
            {
                sp?.Close();
                return true;
            }
            catch { }
            return false;
        }

        public override void Dispose()
        {
            try
            {
                sp?.Close();
                sp?.Dispose();
                sp = null;
            }
            catch { }
        }
    }
}
