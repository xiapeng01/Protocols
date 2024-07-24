using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Protocols.Protocols
{
    internal class MC_3Ebase : ProtocolBase
    {
        int receiveDataHeadLength = 11;
        string pattern = @"[\s\t]";
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

                default: return "00";
            }
        }

        //bool读写
        public override bool[] ReadBool(string regName, int Address, int Count)
        {
            bool[] ret = new bool[0];//初始化返回值
            StringBuilder sbHead = new StringBuilder(iHeadFrameLength);//初始化帧头字符串
            StringBuilder sbData = new StringBuilder(iDataFrameLength);//初始化帧数据字符串
            sbHead.Append("5000");//副头部
            sbHead.Append("00");//网络号
            sbHead.Append("FF");//可编程控制器网络号
            sbHead.Append("FF03");//请求目标模块I/O编号
            sbHead.Append("00");//请求目标模块站号
                                //s.Append("0C");//请求数据长度
            sbData.Append("1000");//CPU监视定时器
            sbData.Append("0104");//指令批量读取
            sbData.Append("0000");//子指令 
            sbData.Append(ToBigEndianHexString(Address).Substring(0, 6));//起始软元件十六进制大端格式
            sbData.Append(GetRegCode(regName));//软元件代码	 
            sbData.Append(ToBigEndianHexString(Count).Substring(0, 4));//软元件点数

            //获取请求数据长度
            var strData = sbData.ToString();
            sbHead.Append(ToBigEndianHexString(strData.Length / 2).Substring(0, 4));//请求数据长度
            var strHead = sbHead.ToString();
            string sendStr = (strHead + strData).Replace(" ", "").ToUpper();//组合字符串格式发送数据,并剔除不必要的空白

            var sendData = HexStringToByteArray(sendStr);//发送数据
            var receiveData = _comm.Send(sendData);//接收数据

            //校验接收到的数据
            if (receiveData != null && //接收内容不为空
                receiveData.Length >= receiveDataHeadLength &&//接收内容长度正常
                BitConverter.ToString(receiveData).Replace("-", "").ToUpper().StartsWith("D0 00 00 FF FF 03 00".Replace(" ", "")))//接收内容符合格式要求
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
            StringBuilder sbHead = new StringBuilder(iHeadFrameLength);//初始化帧头字符串
            StringBuilder sbData = new StringBuilder(iDataFrameLength);//初始化帧数据字符串
            sbHead.Append("5000");//副头部
            sbHead.Append("00");//网络号
            sbHead.Append("FF");//可编程控制器网络号
            sbHead.Append("FF03");//请求目标模块I/O编号
            sbHead.Append("00");//请求目标模块站号
                                //s.Append("0C");//请求数据长度
            sbData.Append("0A00");//CPU监视定时器
            sbData.Append("0114");//指令批量写入
            sbData.Append("0100");//子指令 
            sbData.Append(ToBigEndianHexString(Address).Substring(0, 6));//起始软元件十六进制大端格式
            sbData.Append(GetRegCode(regName));//软元件代码	 
            sbData.Append(ToBigEndianHexString((Int16)values.Count()).Substring(0, 4));//软元件点数
            foreach (var value in values)
            {
                sbData.Append(value ? "1" : "0");
            }
            if (sbData.Replace(" ", "").Length % 2 != 0) sbData.Append("0");//不足偶数个字符补0		

            //获取请求数据长度
            var strData = sbData.ToString();
            sbHead.Append(ToBigEndianHexString(strData.Length / 2).Substring(0, 4));//请求数据长度
            var strHead = sbHead.ToString();
            string sendStr = (strHead + strData).Replace(" ", "").ToUpper();//组合字符串格式发送数据,并剔除不必要的空白

            var sendData = HexStringToByteArray(sendStr);//发送数据
            var receiveData = _comm.Send(sendData);//接收数据

            //校验接收到的数据
            if (receiveData != null && //接收内容不为空
                receiveData.Length >= receiveDataHeadLength &&//接收内容长度正常
                BitConverter.ToString(receiveData).Replace("-", "").ToUpper().StartsWith("D0 00 00 FF FF 03 00".Replace(" ", "")))//接收内容符合格式要求
            {
                return true;//校验成功，返回
            }
            return false;//返回内容
        }

        //16位读写
        public override Int16[] ReadInt16(string regName, int Address, int Count)
        {
            Int16[] ret = new short[0];//初始化返回值
            StringBuilder sbHead = new StringBuilder(iHeadFrameLength);//初始化帧头字符串
            StringBuilder sbData = new StringBuilder(iDataFrameLength);//初始化帧数据字符串
            sbHead.Append("5000");//副头部
            sbHead.Append("00");//网络号
            sbHead.Append("FF");//可编程控制器网络号
            sbHead.Append("FF03");//请求目标模块I/O编号
            sbHead.Append("00");//请求目标模块站号
                                //s.Append("0C");//请求数据长度
            sbData.Append("1000");//CPU监视定时器
            sbData.Append("0104");//指令批量读取
            sbData.Append("0000");//子指令 
            sbData.Append(ToBigEndianHexString(Address).Substring(0, 6));//起始软元件十六进制大端格式
            sbData.Append(GetRegCode(regName));//软元件代码	 
            sbData.Append(ToBigEndianHexString(Count).Substring(0, 4));//软元件点数

            //获取请求数据长度
            var strData = sbData.ToString();
            sbHead.Append(ToBigEndianHexString(strData.Length / 2).Substring(0, 4));//请求数据长度
            var strHead = sbHead.ToString();
            string sendStr = (strHead + strData).Replace(" ", "").ToUpper();//组合字符串格式发送数据,并剔除不必要的空白

            var sendData = HexStringToByteArray(sendStr);//发送数据
            var receiveData = _comm.Send(sendData);//接收数据

            //校验接收到的数据
            if (receiveData != null && //接收内容不为空
                receiveData.Length >= receiveDataHeadLength &&//接收内容长度正常
                BitConverter.ToString(receiveData).Replace("-", "").ToUpper().StartsWith("D0 00 00 FF FF 03 00".Replace(" ", "")))//接收内容符合格式要求
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
            //Int16[] ret = new short[0];//初始化返回值
            StringBuilder sbHead = new StringBuilder(iHeadFrameLength);//初始化帧头字符串
            StringBuilder sbData = new StringBuilder(iDataFrameLength);//初始化帧数据字符串
            sbHead.Append("5000");//副头部
            sbHead.Append("00");//网络号
            sbHead.Append("FF");//可编程控制器网络号
            sbHead.Append("FF03");//请求目标模块I/O编号
            sbHead.Append("00");//请求目标模块站号
                                //s.Append("0C");//请求数据长度
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
            sbHead.Append(ToBigEndianHexString(strData.Length / 2).Substring(0, 4));//请求数据长度
            var strHead = sbHead.ToString();
            string sendStr = (strHead + strData).Replace(" ", "").ToUpper();//组合字符串格式发送数据,并剔除不必要的空白

            var sendData = HexStringToByteArray(sendStr);//发送数据
            var receiveData = _comm.Send(sendData);//接收数据

            //校验接收到的数据
            if (receiveData != null && //接收内容不为空
                receiveData.Length >= receiveDataHeadLength &&//接收内容长度正常
                BitConverter.ToString(receiveData).Replace("-", "").ToUpper().StartsWith("D0 00 00 FF FF 03 00".Replace(" ", "")))//接收内容符合格式要求
            {
                return true;//校验成功，返回
            }
            return false;//返回内容
        }

        public override UInt16[] ReadUInt16(string regName, int Address, int Count)
        {
            UInt16[] ret = new UInt16[0];//初始化返回值
            StringBuilder sbHead = new StringBuilder(iHeadFrameLength);//初始化帧头字符串
            StringBuilder sbData = new StringBuilder(iDataFrameLength);//初始化帧数据字符串
            sbHead.Append("5000");//副头部
            sbHead.Append("00");//网络号
            sbHead.Append("FF");//可编程控制器网络号
            sbHead.Append("FF03");//请求目标模块I/O编号
            sbHead.Append("00");//请求目标模块站号
                                //s.Append("0C");//请求数据长度
            sbData.Append("1000");//CPU监视定时器
            sbData.Append("0104");//指令批量读取
            sbData.Append("0000");//子指令 
            sbData.Append(ToBigEndianHexString(Address).Substring(0, 6));//起始软元件十六进制大端格式
            sbData.Append(GetRegCode(regName));//软元件代码	 
            sbData.Append(ToBigEndianHexString(Count).Substring(0, 4));//软元件点数

            //获取请求数据长度
            var strData = sbData.ToString();
            sbHead.Append(ToBigEndianHexString(strData.Length / 2).Substring(0, 4));//请求数据长度
            var strHead = sbHead.ToString();
            string sendStr = (strHead + strData).Replace(" ", "").ToUpper();//组合字符串格式发送数据,并剔除不必要的空白

            var sendData = HexStringToByteArray(sendStr);//发送数据
            var receiveData = _comm.Send(sendData);//接收数据

            //校验接收到的数据
            if (receiveData != null && //接收内容不为空
                receiveData.Length >= receiveDataHeadLength &&//接收内容长度正常
                BitConverter.ToString(receiveData).Replace("-", "").ToUpper().StartsWith("D0 00 00 FF FF 03 00".Replace(" ", "")))//接收内容符合格式要求
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
            //Int16[] ret = new short[0];//初始化返回值
            StringBuilder sbHead = new StringBuilder(iHeadFrameLength);//初始化帧头字符串
            StringBuilder sbData = new StringBuilder(iDataFrameLength);//初始化帧数据字符串
            sbHead.Append("5000");//副头部
            sbHead.Append("00");//网络号
            sbHead.Append("FF");//可编程控制器网络号
            sbHead.Append("FF03");//请求目标模块I/O编号
            sbHead.Append("00");//请求目标模块站号
                                //s.Append("0C");//请求数据长度
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
            sbHead.Append(ToBigEndianHexString(strData.Length / 2).Substring(0, 4));//请求数据长度
            var strHead = sbHead.ToString();
            string sendStr = (strHead + strData).Replace(" ", "").ToUpper();//组合字符串格式发送数据,并剔除不必要的空白

            var sendData = HexStringToByteArray(sendStr);//发送数据
            var receiveData = _comm.Send(sendData);//接收数据

            //校验接收到的数据
            if (receiveData != null && //接收内容不为空
                receiveData.Length >= receiveDataHeadLength &&//接收内容长度正常
                BitConverter.ToString(receiveData).Replace("-", "").ToUpper().StartsWith("D0 00 00 FF FF 03 00".Replace(" ", "")))//接收内容符合格式要求
            {
                return true;//校验成功，返回
            }
            return false;//返回内容
        }

        //32位读写
        public override Int32[] ReadInt32(string regName, int Address, int Count)
        {
            Int32[] ret = new Int32[0];//初始化返回值
            StringBuilder sbHead = new StringBuilder(iHeadFrameLength);//初始化帧头字符串
            StringBuilder sbData = new StringBuilder(iDataFrameLength);//初始化帧数据字符串
            sbHead.Append("5000");//副头部
            sbHead.Append("00");//网络号
            sbHead.Append("FF");//可编程控制器网络号
            sbHead.Append("FF03");//请求目标模块I/O编号
            sbHead.Append("00");//请求目标模块站号
                                //s.Append("0C");//请求数据长度
            sbData.Append("1000");//CPU监视定时器
            sbData.Append("0104");//指令批量读取
            sbData.Append("0000");//子指令 
            sbData.Append(ToBigEndianHexString(Address).Substring(0, 6));//起始软元件十六进制大端格式
            sbData.Append(GetRegCode(regName));//软元件代码	 
            sbData.Append(ToBigEndianHexString(Count * 2).Substring(0, 4));//软元件点数

            //获取请求数据长度
            var strData = sbData.ToString().Replace(" ", "").ToUpper();
            sbHead.Append(ToBigEndianHexString(strData.Length / 2).Substring(0, 4));//请求数据长度
            var strHead = sbHead.ToString().Replace(" ", "").ToUpper();
            string sendStr = (strHead + strData).Replace(" ", "").ToUpper();//组合字符串格式发送数据,并剔除不必要的空白

            var sendData = HexStringToByteArray(sendStr);//发送数据
            var receiveData = _comm.Send(sendData);//接收数据

            //校验接收到的数据
            if (receiveData != null && //接收内容不为空
                receiveData.Length >= receiveDataHeadLength &&//接收内容长度正常
                BitConverter.ToString(receiveData).Replace("-", "").ToUpper().StartsWith("D0 00 00 FF FF 03 00".Replace(" ", "")))//接收内容符合格式要求
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
            //Int16[] ret = new short[0];//初始化返回值
            StringBuilder sbHead = new StringBuilder(iHeadFrameLength);//初始化帧头字符串
            StringBuilder sbData = new StringBuilder(iDataFrameLength);//初始化帧数据字符串
            sbHead.Append("5000");//副头部
            sbHead.Append("00");//网络号
            sbHead.Append("FF");//可编程控制器网络号
            sbHead.Append("FF03");//请求目标模块I/O编号
            sbHead.Append("00");//请求目标模块站号
                                //s.Append("0C");//请求数据长度
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
            sbHead.Append(ToBigEndianHexString(strData.Length / 2).Substring(0, 4));//请求数据长度
            var strHead = sbHead.ToString();
            string sendStr = (strHead + strData).Replace(" ", "").ToUpper();//组合字符串格式发送数据,并剔除不必要的空白

            var sendData = HexStringToByteArray(sendStr);//发送数据
            var receiveData = _comm.Send(sendData);//接收数据

            //校验接收到的数据
            if (receiveData != null && //接收内容不为空
                receiveData.Length >= receiveDataHeadLength &&//接收内容长度正常
                BitConverter.ToString(receiveData).Replace("-", "").ToUpper().StartsWith("D0 00 00 FF FF 03 00".Replace(" ", "")))//接收内容符合格式要求
            {
                return true;//校验成功，返回
            }
            return false;//返回内容
        }

        public override UInt32[] ReadUInt32(string regName, int Address, int Count)
        {
            UInt32[] ret = new UInt32[0];//初始化返回值
            StringBuilder sbHead = new StringBuilder(iHeadFrameLength);//初始化帧头字符串
            StringBuilder sbData = new StringBuilder(iDataFrameLength);//初始化帧数据字符串
            sbHead.Append("5000");//副头部
            sbHead.Append("00");//网络号
            sbHead.Append("FF");//可编程控制器网络号
            sbHead.Append("FF03");//请求目标模块I/O编号
            sbHead.Append("00");//请求目标模块站号
                                //s.Append("0C");//请求数据长度
            sbData.Append("1000");//CPU监视定时器
            sbData.Append("0104");//指令批量读取
            sbData.Append("0000");//子指令 
            sbData.Append(ToBigEndianHexString(Address).Substring(0, 6));//起始软元件十六进制大端格式
            sbData.Append(GetRegCode(regName));//软元件代码	 
            sbData.Append(ToBigEndianHexString(Count * 2).Substring(0, 4));//软元件点数

            //获取请求数据长度
            var strData = sbData.ToString();
            sbHead.Append(ToBigEndianHexString(strData.Length / 2).Substring(0, 4));//请求数据长度
            var strHead = sbHead.ToString();
            string sendStr = (strHead + strData).Replace(" ", "").ToUpper();//组合字符串格式发送数据,并剔除不必要的空白

            var sendData = HexStringToByteArray(sendStr);//发送数据
            var receiveData = _comm.Send(sendData);//接收数据

            //校验接收到的数据
            if (receiveData != null && //接收内容不为空
                receiveData.Length >= receiveDataHeadLength &&//接收内容长度正常
                BitConverter.ToString(receiveData).Replace("-", "").ToUpper().StartsWith("D0 00 00 FF FF 03 00".Replace(" ", "")))//接收内容符合格式要求
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
            //Int16[] ret = new short[0];//初始化返回值
            StringBuilder sbHead = new StringBuilder(iHeadFrameLength);//初始化帧头字符串
            StringBuilder sbData = new StringBuilder(iDataFrameLength);//初始化帧数据字符串
            sbHead.Append("5000");//副头部
            sbHead.Append("00");//网络号
            sbHead.Append("FF");//可编程控制器网络号
            sbHead.Append("FF03");//请求目标模块I/O编号
            sbHead.Append("00");//请求目标模块站号
                                //s.Append("0C");//请求数据长度
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
            sbHead.Append(ToBigEndianHexString(strData.Length / 2).Substring(0, 4));//请求数据长度
            var strHead = sbHead.ToString();
            string sendStr = (strHead + strData).Replace(" ", "").ToUpper();//组合字符串格式发送数据,并剔除不必要的空白

            var sendData = HexStringToByteArray(sendStr);//发送数据
            var receiveData = _comm.Send(sendData);//接收数据

            //校验接收到的数据
            if (receiveData != null && //接收内容不为空
                receiveData.Length >= receiveDataHeadLength &&//接收内容长度正常
                BitConverter.ToString(receiveData).Replace("-", "").ToUpper().StartsWith("D0 00 00 FF FF 03 00".Replace(" ", "")))//接收内容符合格式要求
            {
                return true;//校验成功，返回
            }
            return false;//返回内容
        }

        public override Single[] ReadSingle(string regName, int Address, int Count)
        {
            Single[] ret = new Single[0];//初始化返回值
            StringBuilder sbHead = new StringBuilder(iHeadFrameLength);//初始化帧头字符串
            StringBuilder sbData = new StringBuilder(iDataFrameLength);//初始化帧数据字符串
            sbHead.Append("5000");//副头部
            sbHead.Append("00");//网络号
            sbHead.Append("FF");//可编程控制器网络号
            sbHead.Append("FF03");//请求目标模块I/O编号
            sbHead.Append("00");//请求目标模块站号
                                //s.Append("0C");//请求数据长度
            sbData.Append("1000");//CPU监视定时器
            sbData.Append("0104");//指令批量读取
            sbData.Append("0000");//子指令 
            sbData.Append(ToBigEndianHexString(Address).Substring(0, 6));//起始软元件十六进制大端格式
            sbData.Append(GetRegCode(regName));//软元件代码	 
            sbData.Append(ToBigEndianHexString(Count * 2).Substring(0, 4));//软元件点数

            //获取请求数据长度
            var strData = sbData.ToString();
            sbHead.Append(ToBigEndianHexString(strData.Length / 2).Substring(0, 4));//请求数据长度
            var strHead = sbHead.ToString();
            string sendStr = (strHead + strData).Replace(" ", "").ToUpper();//组合字符串格式发送数据,并剔除不必要的空白

            var sendData = HexStringToByteArray(sendStr);//发送数据
            var receiveData = _comm.Send(sendData);//接收数据

            //校验接收到的数据
            if (receiveData != null && //接收内容不为空
                receiveData.Length >= receiveDataHeadLength &&//接收内容长度正常
                BitConverter.ToString(receiveData).Replace("-", "").ToUpper().StartsWith("D0 00 00 FF FF 03 00".Replace(" ", "")))//接收内容符合格式要求
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
            //Int16[] ret = new short[0];//初始化返回值
            StringBuilder sbHead = new StringBuilder(iHeadFrameLength);//初始化帧头字符串
            StringBuilder sbData = new StringBuilder(iDataFrameLength);//初始化帧数据字符串
            sbHead.Append("5000");//副头部
            sbHead.Append("00");//网络号
            sbHead.Append("FF");//可编程控制器网络号
            sbHead.Append("FF03");//请求目标模块I/O编号
            sbHead.Append("00");//请求目标模块站号
                                //s.Append("0C");//请求数据长度
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
            sbHead.Append(ToBigEndianHexString(strData.Length / 2).Substring(0, 4));//请求数据长度
            var strHead = sbHead.ToString();
            string sendStr = (strHead + strData).Replace(" ", "").ToUpper();//组合字符串格式发送数据,并剔除不必要的空白

            var sendData = HexStringToByteArray(sendStr);//发送数据
            var receiveData = _comm.Send(sendData);//接收数据

            //校验接收到的数据
            if (receiveData != null && //接收内容不为空
                receiveData.Length >= receiveDataHeadLength &&//接收内容长度正常
                BitConverter.ToString(receiveData).Replace("-", "").ToUpper().StartsWith("D0 00 00 FF FF 03 00".Replace(" ", "")))//接收内容符合格式要求
            {
                return true;//校验成功，返回
            }
            return false;//返回内容
        }

        //读写字符串
        public override string ReadString(string regName, int Address, int Count)
        {
            string ret = "";//初始化返回值
            StringBuilder sbHead = new StringBuilder(iHeadFrameLength);//初始化帧头字符串
            StringBuilder sbData = new StringBuilder(iDataFrameLength);//初始化帧数据字符串
            sbHead.Append("5000");//副头部
            sbHead.Append("00");//网络号
            sbHead.Append("FF");//可编程控制器网络号
            sbHead.Append("FF03");//请求目标模块I/O编号
            sbHead.Append("00");//请求目标模块站号
                                //s.Append("0C");//请求数据长度
            sbData.Append("1000");//CPU监视定时器
            sbData.Append("0104");//指令批量读取
            sbData.Append("0000");//子指令 
            sbData.Append(ToBigEndianHexString(Address).Substring(0, 6));//起始软元件十六进制大端格式
            sbData.Append(GetRegCode(regName));//软元件代码	 
            sbData.Append(ToBigEndianHexString(Count).Substring(0, 4));//软元件点数

            //获取请求数据长度
            var strData = sbData.ToString();
            sbHead.Append(ToBigEndianHexString(strData.Length / 2).Substring(0, 4));//请求数据长度
            var strHead = sbHead.ToString();
            string sendStr = (strHead + strData).Replace(" ", "").ToUpper();//组合字符串格式发送数据,并剔除不必要的空白

            var sendData = HexStringToByteArray(sendStr);//发送数据
            var receiveData = _comm.Send(sendData);//接收数据

            //校验接收到的数据
            if (receiveData != null && //接收内容不为空
                receiveData.Length >= receiveDataHeadLength &&//接收内容长度正常
                BitConverter.ToString(receiveData).Replace("-", "").ToUpper().StartsWith("D0 00 00 FF FF 03 00".Replace(" ", "")))//接收内容符合格式要求
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
            //Int16[] ret = new short[0];//初始化返回值
            StringBuilder sbHead = new StringBuilder(iHeadFrameLength);//初始化帧头字符串
            StringBuilder sbData = new StringBuilder(iDataFrameLength);//初始化帧数据字符串
            sbHead.Append("5000");//副头部
            sbHead.Append("00");//网络号
            sbHead.Append("FF");//可编程控制器网络号
            sbHead.Append("FF03");//请求目标模块I/O编号
            sbHead.Append("00");//请求目标模块站号
                                //s.Append("0C");//请求数据长度
            sbData.Append("1000");//CPU监视定时器
            sbData.Append("0114");//指令批量写入
            sbData.Append("0000");//子指令 
            sbData.Append(ToBigEndianHexString(Address).Substring(0, 6));//起始软元件十六进制大端格式
            sbData.Append(GetRegCode(regName));//软元件代码	 
            sbData.Append(ToBigEndianHexString(values.Count()).Substring(0, 4));//软元件点数

            sbData.Append(BitConverter.ToString(Encoding.ASCII.GetBytes(values)).Replace("-", ""));//转换为ASCII码再写进去

            //获取请求数据长度
            var strData = sbData.ToString();
            sbHead.Append(ToBigEndianHexString(strData.Length / 2).Substring(0, 4));//请求数据长度
            var strHead = sbHead.ToString();
            string sendStr = (strHead + strData).Replace(" ", "").ToUpper();//组合字符串格式发送数据,并剔除不必要的空白

            var sendData = HexStringToByteArray(sendStr);//发送数据
            var receiveData = _comm.Send(sendData);//接收数据

            //校验接收到的数据
            if (receiveData != null && //接收内容不为空
                receiveData.Length >= receiveDataHeadLength &&//接收内容长度正常
                BitConverter.ToString(receiveData).Replace("-", "").ToUpper().StartsWith("D0 00 00 FF FF 03 00".Replace(" ", "")))//接收内容符合格式要求
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
