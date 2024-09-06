using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ConsoleDump;
using Protocols.Protocols;
using Protocols.Omron;
namespace Protocols
{
    internal class TestProtocol
    {
        static void Main(string[] args)
        {
            //测试三菱MC-3E
            //TestProtocolBase(new MC_3E("127.0.0.1", 6000), "M", 100, "D", 100);

            //测试松下Mewtocol 
            //TestProtocolBase(new Mewtocol(new CommSerialPort("COM1", 9600, 8, Parity.None, StopBits.One)),"R",100,"D",100);

            //测试欧姆龙FINS
            //TestProtocolBase(new HostLink_Serial(new CommSerialPort("COM1", 9600, 7, Parity.Even, StopBits.One)),"CIO",10000,"D",100);
            TestProtocolBase(new Fins(new CommTCP("127.0.0.1", 9600)),"CIO",10000,"D",100);

            //TestModbus(new ASCII(new CommSerialPort("COM1", 9600, 8, Parity.None, StopBits.One)));
            //TestModbus(new RTU(new CommSerialPort("COM1", 9600, 8, Parity.None, StopBits.One)));
            //TestModbus(new TCP(new CommNet("127.0.0.1", 502)));

        }

        static void TestModbus(ModbusBase m)
        {

            "".Dump("读写单个寄存器"); 
            m.WriteSingleCoil(1, 100, true).Dump();//只能写线圈
            m.ReadCoils(1, 100, 1).Dump();//此处也只能读线圈，读离散输入无意义 
            m.WriteSingleCoil(1, 100, false).Dump();//只能写线圈
            m.ReadCoils(1, 100, 1).Dump();//此处也只能读线圈，读离散输入无意义

            m.WriteSingleCoil(1, 100, true);
             
            m.WriteMultipleRegisters<Int16>(1, (Int16)100, new Int16[] { 1234 }).Dump(); 
            m.ReadHoldingRegisters<Int16>(1, (Int16)100, 1).Dump();
             
            m.WriteMultipleRegisters<UInt16>(1, (Int16)100, new UInt16[] { 1234 }).Dump(); 
            m.ReadHoldingRegisters<UInt16>(1, (Int16)100, 1).Dump();
             
            m.WriteMultipleRegisters<Int32>(1, (Int16)100, new Int32[] { 1234 }).Dump(); 
            m.ReadHoldingRegisters<Int32>(1, (Int16)100, 1).Dump();
             
            m.WriteMultipleRegisters<UInt32>(1, (Int16)100, new UInt32[] { 1234 }).Dump(); 
            m.ReadHoldingRegisters<UInt32>(1, (Int16)100, 1).Dump();
             
            m.WriteMultipleRegisters<Single>(1, (Int16)100, new Single[] { 3.141592f }).Dump(); 
            m.ReadHoldingRegisters<Single>(1, (Int16)100, 1).Dump();


            "读写多个寄存器".Dump("读写多个寄存器"); 
            m.WriteMultipleCoils(1, 100, new bool[] { true, false, true, false, true, true, false, true, false, true, true, false, true, false, true }).Dump(); 
            m.ReadCoils(1, 100, 20).Dump();
             
            m.WriteMultipleRegisters<Int16>(1, (Int16)100, new Int16[] { 1234, 1234, 1234, 1234, 1234 }).Dump(); 
            m.ReadHoldingRegisters<Int16>(1, (Int16)100, 5).Dump();
             
            m.WriteMultipleRegisters<UInt16>(1, (Int16)100, new UInt16[] { 1234, 1234, 1234, 1234, 1234 }).Dump(); 
            m.ReadHoldingRegisters<UInt16>(1, (Int16)100, 5).Dump();

 
            m.WriteMultipleRegisters<Int32>(1, (Int16)100, new Int32[] { 1234, 1234, 1234, 1234, 1234 }).Dump(); 
            m.ReadHoldingRegisters<Int32>(1, (Int16)100, 5).Dump();
             
            m.WriteMultipleRegisters<UInt32>(1, (Int16)100, new UInt32[] { 1234, 1234, 1234, 1234, 1234 }).Dump(); 
            m.ReadHoldingRegisters<UInt32>(1, (Int16)100, 5).Dump();

             
            m.WriteMultipleRegisters<Single>(1, (Int16)100, new Single[] { 3.141592f, 3.141592f, 3.141592f, 3.141592f, 3.141592f }).Dump(); 
            m.ReadHoldingRegisters<Single>(1, (Int16)100, 5).Dump();


            "字符串读写".Dump("字符串读写");
            m.WriteMultipleRegisters<string>(1, 100, new string[] { "abcdefg" }).Dump();
            m.ReadHoldingRegisters<string>(1, 100, 10).Dump();

            "Done.".Dump();
            Console.ReadLine();
        }

        static void TestProtocolBase(ProtocolBase m,string bitRegName,int bitAddress,string dataRegName,int dataAddress)
        {
            //方法测试
            try
            { 
                //MC_3E mc = new MC_3E("COM1",9600,8,Parity.None,StopBits.One);

                //数组拼接方式-待完善
                //MC_3E2 mc = new MC_3E2("127.0.0.1", 6000);
                //MC_3E mc = new MC_3E("COM1", 9600, 8, Parity.None, StopBits.One);

                Console.WriteLine("基本方法测试：");
                Console.WriteLine("读写单个元件");
                m.WriteBool(bitRegName, bitAddress, true).Dump("写布尔值：");
                m.ReadBool(bitRegName, bitAddress).Dump("读布尔值：");
                 
                m.WriteInt16(dataRegName, dataAddress, 1234).Dump("写有符号字：");
                m.ReadInt16(dataRegName, dataAddress).Dump("读符号字：");
                 
                m.WriteUInt16(dataRegName, dataAddress, 1234).Dump("写无符号字：");
                m.ReadUInt16(dataRegName, dataAddress).Dump("读无符号字：");
                 
                m.WriteInt32(dataRegName, dataAddress, 1234567).Dump("写有符号双字：");
                m.ReadInt32(dataRegName, dataAddress).Dump("读有符号双字：");
                 
                m.WriteUInt32(dataRegName, dataAddress, 1234567).Dump("写无符号双字：");
                m.ReadUInt32(dataRegName, dataAddress).Dump("读无符号双字：");
                 
                m.WriteSingle(dataRegName, dataAddress, 3.141592653f).Dump("写浮点数：");
                m.ReadSingle(dataRegName, dataAddress).Dump("读浮点数：");


                Console.WriteLine("读写多个元件"); 
                m.WriteBool(bitRegName, bitAddress, new bool[] { true, false, true, false, true }).Dump("写5个布尔值：");
                m.ReadBool(bitRegName, bitAddress, 5).Dump("读5个布尔值：");
                 
                m.WriteInt16(dataRegName, dataAddress, new Int16[] { 1, 2, 3, 4, 5 }).Dump("写5个有符号字：");
                m.ReadInt16(dataRegName, dataAddress, 5).Dump("读5个符号字：");
                 
                m.WriteUInt16(dataRegName, dataAddress, new UInt16[] { 1, 2, 3, 4, 5 }).Dump("写5个无符号字：");
                m.ReadUInt16(dataRegName, dataAddress, 5).Dump("读5个无符号字：");
                 
                m.WriteInt32(dataRegName, dataAddress, new Int32[] { 12, 23, 34, 45, 56 }).Dump("写5个有符号双字：");
                m.ReadInt32(dataRegName, dataAddress, 5).Dump("读5个有符号双字：");
                 
                m.WriteUInt32(dataRegName, dataAddress, new UInt32[] { 11, 22, 33, 44, 55 }).Dump("写5个无符号双字：");
                m.ReadUInt32(dataRegName, dataAddress, 5).Dump("读5个无符号双字：");
                 
                m.WriteSingle(dataRegName, dataAddress, new Single[] { 1.1f, 2.2f, 3.3f, 4.4f, 5.5f }).Dump("写5个浮点数：");
                m.ReadSingle(dataRegName, dataAddress, 5).Dump("读5个浮点数：");
                 
                m.WriteString(dataRegName, dataAddress, "abcdefghij").Dump("写字符串：");
                m.ReadString(dataRegName, dataAddress, 10).Dump("读字符串：");


                Console.WriteLine("泛型方法测试");
                Console.WriteLine("读写单个元件");
                
                m.WriteData<bool>(bitRegName, bitAddress, (object)true).Dump("泛型写布尔值");
                m.ReadData<bool>(bitRegName, bitAddress).Dump("泛型读布尔值");
                 
                m.WriteData<Int16>(dataRegName, dataAddress, (object)1234).Dump("泛型写INT16");
                m.ReadData<Int16>(dataRegName, dataAddress).Dump("泛型读INT16");
                 
                m.WriteData<UInt16>(dataRegName, dataAddress, (object)1234).Dump("泛型写UINT16");
                m.ReadData<UInt16>(dataRegName, dataAddress).Dump("泛型读UINT16");
                 
                m.WriteData<Int32>(dataRegName, dataAddress, (object)12345678).Dump("泛型写INT32");
                m.ReadData<Int32>(dataRegName, dataAddress).Dump("泛型读INT16");
                 
                m.WriteData<UInt32>(dataRegName, dataAddress, (object)12345678).Dump("泛型写UINT32");
                m.ReadData<UInt32>(dataRegName, dataAddress).Dump("泛型读UINT16");
                 
                m.WriteData<Single>(dataRegName, dataAddress, (object)1.2345678).Dump("泛型写INT32");
                m.ReadData<Single>(dataRegName, dataAddress).Dump("泛型读INT16");
                 
                m.WriteData<string>(dataRegName, dataAddress, (object)"kkkkkkkkkkk").Dump("泛型写string");
                m.ReadData<string>(dataRegName, dataAddress, 10).Dump("泛型读string");


                //读写多个元件
                Console.WriteLine("读写多个元件"); 
                m.WriteData<bool[]>(bitRegName, bitAddress, (object)new bool[] { true, false, true, false, true }).Dump("泛型写5布尔值");
                m.ReadData<bool[]>(bitRegName, bitAddress, 5).Dump("泛型读5布尔值");
                 
                m.WriteData<Int16[]>(dataRegName, dataAddress, (object)new Int16[] { 1, 2, 3, 4, 5 }).Dump("泛型写5INT16");
                m.ReadData<Int16[]>(dataRegName, dataAddress, 5).Dump("泛型读5INT16");
                 
                m.WriteData<UInt16[]>(dataRegName, dataAddress, (object)new UInt16[] { 1, 2, 3, 4, 5 }).Dump("泛型写5UINT16");
                m.ReadData<UInt16[]>(dataRegName, dataAddress, 5).Dump("泛型读5UINT16");
                 
                m.WriteData<Int32[]>(dataRegName, dataAddress, (object)new Int32[] { 11, 22, 33, 44, 55 }).Dump("泛型写5INT32");
                m.ReadData<Int32[]>(dataRegName, dataAddress, 5).Dump("泛型读5INT16");
                 
                m.WriteData<UInt32[]>(dataRegName, dataAddress, (object)new UInt32[] { 12, 34, 56, 78, 90 }).Dump("泛型写5UINT32");
                m.ReadData<UInt32[]>(dataRegName, dataAddress, 5).Dump("泛型读5UINT16");
                 
                m.WriteData<Single[]>(dataRegName, dataAddress, (object)new Single[] { 1.1f, 2.2f, 3.3f, 4.4f, 5.5f }).Dump("泛型写5INT32");
                m.ReadData<Single[]>(dataRegName, dataAddress, 5).Dump("泛型读5INT16");


                //泛型异步方法测试-------------------------------------------------------------------------------------
                //读写单个元件
                Console.WriteLine("泛型异步方法测试--------------------------------------------------------------------");
                Console.WriteLine("读写单个元件"); 
                m.WriteDataAsync<bool>(bitRegName, bitAddress, (object)true).Result.Dump("泛型写布尔值");
                m.ReadDataAsync<bool>(bitRegName, bitAddress).Result.Dump("泛型读布尔值");
                 
                m.WriteDataAsync<Int16>(dataRegName, dataAddress, (object)1234).Result.Dump("泛型写INT16");
                m.ReadDataAsync<Int16>(dataRegName, dataAddress).Result.Dump("泛型读INT16");
                 
                m.WriteDataAsync<UInt16>(dataRegName, dataAddress, (object)1234).Result.Dump("泛型写UINT16");
                m.ReadDataAsync<UInt16>(dataRegName, dataAddress).Result.Dump("泛型读UINT16");
                 
                m.WriteDataAsync<Int32>(dataRegName, dataAddress, (object)12345678).Result.Dump("泛型写INT32");
                m.ReadDataAsync<Int32>(dataRegName, dataAddress).Result.Dump("泛型读INT16");
                 
                m.WriteDataAsync<UInt32>(dataRegName, dataAddress, (object)12345678).Result.Dump("泛型写UINT32");
                m.ReadDataAsync<UInt32>(dataRegName, dataAddress).Result.Dump("泛型读UINT16");
                 
                m.WriteDataAsync<Single>(dataRegName, dataAddress, (object)1.2345678).Result.Dump("泛型写INT32");
                m.ReadDataAsync<Single>(dataRegName, dataAddress).Result.Dump("泛型读INT16");
                 
                m.WriteDataAsync<string>(dataRegName, dataAddress, (object)"kkkkkkkkkkk").Result.Dump("泛型写string");
                m.ReadDataAsync<string>(dataRegName, dataAddress, 10).Result.Dump("泛型读string");

                 
                Console.WriteLine("读写多个元件"); 
                m.WriteDataAsync<bool[]>(bitRegName, bitAddress, (object)new bool[] { true, false, true, false, true }).Result.Dump("泛型写5布尔值");
                m.ReadDataAsync<bool[]>(bitRegName, bitAddress, 5).Result.Dump("泛型读5布尔值");
                 
                m.WriteDataAsync<Int16[]>(dataRegName, dataAddress, (object)new Int16[] { 1, 2, 3, 4, 5 }).Result.Dump("泛型写5INT16");
                m.ReadDataAsync<Int16[]>(dataRegName, dataAddress, 5).Result.Dump("泛型读5INT16");
                 
                m.WriteDataAsync<UInt16[]>(dataRegName, dataAddress, (object)new UInt16[] { 1, 2, 3, 4, 5 }).Result.Dump("泛型写5UINT16");
                m.ReadDataAsync<UInt16[]>(dataRegName, dataAddress, 5).Result.Dump("泛型读5UINT16");
                 
                m.WriteDataAsync<Int32[]>(dataRegName, dataAddress, (object)new Int32[] { 11, 22, 33, 44, 55 }).Result.Dump("泛型写5INT32");
                m.ReadDataAsync<Int32[]>(dataRegName, dataAddress, 5).Result.Dump("泛型读5INT16");
                 
                m.WriteDataAsync<UInt32[]>(dataRegName, dataAddress, (object)new UInt32[] { 12, 34, 56, 78, 90 }).Result.Dump("泛型写5UINT32");
                m.ReadDataAsync<UInt32[]>(dataRegName, dataAddress, 5).Result.Dump("泛型读5UINT16");
                 
                m.WriteDataAsync<Single[]>(dataRegName, dataAddress, (object)new Single[] { 1.1f, 2.2f, 3.3f, 4.4f, 5.5f }).Result.Dump("泛型写5INT32");
                m.ReadDataAsync<Single[]>(dataRegName, dataAddress, 5).Result.Dump("泛型读5INT16");

                Console.WriteLine("Done.");

                Console.Read();

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }        
    }
}
