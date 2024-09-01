using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Protocols.Omron
{
    public class Fins : ProtocolBase
    {
        bool IsHandShake = false;
        static byte[] handShakeHead = Array.Empty<byte>();

        static byte[] handShakeResponse = Array.Empty<byte>();

        int sid=0;

        byte SA1 = 0x01;//源IP最后一个字段
        byte DA1 = 0x01;//目标IP最后一个字段

        public Fins(IComm comm) : base(comm)
        {
            handShakeHead = HexStringToByteArray("46 49 4E 53 00 00 00 0C 00 00 00 00 00 00 00 00 00 00 00 ".Replace(" ", ""));
            handShakeResponse = HexStringToByteArray("46 49 4E 53 00 00 00 10 00 00 00 01 00 00 00 00 00 00 00 01 00 00 00 02".Replace(" ", ""));
        }

        /// <summary>
        /// Mode取值范围false=字模式，true=位模式
        /// 存储区代号=>D位:02，D字:82，W位:31，C位:30，W字:B1，C字:B0
        /// </summary>
        /// <param name="RegName"></param>
        /// <param name="Mode"></param>
        /// <returns></returns>
        byte GetRegisterCode(string RegName, bool Mode)
        {
            switch (RegName)
            {
                default:
                case "CIO":
                case "C":
                case "": return (byte)(Mode ? 0x30 : 0xB0);
                case "W": return (byte)(Mode ? 0x31 : 0xB1);
                case "D": return (byte)(Mode ? 0x02 : 0x82);
            }
        }

        byte GetSID()
        {
            return (byte)Interlocked.Increment(ref sid);
        }

        bool CheckFrame(byte[] data,byte SID)
        {
            if (data != null
                && data.Length >16
                && data[25]==SID
                && ByteRangeCompare(data.Take(4).ToArray(), new byte[] { 0x46, 0x49, 0x4e, 0x53 })
                && ByteRangeCompare(data.Skip(12).Take(4).ToArray(), new byte[] { 0x00, 0x00, 0x00, 0x00 })
                )
            {
                return true;
            }
            return false;
        }

        void HandShake(Stream s)
        {
            if (!IsHandShake && _comm is CommNet)
            {
                var comm = (CommNet)_comm;
                byte localIpField = byte.Parse(comm.LocalIp.Remove(0, comm.LocalIp.LastIndexOf('.') + 1));

                MemoryStream ms = new MemoryStream();
                ms.Write(handShakeHead, 0, handShakeHead.Length);
                 
                ms.WriteByte(localIpField);

                var remoteIpField = byte.Parse(comm.RemoteIp.Remove(0, comm.LocalIp.LastIndexOf('.') + 1));

                handShakeResponse[19] = localIpField;
                handShakeResponse[23] = remoteIpField;

                DA1 = remoteIpField;
                SA1 = localIpField;

                var sendData = ms.ToArray();

                var s1=BitConverter.ToString(sendData).Replace("-"," ");

                var receiveData = comm.Send(s,sendData);

                if (receiveData != null
                    //&& ByteRangeCompare(receiveData, handShakeResponse)
                    )
                {
                    IsHandShake = true;
                }
            }
        }

        public override bool[] ReadBool(string regName, int Address, int Count)
        {
            var ret = Array.Empty<bool>();
            MemoryStream msHead = new MemoryStream();
            MemoryStream msData = new MemoryStream();

            //写入Header头
            var header = new byte[] { 0x46, 0x49, 0x4e, 0x53 };
            msHead.Write(header, 0, header.Length);

            //写入固定长度
            var length = new byte[] { 0x00, 0x00, 0x00, 0x1A };
            msHead.Write(length, 0, length.Length);

            //写入Command
            var command = new byte[] { 0x00, 0x00, 0x00, 0x02 };
            msHead.Write(command, 0, command.Length);

            //写入ErrorCode
            var errorCode = new byte[] { 0x00, 0x00, 0x00, 0x00 };
            msHead.Write(errorCode, 0, errorCode.Length);

            //写入ICF-固定值0x80
            msHead.WriteByte(0x80);

            //写入RSV-固定值0x00
            msHead.WriteByte(0x00);

            //写入GCT
            msHead.WriteByte(0x02);

            //写入DNA
            msHead.WriteByte(0x00);

            //写入DA1
            msHead.WriteByte(DA1);

            //写入DA2
            msHead.WriteByte(0x00);

            //写入SNA
            msHead.WriteByte(0x00);

            //写入SD1
            msHead.WriteByte(SA1);

            //写入SD2
            msHead.WriteByte(0x00);

            //写入SID
            var SID=GetSID();
            msHead.WriteByte(SID);

            //写入MRC
            msHead.WriteByte(0x01);

            //写入SRC
            msHead.WriteByte(0x01);

            //写入Area-存储区-1个字节
            msHead.WriteByte(GetRegisterCode(regName, true));

            //写入Address-3个字节
            var addr1 = BitConverter.GetBytes((Int16)(Address / 100));
            var addr2 = (byte)(Address % 100);
            Array.Reverse(addr1);
            msHead.Write(addr1, 0, addr1.Length);
            msHead.WriteByte(addr2);

            //写入读取长度
            var len = BitConverter.GetBytes((Int16)Count);
            Array.Reverse(len);
            msHead.Write(len, 0, len.Length);

            //写入Length-2个字节
            var sendData = msHead.ToArray();
            var receiveData = _comm.Send(new Action<Stream>(HandShake), sendData);

            //校验接收数据，然后拆出有用数据
            if (CheckFrame(receiveData, SID))
            {
                //拆数据
                var dataStartPos = 30;
                ret = new bool[Count];
                for (int i = 0; i < Count; i++)
                {
                    ret[i] = receiveData[i + dataStartPos] > 0;
                }

            }
            return ret;
        }

        public override bool WriteBool(string regName, int Address, bool[] Values)
        {
            MemoryStream msHead = new MemoryStream();
            MemoryStream msData = new MemoryStream();

            //写入Header头
            var header = new byte[] { 0x46, 0x49, 0x4e, 0x53 };
            msHead.Write(header, 0, header.Length);

            ////写入长度
            //var length = new byte[] { 0x00, 0x00, 0x00, 0x1A };
            //msHead.Write(length, 0, length.Length);

            //Data区
            //写入Command
            var command = new byte[] { 0x00, 0x00, 0x00, 0x02 };
            msData.Write(command, 0, command.Length);

            //写入ErrorCode
            var errorCode = new byte[] { 0x00, 0x00, 0x00, 0x00 };
            msData.Write(errorCode, 0, errorCode.Length);

            //写入ICF-固定值0x80
            msData.WriteByte(0x80);

            //写入RSV-固定值0x00
            msData.WriteByte(0x00);

            //写入GCT
            msData.WriteByte(0x02);

            //写入DNA
            msData.WriteByte(0x00);

            //写入DA1
            msData.WriteByte(DA1);

            //写入DA2
            msData.WriteByte(0x00);

            //写入SNA
            msData.WriteByte(0x00);

            //写入SD1
            msData.WriteByte(SA1);

            //写入SD2
            msData.WriteByte(0x00);

            //写入SID
            var SID = GetSID();
            msData.WriteByte(SID);

            //写入MRC
            msData.WriteByte(0x01);

            //写入SRC
            msData.WriteByte(0x02);

            //写入Area-存储区-1个字节
            msData.WriteByte(GetRegisterCode(regName, true));

            //写入Address-3个字节
            var addr1 = BitConverter.GetBytes((Int16)(Address / 100));
            var addr2 = (byte)(Address % 100);
            Array.Reverse(addr1);
            msData.Write(addr1, 0, addr1.Length);
            msData.WriteByte(addr2);

            //写入读取长度
            var len1 = BitConverter.GetBytes((Int16)Values.Length);
            Array.Reverse(len1);
            msData.Write(len1, 0, len1.Length);

            //写入值
            foreach (var value in Values)
            {
                msData.WriteByte((byte)(value ? 0x01 : 0x00));
            }

            var data = msData.ToArray();

            //写入长度
            var len2 = BitConverter.GetBytes(data.Length);
            Array.Reverse(len2);
            msHead.Write(len2, 0, len2.Length);

            //合并数组
            msHead.Write(data, 0, data.Length);

            //写入Length-2个字节
            var sendData = msHead.ToArray();
            //var s = BitConverter.ToString(sendData).Replace("-", " ");
            var receiveData = _comm.Send(new Action<Stream>(HandShake), sendData);

            //校验接收数据，然后拆出有用数据
            if (CheckFrame(receiveData, SID))
            {
                return true;

            }
            return false;
        }

        //INT16
        public override Int16[] ReadInt16(string regName, int Address, int Count)
        {
            var ret = Array.Empty<Int16>();
            MemoryStream msHead = new MemoryStream();
            MemoryStream msData = new MemoryStream();

            //写入Header头
            var header = new byte[] { 0x46, 0x49, 0x4e, 0x53 };
            msHead.Write(header, 0, header.Length);

            //写入固定长度
            var length = new byte[] { 0x00, 0x00, 0x00, 0x1A };
            msHead.Write(length, 0, length.Length);

            //写入Command
            var command = new byte[] { 0x00, 0x00, 0x00, 0x02 };
            msHead.Write(command, 0, command.Length);

            //写入ErrorCode
            var errorCode = new byte[] { 0x00, 0x00, 0x00, 0x00 };
            msHead.Write(errorCode, 0, errorCode.Length);

            //写入ICF-固定值0x80
            msHead.WriteByte(0x80);

            //写入RSV-固定值0x00
            msHead.WriteByte(0x00);

            //写入GCT
            msHead.WriteByte(0x02);

            //写入DNA
            msHead.WriteByte(0x00);

            //写入DA1
            msHead.WriteByte(DA1);

            //写入DA2
            msHead.WriteByte(0x00);

            //写入SNA
            msHead.WriteByte(0x00);

            //写入SD1
            msHead.WriteByte(SA1);

            //写入SD2
            msHead.WriteByte(0x00);

            //写入SID
            var SID = GetSID();
            msHead.WriteByte(SID);

            //写入MRC
            msHead.WriteByte(0x01);

            //写入SRC
            msHead.WriteByte(0x01);

            //写入Area-存储区-1个字节
            msHead.WriteByte(GetRegisterCode(regName, false));

            //写入Address-3个字节
            var addr1 = BitConverter.GetBytes((Int16)(Address / 100));
            var addr2 = (byte)(Address % 100);
            Array.Reverse(addr1);
            msHead.Write(addr1, 0, addr1.Length);
            msHead.WriteByte(addr2);

            //写入读取长度
            var len = BitConverter.GetBytes((Int16)Count);
            Array.Reverse(len);
            msHead.Write(len, 0, len.Length);

            //写入Length-2个字节
            var sendData = msHead.ToArray();
            var receiveData = _comm.Send(new Action<Stream>(HandShake), sendData);

            //校验接收数据，然后拆出有用数据
            if (CheckFrame(receiveData, SID))
            {
                //拆数据
                var dataStartPos = 30;
                ret = new short[Count];
                for (int i = 0; i < Count; i++)
                {
                    ret[i] = ToBigEndian(BitConverter.ToInt16(receiveData, (dataStartPos + i * Marshal.SizeOf<Int16>())));
                }
            }
            return ret;
        }

        public override bool WriteInt16(string regName, int Address, Int16[] Values)
        {
            MemoryStream msHead = new MemoryStream();
            MemoryStream msData = new MemoryStream();

            //写入Header头
            var header = new byte[] { 0x46, 0x49, 0x4e, 0x53 };
            msHead.Write(header, 0, header.Length);

            ////写入长度
            //var length = new byte[] { 0x00, 0x00, 0x00, 0x1A };
            //msHead.Write(length, 0, length.Length);

            //Data区
            //写入Command
            var command = new byte[] { 0x00, 0x00, 0x00, 0x02 };
            msData.Write(command, 0, command.Length);

            //写入ErrorCode
            var errorCode = new byte[] { 0x00, 0x00, 0x00, 0x00 };
            msData.Write(errorCode, 0, errorCode.Length);

            //写入ICF-固定值0x80
            msData.WriteByte(0x80);

            //写入RSV-固定值0x00
            msData.WriteByte(0x00);

            //写入GCT
            msData.WriteByte(0x02);

            //写入DNA
            msData.WriteByte(0x00);

            //写入DA1
            msData.WriteByte(DA1);

            //写入DA2
            msData.WriteByte(0x00);

            //写入SNA
            msData.WriteByte(0x00);

            //写入SD1
            msData.WriteByte(SA1);

            //写入SD2
            msData.WriteByte(0x00);

            //写入SID
            var SID = GetSID();
            msData.WriteByte(SID);

            //写入MRC
            msData.WriteByte(0x01);

            //写入SRC
            msData.WriteByte(0x02);

            //写入Area-存储区-1个字节
            msData.WriteByte(GetRegisterCode(regName, false));

            //写入Address-3个字节
            var addr1 = BitConverter.GetBytes((Int16)(Address / 100));
            var addr2 = (byte)(Address % 100);
            Array.Reverse(addr1);
            msData.Write(addr1, 0, addr1.Length);
            msData.WriteByte(addr2);

            //写入读取长度
            var len1 = BitConverter.GetBytes((Int16)Values.Length);
            Array.Reverse(len1);
            msData.Write(len1, 0, len1.Length);

            //写入值
            foreach (var value in Values)
            {
                var tmp= BitConverter.GetBytes((Int16)value);
                Array.Reverse(tmp);
                msData.Write(tmp,0,tmp.Length);
            }

            var data = msData.ToArray();

            //写入长度
            var len2 = BitConverter.GetBytes(data.Length);
            Array.Reverse(len2);
            msHead.Write(len2, 0, len2.Length);

            //合并数组
            msHead.Write(data, 0, data.Length);

            //写入Length-2个字节
            var sendData = msHead.ToArray();
            //var s = BitConverter.ToString(sendData).Replace("-", " ");
            var receiveData = _comm.Send(new Action<Stream>(HandShake), sendData);

            //校验接收数据，然后拆出有用数据
            if (CheckFrame(receiveData, SID))
            {
                return true;

            }
            return false;
        }

        //uint16
        public override UInt16[] ReadUInt16(string regName, int Address, int Count)
        {
            var ret = Array.Empty<UInt16>();
            MemoryStream msHead = new MemoryStream();
            MemoryStream msData = new MemoryStream();

            //写入Header头
            var header = new byte[] { 0x46, 0x49, 0x4e, 0x53 };
            msHead.Write(header, 0, header.Length);

            //写入固定长度
            var length = new byte[] { 0x00, 0x00, 0x00, 0x1A };
            msHead.Write(length, 0, length.Length);

            //写入Command
            var command = new byte[] { 0x00, 0x00, 0x00, 0x02 };
            msHead.Write(command, 0, command.Length);

            //写入ErrorCode
            var errorCode = new byte[] { 0x00, 0x00, 0x00, 0x00 };
            msHead.Write(errorCode, 0, errorCode.Length);

            //写入ICF-固定值0x80
            msHead.WriteByte(0x80);

            //写入RSV-固定值0x00
            msHead.WriteByte(0x00);

            //写入GCT
            msHead.WriteByte(0x02);

            //写入DNA
            msHead.WriteByte(0x00);

            //写入DA1
            msHead.WriteByte(DA1);

            //写入DA2
            msHead.WriteByte(0x00);

            //写入SNA
            msHead.WriteByte(0x00);

            //写入SD1
            msHead.WriteByte(SA1);

            //写入SD2
            msHead.WriteByte(0x00);

            //写入SID
            var SID = GetSID();
            msHead.WriteByte(SID);

            //写入MRC
            msHead.WriteByte(0x01);

            //写入SRC
            msHead.WriteByte(0x01);

            //写入Area-存储区-1个字节
            msHead.WriteByte(GetRegisterCode(regName, false));

            //写入Address-3个字节
            var addr1 = BitConverter.GetBytes((Int16)(Address / 100));
            var addr2 = (byte)(Address % 100);
            Array.Reverse(addr1);
            msHead.Write(addr1, 0, addr1.Length);
            msHead.WriteByte(addr2);

            //写入读取长度
            var len = BitConverter.GetBytes((Int16)Count);
            Array.Reverse(len);
            msHead.Write(len, 0, len.Length);

            //写入Length-2个字节
            var sendData = msHead.ToArray();
            var receiveData = _comm.Send(new Action<Stream>(HandShake), sendData);

            //校验接收数据，然后拆出有用数据
            if (CheckFrame(receiveData, SID))
            {
                //拆数据
                var dataStartPos = 30;
                ret = new ushort[Count];
                for (int i = 0; i < Count; i++)
                {
                    ret[i] = ToBigEndian(BitConverter.ToUInt16(receiveData, (dataStartPos + i * Marshal.SizeOf<UInt16>())));
                }
            }
            return ret;
        }

        public override bool WriteUInt16(string regName, int Address, UInt16[] Values)
        {
            MemoryStream msHead = new MemoryStream();
            MemoryStream msData = new MemoryStream();

            //写入Header头
            var header = new byte[] { 0x46, 0x49, 0x4e, 0x53 };
            msHead.Write(header, 0, header.Length);

            ////写入长度
            //var length = new byte[] { 0x00, 0x00, 0x00, 0x1A };
            //msHead.Write(length, 0, length.Length);

            //Data区
            //写入Command
            var command = new byte[] { 0x00, 0x00, 0x00, 0x02 };
            msData.Write(command, 0, command.Length);

            //写入ErrorCode
            var errorCode = new byte[] { 0x00, 0x00, 0x00, 0x00 };
            msData.Write(errorCode, 0, errorCode.Length);

            //写入ICF-固定值0x80
            msData.WriteByte(0x80);

            //写入RSV-固定值0x00
            msData.WriteByte(0x00);

            //写入GCT
            msData.WriteByte(0x02);

            //写入DNA
            msData.WriteByte(0x00);

            //写入DA1
            msData.WriteByte(DA1);

            //写入DA2
            msData.WriteByte(0x00);

            //写入SNA
            msData.WriteByte(0x00);

            //写入SD1
            msData.WriteByte(SA1);

            //写入SD2
            msData.WriteByte(0x00);

            //写入SID
            var SID = GetSID();
            msData.WriteByte(SID);

            //写入MRC
            msData.WriteByte(0x01);

            //写入SRC
            msData.WriteByte(0x02);

            //写入Area-存储区-1个字节
            msData.WriteByte(GetRegisterCode(regName, false));

            //写入Address-3个字节
            var addr1 = BitConverter.GetBytes((Int16)(Address / 100));
            var addr2 = (byte)(Address % 100);
            Array.Reverse(addr1);
            msData.Write(addr1, 0, addr1.Length);
            msData.WriteByte(addr2);

            //写入读取长度
            var len1 = BitConverter.GetBytes((Int16)Values.Length);
            Array.Reverse(len1);
            msData.Write(len1, 0, len1.Length);

            //写入值
            foreach (var value in Values)
            {
                var tmp = BitConverter.GetBytes((UInt16)value);
                Array.Reverse(tmp);
                msData.Write(tmp, 0, tmp.Length);
            }

            var data = msData.ToArray();

            //写入长度
            var len2 = BitConverter.GetBytes(data.Length);
            Array.Reverse(len2);
            msHead.Write(len2, 0, len2.Length);

            //合并数组
            msHead.Write(data, 0, data.Length);

            //写入Length-2个字节
            var sendData = msHead.ToArray();
            //var s = BitConverter.ToString(sendData).Replace("-", " ");
            var receiveData = _comm.Send(new Action<Stream>(HandShake), sendData);

            //校验接收数据，然后拆出有用数据
            if (CheckFrame(receiveData, SID))
            {
                return true;

            }
            return false;
        }

        //32位 
        public override Int32[] ReadInt32(string regName, int Address, int Count)
        {
            var ret = Array.Empty<Int32>();
            MemoryStream msHead = new MemoryStream();
            MemoryStream msData = new MemoryStream();

            //写入Header头
            var header = new byte[] { 0x46, 0x49, 0x4e, 0x53 };
            msHead.Write(header, 0, header.Length);

            //写入固定长度
            var length = new byte[] { 0x00, 0x00, 0x00, 0x1A };
            msHead.Write(length, 0, length.Length);

            //写入Command
            var command = new byte[] { 0x00, 0x00, 0x00, 0x02 };
            msHead.Write(command, 0, command.Length);

            //写入ErrorCode
            var errorCode = new byte[] { 0x00, 0x00, 0x00, 0x00 };
            msHead.Write(errorCode, 0, errorCode.Length);

            //写入ICF-固定值0x80
            msHead.WriteByte(0x80);

            //写入RSV-固定值0x00
            msHead.WriteByte(0x00);

            //写入GCT
            msHead.WriteByte(0x02);

            //写入DNA
            msHead.WriteByte(0x00);

            //写入DA1
            msHead.WriteByte(DA1);

            //写入DA2
            msHead.WriteByte(0x00);

            //写入SNA
            msHead.WriteByte(0x00);

            //写入SD1
            msHead.WriteByte(SA1);

            //写入SD2
            msHead.WriteByte(0x00);

            //写入SID
            var SID = GetSID();
            msHead.WriteByte(SID);

            //写入MRC
            msHead.WriteByte(0x01);

            //写入SRC
            msHead.WriteByte(0x01);

            //写入Area-存储区-1个字节
            msHead.WriteByte(GetRegisterCode(regName, false));

            //写入Address-3个字节
            var addr1 = BitConverter.GetBytes((Int16)(Address));
            var addr2 = (byte)(0x00);
            Array.Reverse(addr1);
            msHead.Write(addr1, 0, addr1.Length);
            msHead.WriteByte(addr2);

            //写入读取长度
            var len = BitConverter.GetBytes((Int16)(Count*2));
            Array.Reverse(len);
            msHead.Write(len, 0, len.Length);

            //写入Length-2个字节
            var sendData = msHead.ToArray();
            var t = BitConverter.ToString(sendData).Replace("-", " ");
            var receiveData = _comm.Send(new Action<Stream>(HandShake), sendData);

            //校验接收数据，然后拆出有用数据
            if (CheckFrame(receiveData, SID))
            {
                //拆数据
                var dataStartPos = 30;
                ret = new Int32[Count];
                for (int i = 0; i < Count; i++)
                {
                    ret[i] = ToBigEndian(BitConverter.ToInt32(receiveData, (dataStartPos + i * Marshal.SizeOf<Int32>())));
                }
            }
            return ret;
        }

        public override bool WriteInt32(string regName, int Address, Int32[] Values)
        {
            MemoryStream msHead = new MemoryStream();
            MemoryStream msData = new MemoryStream();

            //写入Header头
            var header = new byte[] { 0x46, 0x49, 0x4e, 0x53 };
            msHead.Write(header, 0, header.Length);

            ////写入长度
            //var length = new byte[] { 0x00, 0x00, 0x00, 0x1A };
            //msHead.Write(length, 0, length.Length);

            //Data区
            //写入Command
            var command = new byte[] { 0x00, 0x00, 0x00, 0x02 };
            msData.Write(command, 0, command.Length);

            //写入ErrorCode
            var errorCode = new byte[] { 0x00, 0x00, 0x00, 0x00 };
            msData.Write(errorCode, 0, errorCode.Length);

            //写入ICF-固定值0x80
            msData.WriteByte(0x80);

            //写入RSV-固定值0x00
            msData.WriteByte(0x00);

            //写入GCT
            msData.WriteByte(0x02);

            //写入DNA
            msData.WriteByte(0x00);

            //写入DA1
            msData.WriteByte(DA1);

            //写入DA2
            msData.WriteByte(0x00);

            //写入SNA
            msData.WriteByte(0x00);

            //写入SD1
            msData.WriteByte(SA1);

            //写入SD2
            msData.WriteByte(0x00);

            //写入SID
            var SID = GetSID();
            msData.WriteByte(SID);

            //写入MRC
            msData.WriteByte(0x01);

            //写入SRC
            msData.WriteByte(0x02);

            //写入Area-存储区-1个字节
            msData.WriteByte(GetRegisterCode(regName, false));

            //写入Address-3个字节
            var addr1 = BitConverter.GetBytes((Int16)(Address));
            var addr2 = (byte)(0x00);
            Array.Reverse(addr1);
            msData.Write(addr1, 0, addr1.Length);
            msData.WriteByte(addr2);

            //写入读取长度
            var len1 = BitConverter.GetBytes((Int16)(Values.Length * 2));
            Array.Reverse(len1);
            msData.Write(len1, 0, len1.Length);

            //写入值
            foreach (var value in Values)
            {
                var tmp = BitConverter.GetBytes(value);
                Array.Reverse(tmp);
                msData.Write(tmp, 0, tmp.Length);
            }

            var data = msData.ToArray();

            //写入长度
            var len2 = BitConverter.GetBytes(data.Length);
            Array.Reverse(len2);
            msHead.Write(len2, 0, len2.Length);

            //合并数组
            msHead.Write(data, 0, data.Length);

            //写入Length-2个字节
            var sendData = msHead.ToArray();
            //var s = BitConverter.ToString(sendData).Replace("-", " ");
            var receiveData = _comm.Send(new Action<Stream>(HandShake), sendData);

            //校验接收数据，然后拆出有用数据
            if (CheckFrame(receiveData, SID))
            {
                return true;

            }
            return false;
        }

        //uint32
        public override UInt32[] ReadUInt32(string regName, int Address, int Count)
        {
            var ret = Array.Empty<UInt32>();
            MemoryStream msHead = new MemoryStream();
            MemoryStream msData = new MemoryStream();

            //写入Header头
            var header = new byte[] { 0x46, 0x49, 0x4e, 0x53 };
            msHead.Write(header, 0, header.Length);

            //写入固定长度
            var length = new byte[] { 0x00, 0x00, 0x00, 0x1A };
            msHead.Write(length, 0, length.Length);

            //写入Command
            var command = new byte[] { 0x00, 0x00, 0x00, 0x02 };
            msHead.Write(command, 0, command.Length);

            //写入ErrorCode
            var errorCode = new byte[] { 0x00, 0x00, 0x00, 0x00 };
            msHead.Write(errorCode, 0, errorCode.Length);

            //写入ICF-固定值0x80
            msHead.WriteByte(0x80);

            //写入RSV-固定值0x00
            msHead.WriteByte(0x00);

            //写入GCT
            msHead.WriteByte(0x02);

            //写入DNA
            msHead.WriteByte(0x00);

            //写入DA1
            msHead.WriteByte(DA1);

            //写入DA2
            msHead.WriteByte(0x00);

            //写入SNA
            msHead.WriteByte(0x00);

            //写入SD1
            msHead.WriteByte(SA1);

            //写入SD2
            msHead.WriteByte(0x00);

            //写入SID
            var SID = GetSID();
            msHead.WriteByte(SID);

            //写入MRC
            msHead.WriteByte(0x01);

            //写入SRC
            msHead.WriteByte(0x01);

            //写入Area-存储区-1个字节
            msHead.WriteByte(GetRegisterCode(regName, false));

            //写入Address-3个字节
            var addr1 = BitConverter.GetBytes((Int16)(Address));
            var addr2 = (byte)(0x00);
            Array.Reverse(addr1);
            msHead.Write(addr1, 0, addr1.Length);
            msHead.WriteByte(addr2);

            //写入读取长度
            var len = BitConverter.GetBytes((Int16)(Count * 2));
            Array.Reverse(len);
            msHead.Write(len, 0, len.Length);

            //写入Length-2个字节
            var sendData = msHead.ToArray();
            var receiveData = _comm.Send(new Action<Stream>(HandShake), sendData);

            //校验接收数据，然后拆出有用数据
            if (CheckFrame(receiveData, SID))
            {
                //拆数据
                var dataStartPos = 30;
                ret = new UInt32[Count];
                for (int i = 0; i < Count; i++)
                {
                    ret[i] = ToBigEndian(BitConverter.ToUInt32(receiveData, (dataStartPos + i * Marshal.SizeOf<UInt32>())));
                }
            }
            return ret;
        }

        public override bool WriteUInt32(string regName, int Address, UInt32[] Values)
        {
            MemoryStream msHead = new MemoryStream();
            MemoryStream msData = new MemoryStream();

            //写入Header头
            var header = new byte[] { 0x46, 0x49, 0x4e, 0x53 };
            msHead.Write(header, 0, header.Length);

            ////写入长度
            //var length = new byte[] { 0x00, 0x00, 0x00, 0x1A };
            //msHead.Write(length, 0, length.Length);

            //Data区
            //写入Command
            var command = new byte[] { 0x00, 0x00, 0x00, 0x02 };
            msData.Write(command, 0, command.Length);

            //写入ErrorCode
            var errorCode = new byte[] { 0x00, 0x00, 0x00, 0x00 };
            msData.Write(errorCode, 0, errorCode.Length);

            //写入ICF-固定值0x80
            msData.WriteByte(0x80);

            //写入RSV-固定值0x00
            msData.WriteByte(0x00);

            //写入GCT
            msData.WriteByte(0x02);

            //写入DNA
            msData.WriteByte(0x00);

            //写入DA1
            msData.WriteByte(DA1);

            //写入DA2
            msData.WriteByte(0x00);

            //写入SNA
            msData.WriteByte(0x00);

            //写入SD1
            msData.WriteByte(SA1);

            //写入SD2
            msData.WriteByte(0x00);

            //写入SID
            var SID = GetSID();
            msData.WriteByte(SID);

            //写入MRC
            msData.WriteByte(0x01);

            //写入SRC
            msData.WriteByte(0x02);

            //写入Area-存储区-1个字节
            msData.WriteByte(GetRegisterCode(regName, false));

            //写入Address-3个字节
            var addr1 = BitConverter.GetBytes((Int16)(Address));
            var addr2 = (byte)(0x00);
            Array.Reverse(addr1);
            msData.Write(addr1, 0, addr1.Length);
            msData.WriteByte(addr2);

            //写入读取长度
            var len1 = BitConverter.GetBytes((Int16)(Values.Length * 2));
            Array.Reverse(len1);
            msData.Write(len1, 0, len1.Length);

            //写入值
            foreach (var value in Values)
            {
                var tmp = BitConverter.GetBytes((UInt32)value);
                Array.Reverse(tmp);
                msData.Write(tmp, 0, tmp.Length);
            }

            var data = msData.ToArray();

            //写入长度
            var len2 = BitConverter.GetBytes(data.Length);
            Array.Reverse(len2);
            msHead.Write(len2, 0, len2.Length);

            //合并数组
            msHead.Write(data, 0, data.Length);

            //写入Length-2个字节
            var sendData = msHead.ToArray();
            //var s = BitConverter.ToString(sendData).Replace("-", " ");
            var receiveData = _comm.Send(new Action<Stream>(HandShake), sendData);

            //校验接收数据，然后拆出有用数据
            if (CheckFrame(receiveData, SID))
            {
                return true;

            }
            return false;
        }


        //single
        public override Single[] ReadSingle(string regName, int Address, int Count)
        {
            var ret = Array.Empty<Single>();
            MemoryStream msHead = new MemoryStream();
            MemoryStream msData = new MemoryStream();

            //写入Header头
            var header = new byte[] { 0x46, 0x49, 0x4e, 0x53 };
            msHead.Write(header, 0, header.Length);

            //写入固定长度
            var length = new byte[] { 0x00, 0x00, 0x00, 0x1A };
            msHead.Write(length, 0, length.Length);

            //写入Command
            var command = new byte[] { 0x00, 0x00, 0x00, 0x02 };
            msHead.Write(command, 0, command.Length);

            //写入ErrorCode
            var errorCode = new byte[] { 0x00, 0x00, 0x00, 0x00 };
            msHead.Write(errorCode, 0, errorCode.Length);

            //写入ICF-固定值0x80
            msHead.WriteByte(0x80);

            //写入RSV-固定值0x00
            msHead.WriteByte(0x00);

            //写入GCT
            msHead.WriteByte(0x02);

            //写入DNA
            msHead.WriteByte(0x00);

            //写入DA1
            msHead.WriteByte(DA1);

            //写入DA2
            msHead.WriteByte(0x00);

            //写入SNA
            msHead.WriteByte(0x00);

            //写入SD1
            msHead.WriteByte(SA1);

            //写入SD2
            msHead.WriteByte(0x00);

            //写入SID
            var SID = GetSID();
            msHead.WriteByte(SID);

            //写入MRC
            msHead.WriteByte(0x01);

            //写入SRC
            msHead.WriteByte(0x01);

            //写入Area-存储区-1个字节
            msHead.WriteByte(GetRegisterCode(regName, false));

            //写入Address-3个字节
            var addr1 = BitConverter.GetBytes((Int16)(Address));
            var addr2 = (byte)(0x00);
            Array.Reverse(addr1);
            msHead.Write(addr1, 0, addr1.Length);
            msHead.WriteByte(addr2);

            //写入读取长度
            var len = BitConverter.GetBytes((Int16)(Count * 2));
            Array.Reverse(len);
            msHead.Write(len, 0, len.Length);

            //写入Length-2个字节
            var sendData = msHead.ToArray();
            var receiveData = _comm.Send(new Action<Stream>(HandShake), sendData);

            //校验接收数据，然后拆出有用数据
            if (CheckFrame(receiveData, SID))
            {
                //拆数据
                var dataStartPos = 30;
                ret = new Single[Count];
                for (int i = 0; i < Count; i++)
                {
                    ret[i] = ToBigEndian(BitConverter.ToSingle(receiveData, (dataStartPos + i * Marshal.SizeOf<Single>())));
                }
            }
            return ret;
        }

        public override bool WriteSingle(string regName, int Address, Single[] Values)
        {
            MemoryStream msHead = new MemoryStream();
            MemoryStream msData = new MemoryStream();

            //写入Header头
            var header = new byte[] { 0x46, 0x49, 0x4e, 0x53 };
            msHead.Write(header, 0, header.Length);

            ////写入长度
            //var length = new byte[] { 0x00, 0x00, 0x00, 0x1A };
            //msHead.Write(length, 0, length.Length);

            //Data区
            //写入Command
            var command = new byte[] { 0x00, 0x00, 0x00, 0x02 };
            msData.Write(command, 0, command.Length);

            //写入ErrorCode
            var errorCode = new byte[] { 0x00, 0x00, 0x00, 0x00 };
            msData.Write(errorCode, 0, errorCode.Length);

            //写入ICF-固定值0x80
            msData.WriteByte(0x80);

            //写入RSV-固定值0x00
            msData.WriteByte(0x00);

            //写入GCT
            msData.WriteByte(0x02);

            //写入DNA
            msData.WriteByte(0x00);

            //写入DA1
            msData.WriteByte(DA1);

            //写入DA2
            msData.WriteByte(0x00);

            //写入SNA
            msData.WriteByte(0x00);

            //写入SD1
            msData.WriteByte(SA1);

            //写入SD2
            msData.WriteByte(0x00);

            //写入SID
            var SID = GetSID();
            msData.WriteByte(SID);

            //写入MRC
            msData.WriteByte(0x01);

            //写入SRC
            msData.WriteByte(0x02);

            //写入Area-存储区-1个字节
            msData.WriteByte(GetRegisterCode(regName, false));

            //写入Address-3个字节
            var addr1 = BitConverter.GetBytes((Int16)(Address));
            var addr2 = (byte)(0x00);
            Array.Reverse(addr1);
            msData.Write(addr1, 0, addr1.Length);
            msData.WriteByte(addr2);

            //写入读取长度
            var len1 = BitConverter.GetBytes((Int16)(Values.Length * 2));
            Array.Reverse(len1);
            msData.Write(len1, 0, len1.Length);

            //写入值
            foreach (var value in Values)
            {
                var tmp = BitConverter.GetBytes(value);
                Array.Reverse(tmp);
                msData.Write(tmp, 0, tmp.Length);
            }

            var data = msData.ToArray();

            //写入长度
            var len2 = BitConverter.GetBytes(data.Length);
            Array.Reverse(len2);
            msHead.Write(len2, 0, len2.Length);

            //合并数组
            msHead.Write(data, 0, data.Length);

            //写入Length-2个字节
            var sendData = msHead.ToArray();
            //var s = BitConverter.ToString(sendData).Replace("-", " ");
            var receiveData = _comm.Send(new Action<Stream>(HandShake), sendData);

            //校验接收数据，然后拆出有用数据
            if (CheckFrame(receiveData, SID))
            {
                return true;

            }
            return false;
        }

        //string 
        public override string ReadString(string regName, int Address, int Count)
        {
            string ret = null;
            MemoryStream msHead = new MemoryStream();
            MemoryStream msData = new MemoryStream();

            //写入Header头
            var header = new byte[] { 0x46, 0x49, 0x4e, 0x53 };
            msHead.Write(header, 0, header.Length);

            //写入固定长度
            var length = new byte[] { 0x00, 0x00, 0x00, 0x1A };
            msHead.Write(length, 0, length.Length);

            //写入Command
            var command = new byte[] { 0x00, 0x00, 0x00, 0x02 };
            msHead.Write(command, 0, command.Length);

            //写入ErrorCode
            var errorCode = new byte[] { 0x00, 0x00, 0x00, 0x00 };
            msHead.Write(errorCode, 0, errorCode.Length);

            //写入ICF-固定值0x80
            msHead.WriteByte(0x80);

            //写入RSV-固定值0x00
            msHead.WriteByte(0x00);

            //写入GCT
            msHead.WriteByte(0x02);

            //写入DNA
            msHead.WriteByte(0x00);

            //写入DA1
            msHead.WriteByte(DA1);

            //写入DA2
            msHead.WriteByte(0x00);

            //写入SNA
            msHead.WriteByte(0x00);

            //写入SD1
            msHead.WriteByte(SA1);

            //写入SD2
            msHead.WriteByte(0x00);

            //写入SID
            var SID = GetSID();
            msHead.WriteByte(SID);

            //写入MRC
            msHead.WriteByte(0x01);

            //写入SRC
            msHead.WriteByte(0x01);

            //写入Area-存储区-1个字节
            msHead.WriteByte(GetRegisterCode(regName, false));

            //写入Address-3个字节
            var addr1 = BitConverter.GetBytes((Int16)(Address / 100));
            var addr2 = (byte)(Address % 100);
            Array.Reverse(addr1);
            msHead.Write(addr1, 0, addr1.Length);
            msHead.WriteByte(addr2);

            //写入读取长度
            var len = BitConverter.GetBytes((Int16)Count);
            Array.Reverse(len);
            msHead.Write(len, 0, len.Length);

            //写入Length-2个字节
            var sendData = msHead.ToArray();
            var receiveData = _comm.Send(new Action<Stream>(HandShake), sendData);

            //校验接收数据，然后拆出有用数据
            if (CheckFrame(receiveData, SID))
            {
                //拆数据-不进行高低字节交换
                var dataStartPos = 30;
                ret =Encoding.UTF8.GetString(receiveData.Skip(dataStartPos).Take(Count).ToArray());
            }
            return ret;
        }

        public override bool WriteString(string regName, int Address, String Value)
        {
            MemoryStream msHead = new MemoryStream();
            MemoryStream msData = new MemoryStream();

            //写入Header头
            var header = new byte[] { 0x46, 0x49, 0x4e, 0x53 };
            msHead.Write(header, 0, header.Length);

            ////写入长度
            //var length = new byte[] { 0x00, 0x00, 0x00, 0x1A };
            //msHead.Write(length, 0, length.Length);

            //Data区
            //写入Command
            var command = new byte[] { 0x00, 0x00, 0x00, 0x02 };
            msData.Write(command, 0, command.Length);

            //写入ErrorCode
            var errorCode = new byte[] { 0x00, 0x00, 0x00, 0x00 };
            msData.Write(errorCode, 0, errorCode.Length);

            //写入ICF-固定值0x80
            msData.WriteByte(0x80);

            //写入RSV-固定值0x00
            msData.WriteByte(0x00);

            //写入GCT
            msData.WriteByte(0x02);

            //写入DNA
            msData.WriteByte(0x00);

            //写入DA1
            msData.WriteByte(DA1);

            //写入DA2
            msData.WriteByte(0x00);

            //写入SNA
            msData.WriteByte(0x00);

            //写入SD1
            msData.WriteByte(SA1);

            //写入SD2
            msData.WriteByte(0x00);

            //写入SID
            var SID = GetSID();
            msData.WriteByte(SID);

            //写入MRC
            msData.WriteByte(0x01);

            //写入SRC
            msData.WriteByte(0x02);

            //写入Area-存储区-1个字节
            msData.WriteByte(GetRegisterCode(regName, false));

            //写入Address-3个字节
            var addr1 = BitConverter.GetBytes((Int16)(Address / 100));
            var addr2 = (byte)(Address % 100);
            Array.Reverse(addr1);
            msData.Write(addr1, 0, addr1.Length);
            msData.WriteByte(addr2);

            //写入读取长度
            var len1 = BitConverter.GetBytes((Int16)Value.Length);
            Array.Reverse(len1);
            msData.Write(len1, 0, len1.Length);

            //写入值--不进行高低字节交换试试
            var str = Value;
            if (str.Length % 2 != 0) str += "\0";
            var buffer=Encoding.UTF8.GetBytes(str);
            msData.Write(buffer, 0, buffer.Length); 

            var data = msData.ToArray();

            //写入长度
            var len2 = BitConverter.GetBytes(data.Length);
            Array.Reverse(len2);
            msHead.Write(len2, 0, len2.Length);

            //合并数组
            msHead.Write(data, 0, data.Length);

            //写入Length-2个字节
            var sendData = msHead.ToArray();
            //var s = BitConverter.ToString(sendData).Replace("-", " ");
            var receiveData = _comm.Send(new Action<Stream>(HandShake), sendData);

            //校验接收数据，然后拆出有用数据
            if (CheckFrame(receiveData,SID))
            {
                return true;
            }
            return false;
        }
    }
}
