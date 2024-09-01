# Protocols-工业设备通信协议库
一个为爱发电项目，正在逐步完善中   
在VS2022+.Net Framework4.8 + HslCommunication 本地测试通过（只测试了M,D寄存器）     
目前只支持TCP方式和串口方式，欢迎反馈bug   
感谢以下好友提供的帮助：   
拓拓(https://github.com/kongdetuo)   
SlimeNull(https://github.com/SlimeNull)   
Steve(https://github.com/steveworkshop)   
林德熙(https://github.com/lindexi)   
雾雨氏Bot   
仙女之萌(https://github.com/ilyfairy)   
Shompinice(https://github.com/MicaApps)   
      
20240819：增加ModbusRTU。    
20240820：增加ModbusASCII，ModbusTCP，增加松下Mewtocol（写多个位时还有bug未修复）！   
20240828：调整了结构，并增加欧姆龙HostLink_Serial方式（使用头编码FA），其余头编码和Fins方式待后续添加   
20240901：增加了欧姆龙FinsTCP方式
```
                
        static void Main(string[] args)
        {
            //TestMC(new MC_3E("127.0.0.1", 6000));

            //TestMewtocol(new Mewtocol(new CommSerialPort("COM1", 9600, 8, Parity.None, StopBits.One)));

            //TestOmron(new HostLink_Serial(new CommSerialPort("COM1", 9600, 7, Parity.Even, StopBits.One)));
            TestMC(new HostLink_Serial(new CommSerialPort("COM1", 9600, 7, Parity.Even, StopBits.One)));

            //TestModbus(new ASCII(new CommSerialPort("COM1", 9600, 8, Parity.None, StopBits.One)));
            //TestModbus(new RTU(new CommSerialPort("COM1", 9600, 8, Parity.None, StopBits.One)));
            //TestModbus(new TCP(new CommNet("127.0.0.1", 502)));

        }

        static void TestOmron(ProtocolBase m)
        {
            //单个读写
            //布尔读写
            m.WriteBool("D", 10000, true).Dump("WriteBool");
            m.ReadBool("D", 10000).Dump("ReadBool");

            //16位读写
            m.WriteInt16("D", 100, -1).Dump("WriteInt16");
            m.ReadInt16("D", 100).Dump("ReadInt16");

            m.WriteUInt16("D", 100, 1).Dump("WriteUInt16");
            m.ReadUInt16("D", 100).Dump("ReadUInt16");

            //32位读写
            m.WriteInt32("D", 100, -1111).Dump("WriteInt32");
            m.ReadInt32("D", 100).Dump("ReadInt32");

            m.WriteUInt32("D", 100, 1111).Dump("WriteUInt32");
            m.ReadUInt32("D", 100).Dump("ReadUInt32");

            //浮点数读写
            m.WriteSingle("D", 100, -11.11f).Dump("WriteSingle");
            m.ReadSingle("D", 100).Dump("ReadSingle");

            //字符串读写
            m.WriteString("D", 100, "abcdefghijklmnopqrstuvwxyz").Dump("WriteString");
            m.ReadString("D", 100, 26).Dump("ReadString");

            //多个读写
            //布尔读写
            m.WriteBool("D", 10000, new bool[] { true,false,true,false,true }).Dump("WriteBool");
            m.ReadBool("D", 10000, 5).Dump("ReadBool");

            //16位读写
            m.WriteInt16("D",100,new short[] {-11,-22,-33,-44,-55 }).Dump("WriteInt16");
            m.ReadInt16("D", 100, 5).Dump("ReadInt16");

            m.WriteUInt16("D", 100, new ushort[] { 11, 22, 33, 44, 55 }).Dump("WriteUInt16");
            m.ReadUInt16("D", 100, 5).Dump("ReadUInt16");

            //32位读写
            m.WriteInt32("D", 100, new int[] { -1111, -2222, -3333, -4444, -5555 }).Dump("WriteInt32");
            m.ReadInt32("D", 100, 5).Dump("ReadInt32");

            m.WriteUInt32("D", 100, new uint[] { 1111, 2222, 3333, 4444, 5555 }).Dump("WriteUInt32");
            m.ReadUInt32("D", 100, 5).Dump("ReadUInt32");

            //浮点数读写
            m.WriteSingle("D", 100, new float[] { -11.11f, -22.22f, -33.33f, -44.44f, -55.55f }).Dump("WriteSingle");
            m.ReadSingle("D", 100, 5).Dump("ReadSingle");

            //字符串读写
            m.WriteString("D", 100, "abcdefghijklmnopqrstuvwxyz").Dump("WriteString");
            m.ReadString("D", 100, 26).Dump("ReadString");

            Console.Read();

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

        static void TestMC(ProtocolBase m)
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
                m.WriteBool("M", 100, true).Dump("写布尔值：");
                m.ReadBool("M", 100).Dump("读布尔值：");
                 
                m.WriteInt16("D", 100, 1234).Dump("写有符号字：");
                m.ReadInt16("D", 100).Dump("读符号字：");
                 
                m.WriteUInt16("D", 100, 1234).Dump("写无符号字：");
                m.ReadUInt16("D", 100).Dump("读无符号字：");
                 
                m.WriteInt32("D", 100, 1234567).Dump("写有符号双字：");
                m.ReadInt32("D", 100).Dump("读有符号双字：");
                 
                m.WriteUInt32("D", 100, 1234567).Dump("写无符号双字：");
                m.ReadUInt32("D", 100).Dump("读无符号双字：");
                 
                m.WriteSingle("D", 100, 3.141592653f).Dump("写浮点数：");
                m.ReadSingle("D", 100).Dump("读浮点数：");


                Console.WriteLine("读写多个元件"); 
                m.WriteBool("M", 100, new bool[] { true, false, true, false, true }).Dump("写5个布尔值：");
                m.ReadBool("M", 100, 5).Dump("读5个布尔值：");
                 
                m.WriteInt16("D", 100, new Int16[] { 1, 2, 3, 4, 5 }).Dump("写5个有符号字：");
                m.ReadInt16("D", 100, 5).Dump("读5个符号字：");
                 
                m.WriteUInt16("D", 100, new UInt16[] { 1, 2, 3, 4, 5 }).Dump("写5个无符号字：");
                m.ReadUInt16("D", 100, 5).Dump("读5个无符号字：");
                 
                m.WriteInt32("D", 100, new Int32[] { 12, 23, 34, 45, 56 }).Dump("写5个有符号双字：");
                m.ReadInt32("D", 100, 5).Dump("读5个有符号双字：");
                 
                m.WriteUInt32("D", 100, new UInt32[] { 11, 22, 33, 44, 55 }).Dump("写5个无符号双字：");
                m.ReadUInt32("D", 100, 5).Dump("读5个无符号双字：");
                 
                m.WriteSingle("D", 100, new Single[] { 1.1f, 2.2f, 3.3f, 4.4f, 5.5f }).Dump("写5个浮点数：");
                m.ReadSingle("D", 100, 5).Dump("读5个浮点数：");
                 
                m.WriteString("D", 100, "abcdefghij").Dump("写字符串：");
                m.ReadString("D", 100, 10).Dump("读字符串：");


                Console.WriteLine("泛型方法测试");
                Console.WriteLine("读写单个元件");
                
                m.WriteData<bool>("M", 100, (object)true).Dump("泛型写布尔值");
                m.ReadData<bool>("M", 100).Dump("泛型读布尔值");
                 
                m.WriteData<Int16>("D", 100, (object)1234).Dump("泛型写INT16");
                m.ReadData<Int16>("D", 100).Dump("泛型读INT16");
                 
                m.WriteData<UInt16>("D", 100, (object)1234).Dump("泛型写UINT16");
                m.ReadData<UInt16>("D", 100).Dump("泛型读UINT16");
                 
                m.WriteData<Int32>("D", 100, (object)12345678).Dump("泛型写INT32");
                m.ReadData<Int32>("D", 100).Dump("泛型读INT16");
                 
                m.WriteData<UInt32>("D", 100, (object)12345678).Dump("泛型写UINT32");
                m.ReadData<UInt32>("D", 100).Dump("泛型读UINT16");
                 
                m.WriteData<Single>("D", 100, (object)1.2345678).Dump("泛型写INT32");
                m.ReadData<Single>("D", 100).Dump("泛型读INT16");
                 
                m.WriteData<string>("D", 100, (object)"kkkkkkkkkkk").Dump("泛型写string");
                m.ReadData<string>("D", 100, 10).Dump("泛型读string");


                //读写多个元件
                Console.WriteLine("读写多个元件"); 
                m.WriteData<bool[]>("M", 100, (object)new bool[] { true, false, true, false, true }).Dump("泛型写5布尔值");
                m.ReadData<bool[]>("M", 100, 5).Dump("泛型读5布尔值");
                 
                m.WriteData<Int16[]>("D", 100, (object)new Int16[] { 1, 2, 3, 4, 5 }).Dump("泛型写5INT16");
                m.ReadData<Int16[]>("D", 100, 5).Dump("泛型读5INT16");
                 
                m.WriteData<UInt16[]>("D", 100, (object)new UInt16[] { 1, 2, 3, 4, 5 }).Dump("泛型写5UINT16");
                m.ReadData<UInt16[]>("D", 100, 5).Dump("泛型读5UINT16");
                 
                m.WriteData<Int32[]>("D", 100, (object)new Int32[] { 11, 22, 33, 44, 55 }).Dump("泛型写5INT32");
                m.ReadData<Int32[]>("D", 100, 5).Dump("泛型读5INT16");
                 
                m.WriteData<UInt32[]>("D", 100, (object)new UInt32[] { 12, 34, 56, 78, 90 }).Dump("泛型写5UINT32");
                m.ReadData<UInt32[]>("D", 100, 5).Dump("泛型读5UINT16");
                 
                m.WriteData<Single[]>("D", 100, (object)new Single[] { 1.1f, 2.2f, 3.3f, 4.4f, 5.5f }).Dump("泛型写5INT32");
                m.ReadData<Single[]>("D", 100, 5).Dump("泛型读5INT16");


                //泛型异步方法测试-------------------------------------------------------------------------------------
                //读写单个元件
                Console.WriteLine("泛型异步方法测试--------------------------------------------------------------------");
                Console.WriteLine("读写单个元件"); 
                m.WriteDataAsync<bool>("M", 100, (object)true).Result.Dump("泛型写布尔值");
                m.ReadDataAsync<bool>("M", 100).Result.Dump("泛型读布尔值");
                 
                m.WriteDataAsync<Int16>("D", 100, (object)1234).Result.Dump("泛型写INT16");
                m.ReadDataAsync<Int16>("D", 100).Result.Dump("泛型读INT16");
                 
                m.WriteDataAsync<UInt16>("D", 100, (object)1234).Result.Dump("泛型写UINT16");
                m.ReadDataAsync<UInt16>("D", 100).Result.Dump("泛型读UINT16");
                 
                m.WriteDataAsync<Int32>("D", 100, (object)12345678).Result.Dump("泛型写INT32");
                m.ReadDataAsync<Int32>("D", 100).Result.Dump("泛型读INT16");
                 
                m.WriteDataAsync<UInt32>("D", 100, (object)12345678).Result.Dump("泛型写UINT32");
                m.ReadDataAsync<UInt32>("D", 100).Result.Dump("泛型读UINT16");
                 
                m.WriteDataAsync<Single>("D", 100, (object)1.2345678).Result.Dump("泛型写INT32");
                m.ReadDataAsync<Single>("D", 100).Result.Dump("泛型读INT16");
                 
                m.WriteDataAsync<string>("D", 100, (object)"kkkkkkkkkkk").Result.Dump("泛型写string");
                m.ReadDataAsync<string>("D", 100, 10).Result.Dump("泛型读string");

                 
                Console.WriteLine("读写多个元件"); 
                m.WriteDataAsync<bool[]>("M", 100, (object)new bool[] { true, false, true, false, true }).Result.Dump("泛型写5布尔值");
                m.ReadDataAsync<bool[]>("M", 100, 5).Result.Dump("泛型读5布尔值");
                 
                m.WriteDataAsync<Int16[]>("D", 100, (object)new Int16[] { 1, 2, 3, 4, 5 }).Result.Dump("泛型写5INT16");
                m.ReadDataAsync<Int16[]>("D", 100, 5).Result.Dump("泛型读5INT16");
                 
                m.WriteDataAsync<UInt16[]>("D", 100, (object)new UInt16[] { 1, 2, 3, 4, 5 }).Result.Dump("泛型写5UINT16");
                m.ReadDataAsync<UInt16[]>("D", 100, 5).Result.Dump("泛型读5UINT16");
                 
                m.WriteDataAsync<Int32[]>("D", 100, (object)new Int32[] { 11, 22, 33, 44, 55 }).Result.Dump("泛型写5INT32");
                m.ReadDataAsync<Int32[]>("D", 100, 5).Result.Dump("泛型读5INT16");
                 
                m.WriteDataAsync<UInt32[]>("D", 100, (object)new UInt32[] { 12, 34, 56, 78, 90 }).Result.Dump("泛型写5UINT32");
                m.ReadDataAsync<UInt32[]>("D", 100, 5).Result.Dump("泛型读5UINT16");
                 
                m.WriteDataAsync<Single[]>("D", 100, (object)new Single[] { 1.1f, 2.2f, 3.3f, 4.4f, 5.5f }).Result.Dump("泛型写5INT32");
                m.ReadDataAsync<Single[]>("D", 100, 5).Result.Dump("泛型读5INT16");

                Console.WriteLine("Done.");

                Console.Read();

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        static void TestMewtocol(ProtocolBase m)
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
                m.WriteBool("R", 0x100, true).Dump("写布尔值：");
                m.ReadBool("R", 0x100).Dump("读布尔值：");
                 
                m.WriteInt16("D", 100, 1234).Dump("写有符号字：");
                m.ReadInt16("D", 100).Dump("读符号字：");
                 
                m.WriteUInt16("D", 100, 1234).Dump("写无符号字：");
                m.ReadUInt16("D", 100).Dump("读无符号字：");
                 
                m.WriteInt32("D", 100, 1234567).Dump("写有符号双字：");
                m.ReadInt32("D", 100).Dump("读有符号双字：");
                 
                m.WriteUInt32("D", 100, 1234567).Dump("写无符号双字：");
                m.ReadUInt32("D", 100).Dump("读无符号双字：");
                 
                m.WriteSingle("D", 100, 3.141592653f).Dump("写浮点数：");
                m.ReadSingle("D", 100).Dump("读浮点数：");


                Console.WriteLine("读写多个元件"); 
                m.WriteBool("R", 0x100, new bool[] { true, false, true, false, true }).Dump("写5个布尔值：");
                m.ReadBool("R", 0x100, 5).Dump("读5个布尔值：");
                 
                m.WriteInt16("D", 100, new Int16[] { 1, 2, 3, 4, 5 }).Dump("写5个有符号字：");
                m.ReadInt16("D", 100, 5).Dump("读5个符号字：");
                 
                m.WriteUInt16("D", 100, new UInt16[] { 1, 2, 3, 4, 5 }).Dump("写5个无符号字：");
                m.ReadUInt16("D", 100, 5).Dump("读5个无符号字：");
                 
                m.WriteInt32("D", 100, new Int32[] { 12, 23, 34, 45, 56 }).Dump("写5个有符号双字：");
                m.ReadInt32("D", 100, 5).Dump("读5个有符号双字：");
                 
                m.WriteUInt32("D", 100, new UInt32[] { 11, 22, 33, 44, 55 }).Dump("写5个无符号双字：");
                m.ReadUInt32("D", 100, 5).Dump("读5个无符号双字：");
                 
                m.WriteSingle("D", 100, new Single[] { 1.1f, 2.2f, 3.3f, 4.4f, 5.5f }).Dump("写5个浮点数：");
                m.ReadSingle("D", 100, 5).Dump("读5个浮点数：");
                 
                m.WriteString("D", 100, "abcdefghij").Dump("写字符串：");
                m.ReadString("D", 100, 10).Dump("读字符串：");


                //泛型方法测试
                //读写单个元件
                Console.WriteLine("读写单个元件"); 
                m.WriteData<bool>("R", 0x100, (object)true).Dump("泛型写布尔值");
                m.ReadData<bool>("R", 0x100).Dump("泛型读布尔值");
                 
                m.WriteData<Int16>("D", 100, (object)1234).Dump("泛型写INT16");
                m.ReadData<Int16>("D", 100).Dump("泛型读INT16");
                 
                m.WriteData<UInt16>("D", 100, (object)1234).Dump("泛型写UINT16");
                m.ReadData<UInt16>("D", 100).Dump("泛型读UINT16");
                 
                m.WriteData<Int32>("D", 100, (object)12345678).Dump("泛型写INT32");
                m.ReadData<Int32>("D", 100).Dump("泛型读INT16");
                 
                m.WriteData<UInt32>("D", 100, (object)12345678).Dump("泛型写UINT32");
                m.ReadData<UInt32>("D", 100).Dump("泛型读UINT16");
                 
                m.WriteData<Single>("D", 100, (object)1.2345678).Dump("泛型写INT32");
                m.ReadData<Single>("D", 100).Dump("泛型读INT16");
                 
                m.WriteData<string>("D", 100, (object)"kkkkkkkkkkk").Dump("泛型写string");
                m.ReadData<string>("D", 100, 10).Dump("泛型读string");


                //读写多个元件
                Console.WriteLine("读写多个元件"); 
                m.WriteData<bool[]>("R", 0x100, (object)new bool[] { true, false, true, false, true }).Dump("泛型写5布尔值");
                m.ReadData<bool[]>("R", 0x100, 5).Dump("泛型读5布尔值");
                 
                m.WriteData<Int16[]>("D", 100, (object)new Int16[] { 1, 2, 3, 4, 5 }).Dump("泛型写5INT16");
                m.ReadData<Int16[]>("D", 100, 5).Dump("泛型读5INT16");
                 
                m.WriteData<UInt16[]>("D", 100, (object)new UInt16[] { 1, 2, 3, 4, 5 }).Dump("泛型写5UINT16");
                m.ReadData<UInt16[]>("D", 100, 5).Dump("泛型读5UINT16");
                 
                m.WriteData<Int32[]>("D", 100, (object)new Int32[] { 11, 22, 33, 44, 55 }).Dump("泛型写5INT32");
                m.ReadData<Int32[]>("D", 100, 5).Dump("泛型读5INT16");
                 
                m.WriteData<UInt32[]>("D", 100, (object)new UInt32[] { 12, 34, 56, 78, 90 }).Dump("泛型写5UINT32");
                m.ReadData<UInt32[]>("D", 100, 5).Dump("泛型读5UINT16");
                 
                m.WriteData<Single[]>("D", 100, (object)new Single[] { 1.1f, 2.2f, 3.3f, 4.4f, 5.5f }).Dump("泛型写5INT32");
                m.ReadData<Single[]>("D", 100, 5).Dump("泛型读5INT16");


                //泛型异步方法测试-------------------------------------------------------------------------------------
                //读写单个元件
                Console.WriteLine("泛型异步方法测试--------------------------------------------------------------------");
                Console.WriteLine("读写单个元件"); 
                m.WriteDataAsync<bool>("R", 0x100, (object)true).Result.Dump("泛型写布尔值");
                m.ReadDataAsync<bool>("R", 0x100).Result.Dump("泛型读布尔值");
                 
                m.WriteDataAsync<Int16>("D", 100, (object)1234).Result.Dump("泛型写INT16");
                m.ReadDataAsync<Int16>("D", 100).Result.Dump("泛型读INT16");
                 
                m.WriteDataAsync<UInt16>("D", 100, (object)1234).Result.Dump("泛型写UINT16");
                m.ReadDataAsync<UInt16>("D", 100).Result.Dump("泛型读UINT16");
                 
                m.WriteDataAsync<Int32>("D", 100, (object)12345678).Result.Dump("泛型写INT32");
                m.ReadDataAsync<Int32>("D", 100).Result.Dump("泛型读INT16");
                 
                m.WriteDataAsync<UInt32>("D", 100, (object)12345678).Result.Dump("泛型写UINT32");
                m.ReadDataAsync<UInt32>("D", 100).Result.Dump("泛型读UINT16");
                 
                m.WriteDataAsync<Single>("D", 100, (object)1.2345678).Result.Dump("泛型写INT32");
                m.ReadDataAsync<Single>("D", 100).Result.Dump("泛型读INT16");
                 
                m.WriteDataAsync<string>("D", 100, (object)"kkkkkkkkkkk").Result.Dump("泛型写string");
                m.ReadDataAsync<string>("D", 100, 10).Result.Dump("泛型读string");


                //读写多个元件
                Console.WriteLine("读写多个元件"); 
                m.WriteDataAsync<bool[]>("R", 100, (object)new bool[] { true, false, true, false, true }).Result.Dump("泛型写5布尔值");
                m.ReadDataAsync<bool[]>("R", 100, 5).Result.Dump("泛型读5布尔值");
                 
                m.WriteDataAsync<Int16[]>("D", 100, (object)new Int16[] { 1, 2, 3, 4, 5 }).Result.Dump("泛型写5INT16");
                m.ReadDataAsync<Int16[]>("D", 100, 5).Result.Dump("泛型读5INT16");
                 
                m.WriteDataAsync<UInt16[]>("D", 100, (object)new UInt16[] { 1, 2, 3, 4, 5 }).Result.Dump("泛型写5UINT16");
                m.ReadDataAsync<UInt16[]>("D", 100, 5).Result.Dump("泛型读5UINT16");
                 
                m.WriteDataAsync<Int32[]>("D", 100, (object)new Int32[] { 11, 22, 33, 44, 55 }).Result.Dump("泛型写5INT32");
                m.ReadDataAsync<Int32[]>("D", 100, 5).Result.Dump("泛型读5INT16");
                 
                m.WriteDataAsync<UInt32[]>("D", 100, (object)new UInt32[] { 12, 34, 56, 78, 90 }).Result.Dump("泛型写5UINT32");
                m.ReadDataAsync<UInt32[]>("D", 100, 5).Result.Dump("泛型读5UINT16");
                 
                m.WriteDataAsync<Single[]>("D", 100, (object)new Single[] { 1.1f, 2.2f, 3.3f, 4.4f, 5.5f }).Result.Dump("泛型写5INT32");
                m.ReadDataAsync<Single[]>("D", 100, 5).Result.Dump("泛型读5INT16");

                Console.WriteLine("Done.");

                Console.Read();

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }
```