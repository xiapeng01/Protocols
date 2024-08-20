using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ConsoleDump;
using Protocols.Protocols;

namespace Protocols
{
    internal class TestProtocol
    {
        static void Main(string[] args)
        {
            ModbusBase m;
            m = new ASCII(new CommSerialPort("COM1", 9600, 8, Parity.None, StopBits.One)); 
            m = new RTU(new CommSerialPort("COM1", 9600, 8, Parity.None, StopBits.One)); 
            m = new TCP(new CommNet("127.0.0.1", 502));

            TestModbus(m);
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

        static void TestMC()
        {
            //方法测试
            try
            {
                //字符串拼接方式
                MC_3E mc = new MC_3E("127.0.0.1", 6000);
                //MC_3E mc = new MC_3E("COM1",9600,8,Parity.None,StopBits.One);

                //数组拼接方式-待完善
                //MC_3E2 mc = new MC_3E2("127.0.0.1", 6000);
                //MC_3E mc = new MC_3E("COM1", 9600, 8, Parity.None, StopBits.One);

                Console.WriteLine("基本方法测试：");

                Console.WriteLine("读写单个元件");
                Console.WriteLine("位元件：");
                mc.WriteBool("M", 100, true).Dump("写布尔值：");
                mc.ReadBool("M", 100).Dump("读布尔值：");

                Console.WriteLine("有符号16位字元件：");
                mc.WriteInt16("D", 100, 1234).Dump("写有符号字：");
                mc.ReadInt16("D", 100).Dump("读符号字：");

                Console.WriteLine("无符号16位字元件：");
                mc.WriteUInt16("D", 100, 1234).Dump("写无符号字：");
                mc.ReadUInt16("D", 100).Dump("读无符号字：");

                Console.WriteLine("有符号32位字元件：");
                mc.WriteInt32("D", 100, 1234567).Dump("写有符号双字：");
                mc.ReadInt32("D", 100).Dump("读有符号双字：");

                Console.WriteLine("无符号32位字元件：");
                mc.WriteUInt32("D", 100, 1234567).Dump("写无符号双字：");
                mc.ReadUInt32("D", 100).Dump("读无符号双字：");

                Console.WriteLine("符号浮点数：");
                mc.WriteSingle("D", 100, 3.141592653f).Dump("写浮点数：");
                mc.ReadSingle("D", 100).Dump("读浮点数：");


                Console.WriteLine("读写多个元件");
                Console.WriteLine("位元件：");
                mc.WriteBool("M", 100, new bool[] { true, false, true, false, true }).Dump("写5个布尔值：");
                mc.ReadBool("M", 100, 5).Dump("读5个布尔值：");

                Console.WriteLine("有符号16位字元件：");
                mc.WriteInt16("D", 100, new Int16[] { 1, 2, 3, 4, 5 }).Dump("写5个有符号字：");
                mc.ReadInt16("D", 100, 5).Dump("读5个符号字：");

                Console.WriteLine("无符号16位字元件：");
                mc.WriteUInt16("D", 100, new UInt16[] { 1, 2, 3, 4, 5 }).Dump("写5个无符号字：");
                mc.ReadUInt16("D", 100, 5).Dump("读5个无符号字：");

                Console.WriteLine("有符号32位字元件：");
                mc.WriteInt32("D", 100, new Int32[] { 12, 23, 34, 45, 56 }).Dump("写5个有符号双字：");
                mc.ReadInt32("D", 100, 5).Dump("读5个有符号双字：");

                Console.WriteLine("无符号32位字元件：");
                mc.WriteUInt32("D", 100, new UInt32[] { 11, 22, 33, 44, 55 }).Dump("写5个无符号双字：");
                mc.ReadUInt32("D", 100, 5).Dump("读5个无符号双字：");

                Console.WriteLine("符号浮点数：");
                mc.WriteSingle("D", 100, new Single[] { 1.1f, 2.2f, 3.3f, 4.4f, 5.5f }).Dump("写5个浮点数：");
                mc.ReadSingle("D", 100, 5).Dump("读5个浮点数：");

                Console.WriteLine("读写字符串：");
                mc.WriteString("D", 100, "abcdefghij").Dump("写字符串：");
                mc.ReadString("D", 100, 10).Dump("读字符串：");


                //泛型方法测试
                //读写单个元件
                Console.WriteLine("读写单个元件");
                Console.WriteLine("泛型布尔值测试");
                mc.WriteData<bool>("M", 100, (object)true).Dump("泛型写布尔值");
                mc.ReadData<bool>("M", 100).Dump("泛型读布尔值");

                Console.WriteLine("泛型INT16测试");
                mc.WriteData<Int16>("D", 100, (object)1234).Dump("泛型写INT16");
                mc.ReadData<Int16>("D", 100).Dump("泛型读INT16");

                Console.WriteLine("泛型UINT16测试");
                mc.WriteData<UInt16>("D", 100, (object)1234).Dump("泛型写UINT16");
                mc.ReadData<UInt16>("D", 100).Dump("泛型读UINT16");

                Console.WriteLine("泛型INT32测试");
                mc.WriteData<Int32>("D", 100, (object)12345678).Dump("泛型写INT32");
                mc.ReadData<Int32>("D", 100).Dump("泛型读INT16");

                Console.WriteLine("泛型UINT32测试");
                mc.WriteData<UInt32>("D", 100, (object)12345678).Dump("泛型写UINT32");
                mc.ReadData<UInt32>("D", 100).Dump("泛型读UINT16");

                Console.WriteLine("泛型FLOAT测试");
                mc.WriteData<Single>("D", 100, (object)1.2345678).Dump("泛型写INT32");
                mc.ReadData<Single>("D", 100).Dump("泛型读INT16");

                Console.WriteLine("泛型String测试");
                mc.WriteData<string>("D", 100, (object)"kkkkkkkkkkk").Dump("泛型写string");
                mc.ReadData<string>("D", 100, 10).Dump("泛型读string");


                //读写多个元件
                Console.WriteLine("读写多个元件");
                Console.WriteLine("泛型布尔值测试");
                mc.WriteData<bool[]>("M", 100, (object)new bool[] { true, false, true, false, true }).Dump("泛型写5布尔值");
                mc.ReadData<bool[]>("M", 100, 5).Dump("泛型读5布尔值");

                Console.WriteLine("泛型INT16测试");
                mc.WriteData<Int16[]>("D", 100, (object)new Int16[] { 1, 2, 3, 4, 5 }).Dump("泛型写5INT16");
                mc.ReadData<Int16[]>("D", 100, 5).Dump("泛型读5INT16");

                Console.WriteLine("泛型UINT16测试");
                mc.WriteData<UInt16[]>("D", 100, (object)new UInt16[] { 1, 2, 3, 4, 5 }).Dump("泛型写5UINT16");
                mc.ReadData<UInt16[]>("D", 100, 5).Dump("泛型读5UINT16");

                Console.WriteLine("泛型INT32测试");
                mc.WriteData<Int32[]>("D", 100, (object)new Int32[] { 11, 22, 33, 44, 55 }).Dump("泛型写5INT32");
                mc.ReadData<Int32[]>("D", 100, 5).Dump("泛型读5INT16");

                Console.WriteLine("泛型UINT32测试");
                mc.WriteData<UInt32[]>("D", 100, (object)new UInt32[] { 12, 34, 56, 78, 90 }).Dump("泛型写5UINT32");
                mc.ReadData<UInt32[]>("D", 100, 5).Dump("泛型读5UINT16");

                Console.WriteLine("泛型FLOAT测试");
                mc.WriteData<Single[]>("D", 100, (object)new Single[] { 1.1f, 2.2f, 3.3f, 4.4f, 5.5f }).Dump("泛型写5INT32");
                mc.ReadData<Single[]>("D", 100, 5).Dump("泛型读5INT16");


                //泛型异步方法测试-------------------------------------------------------------------------------------
                //读写单个元件
                Console.WriteLine("泛型异步方法测试--------------------------------------------------------------------");
                Console.WriteLine("读写单个元件");
                Console.WriteLine("泛型布尔值测试");
                mc.WriteDataAsync<bool>("M", 100, (object)true).Result.Dump("泛型写布尔值");
                mc.ReadDataAsync<bool>("M", 100).Result.Dump("泛型读布尔值");

                Console.WriteLine("泛型INT16测试");
                mc.WriteDataAsync<Int16>("D", 100, (object)1234).Result.Dump("泛型写INT16");
                mc.ReadDataAsync<Int16>("D", 100).Result.Dump("泛型读INT16");

                Console.WriteLine("泛型UINT16测试");
                mc.WriteDataAsync<UInt16>("D", 100, (object)1234).Result.Dump("泛型写UINT16");
                mc.ReadDataAsync<UInt16>("D", 100).Result.Dump("泛型读UINT16");

                Console.WriteLine("泛型INT32测试");
                mc.WriteDataAsync<Int32>("D", 100, (object)12345678).Result.Dump("泛型写INT32");
                mc.ReadDataAsync<Int32>("D", 100).Result.Dump("泛型读INT16");

                Console.WriteLine("泛型UINT32测试");
                mc.WriteDataAsync<UInt32>("D", 100, (object)12345678).Result.Dump("泛型写UINT32");
                mc.ReadDataAsync<UInt32>("D", 100).Result.Dump("泛型读UINT16");

                Console.WriteLine("泛型FLOAT测试");
                mc.WriteDataAsync<Single>("D", 100, (object)1.2345678).Result.Dump("泛型写INT32");
                mc.ReadDataAsync<Single>("D", 100).Result.Dump("泛型读INT16");

                Console.WriteLine("泛型String测试");
                mc.WriteDataAsync<string>("D", 100, (object)"kkkkkkkkkkk").Result.Dump("泛型写string");
                mc.ReadDataAsync<string>("D", 100, 10).Result.Dump("泛型读string");


                //读写多个元件
                Console.WriteLine("读写多个元件");
                Console.WriteLine("泛型布尔值测试");
                mc.WriteDataAsync<bool[]>("M", 100, (object)new bool[] { true, false, true, false, true }).Result.Dump("泛型写5布尔值");
                mc.ReadDataAsync<bool[]>("M", 100, 5).Result.Dump("泛型读5布尔值");

                Console.WriteLine("泛型INT16测试");
                mc.WriteDataAsync<Int16[]>("D", 100, (object)new Int16[] { 1, 2, 3, 4, 5 }).Result.Dump("泛型写5INT16");
                mc.ReadDataAsync<Int16[]>("D", 100, 5).Result.Dump("泛型读5INT16");

                Console.WriteLine("泛型UINT16测试");
                mc.WriteDataAsync<UInt16[]>("D", 100, (object)new UInt16[] { 1, 2, 3, 4, 5 }).Result.Dump("泛型写5UINT16");
                mc.ReadDataAsync<UInt16[]>("D", 100, 5).Result.Dump("泛型读5UINT16");

                Console.WriteLine("泛型INT32测试");
                mc.WriteDataAsync<Int32[]>("D", 100, (object)new Int32[] { 11, 22, 33, 44, 55 }).Result.Dump("泛型写5INT32");
                mc.ReadDataAsync<Int32[]>("D", 100, 5).Result.Dump("泛型读5INT16");

                Console.WriteLine("泛型UINT32测试");
                mc.WriteDataAsync<UInt32[]>("D", 100, (object)new UInt32[] { 12, 34, 56, 78, 90 }).Result.Dump("泛型写5UINT32");
                mc.ReadDataAsync<UInt32[]>("D", 100, 5).Result.Dump("泛型读5UINT16");

                Console.WriteLine("泛型FLOAT测试");
                mc.WriteDataAsync<Single[]>("D", 100, (object)new Single[] { 1.1f, 2.2f, 3.3f, 4.4f, 5.5f }).Result.Dump("泛型写5INT32");
                mc.ReadDataAsync<Single[]>("D", 100, 5).Result.Dump("泛型读5INT16");

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
