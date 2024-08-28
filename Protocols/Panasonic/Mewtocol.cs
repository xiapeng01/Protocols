using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Protocols.Protocols
{
    public class Mewtocol : ProtocolBase
    { 
        private readonly object Lock_Serial = new object();
        private readonly object LogFile = new object();

        private string[] RegType = new string[] { "D", "R", "X", "Y" };//地址类型
        private string AdrPatt = @"[0-9a-fA-F]+";//构造地址模式字符串

        public enum AddressingMode { Bit = 0, Word = 1 }

        public Mewtocol(IComm comm) : base(comm)
        {
        }

        public byte[] ReadData(string regName,int Address, AddressingMode AddressMode, int RegisterLength)
        {
            byte[] ret = null;
            if (regName.Contains(".")) return null;//暂不支持带点位寻址的方式
            /////////////////////////////////////////////////////////////////////////////////////////////////////
            ///用正则表达式拆分地址内容

            //var RegPatt = @"[(" + string.Join("),(", RegType) + @")]+";//构造寄存器模式字符串            

            //var res = Regex.Match(regName, RegPatt);//先匹配寄存器名称
            //var temp = regName.Remove(res.Index, res.Value.Length);//去除寄存器名称
            //var Adr = Regex.Match(temp, AdrPatt);//匹配地址

            //string RegisterType = res.Value;//左边第一个为寄存器类型
            //if (string.IsNullOrEmpty(RegisterType)) throw new InvalidOperationException("未设置正确的寄存器类型");
            //string Address = Adr.Value;//地址编号
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////

            if (string.IsNullOrEmpty(regName)) throw new InvalidOperationException("无效的寄存器名称");
            string str = ReadRegister(0xEE, regName, Address, RegisterLength);
            if (str == null) return null;
            ret = Enumerable.Range(0, str.Length / 2).Select(x => Convert.ToByte(str.Substring(x * 2, 2), 16)).ToArray(); ;
            return ret;
        }

        public bool WriteData(string regName,int Address, AddressingMode AddressMode, byte[] Data)
        {
            if (regName.Contains(".")) return false;//暂不支持带点位寻址的方式
            /////////////////////////////////////////////////////////////////////////////////////////////////////
            ///用正则表达式拆分地址内容                         

            if(string.IsNullOrEmpty(regName)) throw new InvalidOperationException("无效的寄存器名称");
            switch (AddressMode)
            {
                case AddressingMode.Bit://位寻址方式
                    {
                        string s = string.Join(string.Empty, Data.Select(b => b == 1 ? "1" : "0"));
                        return WriteRegister(0xEE, regName, Address, Data.Length, s);
                    }
                case AddressingMode.Word://字寻找方式
                    {
                        return WriteRegister(0xEE, regName, Address, Data.Length, BitConverter.ToString(Data).Replace("-", string.Empty));
                    }
            }
            return false;
        }


        public string ReadRegister(int Station, string RegisterName, int Address, int Count)
        {
            string Ret = string.Empty;
            switch (RegisterName.ToUpper())
            {
                case "X":
                case "Y":
                case "R":
                    Ret = ReadBit(Station, RegisterName, Address, Count);//位地址按十六进制解析
                    break;
                case "D":
                case "DT":
                case "DDT":
                    Ret = ReadWord(Station, "D", Address, Count);//字地址按十进制解析
                    break;
                default: throw new InvalidOperationException("不支持的寄存器类型！");
            }
            return Ret;
        }

        public bool WriteRegister(int Station, string RegisterName, int Address, int Count, string Data)
        {
            bool Ret = false;
            switch (RegisterName.ToUpper())
            {
                case "Y":
                case "R":
                    Ret = WriteBit(Station, RegisterName, Address, Count, Data);//位地址按十六进制解析
                    break;
                case "D":
                case "DT":
                case "DDT":
                    Ret = WriteWord(Station, RegisterName, Address, Count / 2, Data);//字地址按十进制解析
                    break;
                default: throw new InvalidOperationException("不支持的寄存器类型！");
            }
            return Ret;
        }

        private string ReadWord(int Station, string strRegName, int Address, int addressCount)
        {
            lock (Lock_Serial)
            {
                string strSend;
                string strReceive = string.Empty;
                string ret = string.Empty;

                // 组帧-字地址时地址宽度为5字符
                strSend = @"%" + Station.ToString("X2") + "#RD" + strRegName.ToUpper() + Address.ToString("D5") + (Address + addressCount - 1).ToString("D5");
                strSend += BCC(strSend) + "\r";// 字符添加校验码和回车结束符

                //发送接收//因Mewtocol是字符串协议，在此处转换 
                strReceive = _comm.Send(strSend);//已改写

                if (strReceive.Length < 4 || strReceive == string.Empty || strReceive[3] != '$') return null;//数据错误

                // 解帧
                ret = strReceive.Substring(6, addressCount * 4);
                return ret;
            }
        }


        private string ReadBit(int Station, string strRegName, int Address, int AddressCount)
        {
            lock (Lock_Serial)
            {
                //位寻址时为Hex地址，末尾最高到F
                //单个位的读取方式RCS+寄存器类型+寄存器个数（固定1）+校验+回车
                //多个位的读取方式RCC+寄存器类型+寄存器起始地址(字地址)+寄存器终点地址（字地址）+校验+回车，然后从中检索内容

                string strSend;
                string strReceive = string.Empty;
                string ret = string.Empty;

                if (AddressCount == 1)//单位模式
                {
                    //组帧
                    strSend = @"%" + Station.ToString("X2") + "#RCS" + strRegName.ToUpper() + Address.ToString("X4");
                    strSend += BCC(strSend) + "\r";

                    //发送接收//因Mewtocol是字符串协议，在此处转换
                    strReceive = _comm.Send(strSend);//已改写

                    //解帧
                    if (strReceive.Length < 4 || strReceive == string.Empty || strReceive[3] != '$') return null;//解帧出错在此处                    
                    ret = strReceive.Substring(6, 1).Insert(0, "0");// 解析串口接收的数据-单位结构解析方法

                    return ret;
                }
                else//多位模式
                {
                    int startAddress = (Address / 0x10);
                    int endAddress = ((Address + AddressCount - 1) / 0x10);

                    // 组帧
                    strSend = @"%" + Station.ToString("X2") + "#RCC" + strRegName.ToUpper() + startAddress.ToString("X4") + endAddress.ToString("X4");

                    // 字符添加校验码和回车结束符
                    strSend += BCC(strSend) + "\r";

                    //发送接收//因Mewtocol是字符串协议，在此处转换
                    strReceive = _comm.Send(strSend);//已改写

                    //解帧
                    if (strReceive.Length < 4 || strReceive == string.Empty || strReceive[3] != '$') return null;//解帧出错在此处

                    //为保持对终端的兼容性
                    var str = strReceive.Substring(6, (endAddress - startAddress + 1) * 4);
                    string str1 = string.Join(string.Empty, Enumerable.Range(0, str.Length / 2).Select(x => string.Join("", Convert.ToString(Convert.ToByte(str.Substring(x * 2, 2), 16), 2).PadLeft(8, '0').Reverse())).ToArray());
                    ret = string.Join("0", str1.Substring(Address % 0X10, AddressCount).ToArray()).Insert(0, "0");

                    return ret;
                }
            }
        }

        private bool WriteWord(int Station, string memory, int address, int addressCount, string data)
        {
            lock (Lock_Serial)
            {
                string strSend;
                string strReceive = string.Empty;

                // 组帧-字地址时地址宽度为5字符
                strSend = @"%" + Station.ToString("X2") + "#WD" + memory.ToUpper() + address.ToString("d5") + (address + addressCount - 1).ToString("d5") + data;
                strSend += BCC(strSend) + "\r";// 字符添加校验码和回车结束符

                //发送接收//因Mewtocol是字符串协议，在此处转换
                strReceive = _comm.Send(strSend);//已改写

                //解帧
                if (strReceive == string.Empty || strReceive[3] != '$') return false;
                return true;
            }
        }

        private bool WriteBit(int Station, string memory, int address, int addressCount, string data)
        {
            lock (Lock_Serial)
            {
                string strSend;
                string strReceive = string.Empty;

                if (addressCount == 1)
                {
                    string strStartAddress = address.ToString("d4");
                    string strEndAddress = (address + addressCount - 1).ToString("d4");

                    //组帧
                    strSend = @"%" + Station.ToString("X2") + "#WCS" + memory.ToUpper() + strStartAddress + data;
                    strSend += BCC(strSend) + "\r";// 字符添加校验码和回车结束符

                    //发送接收//因Mewtocol是字符串协议，在此处转换
                    strReceive = _comm.Send(strSend);//已改写

                    //解帧
                    if (strReceive == string.Empty || strReceive[3] != '$') return false;
                    return true;
                }
                else
                {
                    if (address % 0x10 != 0) return false;//起始地址不为0x10的整数倍
                    if (addressCount % 0x10 != 0) return false;//地址数量不为0x10的整数倍

                    int startAddress = address / 0x10;
                    int endAddress = (address + addressCount - 1) / 0x10;

                    //组帧
                    strSend = @"%" + Station.ToString("X2") + "#WCC" + memory.ToUpper() + startAddress.ToString("X4") + endAddress.ToString("X4") + string.Join(string.Empty, Enumerable.Range(0, data.Length / 8).Select(e => Convert.ToByte(string.Join("", data.Substring(e * 8, 8)), 2).ToString("X2").Reverse().ToArray()));
                    strSend += BCC(strSend) + "\r";// 字符添加校验码和回车结束符

                    //发送接收//因Mewtocol是字符串协议，在此处转换
                    strReceive = _comm.Send(strSend);//已改写

                    //解帧
                    if (strReceive == string.Empty || strReceive[3] != '$') return false;

                    return true;
                }
            }
        }

        /// <summary>
        /// 计算BCC校验码
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        string BCC(string str)
        {
            byte res = 0;
            return str.ToArray().Select(x => res ^= (byte)x).Last().ToString("X2");
        }

        //抽象类的抽象方法必须由子类实现，虚方法可以在只有需要的时候才实现。
        //bool读写
        public override bool[] ReadBool(string regName, int Address, int Count) 
        {
            bool[] ret = null;
            var v = ReadData(regName , Address, AddressingMode.Bit, Count);
            if (v != null) ret = v.Select(x => x == 1).ToArray();
            return ret;
        }
        public override bool WriteBool(string regName, int Address, bool[] Values) 
        {
            if (Values == null) return false;
            return WriteData(regName , Address, AddressingMode.Bit, Values.Select(x => (byte)(x ? 1 : 0)).ToArray());
        }

        //16位读写
        public override Int16[] ReadInt16(string regName, int Address, int Count) 
        {
            Int16[] ret = null;
            int size = sizeof(Int16);
            var v = ReadData(regName , Address, AddressingMode.Word, Count * (size / 2));
            if (v != null) ret = Enumerable.Range(0, Count).Select(x => BitConverter.ToInt16(v, size * x)).ToArray();
            return ret;
        }
        public override bool WriteInt16(string regName, int Address, Int16[] Values) 
        {
            if (Values == null) return false;
            int size = sizeof(short);
            byte[] buffer = new byte[sizeof(short) * Values.Length];
            for (int i = 0; i < Values.Length; i++)
            {
                Array.Copy(BitConverter.GetBytes(Values[i]), 0, buffer, i * size, size);
            }
            return WriteData(regName , Address, AddressingMode.Word, buffer);
        }
        public override UInt16[] ReadUInt16(string regName, int Address, int Count) 
        {
            UInt16[] ret = null;
            int size = sizeof(UInt16);
            var v = ReadData(regName , Address, AddressingMode.Word, Count * (size / 2));
            if (v != null) ret = Enumerable.Range(0, Count).Select(x => BitConverter.ToUInt16(v, size * x)).ToArray();
            return ret;
        }
        public override bool WriteUInt16(string regName, int Address, UInt16[] Values) 
        {
            if (Values == null) return false;
            int size = sizeof(UInt16);
            byte[] buffer = new byte[sizeof(UInt16) * Values.Length];
            for (int i = 0; i < Values.Length; i++)
            {
                Array.Copy(BitConverter.GetBytes(Values[i]), 0, buffer, i * size, size);
            }
            return WriteData(regName , Address, AddressingMode.Word, buffer);
        }

        //32位读写
        public override Int32[] ReadInt32(string regName, int Address, int Count)
        {
            Int32[] ret = null;
            int size = sizeof(Int32);
            var v = ReadData(regName , Address, AddressingMode.Word, Count * (size / 2));
            if (v != null) ret = Enumerable.Range(0, Count).Select(x => BitConverter.ToInt32(v, size * x)).ToArray();
            return ret;
        }
        public override bool WriteInt32(string regName, int Address, Int32[] Values)
        {
            if (Values == null) return false;
            int size = sizeof(Int32);
            byte[] buffer = new byte[sizeof(Int32) * Values.Length];
            for (int i = 0; i < Values.Length; i++)
            {
                Array.Copy(BitConverter.GetBytes(Values[i]), 0, buffer, i * size, size);
            }
            return WriteData(regName , Address, AddressingMode.Word, buffer);
        }
        public override UInt32[] ReadUInt32(string regName, int Address, int Count)
        {
            UInt32[] ret = null;
            int size = sizeof(UInt32);
            var v = ReadData(regName , Address, AddressingMode.Word, Count * (size / 2));
            if (v != null) ret = Enumerable.Range(0, Count).Select(x => BitConverter.ToUInt32(v, size * x)).ToArray();
            return ret;
        }
        public override bool WriteUInt32(string regName, int Address, UInt32[] Values)
        {
            if (Values == null) return false;
            int size = sizeof(UInt32);
            byte[] buffer = new byte[sizeof(UInt32) * Values.Length];
            for (int i = 0; i < Values.Length; i++)
            {
                Array.Copy(BitConverter.GetBytes(Values[i]), 0, buffer, i * size, size);
            }
            return WriteData(regName , Address, AddressingMode.Word, buffer);
        }



        public override Single[] ReadSingle(string regName, int Address, int Count)
        {
            float[] ret = null;
            int size = sizeof(Single);
            var v = ReadData(regName , Address, AddressingMode.Word, Count * (size / 2));
            if (v != null) ret = Enumerable.Range(0, Count).Select(x => BitConverter.ToSingle(v, size * x)).ToArray();
            return ret;
        }
        public override bool WriteSingle(string regName, int Address, Single[] Values)
        {
            if (Values == null) return false;
            int size = sizeof(Single);
            byte[] buffer = new byte[sizeof(Single) * Values.Length];
            for (int i = 0; i < Values.Length; i++)
            {
                Array.Copy(BitConverter.GetBytes(Values[i]), 0, buffer, i * size, size);
            }
            return WriteData(regName , Address, AddressingMode.Word, buffer);
        }


        public override string ReadString(string regName, int Address, int Count)
        {
            string ret = null;
            var v = ReadData(regName , Address, AddressingMode.Word, Count % 2 == 0 ? Count / 2 : (Count / 2 + 1));
            if (v != null)
            {
                ret = Encoding.ASCII.GetString(v);
                if (Count % 2 == 1) ret = ret.Remove(ret.Length - 1);//如果字符数为基数，去除末尾内容
            }
            return ret;
        }
        public override bool WriteString(string regName, int Address, string Values)
        {
            if (Values == null) return false;
            var buffer = Encoding.ASCII.GetBytes(Values.Length % 2 == 0 ? Values : Values + (char)0x00);
            return WriteData(regName , Address , AddressingMode.Word, buffer);
        }
    }

}
