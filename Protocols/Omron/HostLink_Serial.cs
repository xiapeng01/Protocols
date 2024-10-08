using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Protocols.Omron
{
    /// <summary>
    /// 使用头编码FA的情况
    /// </summary>
    public class HostLink_Serial : ProtocolBase
    {
        string frameHead = "@00FA";

        /// <summary>
        /// 串口时固定使用7位偶校验1位停止位，默认9600
        /// </summary>
        /// <param name="comm"></param>
        public HostLink_Serial(IComm comm) : base(comm)
        {
        }        

        /// <summary>
        /// Mode取值范围false=字模式，true=位模式
        /// 存储区代号=>D位:02，D字:82，W位:31，C位:30，W字:B1，C字:B0
        /// </summary>
        /// <param name="RegName"></param>
        /// <param name="Mode"></param>
        /// <returns></returns>
        string GetRegisterCode(string RegName,bool Mode)
        {
            switch (RegName)
            {
                default:
                case "CIO":
                case "": return Mode ? "30" : "B0";
                case "W": return Mode ? "31" : "B1";
                case "D": return Mode ? "02" : "82";
            }
        }

        /// <summary>
        /// 校验码
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        byte FCS(string str)
        {
            var data = Encoding.ASCII.GetBytes(str);
            byte ret = 0;
            foreach (var item in data)
            {
                ret ^= item;
            }
            return (byte)ret;
        }

        bool CheckFCS(string frame)
        { 
            string FCS1 = frame.Substring(frame.Length-4,2);
            string FCS2 = FCS(frame.Substring(0,frame.Length-4)).ToString("X2");
            return FCS1.Equals(FCS2);
        }

        bool CheckFrame(ref string frame)
        {
            if(CheckFrame(ref frame,frameHead))
            {
                return CheckFCS(frame);
            }
            return false;
        }

        //发送：@00FA000000000010130000000000571*\CR
        //接收：@00FA0040000000 01010000010000010142*\CR

        /// <summary>
        /// 
        /// </summary>
        /// <param name="regName">寄存器名称</param>
        /// <param name="Address">十六进制格式地址</param>
        /// <param name="Count">读取数量</param>
        /// <returns></returns>
        public override bool[] ReadBool(string regName, int Address, int Count)
        {
            var ret = Array.Empty<bool>();
            string strHead = "";
            string strData = "";
            strHead += "@";//起始标志
            strHead += "00";//PLC地址
            strHead += "FA";//头编码取值范围:FA,RD,WR
            strHead += "0";//等待单位
            strHead += "00";//SID
            strHead += "00";//SA2
            strHead += "00";//DA2
            strHead += "00";//ICF
            strHead += "0101";//0101=读取命令，0102=写入命令
            strHead += GetRegisterCode(regName, true);//获取存储区代码
            strHead += ((Int16)(Address / 100)).ToString("X4") + ((byte)(Address % 100)).ToString("X2");//起始地址
            strHead += ((Int16)Count).ToString("X4");//读取个数

            strData += FCS(strHead).ToString("X2");
            strData += "*";
            strData += '\r';

            string sendStr=strHead + strData;
            var receiveStr=_comm.Send(sendStr);
            if (CheckFrame(ref receiveStr))//帧校验通过
            {
                //解析接收到的数据
                ret = new bool[Count];
                int DataStartPos = 23;
                for(int i=0;i<Count;i++)
                {
                    ret[i] = receiveStr.Substring(DataStartPos + i * 2, 2).Equals("01");
                }
                return ret;
            }
            return null;            
        }

        public override bool WriteBool(string regName, int Address, bool[] Values)
        {
            string strHead = "";
            string strData = "";
            strHead += "@";//起始标志
            strHead += "00";//PLC地址
            strHead += "FA";//头编码取值范围:FA,RD,WR
            strHead += "0";//等待单位
            strHead += "00";//SID
            strHead += "00";//SA2
            strHead += "00";//DA2
            strHead += "00";//ICF
            strHead += "0102";//0101=读取命令，0102=写入命令
            strHead += GetRegisterCode(regName, true);//获取存储区代码
            strHead += ((Int16)(Address / 100)).ToString("X4") + ((byte)(Address % 100)).ToString("X2");//起始地址
            strHead += (((Int16)Values.Length)).ToString("X4");//读取个数

            //插入数据
            foreach (var item in Values)
            {
                strHead += item?"01":"00";
            }

            strData += FCS(strHead).ToString("X2");
            strData += "*";
            strData += '\r';

            string sendStr = strHead + strData;
            var receiveStr = _comm.Send(sendStr);
            if (CheckFrame(ref receiveStr))//帧校验通过
            {
                return true;
            }
            return false;
        }

        //16位字读写
        public override short[] ReadInt16(string regName, int Address, int Count)
        {
            var ret = Array.Empty<Int16>();
            string strHead = "";
            string strData = "";
            strHead += "@";//起始标志
            strHead += "00";//PLC地址
            strHead += "FA";//头编码取值范围:FA,RD,WR
            strHead += "0";//等待单位
            strHead += "00";//SID
            strHead += "00";//SA2
            strHead += "00";//DA2
            strHead += "00";//ICF
            strHead += "0101";//0101=读取命令，0102=写入命令
            strHead += GetRegisterCode(regName, false);//获取存储区代码
            strHead += ((Int16)Address).ToString("X4") + "00";//起始地址
            strHead += ((Int16)Count).ToString("X4");//读取个数

            strData += FCS(strHead).ToString("X2");
            strData += "*";
            strData += '\r';

            string sendStr = strHead + strData;
            var receiveStr = _comm.Send(sendStr);
            if (CheckFrame(ref receiveStr))//帧校验通过
            {
                //解析接收到的数据
                int DataStartPos = 23;
                ret = new short[Count];

                var s1 = receiveStr.Substring(DataStartPos, Count * 4);

                for(int i=0;i<Count;i++)
                {
                    ret[i] = BitConverter.ToInt16(HexStringToByteArray(receiveStr.Substring(DataStartPos+i*4,4)).Reverse().ToArray(), 0);
                }
                return ret;
            }
            return null;
        }

        public override bool WriteInt16(string regName, int Address, short[] Values)
        { 
            string strHead = "";
            string strData = "";
            strHead += "@";//起始标志
            strHead += "00";//PLC地址
            strHead += "FA";//头编码取值范围:FA,RD,WR
            strHead += "0";//等待单位
            strHead += "00";//SID
            strHead += "00";//SA2
            strHead += "00";//DA2
            strHead += "00";//ICF
            strHead += "0102";//0101=读取命令，0102=写入命令
            strHead += GetRegisterCode(regName, false);//获取存储区代码
            strHead += ((Int16)Address).ToString("X4") + "00";//起始地址
            strHead += (((Int16)Values.Length)).ToString("X4");//读取个数

            //插入数据
            foreach(var item in Values)
            {
                strHead += BitConverter.ToString(BitConverter.GetBytes(item).Reverse().ToArray()).Replace("-","");
            }

            strData += FCS(strHead).ToString("X2");
            strData += "*";
            strData += '\r';

            string sendStr = strHead + strData;
            var receiveStr = _comm.Send(sendStr);
            if (CheckFrame(ref receiveStr))//帧校验通过
            { 
                return true;
            }
            return false;
        }

        //无符号16位字读写
        public override ushort[] ReadUInt16(string regName, int Address, int Count)
        {
            var ret = Array.Empty<UInt16>();
            string strHead = "";
            string strData = "";
            strHead += "@";//起始标志
            strHead += "00";//PLC地址
            strHead += "FA";//头编码取值范围:FA,RD,WR
            strHead += "0";//等待单位
            strHead += "00";//SID
            strHead += "00";//SA2
            strHead += "00";//DA2
            strHead += "00";//ICF
            strHead += "0101";//0101=读取命令，0102=写入命令
            strHead += GetRegisterCode(regName, false);//获取存储区代码
            strHead += ((Int16)Address).ToString("X4") + "00";//起始地址
            strHead += ((Int16)Count).ToString("X4");//读取个数

            strData += FCS(strHead).ToString("X2");
            strData += "*";
            strData += '\r';

            string sendStr = strHead + strData;
            var receiveStr = _comm.Send(sendStr);
            if (CheckFrame(ref receiveStr))//帧校验通过
            {
                //解析接收到的数据
                int DataStartPos = 23;
                ret = new ushort[Count];

                var s1 = receiveStr.Substring(DataStartPos, Count * 4);

                for (int i = 0; i < Count; i++)
                {
                    ret[i] = BitConverter.ToUInt16(HexStringToByteArray(receiveStr.Substring(DataStartPos + i * 4, 4)).Reverse().ToArray(), 0);
                }
                return ret;
            }
            return null;
        }

        public override bool WriteUInt16(string regName, int Address, ushort[] Values)
        {
            string strHead = "";
            string strData = "";
            strHead += "@";//起始标志
            strHead += "00";//PLC地址
            strHead += "FA";//头编码取值范围:FA,RD,WR
            strHead += "0";//等待单位
            strHead += "00";//SID
            strHead += "00";//SA2
            strHead += "00";//DA2
            strHead += "00";//ICF
            strHead += "0102";//0101=读取命令，0102=写入命令
            strHead += GetRegisterCode(regName, false);//获取存储区代码
            strHead += ((Int16)Address).ToString("X4") + "00";//起始地址
            strHead += (((Int16)Values.Length)).ToString("X4");//读取个数

            //插入数据
            foreach (var item in Values)
            {
                strHead += BitConverter.ToString(BitConverter.GetBytes(item).Reverse().ToArray()).Replace("-", "");
            }

            strData += FCS(strHead).ToString("X2");
            strData += "*";
            strData += '\r';

            string sendStr = strHead + strData;
            var receiveStr = _comm.Send(sendStr);
            if (CheckFrame(ref receiveStr))//帧校验通过
            {
                return true;
            }
            return false;
        }

        //32位字读写
        public override Int32[] ReadInt32(string regName, int Address, int Count)
        {
            var ret = Array.Empty<Int32>();
            string strHead = "";
            string strData = "";
            strHead += "@";//起始标志
            strHead += "00";//PLC地址
            strHead += "FA";//头编码取值范围:FA,RD,WR
            strHead += "0";//等待单位
            strHead += "00";//SID
            strHead += "00";//SA2
            strHead += "00";//DA2
            strHead += "00";//ICF
            strHead += "0101";//0101=读取命令，0102=写入命令
            strHead += GetRegisterCode(regName, false);//获取存储区代码
            strHead += ((Int16)Address).ToString("X4") + "00";//起始地址
            strHead += ((Int16)Count*2).ToString("X4");//读取个数

            strData += FCS(strHead).ToString("X2");
            strData += "*";
            strData += '\r';

            string sendStr = strHead + strData;
            var receiveStr = _comm.Send(sendStr);
            if (CheckFrame(ref receiveStr))//帧校验通过
            {
                //解析接收到的数据
                int DataStartPos = 23;
                ret = new Int32[Count];

                var s1 = receiveStr.Substring(DataStartPos, Count * 4);

                for (int i = 0; i < Count; i++)
                {
                    ret[i] = ToBigEndian(BitConverter.ToInt32(HexStringToByteArray(receiveStr.Substring(DataStartPos + i * 8, 8)).Reverse().ToArray(), 0));
                }
                return ret;
            }
            return null;
        }

        public override bool WriteInt32(string regName, int Address, Int32[] Values)
        {
            string strHead = "";
            string strData = "";
            strHead += "@";//起始标志
            strHead += "00";//PLC地址
            strHead += "FA";//头编码取值范围:FA,RD,WR
            strHead += "0";//等待单位
            strHead += "00";//SID
            strHead += "00";//SA2
            strHead += "00";//DA2
            strHead += "00";//ICF
            strHead += "0102";//0101=读取命令，0102=写入命令
            strHead += GetRegisterCode(regName, false);//获取存储区代码
            strHead += ((Int16)Address).ToString("X4") + "00";//起始地址
            strHead += (((Int16)Values.Length*2)).ToString("X4");//读取个数

            //插入数据
            foreach (var item in Values)
            {
                strHead += BitConverter.ToString(BitConverter.GetBytes((item))).Replace("-", "");
            }

            strData += FCS(strHead).ToString("X2");
            strData += "*";
            strData += '\r';

            string sendStr = strHead + strData;
            var receiveStr = _comm.Send(sendStr);
            if (CheckFrame(ref receiveStr))//帧校验通过
            {
                return true;
            }
            return false;
        }


        //无符号32位字读写
        public override UInt32[] ReadUInt32(string regName, int Address, int Count)
        {
            var ret = Array.Empty<UInt32>();
            string strHead = "";
            string strData = "";
            strHead += "@";//起始标志
            strHead += "00";//PLC地址
            strHead += "FA";//头编码取值范围:FA,RD,WR
            strHead += "0";//等待单位
            strHead += "00";//SID
            strHead += "00";//SA2
            strHead += "00";//DA2
            strHead += "00";//ICF
            strHead += "0101";//0101=读取命令，0102=写入命令
            strHead += GetRegisterCode(regName, false);//获取存储区代码
            strHead += ((Int16)Address).ToString("X4") + "00";//起始地址
            strHead += ((Int16)Count * 2).ToString("X4");//读取个数

            strData += FCS(strHead).ToString("X2");
            strData += "*";
            strData += '\r';

            string sendStr = strHead + strData;
            var receiveStr = _comm.Send(sendStr);
            if (CheckFrame(ref receiveStr))//帧校验通过
            {
                //解析接收到的数据
                int DataStartPos = 23;
                ret = new UInt32[Count];

                var s1 = receiveStr.Substring(DataStartPos, Count * 4);

                for (int i = 0; i < Count; i++)
                {
                    ret[i] = ToBigEndian(BitConverter.ToUInt32(HexStringToByteArray(receiveStr.Substring(DataStartPos + i * 8, 8)).Reverse().ToArray(), 0));
                }
                return ret;
            }
            return null;
        }

        public override bool WriteUInt32(string regName, int Address, UInt32[] Values)
        {
            string strHead = "";
            string strData = "";
            strHead += "@";//起始标志
            strHead += "00";//PLC地址
            strHead += "FA";//头编码取值范围:FA,RD,WR
            strHead += "0";//等待单位
            strHead += "00";//SID
            strHead += "00";//SA2
            strHead += "00";//DA2
            strHead += "00";//ICF
            strHead += "0102";//0101=读取命令，0102=写入命令
            strHead += GetRegisterCode(regName, false);//获取存储区代码
            strHead += ((Int16)Address).ToString("X4") + "00";//起始地址
            strHead += (((Int16)Values.Length * 2)).ToString("X4");//读取个数

            //插入数据
            foreach (var item in Values)
            {
                strHead += BitConverter.ToString(BitConverter.GetBytes((item))).Replace("-", "");
            }

            strData += FCS(strHead).ToString("X2");
            strData += "*";
            strData += '\r';

            string sendStr = strHead + strData;
            var receiveStr = _comm.Send(sendStr);
            if (CheckFrame(ref receiveStr))//帧校验通过
            {
                return true;
            }
            return false;
        }

        //无符号浮点数位字读写
        public override Single[] ReadSingle(string regName, int Address, int Count)
        {
            var ret = Array.Empty<Single>();
            string strHead = "";
            string strData = "";
            strHead += "@";//起始标志
            strHead += "00";//PLC地址
            strHead += "FA";//头编码取值范围:FA,RD,WR
            strHead += "0";//等待单位
            strHead += "00";//SID
            strHead += "00";//SA2
            strHead += "00";//DA2
            strHead += "00";//ICF
            strHead += "0101";//0101=读取命令，0102=写入命令
            strHead += GetRegisterCode(regName, false);//获取存储区代码
            strHead += ((Int16)Address).ToString("X4") + "00";//起始地址
            strHead += ((Int16)Count * 2).ToString("X4");//读取个数

            strData += FCS(strHead).ToString("X2");
            strData += "*";
            strData += '\r';

            string sendStr = strHead + strData;
            var receiveStr = _comm.Send(sendStr);
            if (CheckFrame(ref receiveStr))//帧校验通过
            {
                //解析接收到的数据
                int DataStartPos = 23;
                ret = new Single[Count];

                var s1 = receiveStr.Substring(DataStartPos, Count * 4);

                for (int i = 0; i < Count; i++)
                {
                    ret[i] = ToBigEndian(BitConverter.ToSingle(HexStringToByteArray(receiveStr.Substring(DataStartPos + i * 8, 8)).Reverse().ToArray(), 0));
                }
                return ret;
            }
            return null;
        }

        public override bool WriteSingle(string regName, int Address, Single[] Values)
        {
            string strHead = "";
            string strData = "";
            strHead += "@";//起始标志
            strHead += "00";//PLC地址
            strHead += "FA";//头编码取值范围:FA,RD,WR
            strHead += "0";//等待单位
            strHead += "00";//SID
            strHead += "00";//SA2
            strHead += "00";//DA2
            strHead += "00";//ICF
            strHead += "0102";//0101=读取命令，0102=写入命令
            strHead += GetRegisterCode(regName, false);//获取存储区代码
            strHead += ((Int16)Address).ToString("X4") + "00";//起始地址
            strHead += (((Int16)Values.Length * 2)).ToString("X4");//读取个数

            //插入数据
            foreach (var item in Values)
            {
                strHead += BitConverter.ToString(BitConverter.GetBytes((item))).Replace("-", "");
            }

            strData += FCS(strHead).ToString("X2");
            strData += "*";
            strData += '\r';

            string sendStr = strHead + strData;
            var receiveStr = _comm.Send(sendStr);
            if (CheckFrame(ref receiveStr))//帧校验通过
            {
                return true;
            }
            return false;
        }

        //字符串字读写
        public override string ReadString(string regName, int Address, int Count)
        {
            var ret = "";
            string strHead = "";
            string strData = "";
            strHead += "@";//起始标志
            strHead += "00";//PLC地址
            strHead += "FA";//头编码取值范围:FA,RD,WR
            strHead += "0";//等待单位
            strHead += "00";//SID
            strHead += "00";//SA2
            strHead += "00";//DA2
            strHead += "00";//ICF
            strHead += "0101";//0101=读取命令，0102=写入命令
            strHead += GetRegisterCode(regName, false);//获取存储区代码
            strHead += ((Int16)Address).ToString("X4") + "00";//起始地址
            strHead += ((Int16)Count/2).ToString("X4");//读取个数

            strData += FCS(strHead).ToString("X2");
            strData += "*";
            strData += '\r';

            string sendStr = strHead + strData;
            var receiveStr = _comm.Send(sendStr);
            if (CheckFrame(ref receiveStr))//帧校验通过
            {
                //解析接收到的数据
                int DataStartPos = 23;
                var str = receiveStr.Substring(DataStartPos, Count * 2);
                var data=HexStringToByteArray(str);
                ByteArraySwap(ref data);
                ret = Encoding.ASCII.GetString(data);
                return ret;
            }
            return null;
        }

        public override bool WriteString(string regName, int Address, string Values)
        {
            string strHead = "";
            string strData = "";
            strHead += "@";//起始标志
            strHead += "00";//PLC地址
            strHead += "FA";//头编码取值范围:FA,RD,WR
            strHead += "0";//等待单位
            strHead += "00";//SID
            strHead += "00";//SA2
            strHead += "00";//DA2
            strHead += "00";//ICF
            strHead += "0102";//0101=读取命令，0102=写入命令
            strHead += GetRegisterCode(regName, false);//获取存储区代码
            strHead += ((Int16)Address).ToString("X4") + "00";//起始地址
            strHead += (((Int16)Values.Length/2)).ToString("X4");//读取个数

            //插入数据
            string value = Values;
            if (value.Length % 2 != 0) value += " ";//长度不为偶数，加一个空白字符
            var data = Encoding.ASCII.GetBytes(value);

            ByteArraySwap(ref data);

            strHead += BitConverter.ToString(data);

            strData += FCS(strHead).ToString("X2");
            strData += "*";
            strData += '\r';

            string sendStr = strHead + strData;
            var receiveStr = _comm.Send(sendStr);
            if (CheckFrame(ref receiveStr))//帧校验通过
            {
                return true;
            }
            return false;
        }

    }
}
