# Protocols-工业设备通信协议库<br>
一个为爱发电项目，正在逐步完善中   
在VS2022+.Net Framework4.8 + HslCommunication 本地测试通过（只测试了M,D寄存器）<br> 
目前基本功能能用但并没有达到理想的程度，且性能并没有优化到最佳的状态，修复了部分已知bug，欢迎反馈bug（QQ:706806660）<br>
感谢以下好友提供的帮助：<br>
拓拓(https://github.com/kongdetuo)<br>
SlimeNull(https://github.com/SlimeNull)<br>
Steve(https://github.com/steveworkshop)<br>
林德熙(https://github.com/lindexi)<br>
雾雨氏Bot<br>
仙女之萌(https://github.com/ilyfairy)<br>
Shompinice(https://github.com/MicaApps)<br>
Wang(https://github.com/2236721325)<br>
<br>
20240819：增加ModbusRTU。<br>
20240820：增加ModbusASCII，ModbusTCP，增加松下Mewtocol（写多个位时还有bug未修复）！<br>
20240828：调整了结构，并增加欧姆龙HostLink_Serial方式（使用头编码FA），其余头编码和Fins方式待后续添加<br> 
20240901：增加了欧姆龙FinsTCP方式<br>
20240916：增加了UDP方式<br>
20241008：增加了帧头校验，解决沾包的问题，性能会有一定程度下降<br>
```
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
using System.Xml.Linq;
using Protocols;
using System.Diagnostics;

namespace TestProtocols
{
    internal class TestProtocol
    {
        static void Main(string[] args)
        {
            ////测试三菱MC-3E
            //TestProtocolBase(new MC_3E("127.0.0.1", 6000), "M", 100, "D", 100);
            //TestProtocolBase(new MC_3E2("127.0.0.1", 6000), "M", 100, "D", 100);

            ////测试松下Mewtocol 
            //TestProtocolBase(new Mewtocol("COM1", 9600, 8, Parity.None, StopBits.One),"R",0x100,"D",100);

            ////测试欧姆龙FINS
            ////使用HostLinkServer
            //TestProtocolBase(new HostLink_Serial("COM1", 9600, 7, Parity.Even, StopBits.One),"CIO",10000,"D",100);
            //TestProtocolBase(new Fins("127.0.0.1", 9600),"CIO",10000,"D",100);//使用 Fins Virtual Server
            //TestProtocolBase(new Fins("127.0.0.1", 9600), "CIO", 10000, "D", 100);//帧格式不对

            ///测试Modbus
            //TestModbus(new Modbus_ASCII("COM1", 9600, 8, Parity.None, StopBits.One));
            //TestModbus(new Modbus_RTU("COM1", 9600, 8, Parity.None, StopBits.One));
            //TestModbus(new Modbus_TCP("127.0.0.1", 502));
            //TestModbus(new Modbus_TCP("127.0.0.1", 502));

            Analyse(new MC_3E2("127.0.0.1", 6000),"D",100, 950);
            Console.Read();
        }

        static void Analyse(ProtocolBase m,string regName,int address,int count)
        {
            var sw2 = new Stopwatch();
            sw2.Start();
            for (int i=0;i<1000;i++)
            {
                var sw1 = new Stopwatch();
                sw1.Start();
                m.WriteInt16(regName,address,(Int16)i);
                var res= m.ReadInt16(regName,address,count);
                sw1.Stop();
                Console.WriteLine($"{regName}:{res.First()},Elapsed:{sw1.ElapsedMilliseconds}ms");
            }
            sw2.Stop();
            Console.WriteLine($"总耗时：{sw2.ElapsedMilliseconds}ms.");
        }


        static void TestModbus(AModbus m)
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
            m.WriteMultipleCoils(1, 100, new bool[] { true, false, true, false, true, true, false, true, false, true, true, false, true, false, true, false }).Dump(); 
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
                m.WriteBool(bitRegName, bitAddress, new bool[] { true, false, true, false, true, true, false, true, false, true, true, false, true, false, true, false }).Dump("写5个布尔值：");
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
                m.WriteData<bool[]>(bitRegName, bitAddress, (object)new bool[] { true, false, true, false, true, true, false, true, false, true, true, false, true, false, true, false }).Dump("泛型写5布尔值");
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
                m.WriteDataAsync<bool[]>(bitRegName, bitAddress, (object)new bool[] { true, false, true, false, true, true, false, true, false, true, true, false, true, false, true, false }).Result.Dump("泛型写5布尔值");
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

```