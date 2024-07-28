using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Protocols
{
    internal class MC_3Ebase : ProtocolBase
    {
        const int iHeadFrameLength = 32;
        const int iDataFrameLength = 64;
        const int receiveDataHeadLength = 11;
        const string pattern = @"[\s\t]";

        const string sendHead = "500000FFFF0300";
        const string receiveHead = "D00000FFFF0300";

        //带IP，端口号设置的构造函数
        public MC_3Ebase(IComm comm) : base(comm)//显式调用基类的构造函数
        {

        }

        //获取寄存器对应的符号编号
        string GetRegCode(string regName)
        {
            switch (regName.Trim().ToUpper())
            {
                case "SM": return "91";
                case "SD": return "A9";
                case "X": return "9C";
                case "Y": return "9D";
                case "M": return "90";
                case "L": return "92";
                case "F": return "93";
                case "V": return "94";
                case "B": return "A0";
                case "D": return "A8";
                case "W": return "B4";

                case "TS": return "C1";
                case "TC": return "C0";
                case "TN": return "C2";
                case "SS": return "C7";
                case "SC": return "C6";

                case "SN": return "C8";
                case "CS": return "C4";
                case "CC": return "C3";
                case "CN": return "C5";
                case "SB": return "A1";

                case "SW": return "B5";
                case "S": return "98";
                case "DX": return "A2";
                case "DY": return "A3";
                case "Z": return "CC";

                case "R": return "AF";
                case "ZR": return "B0";

                default: throw new Exception("不支持的寄存器！");
            }
        }

        //保留项目
        string GetHeadString()
        {
            StringBuilder sbHead = new StringBuilder(iHeadFrameLength);//初始化帧头字符串
            sbHead.Append("5000");//副头部
            sbHead.Append("00");//网络号
            sbHead.Append("FF");//可编程控制器网络号
            sbHead.Append("FF03");//请求目标模块I/O编号
            sbHead.Append("00");//请求目标模块站号
            return sbHead.ToString();
        }

        //读Bool型变量时，子指令为0000时的报文格式（读5个数据，返回内容为0x15：true,false,true,false,true）-此时数量取（(Count % 0x10)>0?Count/0x10+1:Count/0x10）
        //发送: 50 00 00 FF FF 03 00 0C 00 10 00 01 04 00 00 64 00 00 90 01 00
        //返回: D0 00 00 FF FF 03 00 0C 00 00 00 15 00 

        //读Bool型变量时，子指令为0100时的报文格式（读5个数据，返回内容为布尔值形式：true,false,true,false,true）-此时数量取5
        //发送 : 50 00 00 FF FF 03 00 0C 00 10 00 01 04 01 00 64 00 00 90 05 00
        //返回 : D0 00 00 FF FF 03 00 05 00 00 00 10 10 10

        //写Bool型变量时亦是如此，长度不足偶数时在末尾追加0，设备会根据读写长度将末尾的0删除

        //以下方法使用不同子指令实现

        //bool读写
        public override bool[] ReadBool(string regName, int Address, int Count)
        {
            bool[] ret = Array.Empty<bool>();//初始化返回值
            
            StringBuilder sbData = new StringBuilder(iDataFrameLength);//初始化帧数据字符串

            sbData.Append("1000");//CPU监视定时器
            sbData.Append("0104");//指令批量读取
            sbData.Append("0000");//子指令 
            sbData.Append(ToBigEndianHexString(Address).Substring(0, 6));//起始软元件十六进制大端格式
            sbData.Append(GetRegCode(regName));//软元件代码	 
            sbData.Append(ToBigEndianHexString((Count % 0x10) > 0 ? Count / 0x10 + 1 : Count / 0x10).Substring(0, 4));//软元件点数

            //获取请求数据长度
            var strData = sbData.ToString();  
            string sendStr = (sendHead + ToBigEndianHexString(strData.Length / 2).Substring(0, 4)  + strData);//组合字符串格式发送数据

            var sendData = HexStringToByteArray(sendStr);//发送数据
            var receiveData = _comm.Send(sendData);//接收数据

            //校验接收到的数据
            if (receiveData != null && //接收内容不为空
                receiveData.Length >= receiveDataHeadLength &&//接收内容长度正常
                BitConverter.ToString(receiveData).Replace("-", "").StartsWith(receiveHead))//接收内容符合格式要求
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

        //使用子指令00
        public override bool WriteBool(string regName, int Address, bool[] values)
        { 
            StringBuilder sbData = new StringBuilder(iDataFrameLength);//初始化帧数据字符串 

            sbData.Append("1000");//CPU监视定时器
            sbData.Append("0114");//指令批量写入
            sbData.Append("0000");//子指令 
            sbData.Append(ToBigEndianHexString(Address).Substring(0, 6));//起始软元件十六进制大端格式
            sbData.Append(GetRegCode(regName));//软元件代码	 
            sbData.Append(ToBigEndianHexString((Int16)values.Count()).Substring(0, 4));//软元件点数
            int i = 0;
            int j = 0;
            byte dat = 0;

            foreach (var value in values)
            {
                dat *= 2;
                dat += value?(byte)1:(byte)0;
                i++;
                j++;
                if (i == values.Length) break;
                if (j == 8)
                {
                    sbData.Append(dat.ToString("X2"));
                    dat = 0;
                    j = 0;
                }                
            }
            if (sbData.Replace(" ", "").Length % 2 != 0) sbData.Append("0");//不足偶数个字符补0		
             
            //获取请求数据长度
            var strData = sbData.ToString(); 
            string sendStr = (sendHead + ToBigEndianHexString(strData.Length / 2).Substring(0, 4) + strData);//组合字符串格式发送数据

            var sendData = HexStringToByteArray(sendStr);//发送数据
            var receiveData = _comm.SendAsync(sendData).Result;//接收数据

            //校验接收到的数据
            if (receiveData != null && //接收内容不为空
                receiveData.Length >= receiveDataHeadLength &&//接收内容长度正常
                BitConverter.ToString(receiveData).Replace("-", "").StartsWith(receiveHead))//接收内容符合格式要求
            {
                return true;//校验成功，返回
            }
            return false;//返回内容
        }

        ////使用子指令01
        //public bool WriteBool(string regName, int Address, bool[] values)
        //{
        //    StringBuilder sbData = new StringBuilder(iDataFrameLength);//初始化帧数据字符串 

        //    sbData.Append("1000");//CPU监视定时器
        //    sbData.Append("0114");//指令批量写入
        //    sbData.Append("0100");//子指令 
        //    sbData.Append(ToBigEndianHexString(Address).Substring(0, 6));//起始软元件十六进制大端格式
        //    sbData.Append(GetRegCode(regName));//软元件代码	 
        //    sbData.Append(ToBigEndianHexString((Int16)values.Count()).Substring(0, 4));//软元件点数
        //    foreach (var value in values)
        //    {
        //        sbData.Append(value ? "1" : "0");
        //    }
        //    if (sbData.Replace(" ", "").Length % 2 != 0) sbData.Append("0");//不足偶数个字符补0		

        //    //获取请求数据长度
        //    var strData = sbData.ToString();
        //    string sendStr = (sendHead + ToBigEndianHexString(strData.Length / 2).Substring(0, 4) + strData);//组合字符串格式发送数据

        //    var sendData = HexStringToByteArray(sendStr);//发送数据
        //    var receiveData = _comm.SendAsync(sendData).Result;//接收数据

        //    //校验接收到的数据
        //    if (receiveData != null && //接收内容不为空
        //        receiveData.Length >= receiveDataHeadLength &&//接收内容长度正常
        //        BitConverter.ToString(receiveData).Replace("-", "").StartsWith(receiveHead))//接收内容符合格式要求
        //    {
        //        return true;//校验成功，返回
        //    }
        //    return false;//返回内容
        //}

        //16位读写
        public override Int16[] ReadInt16(string regName, int Address, int Count)
        {
            Int16[] ret = Array.Empty<Int16>();//初始化返回值 
            StringBuilder sbData = new StringBuilder(iDataFrameLength);//初始化帧数据字符串 

            sbData.Append("1000");//CPU监视定时器
            sbData.Append("0104");//指令批量读取
            sbData.Append("0000");//子指令 
            sbData.Append(ToBigEndianHexString(Address).Substring(0, 6));//起始软元件十六进制大端格式
            sbData.Append(GetRegCode(regName));//软元件代码	 
            sbData.Append(ToBigEndianHexString(Count).Substring(0, 4));//软元件点数
             
            //获取请求数据长度
            var strData = sbData.ToString(); 
            string sendStr = (sendHead + ToBigEndianHexString(strData.Length / 2).Substring(0, 4) + strData);//组合字符串格式发送数据

            var sendData = HexStringToByteArray(sendStr);//发送数据
            var receiveData = _comm.Send(sendData);//接收数据

            //校验接收到的数据
            if (receiveData != null && //接收内容不为空
                receiveData.Length >= receiveDataHeadLength &&//接收内容长度正常
                BitConverter.ToString(receiveData).Replace("-", "").StartsWith(receiveHead))//接收内容符合格式要求
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
            StringBuilder sbData = new StringBuilder(iDataFrameLength);//初始化帧数据字符串 

            sbData.Append("1000");//CPU监视定时器
            sbData.Append("0114");//指令批量写入
            sbData.Append("0000");//子指令 
            sbData.Append(ToBigEndianHexString(Address).Substring(0, 6));//起始软元件十六进制大端格式
            sbData.Append(GetRegCode(regName));//软元件代码	 
            sbData.Append(ToBigEndianHexString(values.Count()).Substring(0, 4));//软元件点数
            foreach (var value in values)
            {
                sbData.Append(ToBigEndianHexString(value).Substring(0, 4));
            }
             
            //获取请求数据长度
            var strData = sbData.ToString(); 
            string sendStr = (sendHead + ToBigEndianHexString(strData.Length / 2).Substring(0, 4) + strData);//组合字符串格式发送数据

            var sendData = HexStringToByteArray(sendStr);//发送数据
            var receiveData = _comm.Send(sendData);//接收数据

            //校验接收到的数据
            if (receiveData != null && //接收内容不为空
                receiveData.Length >= receiveDataHeadLength &&//接收内容长度正常
                BitConverter.ToString(receiveData).Replace("-", "").StartsWith(receiveHead))//接收内容符合格式要求
            {
                return true;//校验成功，返回
            }
            return false;//返回内容
        }

        public override UInt16[] ReadUInt16(string regName, int Address, int Count)
        {
            UInt16[] ret = Array.Empty<UInt16>();//初始化返回值 
            StringBuilder sbData = new StringBuilder(iDataFrameLength);//初始化帧数据字符串 

            sbData.Append("1000");//CPU监视定时器
            sbData.Append("0104");//指令批量读取
            sbData.Append("0000");//子指令 
            sbData.Append(ToBigEndianHexString(Address).Substring(0, 6));//起始软元件十六进制大端格式
            sbData.Append(GetRegCode(regName));//软元件代码	 
            sbData.Append(ToBigEndianHexString(Count).Substring(0, 4));//软元件点数
             
            //获取请求数据长度
            var strData = sbData.ToString(); 
            string sendStr = (sendHead + ToBigEndianHexString(strData.Length / 2).Substring(0, 4) + strData);//组合字符串格式发送数据

            var sendData = HexStringToByteArray(sendStr);//发送数据
            var receiveData = _comm.Send(sendData);//接收数据

            //校验接收到的数据
            if (receiveData != null && //接收内容不为空
                receiveData.Length >= receiveDataHeadLength &&//接收内容长度正常
                BitConverter.ToString(receiveData).Replace("-", "").StartsWith(receiveHead))//接收内容符合格式要求
            {
                ret = new UInt16[Count];//初始化返回值数组
                for (int i = 0; i < Count; i++)//填充返回值数组
                {
                    ret[i] = BitConverter.ToUInt16(receiveData, receiveDataHeadLength + i * 2);
                }
            }
            return ret;//返回内容
        }

        public override bool WriteUInt16(string regName, int Address, UInt16[] values)
        { 
            StringBuilder sbData = new StringBuilder(iDataFrameLength);//初始化帧数据字符串 

            sbData.Append("1000");//CPU监视定时器
            sbData.Append("0114");//指令批量写入
            sbData.Append("0000");//子指令 
            sbData.Append(ToBigEndianHexString(Address).Substring(0, 6));//起始软元件十六进制大端格式
            sbData.Append(GetRegCode(regName));//软元件代码	 
            sbData.Append(ToBigEndianHexString(values.Count()).Substring(0, 4));//软元件点数
            foreach (var value in values)
            {
                sbData.Append(ToBigEndianHexString(value).Substring(0, 4));
            }
             
            //获取请求数据长度
            var strData = sbData.ToString(); 
            string sendStr = (sendHead + ToBigEndianHexString(strData.Length / 2).Substring(0, 4) + strData);//组合字符串格式发送数据

            var sendData = HexStringToByteArray(sendStr);//发送数据
            var receiveData = _comm.Send(sendData);//接收数据

            //校验接收到的数据
            if (receiveData != null && //接收内容不为空
                receiveData.Length >= receiveDataHeadLength &&//接收内容长度正常
                BitConverter.ToString(receiveData).Replace("-", "").StartsWith(receiveHead))//接收内容符合格式要求
            {
                return true;//校验成功，返回
            }
            return false;//返回内容
        }

        //32位读写
        public override Int32[] ReadInt32(string regName, int Address, int Count)
        {
            Int32[] ret = Array.Empty<Int32>();//初始化返回值 
            StringBuilder sbData = new StringBuilder(iDataFrameLength);//初始化帧数据字符串 

            sbData.Append("1000");//CPU监视定时器
            sbData.Append("0104");//指令批量读取
            sbData.Append("0000");//子指令 
            sbData.Append(ToBigEndianHexString(Address).Substring(0, 6));//起始软元件十六进制大端格式
            sbData.Append(GetRegCode(regName));//软元件代码	 
            sbData.Append(ToBigEndianHexString(Count * 2).Substring(0, 4));//软元件点数
             
            //获取请求数据长度
            var strData = sbData.ToString(); 
            string sendStr = (sendHead + ToBigEndianHexString(strData.Length / 2).Substring(0, 4) + strData);//组合字符串格式发送数据

            var sendData = HexStringToByteArray(sendStr);//发送数据
            var receiveData = _comm.Send(sendData);//接收数据

            //校验接收到的数据
            if (receiveData != null && //接收内容不为空
                receiveData.Length >= receiveDataHeadLength &&//接收内容长度正常
                BitConverter.ToString(receiveData).Replace("-", "").StartsWith(receiveHead))//接收内容符合格式要求
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
            StringBuilder sbData = new StringBuilder(iDataFrameLength);//初始化帧数据字符串 

            sbData.Append("1000");//CPU监视定时器
            sbData.Append("0114");//指令批量写入
            sbData.Append("0000");//子指令 
            sbData.Append(ToBigEndianHexString(Address).Substring(0, 6));//起始软元件十六进制大端格式
            sbData.Append(GetRegCode(regName));//软元件代码	 
            sbData.Append(ToBigEndianHexString(values.Count() * 2).Substring(0, 4));//软元件点数
            foreach (var value in values)
            {
                sbData.Append(ToBigEndianHexString(value).Substring(0, 8));
            }
             
            //获取请求数据长度
            var strData = sbData.ToString(); 
            string sendStr = (sendHead + ToBigEndianHexString(strData.Length / 2).Substring(0, 4) + strData);//组合字符串格式发送数据

            var sendData = HexStringToByteArray(sendStr);//发送数据
            var receiveData = _comm.Send(sendData);//接收数据

            //校验接收到的数据
            if (receiveData != null && //接收内容不为空
                receiveData.Length >= receiveDataHeadLength &&//接收内容长度正常
                BitConverter.ToString(receiveData).Replace("-", "").StartsWith(receiveHead))//接收内容符合格式要求
            {
                return true;//校验成功，返回
            }
            return false;//返回内容
        }

        public override UInt32[] ReadUInt32(string regName, int Address, int Count)
        {
            UInt32[] ret = Array.Empty<UInt32>();//初始化返回值 
            StringBuilder sbData = new StringBuilder(iDataFrameLength);//初始化帧数据字符串 

            sbData.Append("1000");//CPU监视定时器
            sbData.Append("0104");//指令批量读取
            sbData.Append("0000");//子指令 
            sbData.Append(ToBigEndianHexString(Address).Substring(0, 6));//起始软元件十六进制大端格式
            sbData.Append(GetRegCode(regName));//软元件代码	 
            sbData.Append(ToBigEndianHexString(Count * 2).Substring(0, 4));//软元件点数

            //获取请求数据长度
            var strData = sbData.ToString(); 
            string sendStr = (sendHead + ToBigEndianHexString(strData.Length / 2).Substring(0, 4) + strData);//组合字符串格式发送数据

            var sendData = HexStringToByteArray(sendStr);//发送数据
            var receiveData = _comm.Send(sendData);//接收数据

            //校验接收到的数据
            if (receiveData != null && //接收内容不为空
                receiveData.Length >= receiveDataHeadLength &&//接收内容长度正常
                BitConverter.ToString(receiveData).Replace("-", "").StartsWith(receiveHead))//接收内容符合格式要求
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
            StringBuilder sbData = new StringBuilder(iDataFrameLength);//初始化帧数据字符串 

            sbData.Append("1000");//CPU监视定时器
            sbData.Append("0114");//指令批量写入
            sbData.Append("0000");//子指令 
            sbData.Append(ToBigEndianHexString(Address).Substring(0, 6));//起始软元件十六进制大端格式
            sbData.Append(GetRegCode(regName));//软元件代码	 
            sbData.Append(ToBigEndianHexString(values.Count() * 2).Substring(0, 4));//软元件点数
            foreach (var value in values)
            {
                sbData.Append(ToBigEndianHexString(value).Substring(0, 8));
            }

            //获取请求数据长度
            var strData = sbData.ToString(); 
            string sendStr = (sendHead + ToBigEndianHexString(strData.Length / 2).Substring(0, 4) + strData);//组合字符串格式发送数据

            var sendData = HexStringToByteArray(sendStr);//发送数据
            var receiveData = _comm.Send(sendData);//接收数据

            //校验接收到的数据
            if (receiveData != null && //接收内容不为空
                receiveData.Length >= receiveDataHeadLength &&//接收内容长度正常
                BitConverter.ToString(receiveData).Replace("-", "").StartsWith(receiveHead))//接收内容符合格式要求
            {
                return true;//校验成功，返回
            }
            return false;//返回内容
        }

        public override Single[] ReadSingle(string regName, int Address, int Count)
        {
            Single[] ret = Array.Empty<Single>();//初始化返回值 
            StringBuilder sbData = new StringBuilder(iDataFrameLength);//初始化帧数据字符串 

            sbData.Append("1000");//CPU监视定时器
            sbData.Append("0104");//指令批量读取
            sbData.Append("0000");//子指令 
            sbData.Append(ToBigEndianHexString(Address).Substring(0, 6));//起始软元件十六进制大端格式
            sbData.Append(GetRegCode(regName));//软元件代码	 
            sbData.Append(ToBigEndianHexString(Count * 2).Substring(0, 4));//软元件点数

            //获取请求数据长度
            var strData = sbData.ToString(); 
            string sendStr = (sendHead + ToBigEndianHexString(strData.Length / 2).Substring(0, 4) + strData);//组合字符串格式发送数据

            var sendData = HexStringToByteArray(sendStr);//发送数据
            var receiveData = _comm.Send(sendData);//接收数据

            //校验接收到的数据
            if (receiveData != null && //接收内容不为空
                receiveData.Length >= receiveDataHeadLength &&//接收内容长度正常
                BitConverter.ToString(receiveData).Replace("-", "").StartsWith(receiveHead))//接收内容符合格式要求
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
            StringBuilder sbData = new StringBuilder(iDataFrameLength);//初始化帧数据字符串 

            sbData.Append("1000");//CPU监视定时器
            sbData.Append("0114");//指令批量写入
            sbData.Append("0000");//子指令 
            sbData.Append(ToBigEndianHexString(Address).Substring(0, 6));//起始软元件十六进制大端格式
            sbData.Append(GetRegCode(regName));//软元件代码	 
            sbData.Append(ToBigEndianHexString(values.Count() * 2).Substring(0, 4));//软元件点数
            foreach (var value in values)
            {
                sbData.Append(ToBigEndianHexString(value).Substring(0, 8));
            }

            //获取请求数据长度
            var strData = sbData.ToString(); 
            string sendStr = (sendHead + ToBigEndianHexString(strData.Length / 2).Substring(0, 4) + strData);//组合字符串格式发送数据

            var sendData = HexStringToByteArray(sendStr);//发送数据
            var receiveData = _comm.Send(sendData);//接收数据

            //校验接收到的数据
            if (receiveData != null && //接收内容不为空
                receiveData.Length >= receiveDataHeadLength &&//接收内容长度正常
                BitConverter.ToString(receiveData).Replace("-", "").StartsWith(receiveHead))//接收内容符合格式要求
            {
                return true;//校验成功，返回
            }
            return false;//返回内容
        }

        //读写字符串
        public override string ReadString(string regName, int Address, int Count)
        {
            string ret = "";//初始化返回值 
            StringBuilder sbData = new StringBuilder(iDataFrameLength);//初始化帧数据字符串 

            sbData.Append("1000");//CPU监视定时器
            sbData.Append("0104");//指令批量读取
            sbData.Append("0000");//子指令 
            sbData.Append(ToBigEndianHexString(Address).Substring(0, 6));//起始软元件十六进制大端格式
            sbData.Append(GetRegCode(regName));//软元件代码	 
            sbData.Append(ToBigEndianHexString(Count).Substring(0, 4));//软元件点数

            //获取请求数据长度
            var strData = sbData.ToString(); 
            string sendStr = (sendHead + ToBigEndianHexString(strData.Length / 2).Substring(0, 4) + strData);//组合字符串格式发送数据

            var sendData = HexStringToByteArray(sendStr);//发送数据
            var receiveData = _comm.Send(sendData);//接收数据

            //校验接收到的数据
            if (receiveData != null && //接收内容不为空
                receiveData.Length >= receiveDataHeadLength &&//接收内容长度正常
                BitConverter.ToString(receiveData).Replace("-", "").StartsWith(receiveHead))//接收内容符合格式要求
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
            StringBuilder sbData = new StringBuilder(iDataFrameLength);//初始化帧数据字符串 

            sbData.Append("1000");//CPU监视定时器
            sbData.Append("0114");//指令批量写入
            sbData.Append("0000");//子指令 
            sbData.Append(ToBigEndianHexString(Address).Substring(0, 6));//起始软元件十六进制大端格式
            sbData.Append(GetRegCode(regName));//软元件代码	 
            sbData.Append(ToBigEndianHexString(values.Count()).Substring(0, 4));//软元件点数

            sbData.Append(BitConverter.ToString(Encoding.ASCII.GetBytes(values)).Replace("-", ""));//转换为ASCII码再写进去

            //获取请求数据长度
            var strData = sbData.ToString(); 
            string sendStr = (sendHead + ToBigEndianHexString(strData.Length / 2).Substring(0, 4) + strData);//组合字符串格式发送数据

            var sendData = HexStringToByteArray(sendStr);//发送数据
            var receiveData = _comm.Send(sendData);//接收数据

            //校验接收到的数据
            if (receiveData != null && //接收内容不为空
                receiveData.Length >= receiveDataHeadLength &&//接收内容长度正常
                BitConverter.ToString(receiveData).Replace("-", "").StartsWith(receiveHead))//接收内容符合格式要求
            {
                return true;//校验成功，返回
            }
            return false;//返回内容
        }
    }

    //为保持兼容而封装的密封类
    internal sealed class MC_3E : MC_3Ebase
    {
        //以太网方式
        //最简构造函数
        public MC_3E(string ip, int port) : base(new CommNet(ip, port)) {; }

        //不带信号量初始的构造函数
        public MC_3E(string ip, int port, int timeOut) : base(new CommNet(ip, port, timeOut)) {; }

        //全参构造函数
        public MC_3E(string ip, int port, int timeOut, int minSemaphore, int maxSemaphore) : base(new CommNet(ip, port, timeOut, minSemaphore, maxSemaphore)) {; }

        //串口方式
        //最简构造函数
        public MC_3E(string portName, int baudRate, int dataBits, Parity parity, StopBits stopBits) : base(new CommSerialPort(portName, baudRate, dataBits, parity, stopBits)) {; }

        //不带信号量初始的构造函数
        public MC_3E(string portName, int baudRate, int dataBits, Parity parity, StopBits stopBits, int timeOut) : base(new CommSerialPort(portName, baudRate, dataBits, parity, stopBits, timeOut)) {; }

        //全参构造函数
        public MC_3E(string portName, int baudRate, int dataBits, Parity parity, StopBits stopBits, int timeOut, int minSemaphore, int maxSemaphore) : base(new CommSerialPort(portName, baudRate, dataBits, parity, stopBits, timeOut, minSemaphore, maxSemaphore)) {; }

    }
}
