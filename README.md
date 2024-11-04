# Protocols-��ҵ�豸ͨ��Э���<br>
һ��Ϊ��������Ŀ��������������   
��VS2022+.Net Framework4.8 + HslCommunication ���ز���ͨ����ֻ������M,D�Ĵ�����<br> 
Ŀǰ�����������õ���û�дﵽ����ĳ̶ȣ������ܲ�û���Ż�����ѵ�״̬���޸��˲�����֪bug����ӭ����bug��QQ:706806660��<br>
��л���º����ṩ�İ�����<br>
����(https://github.com/kongdetuo)<br>
SlimeNull(https://github.com/SlimeNull)<br>
Steve(https://github.com/steveworkshop)<br>
�ֵ���(https://github.com/lindexi)<br>
������Bot<br>
��Ů֮��(https://github.com/ilyfairy)<br>
Shompinice(https://github.com/MicaApps)<br>
Wang(https://github.com/2236721325)<br>
<br>
20240819������ModbusRTU��<br>
20240820������ModbusASCII��ModbusTCP����������Mewtocol��д���λʱ����bugδ�޸�����<br>
20240828�������˽ṹ��������ŷķ��HostLink_Serial��ʽ��ʹ��ͷ����FA��������ͷ�����Fins��ʽ���������<br> 
20240901��������ŷķ��FinsTCP��ʽ<br>
20240916��������UDP��ʽ<br>
20241008��������֡ͷУ�飬���մ�������⣬���ܻ���һ���̶��½�<br>
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
            ////��������MC-3E
            //TestProtocolBase(new MC_3E("127.0.0.1", 6000), "M", 100, "D", 100);
            //TestProtocolBase(new MC_3E2("127.0.0.1", 6000), "M", 100, "D", 100);

            ////��������Mewtocol 
            //TestProtocolBase(new Mewtocol("COM1", 9600, 8, Parity.None, StopBits.One),"R",0x100,"D",100);

            ////����ŷķ��FINS
            ////ʹ��HostLinkServer
            //TestProtocolBase(new HostLink_Serial("COM1", 9600, 7, Parity.Even, StopBits.One),"CIO",10000,"D",100);
            //TestProtocolBase(new Fins("127.0.0.1", 9600),"CIO",10000,"D",100);//ʹ�� Fins Virtual Server
            //TestProtocolBase(new Fins("127.0.0.1", 9600), "CIO", 10000, "D", 100);//֡��ʽ����

            ///����Modbus
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
            Console.WriteLine($"�ܺ�ʱ��{sw2.ElapsedMilliseconds}ms.");
        }


        static void TestModbus(AModbus m)
        {

            "".Dump("��д�����Ĵ���"); 
            m.WriteSingleCoil(1, 100, true).Dump();//ֻ��д��Ȧ
            m.ReadCoils(1, 100, 1).Dump();//�˴�Ҳֻ�ܶ���Ȧ������ɢ���������� 
            m.WriteSingleCoil(1, 100, false).Dump();//ֻ��д��Ȧ
            m.ReadCoils(1, 100, 1).Dump();//�˴�Ҳֻ�ܶ���Ȧ������ɢ����������

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


            "��д����Ĵ���".Dump("��д����Ĵ���"); 
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


            "�ַ�����д".Dump("�ַ�����д");
            m.WriteMultipleRegisters<string>(1, 100, new string[] { "abcdefg" }).Dump();
            m.ReadHoldingRegisters<string>(1, 100, 10).Dump();

            "Done.".Dump();
            Console.ReadLine();
        }

        static void TestProtocolBase(ProtocolBase m,string bitRegName,int bitAddress,string dataRegName,int dataAddress)
        {
            //��������
            try
            { 
                //MC_3E mc = new MC_3E("COM1",9600,8,Parity.None,StopBits.One);

                //����ƴ�ӷ�ʽ-������
                //MC_3E2 mc = new MC_3E2("127.0.0.1", 6000);
                //MC_3E mc = new MC_3E("COM1", 9600, 8, Parity.None, StopBits.One);

                Console.WriteLine("�����������ԣ�");
                Console.WriteLine("��д����Ԫ��");
                m.WriteBool(bitRegName, bitAddress, true).Dump("д����ֵ��");
                m.ReadBool(bitRegName, bitAddress).Dump("������ֵ��");
                 
                m.WriteInt16(dataRegName, dataAddress, 1234).Dump("д�з����֣�");
                m.ReadInt16(dataRegName, dataAddress).Dump("�������֣�");
                 
                m.WriteUInt16(dataRegName, dataAddress, 1234).Dump("д�޷����֣�");
                m.ReadUInt16(dataRegName, dataAddress).Dump("���޷����֣�");
                 
                m.WriteInt32(dataRegName, dataAddress, 1234567).Dump("д�з���˫�֣�");
                m.ReadInt32(dataRegName, dataAddress).Dump("���з���˫�֣�");
                 
                m.WriteUInt32(dataRegName, dataAddress, 1234567).Dump("д�޷���˫�֣�");
                m.ReadUInt32(dataRegName, dataAddress).Dump("���޷���˫�֣�");
                 
                m.WriteSingle(dataRegName, dataAddress, 3.141592653f).Dump("д��������");
                m.ReadSingle(dataRegName, dataAddress).Dump("����������");


                Console.WriteLine("��д���Ԫ��"); 
                m.WriteBool(bitRegName, bitAddress, new bool[] { true, false, true, false, true, true, false, true, false, true, true, false, true, false, true, false }).Dump("д5������ֵ��");
                m.ReadBool(bitRegName, bitAddress, 5).Dump("��5������ֵ��");
                 
                m.WriteInt16(dataRegName, dataAddress, new Int16[] { 1, 2, 3, 4, 5 }).Dump("д5���з����֣�");
                m.ReadInt16(dataRegName, dataAddress, 5).Dump("��5�������֣�");
                 
                m.WriteUInt16(dataRegName, dataAddress, new UInt16[] { 1, 2, 3, 4, 5 }).Dump("д5���޷����֣�");
                m.ReadUInt16(dataRegName, dataAddress, 5).Dump("��5���޷����֣�");
                 
                m.WriteInt32(dataRegName, dataAddress, new Int32[] { 12, 23, 34, 45, 56 }).Dump("д5���з���˫�֣�");
                m.ReadInt32(dataRegName, dataAddress, 5).Dump("��5���з���˫�֣�");
                 
                m.WriteUInt32(dataRegName, dataAddress, new UInt32[] { 11, 22, 33, 44, 55 }).Dump("д5���޷���˫�֣�");
                m.ReadUInt32(dataRegName, dataAddress, 5).Dump("��5���޷���˫�֣�");
                 
                m.WriteSingle(dataRegName, dataAddress, new Single[] { 1.1f, 2.2f, 3.3f, 4.4f, 5.5f }).Dump("д5����������");
                m.ReadSingle(dataRegName, dataAddress, 5).Dump("��5����������");
                 
                m.WriteString(dataRegName, dataAddress, "abcdefghij").Dump("д�ַ�����");
                m.ReadString(dataRegName, dataAddress, 10).Dump("���ַ�����");


                Console.WriteLine("���ͷ�������");
                Console.WriteLine("��д����Ԫ��");
                
                m.WriteData<bool>(bitRegName, bitAddress, (object)true).Dump("����д����ֵ");
                m.ReadData<bool>(bitRegName, bitAddress).Dump("���Ͷ�����ֵ");
                 
                m.WriteData<Int16>(dataRegName, dataAddress, (object)1234).Dump("����дINT16");
                m.ReadData<Int16>(dataRegName, dataAddress).Dump("���Ͷ�INT16");
                 
                m.WriteData<UInt16>(dataRegName, dataAddress, (object)1234).Dump("����дUINT16");
                m.ReadData<UInt16>(dataRegName, dataAddress).Dump("���Ͷ�UINT16");
                 
                m.WriteData<Int32>(dataRegName, dataAddress, (object)12345678).Dump("����дINT32");
                m.ReadData<Int32>(dataRegName, dataAddress).Dump("���Ͷ�INT16");
                 
                m.WriteData<UInt32>(dataRegName, dataAddress, (object)12345678).Dump("����дUINT32");
                m.ReadData<UInt32>(dataRegName, dataAddress).Dump("���Ͷ�UINT16");
                 
                m.WriteData<Single>(dataRegName, dataAddress, (object)1.2345678).Dump("����дINT32");
                m.ReadData<Single>(dataRegName, dataAddress).Dump("���Ͷ�INT16");
                 
                m.WriteData<string>(dataRegName, dataAddress, (object)"kkkkkkkkkkk").Dump("����дstring");
                m.ReadData<string>(dataRegName, dataAddress, 10).Dump("���Ͷ�string");


                //��д���Ԫ��
                Console.WriteLine("��д���Ԫ��"); 
                m.WriteData<bool[]>(bitRegName, bitAddress, (object)new bool[] { true, false, true, false, true, true, false, true, false, true, true, false, true, false, true, false }).Dump("����д5����ֵ");
                m.ReadData<bool[]>(bitRegName, bitAddress, 5).Dump("���Ͷ�5����ֵ");
                 
                m.WriteData<Int16[]>(dataRegName, dataAddress, (object)new Int16[] { 1, 2, 3, 4, 5 }).Dump("����д5INT16");
                m.ReadData<Int16[]>(dataRegName, dataAddress, 5).Dump("���Ͷ�5INT16");
                 
                m.WriteData<UInt16[]>(dataRegName, dataAddress, (object)new UInt16[] { 1, 2, 3, 4, 5 }).Dump("����д5UINT16");
                m.ReadData<UInt16[]>(dataRegName, dataAddress, 5).Dump("���Ͷ�5UINT16");
                 
                m.WriteData<Int32[]>(dataRegName, dataAddress, (object)new Int32[] { 11, 22, 33, 44, 55 }).Dump("����д5INT32");
                m.ReadData<Int32[]>(dataRegName, dataAddress, 5).Dump("���Ͷ�5INT16");
                 
                m.WriteData<UInt32[]>(dataRegName, dataAddress, (object)new UInt32[] { 12, 34, 56, 78, 90 }).Dump("����д5UINT32");
                m.ReadData<UInt32[]>(dataRegName, dataAddress, 5).Dump("���Ͷ�5UINT16");
                 
                m.WriteData<Single[]>(dataRegName, dataAddress, (object)new Single[] { 1.1f, 2.2f, 3.3f, 4.4f, 5.5f }).Dump("����д5INT32");
                m.ReadData<Single[]>(dataRegName, dataAddress, 5).Dump("���Ͷ�5INT16");


                //�����첽��������-------------------------------------------------------------------------------------
                //��д����Ԫ��
                Console.WriteLine("�����첽��������--------------------------------------------------------------------");
                Console.WriteLine("��д����Ԫ��"); 
                m.WriteDataAsync<bool>(bitRegName, bitAddress, (object)true).Result.Dump("����д����ֵ");
                m.ReadDataAsync<bool>(bitRegName, bitAddress).Result.Dump("���Ͷ�����ֵ");
                 
                m.WriteDataAsync<Int16>(dataRegName, dataAddress, (object)1234).Result.Dump("����дINT16");
                m.ReadDataAsync<Int16>(dataRegName, dataAddress).Result.Dump("���Ͷ�INT16");
                 
                m.WriteDataAsync<UInt16>(dataRegName, dataAddress, (object)1234).Result.Dump("����дUINT16");
                m.ReadDataAsync<UInt16>(dataRegName, dataAddress).Result.Dump("���Ͷ�UINT16");
                 
                m.WriteDataAsync<Int32>(dataRegName, dataAddress, (object)12345678).Result.Dump("����дINT32");
                m.ReadDataAsync<Int32>(dataRegName, dataAddress).Result.Dump("���Ͷ�INT16");
                 
                m.WriteDataAsync<UInt32>(dataRegName, dataAddress, (object)12345678).Result.Dump("����дUINT32");
                m.ReadDataAsync<UInt32>(dataRegName, dataAddress).Result.Dump("���Ͷ�UINT16");
                 
                m.WriteDataAsync<Single>(dataRegName, dataAddress, (object)1.2345678).Result.Dump("����дINT32");
                m.ReadDataAsync<Single>(dataRegName, dataAddress).Result.Dump("���Ͷ�INT16");
                 
                m.WriteDataAsync<string>(dataRegName, dataAddress, (object)"kkkkkkkkkkk").Result.Dump("����дstring");
                m.ReadDataAsync<string>(dataRegName, dataAddress, 10).Result.Dump("���Ͷ�string");

                 
                Console.WriteLine("��д���Ԫ��"); 
                m.WriteDataAsync<bool[]>(bitRegName, bitAddress, (object)new bool[] { true, false, true, false, true, true, false, true, false, true, true, false, true, false, true, false }).Result.Dump("����д5����ֵ");
                m.ReadDataAsync<bool[]>(bitRegName, bitAddress, 5).Result.Dump("���Ͷ�5����ֵ");
                 
                m.WriteDataAsync<Int16[]>(dataRegName, dataAddress, (object)new Int16[] { 1, 2, 3, 4, 5 }).Result.Dump("����д5INT16");
                m.ReadDataAsync<Int16[]>(dataRegName, dataAddress, 5).Result.Dump("���Ͷ�5INT16");
                 
                m.WriteDataAsync<UInt16[]>(dataRegName, dataAddress, (object)new UInt16[] { 1, 2, 3, 4, 5 }).Result.Dump("����д5UINT16");
                m.ReadDataAsync<UInt16[]>(dataRegName, dataAddress, 5).Result.Dump("���Ͷ�5UINT16");
                 
                m.WriteDataAsync<Int32[]>(dataRegName, dataAddress, (object)new Int32[] { 11, 22, 33, 44, 55 }).Result.Dump("����д5INT32");
                m.ReadDataAsync<Int32[]>(dataRegName, dataAddress, 5).Result.Dump("���Ͷ�5INT16");
                 
                m.WriteDataAsync<UInt32[]>(dataRegName, dataAddress, (object)new UInt32[] { 12, 34, 56, 78, 90 }).Result.Dump("����д5UINT32");
                m.ReadDataAsync<UInt32[]>(dataRegName, dataAddress, 5).Result.Dump("���Ͷ�5UINT16");
                 
                m.WriteDataAsync<Single[]>(dataRegName, dataAddress, (object)new Single[] { 1.1f, 2.2f, 3.3f, 4.4f, 5.5f }).Result.Dump("����д5INT32");
                m.ReadDataAsync<Single[]>(dataRegName, dataAddress, 5).Result.Dump("���Ͷ�5INT16");

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