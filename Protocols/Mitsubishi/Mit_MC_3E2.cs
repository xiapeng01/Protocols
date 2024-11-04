﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Protocols
{
    public class MC_3Ebase2 : ProtocolBase
    {
        const int iHeadFrameLength = 32;
        const int iDataFrameLength = 64;
        const int receiveDataHeadLength = 11;
        const string pattern = @"[\s\t]";

        readonly byte[] sendHead = new byte[]{ 0x50, 0x00, 0x00, 0xFF, 0xFF, 0x03, 0x00 };
        readonly byte[] receiveHead = new byte[] { 0xD0, 0x00, 0x00, 0xFF, 0xFF, 0x03, 0x00 };

        //带IP，端口号设置的构造函数
        public MC_3Ebase2(IComm comm) : base(comm)//显式调用基类的构造函数
        {
            comm.Open();
        }

        //获取寄存器对应的符号编号
        byte GetRegCode(string regName)
        {
            switch (regName.Trim().ToUpper())
            {
                case "SM": return 0x91;
                case "SD": return 0xA9;
                case "X": return 0x9C;
                case "Y": return 0x9D;
                case "M": return 0x90;
                case "L": return 0x92;
                case "F": return 0x93;
                case "V": return 0x94;
                case "B": return 0xA0;
                case "D": return 0xA8;
                case "W": return 0xB4;

                case "TS": return 0xC1;
                case "TC": return 0xC0;
                case "TN": return 0xC2;
                case "SS": return 0xC7;
                case "SC": return 0xC6;

                case "SN": return 0xC8;
                case "CS": return 0xC4;
                case "CC": return 0xC3;
                case "CN": return 0xC5;
                case "SB": return 0xA1;

                case "SW": return 0xB5;
                case "S": return 0x98;
                case "DX": return 0xA2;
                case "DY": return 0xA3;
                case "Z": return 0xCC;

                case "R": return 0xAF;
                case "ZR": return 0xB0;

                default: throw new Exception("不支持的寄存器！");
            }
        }

        //bool读写
        public override bool[] ReadBool(string regName, int Address, int Count)
        {
            bool[] ret = Array.Empty<bool>();//初始化返回值

            List<byte> sbData = new List<byte>();//初始化帧数据字符串
            sbData.AddRange(new byte[] { 0x10, 0x00, 0x01, 0x04, 0x00, 0x00 });
            sbData.AddRange(BitConverter.GetBytes(Address).Take(3));//起始软元件十六进制大端格式
            sbData.AddRange(new byte[] { GetRegCode(regName) });//软元件代码	 
            sbData.AddRange(BitConverter.GetBytes((Int16)Count));//软元件点数

            //获取请求数据长度  
            var sendData = new List<byte>();//发送数据
            sendData.AddRange(sendHead);
            sendData.AddRange(BitConverter.GetBytes((Int16)sbData.Count()));
            sendData.AddRange(sbData);

            var receiveData = _comm.Send(sendData.ToArray());//接收数据
            
            //校验接收到的数据
            if (receiveData != null && //接收内容不为空
                receiveData.Length >= receiveDataHeadLength &&//接收内容长度正常
                ByteRangeCompare(receiveData.Take(receiveHead.Length).ToArray(),receiveHead))//接收内容符合格式要求                
            {
                ret = new bool[Count];//初始化返回值数组
                string strContent = "";//要将内容填充进去
                strContent = ByteAryToBinString(receiveData.Skip(receiveDataHeadLength).Take(receiveData.Length - receiveDataHeadLength).ToArray());

                for (int i = 0; i < Count; i++)//填充返回值数组
                {
                    ret[i] = strContent.Substring(i, 1) == "1" ? true : false;
                }
                return ret;
            }
            return ret;//返回内容
        }

        public override bool WriteBool(string regName, int Address, bool[] values)
        { 
            List<byte> sbData = new List<byte>();//初始化帧数据字符串
            sbData.AddRange(new byte[] { 0x10, 0x00, 0x01, 0x14, 0x00, 0x00 });
            sbData.AddRange(BitConverter.GetBytes(Address).Take(3));//起始软元件十六进制大端格式
            sbData.AddRange(new byte[] { GetRegCode(regName) });//软元件代码	 
            sbData.AddRange(BitConverter.GetBytes((Int16)values.Count()));//软元件点数

            int i = 0;
            int j = 0;
            byte dat = 0;
            foreach (var value in values)
            {
                dat *= 2;
                dat += value ? (byte)1 : (byte)0;
                i++;
                j++;
                if (i == values.Length)
                {
                    sbData.Add(dat);
                    break;
                }
                if (j == 8)
                {
                    sbData.Add(dat);
                    dat = 0;
                    j = 0;
                }
            } 

            //拼接发送内容
            var sendData =new List<byte>();
            sendData.AddRange(sendHead);
            sendData.AddRange(BitConverter.GetBytes(((Int16)sbData.Count())));
            sendData.AddRange(sbData.ToArray());

            var receiveData = _comm.Send(sendData.ToArray());//接收数据

            //校验接收到的数据
            if (receiveData != null && //接收内容不为空
                receiveData.Length >= receiveDataHeadLength &&//接收内容长度正常
                ByteRangeCompare(receiveData.Take(receiveHead.Length).ToArray(), receiveHead))//接收内容符合格式要求
            {
                return true;//校验成功，返回
            }
            return false;//返回内容
        }

        //16位读写
        public override Int16[] ReadInt16(string regName, int Address, int Count)
        {
            Int16[] ret = Array.Empty<Int16>();//初始化返回值 

            List<byte> sbData = new List<byte>();//初始化帧数据字符串
            sbData.AddRange(new byte[] { 0x10, 0x00, 0x01, 0x04, 0x00, 0x00 });
            sbData.AddRange(BitConverter.GetBytes(Address).Take(3));//起始软元件十六进制大端格式
            sbData.AddRange(new byte[] { GetRegCode(regName) });//软元件代码	 
            sbData.AddRange(BitConverter.GetBytes((Int16)Count));//软元件点数

            //获取请求数据长度  
            var sendData = new List<byte>();//发送数据
            sendData.AddRange(sendHead);
            sendData.AddRange(BitConverter.GetBytes((Int16)sbData.Count()));
            sendData.AddRange(sbData);

            var receiveData = _comm.Send(sendData.ToArray());//接收数据

            //校验接收到的数据
            if (receiveData != null && //接收内容不为空
                receiveData.Length >= receiveDataHeadLength &&//接收内容长度正常
                ByteRangeCompare(receiveData.Take(receiveHead.Length).ToArray(), receiveHead))//接收内容符合格式要求
            {
                ret = new short[Count];//初始化返回值数组
                for (int i = 0; i < Count; i++)//填充返回值数组
                {
                    ret[i] = BitConverter.ToInt16(receiveData, receiveDataHeadLength + i * 2);
                }
            }
            return ret;//返回内容
        }

        public override bool WriteInt16(string regName, int Address, Int16[] values)
        {
            List<byte> sbData = new List<byte>();//初始化帧数据字符串
            sbData.AddRange(new byte[] { 0x10, 0x00, 0x01, 0x14, 0x00, 0x00 });
            sbData.AddRange(BitConverter.GetBytes(Address).Take(3));//起始软元件十六进制大端格式
            sbData.AddRange(new byte[] { GetRegCode(regName) });//软元件代码	 
            sbData.AddRange(BitConverter.GetBytes((Int16)values.Count()));//软元件点数
            foreach (var value in values)
            {
                sbData.AddRange(BitConverter.GetBytes(value));
            }

            //拼接发送内容
            var sendData = new List<byte>();
            sendData.AddRange(sendHead);
            sendData.AddRange(BitConverter.GetBytes(((Int16)sbData.Count())));
            sendData.AddRange(sbData.ToArray());

            var receiveData = _comm.Send(sendData.ToArray());//接收数据

            //校验接收到的数据
            if (receiveData != null && //接收内容不为空
                receiveData.Length >= receiveDataHeadLength &&//接收内容长度正常
                ByteRangeCompare(receiveData.Take(receiveHead.Length).ToArray(), receiveHead))//接收内容符合格式要求
            {
                return true;//校验成功，返回
            }
            return false;//返回内容
        }

        public override UInt16[] ReadUInt16(string regName, int Address, int Count)
        {
            UInt16[] ret = Array.Empty<UInt16>();//初始化返回值 

            List<byte> sbData = new List<byte>();//初始化帧数据字符串
            sbData.AddRange(new byte[] { 0x10, 0x00, 0x01, 0x04, 0x00, 0x00 });
            sbData.AddRange(BitConverter.GetBytes(Address).Take(3));//起始软元件十六进制大端格式
            sbData.AddRange(new byte[] { GetRegCode(regName) });//软元件代码	 
            sbData.AddRange(BitConverter.GetBytes((Int16)Count));//软元件点数

            //获取请求数据长度  
            var sendData = new List<byte>();//发送数据
            sendData.AddRange(sendHead);
            sendData.AddRange(BitConverter.GetBytes((Int16)sbData.Count()));
            sendData.AddRange(sbData);

            var receiveData = _comm.Send(sendData.ToArray());//接收数据

            //校验接收到的数据
            if (receiveData != null && //接收内容不为空
                receiveData.Length >= receiveDataHeadLength &&//接收内容长度正常
                ByteRangeCompare(receiveData.Take(receiveHead.Length).ToArray(), receiveHead))//接收内容符合格式要求
            {
                ret = new ushort[Count];//初始化返回值数组
                for (int i = 0; i < Count; i++)//填充返回值数组
                {
                    ret[i] = BitConverter.ToUInt16(receiveData, receiveDataHeadLength + i * 2);
                }
            }
            return ret;//返回内容
        }

        public override bool WriteUInt16(string regName, int Address, UInt16[] values)
        {
            List<byte> sbData = new List<byte>();//初始化帧数据字符串
            sbData.AddRange(new byte[] { 0x10, 0x00, 0x01, 0x14, 0x00, 0x00 });
            sbData.AddRange(BitConverter.GetBytes(Address).Take(3));//起始软元件十六进制大端格式
            sbData.AddRange(new byte[] { GetRegCode(regName) });//软元件代码	 
            sbData.AddRange(BitConverter.GetBytes((Int16)values.Count()));//软元件点数

            foreach (var value in values)
            {
                sbData.AddRange(BitConverter.GetBytes(value));
            }

            //拼接发送内容
            var sendData = new List<byte>();
            sendData.AddRange(sendHead);
            sendData.AddRange(BitConverter.GetBytes(((Int16)sbData.Count())));
            sendData.AddRange(sbData.ToArray());

            var receiveData = _comm.Send(sendData.ToArray());//接收数据

            //校验接收到的数据
            if (receiveData != null && //接收内容不为空
                receiveData.Length >= receiveDataHeadLength &&//接收内容长度正常
                ByteRangeCompare(receiveData.Take(receiveHead.Length).ToArray(), receiveHead))//接收内容符合格式要求
            {
                return true;//校验成功，返回
            }
            return false;//返回内容
        }

        //32位读写 
        public override Int32[] ReadInt32(string regName, int Address, int Count)
        {
            Int32[] ret = Array.Empty<Int32>();//初始化返回值 

            List<byte> sbData = new List<byte>();//初始化帧数据字符串
            sbData.AddRange(new byte[] { 0x10, 0x00, 0x01, 0x04, 0x00, 0x00 });
            sbData.AddRange(BitConverter.GetBytes(Address).Take(3));//起始软元件十六进制大端格式
            sbData.AddRange(new byte[] { GetRegCode(regName) });//软元件代码	 
            sbData.AddRange(BitConverter.GetBytes((Int16)(Count * 2)));//软元件点数

            //获取请求数据长度  
            var sendData = new List<byte>();//发送数据
            sendData.AddRange(sendHead);
            sendData.AddRange(BitConverter.GetBytes((Int16)sbData.Count()));
            sendData.AddRange(sbData);

            var receiveData = _comm.Send(sendData.ToArray());//接收数据

            //校验接收到的数据
            if (receiveData != null && //接收内容不为空
                receiveData.Length >= receiveDataHeadLength &&//接收内容长度正常
                ByteRangeCompare(receiveData.Take(receiveHead.Length).ToArray(), receiveHead))//接收内容符合格式要求
            {
                ret = new Int32[Count];//初始化返回值数组
                for (int i = 0; i < Count; i++)//填充返回值数组
                {
                    ret[i] = BitConverter.ToInt32(receiveData, receiveDataHeadLength + i * 4);
                }
            }
            return ret;//返回内容
        }

        public override bool WriteInt32(string regName, int Address, Int32[] values)
        {
            List<byte> sbData = new List<byte>();//初始化帧数据字符串
            sbData.AddRange(new byte[] { 0x10, 0x00, 0x01, 0x14, 0x00, 0x00 });
            sbData.AddRange(BitConverter.GetBytes(Address).Take(3));//起始软元件十六进制大端格式
            sbData.AddRange(new byte[] { GetRegCode(regName) });//软元件代码	 
            sbData.AddRange(BitConverter.GetBytes((Int16)(values.Count() * 2)));//软元件点数
            foreach (var value in values)
            {
                sbData.AddRange(BitConverter.GetBytes(value));
            }

            //拼接发送内容
            var sendData = new List<byte>();
            sendData.AddRange(sendHead);
            sendData.AddRange(BitConverter.GetBytes(((Int16)sbData.Count())));
            sendData.AddRange(sbData.ToArray());

            var receiveData = _comm.Send(sendData.ToArray());//接收数据

            //校验接收到的数据
            if (receiveData != null && //接收内容不为空
                receiveData.Length >= receiveDataHeadLength &&//接收内容长度正常
                ByteRangeCompare(receiveData.Take(receiveHead.Length).ToArray(), receiveHead))//接收内容符合格式要求
            {
                return true;//校验成功，返回
            }
            return false;//返回内容
        }

        public override UInt32[] ReadUInt32(string regName, int Address, int Count)
        {
            UInt32[] ret = Array.Empty<UInt32>();//初始化返回值 

            List<byte> sbData = new List<byte>();//初始化帧数据字符串
            sbData.AddRange(new byte[] { 0x10, 0x00, 0x01, 0x04, 0x00, 0x00 });
            sbData.AddRange(BitConverter.GetBytes(Address).Take(3));//起始软元件十六进制大端格式
            sbData.AddRange(new byte[] { GetRegCode(regName) });//软元件代码	 
            sbData.AddRange(BitConverter.GetBytes((Int16)Count * 2));//软元件点数

            //获取请求数据长度  
            var sendData = new List<byte>();//发送数据
            sendData.AddRange(sendHead);
            sendData.AddRange(BitConverter.GetBytes((Int16)sbData.Count()));
            sendData.AddRange(sbData);

            var receiveData = _comm.Send(sendData.ToArray());//接收数据

            //校验接收到的数据
            if (receiveData != null && //接收内容不为空
                receiveData.Length >= receiveDataHeadLength &&//接收内容长度正常
                ByteRangeCompare(receiveData.Take(receiveHead.Length).ToArray(), receiveHead))//接收内容符合格式要求
            {
                ret = new UInt32[Count];//初始化返回值数组
                for (int i = 0; i < Count; i++)//填充返回值数组
                {
                    ret[i] = BitConverter.ToUInt32(receiveData, receiveDataHeadLength + i * 4);
                }
            }
            return ret;//返回内容
        }

        public override bool WriteUInt32(string regName, int Address, UInt32[] values)
        {
            List<byte> sbData = new List<byte>();//初始化帧数据字符串
            sbData.AddRange(new byte[] { 0x10, 0x00, 0x01, 0x14, 0x00, 0x00 });
            sbData.AddRange(BitConverter.GetBytes(Address).Take(3));//起始软元件十六进制大端格式
            sbData.AddRange(new byte[] { GetRegCode(regName) });//软元件代码	 
            sbData.AddRange(BitConverter.GetBytes((Int16)(values.Count() * 2)));//软元件点数
            foreach (var value in values)
            {
                sbData.AddRange(BitConverter.GetBytes(value));
            }

            //拼接发送内容
            var sendData = new List<byte>();
            sendData.AddRange(sendHead);
            sendData.AddRange(BitConverter.GetBytes(((Int16)sbData.Count())));
            sendData.AddRange(sbData.ToArray());

            var receiveData = _comm.Send(sendData.ToArray());//接收数据

            //校验接收到的数据
            if (receiveData != null && //接收内容不为空
                receiveData.Length >= receiveDataHeadLength &&//接收内容长度正常
                ByteRangeCompare(receiveData.Take(receiveHead.Length).ToArray(), receiveHead))//接收内容符合格式要求
            {
                return true;//校验成功，返回
            }
            return false;//返回内容
        }

        //读写浮点数
        public override Single[] ReadSingle(string regName, int Address, int Count)
        {
            Single[] ret = Array.Empty<Single>();//初始化返回值 

            List<byte> sbData = new List<byte>();//初始化帧数据字符串
            sbData.AddRange(new byte[] { 0x10, 0x00, 0x01, 0x04, 0x00, 0x00 });
            sbData.AddRange(BitConverter.GetBytes(Address).Take(3));//起始软元件十六进制大端格式
            sbData.AddRange(new byte[] { GetRegCode(regName) });//软元件代码	 
            sbData.AddRange(BitConverter.GetBytes((Int16)(Count * 2)));//软元件点数

            //获取请求数据长度  
            var sendData = new List<byte>();//发送数据
            sendData.AddRange(sendHead);
            sendData.AddRange(BitConverter.GetBytes((Int16)sbData.Count()));
            sendData.AddRange(sbData);

            var receiveData = _comm.Send(sendData.ToArray());//接收数据

            //校验接收到的数据
            if (receiveData != null && //接收内容不为空
                receiveData.Length >= receiveDataHeadLength &&//接收内容长度正常
                ByteRangeCompare(receiveData.Take(receiveHead.Length).ToArray(), receiveHead))//接收内容符合格式要求
            {
                ret = new Single[Count];//初始化返回值数组
                for (int i = 0; i < Count; i++)//填充返回值数组
                {
                    ret[i] = BitConverter.ToSingle(receiveData, receiveDataHeadLength + i * 4);
                }
            }
            return ret;//返回内容
        }

        public override bool WriteSingle(string regName, int Address, Single[] values)
        {
            List<byte> sbData = new List<byte>();//初始化帧数据字符串
            sbData.AddRange(new byte[] { 0x10, 0x00, 0x01, 0x14, 0x00, 0x00 });
            sbData.AddRange(BitConverter.GetBytes(Address).Take(3));//起始软元件十六进制大端格式
            sbData.AddRange(new byte[] { GetRegCode(regName) });//软元件代码	 
            sbData.AddRange(BitConverter.GetBytes((Int16)(values.Count() * 2)));//软元件点数
            foreach (var value in values)
            {
                sbData.AddRange(BitConverter.GetBytes(value));
            }

            //拼接发送内容
            var sendData = new List<byte>();
            sendData.AddRange(sendHead);
            sendData.AddRange(BitConverter.GetBytes(((Int16)sbData.Count())));
            sendData.AddRange(sbData.ToArray());

            var receiveData = _comm.Send(sendData.ToArray());//接收数据

            //校验接收到的数据
            if (receiveData != null && //接收内容不为空
                receiveData.Length >= receiveDataHeadLength &&//接收内容长度正常
                ByteRangeCompare(receiveData.Take(receiveHead.Length).ToArray(), receiveHead))//接收内容符合格式要求
            {
                return true;//校验成功，返回
            }
            return false;//返回内容
        }

        //读写字符串
        public override string ReadString(string regName, int Address, int Count)
        {
            string ret = "";//初始化返回值 

            List<byte> sbData = new List<byte>();//初始化帧数据字符串
            sbData.AddRange(new byte[] { 0x10, 0x00, 0x01, 0x04, 0x00, 0x00 });
            sbData.AddRange(BitConverter.GetBytes(Address).Take(3));//起始软元件十六进制大端格式
            sbData.AddRange(new byte[] { GetRegCode(regName) });//软元件代码	 
            sbData.AddRange(BitConverter.GetBytes((Int16)Count));//软元件点数 

            //获取请求数据长度  
            var sendData = new List<byte>();//发送数据
            sendData.AddRange(sendHead);
            sendData.AddRange(BitConverter.GetBytes((Int16)sbData.Count()));
            sendData.AddRange(sbData);

            var receiveData = _comm.Send(sendData.ToArray());//接收数据

            //校验接收到的数据
            if (receiveData != null && //接收内容不为空
                receiveData.Length >= receiveDataHeadLength &&//接收内容长度正常
                ByteRangeCompare(receiveData.Take(receiveHead.Length).ToArray(), receiveHead))//接收内容符合格式要求
            {
                byte[] dat = new byte[Count];
                Array.Copy(receiveData, receiveDataHeadLength, dat, 0, Count);
                return Encoding.ASCII.GetString(dat);
                //return Encoding.ASCII.GetString(receiveData.Skip(receiveDataHeadLength).Take(Count).ToArray());
            }
            return ret;//返回内容
        }

        public override bool WriteString(string regName, int Address, string values)
        {
            List<byte> sbData = new List<byte>();//初始化帧数据字符串
            sbData.AddRange(new byte[] { 0x10, 0x00, 0x01, 0x14, 0x00, 0x00 });
            sbData.AddRange(BitConverter.GetBytes(Address).Take(3));//起始软元件十六进制大端格式
            sbData.AddRange(new byte[] { GetRegCode(regName) });//软元件代码	 
            sbData.AddRange(BitConverter.GetBytes((Int16)values.Count()));//软元件点数

            sbData.AddRange(Encoding.ASCII.GetBytes(values));//转换为ASCII码再写进去

            //获取请求数据长度  
            var sendData = new List<byte>();//发送数据
            sendData.AddRange(sendHead);
            sendData.AddRange(BitConverter.GetBytes((Int16)sbData.Count()));
            sendData.AddRange(sbData);

            var receiveData = _comm.Send(sendData.ToArray());//接收数据

            //校验接收到的数据
            if (receiveData != null && //接收内容不为空
                receiveData.Length >= receiveDataHeadLength &&//接收内容长度正常
                ByteRangeCompare(receiveData.Take(receiveHead.Length).ToArray(), receiveHead))//接收内容符合格式要求
            {
                return true;//校验成功，返回
            }
            return false;//返回内容
        }
    }

    /// <summary>
    /// 为保持兼容而封装的密封类
    /// </summary>
    public sealed class MC_3E2 : MC_3Ebase2
    {
        //以太网方式
        //最简构造函数
        public MC_3E2(string ip, int port) : base(new CommTCP(ip, port)) {; }

        //不带信号量初始的构造函数
        public MC_3E2(string ip, int port, int timeOut) : base(new CommTCP(ip, port, timeOut)) {; }

        //全参构造函数
        public MC_3E2(string ip, int port, int timeOut, int minSemaphore, int maxSemaphore) : base(new CommTCP(ip, port, timeOut, minSemaphore, maxSemaphore)) {; }

        //串口方式
        //最简构造函数
        public MC_3E2(string portName, int baudRate, int dataBits, Parity parity, StopBits stopBits) : base(new CommSerialPort(portName, baudRate, dataBits, parity, stopBits)) {; }

        //不带信号量初始的构造函数
        public MC_3E2(string portName, int baudRate, int dataBits, Parity parity, StopBits stopBits, int timeOut) : base(new CommSerialPort(portName, baudRate, dataBits, parity, stopBits, timeOut)) {; }

        //全参构造函数
        public MC_3E2(string portName, int baudRate, int dataBits, Parity parity, StopBits stopBits, int timeOut, int minSemaphore, int maxSemaphore) : base(new CommSerialPort(portName, baudRate, dataBits, parity, stopBits, timeOut, minSemaphore, maxSemaphore)) {; }

    }
}
