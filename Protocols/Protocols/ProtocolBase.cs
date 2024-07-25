using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Protocols.Protocols
{
    //包含基本方法的基类-本类只能被继承，不能直接创建实例
    internal abstract class ProtocolBase
    {
        protected IComm _comm;
        public ProtocolBase(IComm comm)
        {
            _comm = comm;
        }
        //工具方法组
        //十六进制字符串转字节数组
        protected byte[] HexStringToByteArray(string str)
        {
            byte[] ret;
            string t = str.Replace(" ", "").ToUpper();

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

        //重载的大转小方法
        protected UInt16 ToLittleEndian(UInt16 Dat)
        {
            return BitConverter.ToUInt16(BitConverter.GetBytes(Dat).Reverse().ToArray(), 0);
        }

        protected Int16 ToLittleEndian(Int16 Dat)
        {
            return BitConverter.ToInt16(BitConverter.GetBytes(Dat).Reverse().ToArray(), 0);
        }

        protected UInt32 ToLittleEndian(UInt32 Dat)
        {
            return BitConverter.ToUInt32(BitConverter.GetBytes(Dat).Reverse().ToArray(), 0);
        }

        protected Int32 ToLittleEndian(Int32 Dat)
        {
            return BitConverter.ToInt32(BitConverter.GetBytes(Dat).Reverse().ToArray(), 0);
        }

        protected Single ToLittleEndian(Single Dat)
        {
            return BitConverter.ToSingle(BitConverter.GetBytes(Dat).Reverse().ToArray(), 0);
        }

        //工具方法组2
        protected string ToBigEndianHexString(Int16 dat)
        {
            return BitConverter.ToString(BitConverter.GetBytes(dat)).Replace("-", "");
        }

        protected string ToBigEndianHexString(UInt16 dat)
        {
            return BitConverter.ToString(BitConverter.GetBytes(dat)).Replace("-", "");
        }

        protected string ToBigEndianHexString(Int32 dat)
        {
            return BitConverter.ToString(BitConverter.GetBytes(dat)).Replace("-", "");
        }

        protected string ToBigEndianHexString(UInt32 dat)
        {
            return BitConverter.ToString(BitConverter.GetBytes(dat)).Replace("-", "");
        }

        protected string ToBigEndianHexString(Single dat)
        {
            return BitConverter.ToString(BitConverter.GetBytes(dat)).Replace("-", "");
        }

        protected string ByteAryToBinString(byte[] dats)
        {
            StringBuilder ret = new StringBuilder();
            foreach (var dat in dats)
            {
                var tmp = dat;
                for (int i = 0; i < 8; i++)
                {
                    ret.Append(tmp % 2 != 0 ? "1" : "0");
                    tmp /= 2;
                }
            }
            return ret.ToString();
        }

        //泛型版本
        public object ReadData<T>(string regName, int Address)
        {
            return ReadData<T>(regName, Address, 1);
        }

        public object ReadData<T>(string regName, int Address, int Count)
        {
            if (Count == 1)
            {
                if (typeof(T).Equals(typeof(bool))) return ReadBool(regName, Address, Count).FirstOrDefault();
                if (typeof(T).Equals(typeof(Int16))) return ReadInt16(regName, Address, Count).FirstOrDefault();
                if (typeof(T).Equals(typeof(UInt16))) return ReadUInt16(regName, Address, Count).FirstOrDefault();
                if (typeof(T).Equals(typeof(Int32))) return ReadInt32(regName, Address, Count).FirstOrDefault();
                if (typeof(T).Equals(typeof(UInt32))) return ReadUInt32(regName, Address, Count).FirstOrDefault();
                if (typeof(T).Equals(typeof(Single))) return ReadSingle(regName, Address, Count).FirstOrDefault();
            }
            else
            {
                if (typeof(T).Equals(typeof(bool[]))) return ReadBool(regName, Address, Count);
                if (typeof(T).Equals(typeof(Int16[]))) return ReadInt16(regName, Address, Count);
                if (typeof(T).Equals(typeof(UInt16[]))) return ReadUInt16(regName, Address, Count);
                if (typeof(T).Equals(typeof(Int32[]))) return ReadInt32(regName, Address, Count);
                if (typeof(T).Equals(typeof(UInt32[]))) return ReadUInt32(regName, Address, Count);
                if (typeof(T).Equals(typeof(Single[]))) return ReadSingle(regName, Address, Count);
            }
            if (typeof(T).Equals(typeof(string))) return ReadString(regName, Address, Count);
            return null;
        }

        public bool WriteData<T>(string regName, int Address, object values)
        {
            if (values is Array)
            {
                if (typeof(T).Equals(typeof(bool[]))) return WriteBool(regName, Address, (bool[])Convert.ChangeType(values, typeof(T)));
                if (typeof(T).Equals(typeof(Int16[]))) return WriteInt16(regName, Address, (Int16[])Convert.ChangeType(values, typeof(T)));
                if (typeof(T).Equals(typeof(UInt16[]))) return WriteUInt16(regName, Address, (UInt16[])Convert.ChangeType(values, typeof(T)));
                if (typeof(T).Equals(typeof(Int32[]))) return WriteInt32(regName, Address, (Int32[])Convert.ChangeType(values, typeof(T)));
                if (typeof(T).Equals(typeof(UInt32[]))) return WriteUInt32(regName, Address, (UInt32[])Convert.ChangeType(values, typeof(T)));
                if (typeof(T).Equals(typeof(Single[]))) return WriteSingle(regName, Address, (Single[])Convert.ChangeType(values, typeof(T)));
            }
            else
            {
                if (typeof(T).Equals(typeof(bool))) return WriteBool(regName, Address, (bool)Convert.ChangeType(values, typeof(T)));
                if (typeof(T).Equals(typeof(Int16))) return WriteInt16(regName, Address, (Int16)Convert.ChangeType(values, typeof(T)));
                if (typeof(T).Equals(typeof(UInt16))) return WriteUInt16(regName, Address, (UInt16)Convert.ChangeType(values, typeof(T)));
                if (typeof(T).Equals(typeof(Int32))) return WriteInt32(regName, Address, (Int32)Convert.ChangeType(values, typeof(T)));
                if (typeof(T).Equals(typeof(UInt32))) return WriteUInt32(regName, Address, (UInt32)Convert.ChangeType(values, typeof(T)));
                if (typeof(T).Equals(typeof(Single))) return WriteSingle(regName, Address, (Single)Convert.ChangeType(values, typeof(T)));
            }
            if (typeof(T).Equals(typeof(string))) return WriteString(regName, Address, (string)Convert.ChangeType(values, typeof(T)));

            return false;
        }



        //重载的布尔读写
        public bool ReadBool(string regName, int Address)
        {
            return ReadBool(regName, Address, 1).First();
        }

        public bool WriteBool(string regName, int Address, bool value)
        {
            return WriteBool(regName, Address, new bool[] { value });
        }

        //重载的Int16读写
        public Int16 ReadInt16(string regName, int Address)
        {
            return ReadInt16(regName, Address, 1).First();
        }

        public bool WriteInt16(string regName, int Address, Int16 value)
        {
            return WriteInt16(regName, Address, new Int16[] { value });
        }

        public UInt16 ReadUInt16(string regName, int Address)
        {
            return ReadUInt16(regName, Address, 1).First();
        }

        public bool WriteUInt16(string regName, int Address, UInt16 value)
        {
            return WriteUInt16(regName, Address, new UInt16[] { value });
        }

        //重载的Int32读写
        public Int32 ReadInt32(string regName, int Address)
        {
            return ReadInt32(regName, Address, 1).First();
        }

        public bool WriteInt32(string regName, int Address, Int32 value)
        {
            return WriteInt32(regName, Address, new Int32[] { value });
        }

        public UInt32 ReadUInt32(string regName, int Address)
        {
            return ReadUInt32(regName, Address, 1).First();
        }

        public bool WriteUInt32(string regName, int Address, UInt32 value)
        {
            return WriteUInt32(regName, Address, new UInt32[] { value });
        }

        //重载的Single读写
        public Single ReadSingle(string regName, int Address)
        {
            return ReadSingle(regName, Address, 1).First();
        }

        public bool WriteSingle(string regName, int Address, Single value)
        {
            return WriteSingle(regName, Address, new Single[] { value });
        }

        //抽象类的抽象方法必须由子类实现，虚方法可以在只有需要的时候才实现。
        //bool读写
        public virtual bool[] ReadBool(string regName, int Address, int Count) { throw new Exception("此方法需由子类实现！"); }
        public virtual bool WriteBool(string regName, int Address, bool[] values) { throw new Exception("此方法需由子类实现！"); }

        //16位读写
        public virtual Int16[] ReadInt16(string regName, int Address, int Count) { throw new Exception("此方法需由子类实现！"); }
        public virtual bool WriteInt16(string regName, int Address, Int16[] values) { throw new Exception("此方法需由子类实现！"); }
        public virtual UInt16[] ReadUInt16(string regName, int Address, int Count) { throw new Exception("此方法需由子类实现！"); }
        public virtual bool WriteUInt16(string regName, int Address, UInt16[] values) { throw new Exception("此方法需由子类实现！"); }

        //32位读写
        public virtual Int32[] ReadInt32(string regName, int Address, int Count) { throw new Exception("此方法需由子类实现！"); }
        public virtual bool WriteInt32(string regName, int Address, Int32[] values) { throw new Exception("此方法需由子类实现！"); }
        public virtual UInt32[] ReadUInt32(string regName, int Address, int Count) { throw new Exception("此方法需由子类实现！"); }
        public virtual bool WriteUInt32(string regName, int Address, UInt32[] values) { throw new Exception("此方法需由子类实现！"); }
        public virtual Single[] ReadSingle(string regName, int Address, int Count) { throw new Exception("此方法需由子类实现！"); }
        public virtual bool WriteSingle(string regName, int Address, Single[] values) { throw new Exception("此方法需由子类实现！"); }
        public virtual string ReadString(string regName, int Address, int Count) { throw new Exception("此方法需由子类实现！"); }
        public virtual bool WriteString(string regName, int Address, string values) { throw new Exception("此方法需由子类实现！"); }
    }
}
