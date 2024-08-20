using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Protocols.Protocols
{
    internal abstract class ModbusBase 
    {
        protected IComm Comm;
        public ModbusBase(IComm comm) 
        { 
            Comm = comm; 
        }

        public enum FrameFormatEnum{ABCD,BADC,CDAB,DCBA }//4种字节格式

        public FrameFormatEnum FrameFormat { get; set; }= FrameFormatEnum.DCBA;//默认的帧格式

        protected UInt16 UInt16CRC16(byte[] bytes)
        {
            ushort crc = 0xFFFF;
            for (int i = 0; i < bytes.Length; i++)
            {
                crc = (ushort)(crc ^ bytes[i]);
                for (int j = 0; j < 8; j++)
                {
                    crc = (crc & 0x0001) != 0 ? (ushort)((crc >> 1) ^ 0xA001) : (ushort)(crc >> 1);
                }
            }
            return crc;
        }
        protected byte[] byteCRC16(byte[] bytes)
        {
            ushort crc = 0xFFFF;
            for (int i = 0; i < bytes.Length; i++)
            {
                crc = (ushort)(crc ^ bytes[i]);
                for (int j = 0; j < 8; j++)
                {
                    crc = (crc & 0x0001) != 0 ? (ushort)((crc >> 1) ^ 0xA001) : (ushort)(crc >> 1);
                }
            }
            return BitConverter.GetBytes(crc);
        }

        protected byte[] BoolArrayToByteArray(bool[] boolArray)
        {
            int bytesLength = (boolArray.Length + 7) / 8;    //计算字节长度喵
            byte[] byteArray = new byte[bytesLength];
            for (int i = 0; i < boolArray.Length; i++)
            {
                if (boolArray[i])
                {
                    int byteIndex = i / 8;              //确定目标byte喵
                    int bitIndex = i % 8;               //确定bit的位置喵
                    byteArray[byteIndex] |= (byte)(1 << bitIndex); //使用位运算将bit设置为1喵
                }
            }
            return byteArray;
        }

        protected byte LRC8(byte[] dat)
        {
            short ret = 0; 
            for (int i = 0; i < dat.Length; i++)
            {
                ret += dat[i];
            }
            return (byte)(-ret);
        }

        protected byte LRC8(string str)
        {
            short ret = 0;
            var dat = HexStringToByteArray(str);
            for (int i = 0; i < dat.Length; i++)
            {
                ret += dat[i];
            }
            return (byte)(- ret);
        }

        protected byte[] HexStringToByteArray(string str)
        {
            byte[] ret;
            string t = str.Trim().Replace(" ", "").ToUpper();

            ret = Enumerable.Range(0, t.Length)
            .Where(x => x % 2 == 0)
            .Select(y => Convert.ToByte(t.Substring(y, 2), 16))
            .ToArray();
            return ret;
        }

        //各基本类型的大小端转换，大转小，小转大方法相同
        protected UInt16 ToBigEndian(UInt16 Dat)
        {
            return BitConverter.ToUInt16(BitConverter.GetBytes(Dat).Reverse().ToArray(), 0);
        }

        protected Int16 ToBigEndian(Int16 Dat)
        {
            return BitConverter.ToInt16(BitConverter.GetBytes(Dat).Reverse().ToArray(), 0);
        }

        protected UInt32 ToBigEndian(UInt32 Dat)
        {
            return BitConverter.ToUInt32(BitConverter.GetBytes(Dat).Reverse().ToArray(), 0);
        }

        protected Int32 ToBigEndian(Int32 Dat)
        {
            return BitConverter.ToInt32(BitConverter.GetBytes(Dat).Reverse().ToArray(), 0);
        }

        protected Single ToBigEndian(Single Dat)
        {
            return BitConverter.ToSingle(BitConverter.GetBytes(Dat).Reverse().ToArray(), 0);
        }


        ////
        protected Int32 ToLocalEndian(Int32 data)
        {
            var source = BitConverter.GetBytes(data);
            byte[] dst = new byte[4];
            switch (FrameFormat)
            {
                default:
                case FrameFormatEnum.DCBA:
                    {
                        dst[0] = source[0];
                        dst[1] = source[1];
                        dst[2] = source[2];
                        dst[3] = source[3];
                        return BitConverter.ToInt32(dst, 0);
                    }
                case FrameFormatEnum.CDAB:
                    {
                        dst[0] = source[1];
                        dst[1] = source[0];
                        dst[2] = source[3];
                        dst[3] = source[2];
                        return BitConverter.ToInt32(dst, 0);
                    }
                case FrameFormatEnum.BADC:
                    {
                        dst[0] = source[2];
                        dst[1] = source[3];
                        dst[2] = source[0];
                        dst[3] = source[1];
                        return BitConverter.ToInt32(dst, 0);
                    }
                case FrameFormatEnum.ABCD:
                    {
                        dst[0] = source[3];
                        dst[1] = source[2];
                        dst[2] = source[1];
                        dst[3] = source[0];
                        return BitConverter.ToInt32(dst, 0);
                    }
            }
        }
        protected UInt32 ToLocalEndian(UInt32 data)
        {
            var source = BitConverter.GetBytes(data);
            byte[] dst = new byte[4];
            switch (FrameFormat)
            {
                default:
                case FrameFormatEnum.DCBA:
                    {
                        dst[0] = source[0];
                        dst[1] = source[1];
                        dst[2] = source[2];
                        dst[3] = source[3];
                        return BitConverter.ToUInt32(dst, 0);
                    }
                case FrameFormatEnum.CDAB:
                    {
                        dst[0] = source[1];
                        dst[1] = source[0];
                        dst[2] = source[3];
                        dst[3] = source[2];
                        return BitConverter.ToUInt32(dst, 0);
                    }
                case FrameFormatEnum.BADC:
                    {
                        dst[0] = source[2];
                        dst[1] = source[3];
                        dst[2] = source[0];
                        dst[3] = source[1];
                        return BitConverter.ToUInt32(dst, 0);
                    }
                case FrameFormatEnum.ABCD:
                    {
                        dst[0] = source[3];
                        dst[1] = source[2];
                        dst[2] = source[1];
                        dst[3] = source[0];
                        return BitConverter.ToUInt32(dst, 0);
                    }
            }
        }

        protected Single ToLocalEndian(Single data)
        {
            var source = BitConverter.GetBytes(data);
            byte[] dst = new byte[4];
            switch (FrameFormat)
            {
                default:
                case FrameFormatEnum.DCBA:
                    {
                        dst[0] = source[0];
                        dst[1] = source[1];
                        dst[2] = source[2];
                        dst[3] = source[3];
                        return BitConverter.ToSingle(dst, 0);
                    }
                case FrameFormatEnum.CDAB:
                    {
                        dst[0] = source[1];
                        dst[1] = source[0];
                        dst[2] = source[3];
                        dst[3] = source[2];
                        return BitConverter.ToSingle(dst, 0);
                    }
                case FrameFormatEnum.BADC:
                    {
                        dst[0] = source[2];
                        dst[1] = source[3];
                        dst[2] = source[0];
                        dst[3] = source[1];
                        return BitConverter.ToSingle(dst, 0);
                    }
                case FrameFormatEnum.ABCD:
                    {
                        dst[0] = source[3];
                        dst[1] = source[2];
                        dst[2] = source[1];
                        dst[3] = source[0];
                        return BitConverter.ToSingle(dst, 0);
                    }
            }
        }


        /////////////////////////////////////////////////////////////////////////简化方法

        /// <summary>
        /// Use FunctionCode:03
        /// 字操作，单个或多个
        /// </summary> 
        /// <param name="StationNumber"></param>
        /// <param name="Address"></param>
        /// <param name="Count"></param>
        /// <returns></returns>
        public Int16[] ReadHoldingRegisters(int StationNumber, int Address)
        {
            return ReadHoldingRegisters<Int16>(StationNumber, Address, 1);
        }

        /// <summary>
        /// Use FunctionCode:03
        /// 字操作，单个或多个
        /// </summary> 
        /// <param name="StationNumber"></param>
        /// <param name="Address"></param>
        /// <param name="Count"></param>
        /// <returns></returns>
        public Int16[] ReadHoldingRegisters(int StationNumber, int Address, int Count)
        {
            return ReadHoldingRegisters<Int16>(StationNumber,Address,Count);
        }

        /// <summary>
        /// Use FunctionCode:04
        /// 字操作，单个或多个
        /// </summary> 
        /// <param name="StationNumber"></param>
        /// <param name="Address"></param>
        /// <param name="Count"></param>
        /// <returns></returns>
        public Int16[] ReadInputRegisters(int StationNumber, int Address)
        {
            return ReadInputRegisters<Int16>(StationNumber, Address, 1);
        }

        /// <summary>
        /// Use FunctionCode:04
        /// 字操作，单个或多个
        /// </summary> 
        /// <param name="StationNumber"></param>
        /// <param name="Address"></param>
        /// <param name="Count"></param>
        /// <returns></returns>
        public Int16[] ReadInputRegisters(int StationNumber, int Address, int Count)
        {
            return ReadInputRegisters<Int16>(StationNumber, Address, Count);
        }


        /// <summary>
        /// Use FunctionCode:06
        /// 字操作，单个
        /// </summary> 
        /// <param name="StationNumber"></param>
        /// <param name="Address"></param>
        /// <param name="Value"></param>
        /// <returns></returns>
        public  bool WriteSingleRegister(int StationNumber, int Address, Int16 Value)
        {
            return WriteSingleRegister<Int16>(StationNumber,Address,(Int16)Value);
        }

        /// <summary>
        /// Use FunctionCode:16
        /// 字操作，多个
        /// </summary> 
        /// <param name="StationNumber"></param>
        /// <param name="Address"></param>
        /// <param name="Values"></param>
        /// <returns></returns>
        public bool WriteMultipleRegisters(int StationNumber, int Address, Int16[] Values)
        {
            return WriteMultipleRegisters<Int16>(StationNumber, Address, Values);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////完整方法

        /// <summary>
        /// Use FunctionCode:01
        /// 位操作，单个或多个
        /// </summary>
        /// <param name="StationNumber"></param>
        /// <param name="Address"></param>
        /// <param name="Count"></param>
        /// <returns></returns>
        public virtual bool[] ReadCoils(int StationNumber,int Address,int Count)
        {
            return ReadCoils(StationNumber,(byte)0x01,Address,Count);
        }

        /// <summary>
        /// Use FunctionCode:02
        /// 位操作，单个或多个
        /// </summary>
        /// <param name="StationNumber"></param>
        /// <param name="Address"></param>
        /// <param name="Count"></param>
        /// <returns></returns>
        public virtual bool[] ReadDiscreteInputs(int StationNumber, int Address, int Count)
        {
            return ReadCoils(StationNumber,(byte)0x02,Address,Count);
        }

        /// <summary>
        /// Use FunctionCode:03
        /// 字操作，单个或多个
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="StationNumber"></param>
        /// <param name="Address"></param>
        /// <param name="Count"></param>
        /// <returns></returns>
        public virtual T[] ReadHoldingRegisters<T>(int StationNumber, int Address, int Count)
        {
            return ReadRegisters<T>(StationNumber, (byte)0x03,Address,Count);
        }

        /// <summary>
        /// Use FunctionCode:04
        /// 字操作，单个或多个
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="StationNumber"></param>
        /// <param name="Address"></param>
        /// <param name="Count"></param>
        /// <returns></returns>
        public virtual T[] ReadInputRegisters<T>(int StationNumber, int Address, int Count)
        {
            return ReadRegisters<T>(StationNumber, (byte)0x04, Address, Count);
        }

        /// <summary>
        /// Use FunctionCode:05
        /// 位操作，单个
        /// </summary>
        /// <param name="StationNumber"></param>
        /// <param name="Address"></param>
        /// <param name="Value"></param>
        /// <returns></returns>
        public virtual bool WriteSingleCoil(int StationNumber, int Address, bool Value)
        {
            return WriteCoils(StationNumber, (byte)0x05, Address, new bool[] { Value});
        }

        /// <summary>
        /// Use FunctionCode:06
        /// 字操作，单个
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="StationNumber"></param>
        /// <param name="Address"></param>
        /// <param name="Value"></param>
        /// <returns></returns>
        public virtual bool WriteSingleRegister<T>(int StationNumber, int Address, T Value)
        {
            if(typeof(T).Equals(typeof(Int16)) || typeof(T).Equals(typeof(UInt16)))
                return WriteRegisters<T>(StationNumber, (byte)0x06, Address, new T[] { Value });
            else
                return WriteRegisters<T>(StationNumber, (byte)0x10, Address, new T[] { Value });
        }

        /// <summary>
        /// Use FunctionCode:15
        /// 字操作，多个
        /// </summary>
        /// <param name="StationNumber"></param>
        /// <param name="Address"></param>
        /// <param name="Values"></param>
        /// <returns></returns>
        public virtual bool WriteMultipleCoils(int StationNumber, int Address, bool[] Values)
        {
            return WriteCoils(StationNumber, (byte)0x0F,Address,Values);
        }

        /// <summary>
        /// Use FunctionCode:16
        /// 字操作，多个
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="StationNumber"></param>
        /// <param name="Address"></param>
        /// <param name="Values"></param>
        /// <returns></returns>
        public virtual bool WriteMultipleRegisters<T>(int StationNumber, int Address, T[] Values)
        {
            return WriteRegisters<T>(StationNumber, (byte)0x10, Address, Values);
        }


        /////////////////////////////////////////////////////以下是需要子类实现的方法

        /// <summary>
        /// 读线圈
        /// </summary>
        /// <param name="StationNumber"></param>
        /// <param name="FunctionCode"></param>
        /// <param name="Address"></param>
        /// <param name="Count"></param>
        /// <returns></returns>
        protected abstract bool[] ReadCoils(int StationNumber,byte FunctionCode,int Address,int Count);

        /// <summary>
        /// 写线圈
        /// </summary>
        /// <param name="StationNumber"></param>
        /// <param name="FunctionCode"></param>
        /// <param name="Address"></param>
        /// <param name="Values"></param>
        /// <returns></returns>
        protected abstract bool WriteCoils(int StationNumber,byte FunctionCode,int Address,bool[] Values);


        /// <summary>
        /// 读寄存器
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="StationNumber"></param>
        /// <param name="FunctionCode"></param>
        /// <param name="Address"></param>
        /// <param name="Count"></param>
        /// <returns></returns>
        protected abstract T[] ReadRegisters<T>(int StationNumber, byte FunctionCode, int Address, int Count);

        /// <summary>
        /// 写寄存器
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="StationNumber"></param>
        /// <param name="FunctionCode"></param>
        /// <param name="Address"></param>
        /// <param name="Values"></param>
        /// <returns></returns>
        protected abstract bool WriteRegisters<T>(int StationNumber, byte FunctionCode, int Address, T[] Values);

    }

    internal class RTU : ModbusBase
    {
        public RTU(IComm comm) : base(comm)
        {

        }

        protected bool CheckCrc16(byte[] data)
        {
            int len = data.Length;
            var crc1 = byteCRC16(data.Take(len - 2).ToArray());
            var crc2 = data.Skip(len - 2).Take(2).ToArray();
            return crc1.SequenceEqual(crc2);

        }

        protected override bool[] ReadCoils(int StationNumber, byte FunctionCode, int Address, int Count)
        {
            bool[] ret = Array.Empty<bool>();
            var ms = new MemoryStream();


            ms.WriteByte((byte)StationNumber);//站号            
            ms.WriteByte((byte)FunctionCode);//功能码

            //地址
            var adr = BitConverter.GetBytes((Int16)Address);
            Array.Reverse(adr);//转换为大端
            ms.Write(adr, 0, adr.Length);

            //长度
            var n = BitConverter.GetBytes((Int16)Count);
            Array.Reverse(n);//转换为大端
            ms.Write(n, 0, n.Length);

            //CRC
            var crc = byteCRC16(ms.ToArray());
            ms.Write(crc, 0, crc.Length);

            //发送数据
            var sendData = ms.ToArray();
            var receiveData = Comm.Send(sendData);

            bool status = CheckCrc16(receiveData);

            //解析接受数据
            if (receiveData != null //判断是否为空
                && receiveData.Length >= 6 //判断长度
                && receiveData.Take(2).SequenceEqual(sendData.Take(2)) //比较文件头
                && CheckCrc16(receiveData)//校验CRC16 
                )
            {
                ret = new bool[Count];
                int dataOffset = 3;
                int j = 0;
                byte value = receiveData[dataOffset];
                for (int i = 0; i < Count; i++)
                {
                    ret[i] = value % 2 == 1;//返回的每个位占用一个字节，true=0x01,false=0x00
                    value /= 2;
                    j++;
                    if (j >= 8)
                    {
                        value = receiveData[++dataOffset];
                        j = 0;
                    }
                }
            }

            return ret;
        }

        protected override T[] ReadRegisters<T>(int StationNumber, byte FunctionCode, int Address, int Count)
        {
            T[] ret = Array.Empty<T>();
            var ms = new MemoryStream();


            ms.WriteByte((byte)StationNumber);//站号            
            ms.WriteByte((byte)FunctionCode);//功能码

            //地址
            var adr = BitConverter.GetBytes((Int16)Address);
            Array.Reverse(adr);//转换为大端
            ms.Write(adr, 0, adr.Length);

            //长度
            if (!typeof(T).Equals(typeof(string)))
            {
                var n = BitConverter.GetBytes((Int16)(Count * Marshal.SizeOf<T>() / 2));
                Array.Reverse(n);//转换为大端
                ms.Write(n, 0, n.Length);
            }
            else
            {
                var n = BitConverter.GetBytes((Int16)(Count));
                Array.Reverse(n);//转换为大端
                ms.Write(n, 0, n.Length);
            }

            //CRC
            var crc = byteCRC16(ms.ToArray());
            ms.Write(crc, 0, crc.Length);

            //发送数据
            var sendData = ms.ToArray();
            var receiveData = Comm.Send(sendData);

            bool status = CheckCrc16(receiveData);

            //解析接受数据
            if (receiveData != null //判断是否为空
                && receiveData.Length >= 6 //判断长度
                && receiveData.Take(2).SequenceEqual(sendData.Take(2)) //比较文件头
                && CheckCrc16(receiveData)//校验CRC16 
                )
            {
                if (!typeof(T).Equals(typeof(string))) ret = new T[Count]; else { ret = new T[1]; }
                if (typeof(T).Equals(typeof(string))) ret[0] = (T)Convert.ChangeType(Encoding.UTF8.GetString(receiveData.Skip(3).Take(Count).ToArray()), typeof(T));//字符串读只返回第一个
                else
                {
                    for (int i = 0; i < Count; i++)
                    {
                        if (typeof(T).Equals(typeof(Int16))) ret[i] = (T)Convert.ChangeType(ToBigEndian(BitConverter.ToInt16(receiveData, i * Marshal.SizeOf<T>() + 3)), typeof(T));
                        if (typeof(T).Equals(typeof(UInt16))) ret[i] = (T)Convert.ChangeType(ToBigEndian(BitConverter.ToUInt16(receiveData, i * Marshal.SizeOf<T>() + 3)), typeof(T));
                        if (typeof(T).Equals(typeof(Int32))) ret[i] = (T)Convert.ChangeType(ToLocalEndian(BitConverter.ToInt32(receiveData, i * Marshal.SizeOf<T>() + 3)), typeof(T));
                        if (typeof(T).Equals(typeof(UInt32))) ret[i] = (T)Convert.ChangeType(ToLocalEndian(BitConverter.ToUInt32(receiveData, i * Marshal.SizeOf<T>() + 3)), typeof(T));
                        if (typeof(T).Equals(typeof(Single))) ret[i] = (T)Convert.ChangeType(ToLocalEndian(BitConverter.ToSingle(receiveData, i * Marshal.SizeOf<T>() + 3)), typeof(T));
                    }
                }
            }

            return ret;

        }

        protected override bool WriteCoils(int StationNumber, byte FunctionCode, int Address, bool[] Values)
        {
            var ms = new MemoryStream();


            ms.WriteByte((byte)StationNumber);//站号            
            ms.WriteByte((byte)FunctionCode);//功能码

            //地址
            var adr = BitConverter.GetBytes((Int16)Address);
            Array.Reverse(adr);
            ms.Write(adr, 0, adr.Length);

            //写单个
            if (FunctionCode == 0x05)
            {
                var n = BitConverter.GetBytes((Int16)(Values[0] == true ? 0xFF : 0x00));
                ms.Write(n, 0, n.Length);
            }
            else
            {
                var data = BoolArrayToByteArray(Values);

                var len1 = BitConverter.GetBytes((Int16)(Values.Length));
                Array.Reverse(len1);
                ms.Write(len1, 0, len1.Length);

                var len2 = (byte)data.Length;
                ms.WriteByte(len2);

                ms.Write(data, 0, data.Length);
            }


            //CRC
            var crc = byteCRC16(ms.ToArray());
            ms.Write(crc, 0, crc.Length);

            //发送数据
            var sendData = ms.ToArray();
            var receiveData = Comm.Send(sendData);

            //解析接受数据
            if (receiveData != null //判断是否为空
                && receiveData.Length >= 6 //判断长度
                && receiveData.Take(2).SequenceEqual(sendData.Take(2)) //比较文件头
                && CheckCrc16(receiveData)//校验CRC16 
                )
            {
                return true;
            }

            return false;
        }

        protected override bool WriteRegisters<T>(int StationNumber, byte FunctionCode, int Address, T[] Values)
        {
            var ms = new MemoryStream();

            string str = "";
            if (typeof(T).Equals(typeof(string))) str = Values[0] as string;
            if (str.Length % 2 != 0) str += '\0';


            ms.WriteByte((byte)StationNumber);//站号            
            ms.WriteByte((byte)FunctionCode);//功能码

            //地址
            var adr = BitConverter.GetBytes((Int16)Address);
            Array.Reverse(adr);
            ms.Write(adr, 0, adr.Length);

            //写入寄存器数量
            if (!typeof(T).Equals(typeof(string)))
            {
                var count = BitConverter.GetBytes((Int16)(Values.Length * Marshal.SizeOf<T>() / 2));
                Array.Reverse(count);
                ms.Write(count, 0, count.Length);
            }
            else
            {
                var count = BitConverter.GetBytes((Int16)((str).Length / 2));
                Array.Reverse(count);
                ms.Write(count, 0, count.Length);
            }

            //数据长度
            if (!typeof(T).Equals(typeof(string)))
            {
                var length = (byte)(Values.Length * Marshal.SizeOf<T>());
                ms.WriteByte(length);
            }
            else
            {
                var length = (byte)(str.Length);
                ms.WriteByte(length);
            }


            //写内容
            if (typeof(T).Equals(typeof(string)))
            {
                byte[] data = null;
                data = Encoding.UTF8.GetBytes(str);
                ms.Write(data, 0, data.Length);
            }
            else
            {
                foreach (var value in Values)
                {
                    byte[] data = null;
                    if (typeof(T).Equals(typeof(Int16))) data = BitConverter.GetBytes(ToBigEndian((Int16)Convert.ChangeType(value, typeof(Int16))));
                    if (typeof(T).Equals(typeof(UInt16))) data = BitConverter.GetBytes(ToBigEndian((UInt16)Convert.ChangeType(value, typeof(UInt16))));
                    if (typeof(T).Equals(typeof(Int32))) data = BitConverter.GetBytes(ToLocalEndian((Int32)Convert.ChangeType(value, typeof(Int32))));
                    if (typeof(T).Equals(typeof(UInt32))) data = BitConverter.GetBytes(ToLocalEndian((UInt32)Convert.ChangeType(value, typeof(UInt32))));
                    if (typeof(T).Equals(typeof(Single))) data = BitConverter.GetBytes(ToLocalEndian((Single)Convert.ChangeType(value, typeof(Single))));

                    ms.Write(data, 0, data.Length);
                }
            }


            //CRC
            var crc = byteCRC16(ms.ToArray());
            ms.Write(crc, 0, crc.Length);

            //发送数据
            var sendData = ms.ToArray();
            var receiveData = Comm.Send(sendData);

            //解析接受数据
            if (receiveData != null //判断是否为空
                && receiveData.Length >= 6 //判断长度
                && receiveData.Take(2).SequenceEqual(sendData.Take(2)) //比较文件头
                && CheckCrc16(receiveData)//校验CRC16 
                )
            {
                return true;
            }

            return false;
        }
    }


    internal class ASCII : ModbusBase
    {
        public ASCII(IComm comm) : base(comm)
        {

        }

        protected bool CheckLRC8(string frame)
        {
            string pattern = @"[^0-9a-fA-F]";

            //string str=Regex.Replace(frame,pattern,"").Trim().ToUpper();//去掉回车和换行,并替换不需要的字符
            string str = frame.Remove(0, 1).Trim();//去掉回车和换行,并替换不需要的字符
            int len = str.Length;
            var lrc1 = LRC8(str.Substring(0,len-2)).ToString("X2");
            var lrc2 = str.Substring(len - 2, 2);
            return lrc1.Equals(lrc2);

        }

        protected override bool[] ReadCoils(int StationNumber, byte FunctionCode, int Address, int Count)
        {
            bool[] ret = Array.Empty<bool>();
            var ms = new MemoryStream();


            ms.WriteByte((byte)StationNumber);//站号            
            ms.WriteByte((byte)FunctionCode);//功能码

            //地址
            var adr = BitConverter.GetBytes((Int16)Address);
            Array.Reverse(adr);//转换为大端
            ms.Write(adr, 0, adr.Length);

            //长度
            var n = BitConverter.GetBytes((Int16)Count);
            Array.Reverse(n);//转换为大端
            ms.Write(n, 0, n.Length);
            
            //LRC
            var LRC = LRC8(ms.ToArray());
            ms.WriteByte(LRC);

            //发送数据
            var sendStr = BitConverter.ToString(ms.ToArray()).Replace("-","");
            var receiveStr = SendAOP(sendStr);

            bool status = CheckLRC8(receiveStr);
            var str1 = sendStr.Substring(0,5);
            var str2 = receiveStr.Substring(1,5);

            //解析接受数据
            if (receiveStr != null //判断是否为空
                && receiveStr.Length >= 6 //判断长度
                && receiveStr.StartsWith(":")
                && receiveStr.Substring(1,4).Equals(sendStr.Substring(0,4)) //比较文件头
                && CheckLRC8(receiveStr)//校验CRC16 
                )
            {
                ret = new bool[Count];
                int dataOffset = 3;
                int j = 0;
                var receiveData = HexStringToByteArray(receiveStr.Remove(0,1).Trim());
                byte value = receiveData[dataOffset];
                for (int i = 0; i < Count; i++)
                {
                    ret[i] = value % 2 == 1;//返回的每个位占用一个字节，true=0x01,false=0x00
                    value /= 2;
                    j++;
                    if (j >= 8)
                    {
                        value = receiveData[++dataOffset];
                        j = 0;
                    }
                }
            }

            return ret;
        }

        protected override T[] ReadRegisters<T>(int StationNumber, byte FunctionCode, int Address, int Count)
        {
            T[] ret = Array.Empty<T>();
            var ms = new MemoryStream();


            ms.WriteByte((byte)StationNumber);//站号            
            ms.WriteByte((byte)FunctionCode);//功能码

            //地址
            var adr = BitConverter.GetBytes((Int16)Address);
            Array.Reverse(adr);//转换为大端
            ms.Write(adr, 0, adr.Length);

            //长度
            if (!typeof(T).Equals(typeof(string)))
            {
                var n = BitConverter.GetBytes((Int16)(Count * Marshal.SizeOf<T>() / 2));
                Array.Reverse(n);//转换为大端
                ms.Write(n, 0, n.Length);
            }
            else
            {
                var n = BitConverter.GetBytes((Int16)(Count));
                Array.Reverse(n);//转换为大端
                ms.Write(n, 0, n.Length);
            }

            //LRC
            var LRC = LRC8(ms.ToArray());
            ms.WriteByte(LRC);

            //发送数据
            var sendStr = BitConverter.ToString(ms.ToArray()).Replace("-", "");
            var receiveStr = SendAOP(sendStr);

            //解析接受数据
            if (receiveStr != null //判断是否为空
                && receiveStr.Length >= 6 //判断长度
                && receiveStr.StartsWith(":")
                && receiveStr.Substring(1, 4).Equals(sendStr.Substring(0, 4)) //比较文件头
                && CheckLRC8(receiveStr)//校验CRC16 
                )
            {
                var receiveData = HexStringToByteArray(receiveStr.Remove(0, 1).Trim());
                if (!typeof(T).Equals(typeof(string))) ret = new T[Count]; else { ret = new T[1]; }
                if (typeof(T).Equals(typeof(string))) ret[0] = (T)Convert.ChangeType(Encoding.UTF8.GetString(receiveData.Skip(3).Take(Count).ToArray()), typeof(T));//字符串读只返回第一个
                else
                {
                    for (int i = 0; i < Count; i++)
                    {
                        if (typeof(T).Equals(typeof(Int16))) ret[i] = (T)Convert.ChangeType(ToBigEndian(BitConverter.ToInt16(receiveData, i * Marshal.SizeOf<T>() + 3)), typeof(T));
                        if (typeof(T).Equals(typeof(UInt16))) ret[i] = (T)Convert.ChangeType(ToBigEndian(BitConverter.ToUInt16(receiveData, i * Marshal.SizeOf<T>() + 3)), typeof(T));
                        if (typeof(T).Equals(typeof(Int32))) ret[i] = (T)Convert.ChangeType(ToLocalEndian(BitConverter.ToInt32(receiveData, i * Marshal.SizeOf<T>() + 3)), typeof(T));
                        if (typeof(T).Equals(typeof(UInt32))) ret[i] = (T)Convert.ChangeType(ToLocalEndian(BitConverter.ToUInt32(receiveData, i * Marshal.SizeOf<T>() + 3)), typeof(T));
                        if (typeof(T).Equals(typeof(Single))) ret[i] = (T)Convert.ChangeType(ToLocalEndian(BitConverter.ToSingle(receiveData, i * Marshal.SizeOf<T>() + 3)), typeof(T));
                    }
                }
            }

            return ret;

        }

        protected override bool WriteCoils(int StationNumber, byte FunctionCode, int Address, bool[] Values)
        {
            var ms = new MemoryStream();


            ms.WriteByte((byte)StationNumber);//站号            
            ms.WriteByte((byte)FunctionCode);//功能码

            //地址
            var adr = BitConverter.GetBytes((Int16)Address);
            Array.Reverse(adr);
            ms.Write(adr, 0, adr.Length);

            //写单个
            if (FunctionCode == 0x05)
            {
                var n = BitConverter.GetBytes((Int16)(Values[0] == true ? 0xFF : 0x00));
                ms.Write(n, 0, n.Length);
            }
            else
            {
                var data = BoolArrayToByteArray(Values);

                var len1 = BitConverter.GetBytes((Int16)(Values.Length));
                Array.Reverse(len1);
                ms.Write(len1, 0, len1.Length);

                var len2 = (byte)data.Length;
                ms.WriteByte(len2);

                ms.Write(data, 0, data.Length);
            }


            //LRC
            var LRC = LRC8(ms.ToArray());
            ms.WriteByte(LRC);

            //发送数据
            var sendStr = BitConverter.ToString(ms.ToArray()).Replace("-", "");
            var receiveStr = SendAOP(sendStr);

            //解析接受数据
            if (receiveStr != null //判断是否为空
                && receiveStr.Length >= 6 //判断长度
                && receiveStr.StartsWith(":")
                && receiveStr.Substring(1, 4).Equals(sendStr.Substring(0, 4)) //比较文件头
                && CheckLRC8(receiveStr)//校验CRC16 
                )
            {
                return true;
            }

            return false;
        }

        protected override bool WriteRegisters<T>(int StationNumber, byte FunctionCode, int Address, T[] Values)
        {
            var ms = new MemoryStream();

            string str = "";
            if (typeof(T).Equals(typeof(string))) str = Values[0] as string;
            if (str.Length % 2 != 0) str += '\0';


            ms.WriteByte((byte)StationNumber);//站号            
            ms.WriteByte((byte)FunctionCode);//功能码

            //地址
            var adr = BitConverter.GetBytes((Int16)Address);
            Array.Reverse(adr);
            ms.Write(adr, 0, adr.Length);

            //写入寄存器数量
            if (!typeof(T).Equals(typeof(string)))
            {
                var count = BitConverter.GetBytes((Int16)(Values.Length * Marshal.SizeOf<T>() / 2));
                Array.Reverse(count);
                ms.Write(count, 0, count.Length);
            }
            else
            {
                var count = BitConverter.GetBytes((Int16)((str).Length / 2));
                Array.Reverse(count);
                ms.Write(count, 0, count.Length);
            }

            //数据长度
            if (!typeof(T).Equals(typeof(string)))
            {
                var length = (byte)(Values.Length * Marshal.SizeOf<T>());
                ms.WriteByte(length);
            }
            else
            {
                var length = (byte)(str.Length);
                ms.WriteByte(length);
            }


            //写内容
            if (typeof(T).Equals(typeof(string)))
            {
                byte[] data = null;
                data = Encoding.UTF8.GetBytes(str);
                ms.Write(data, 0, data.Length);
            }
            else
            {
                foreach (var value in Values)
                {
                    byte[] data = null;
                    if (typeof(T).Equals(typeof(Int16))) data = BitConverter.GetBytes(ToBigEndian((Int16)Convert.ChangeType(value, typeof(Int16))));
                    if (typeof(T).Equals(typeof(UInt16))) data = BitConverter.GetBytes(ToBigEndian((UInt16)Convert.ChangeType(value, typeof(UInt16))));
                    if (typeof(T).Equals(typeof(Int32))) data = BitConverter.GetBytes(ToLocalEndian((Int32)Convert.ChangeType(value, typeof(Int32))));
                    if (typeof(T).Equals(typeof(UInt32))) data = BitConverter.GetBytes(ToLocalEndian((UInt32)Convert.ChangeType(value, typeof(UInt32))));
                    if (typeof(T).Equals(typeof(Single))) data = BitConverter.GetBytes(ToLocalEndian((Single)Convert.ChangeType(value, typeof(Single))));

                    ms.Write(data, 0, data.Length);
                }
            }


            //LRC
            var LRC = LRC8(ms.ToArray());
            ms.WriteByte(LRC);

            //发送数据
            var sendStr = BitConverter.ToString(ms.ToArray()).Replace("-", "");
            var receiveStr = SendAOP(sendStr);

            //解析接受数据
            if (receiveStr != null //判断是否为空
                && receiveStr.Length >= 6 //判断长度
                && receiveStr.StartsWith(":")
                && receiveStr.Substring(1, 4).Equals(sendStr.Substring(0, 4)) //比较文件头
                && CheckLRC8(receiveStr)//校验CRC16 
                )
            {
                return true;
            }

            return false;
        }

        string SendAOP(string str)
        {
            return Comm.Send(str.Insert(0,":")+"\r\n");
        }
    }
}
