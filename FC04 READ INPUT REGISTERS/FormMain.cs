using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using Microsoft;

namespace FC04_READ_INPUT_REGISTERS
{
    public partial class FormMain : Form
    {
        private SerialPort serialPort1 = null;
        
        private byte function = 3;   // команда чтения регистра
        private int startAddress;    // = 11029;
        private uint numberOfPoints; // МАКСИМУМ 64
        private int cnt; //счетчик для кнопки
        private int p;   //счетчик для смены первого регистра
        private int LengthTRND; // длина тренда в точках

        private int tm; // счетчик для switch, по регистрам
        private int CurPNT; // счетчик распакованных точек в том же switch
        private int Ntrd;  // номер графика архивных данных
        private int NtrdStreams; // номер тренда, который надо скинуть
        private int itd; 

        private string year;  // создание структуры даты
        private string mounth;
        private string day;
        private string hour;
        private string minute;
        private string second;
        private string date;

        private double downloadTime; 
        private bool foolYear;  // поврежденная дата (условно назвал год)  НЕ ИСПОЛЬЗ

        private int iyear;  // элементы даты в INT для 
        private int imounth;
        private int iday;
        private int ihour;
        private int iminute;
        private int isecond;

        private int tmn; // index for cycle
        private string forval1; 
        private string forval2; // если два тренда, временные переменные

        public int startAddrTrd = 11030; // он всегда фиксированный, начало читаемой части, 1030 иначе. НА СП-270 ЛЮБОЙ АДРЕС В МОДБАСЕ +10000, ЕСЛИ PFW
        public int CurrRegr;
        public int ix = 1;
        
        public int speed = 9600;   // ПО УМОЛЧАНИЮ НАСТРОЙКИ ТАКИЕ БУДУТ, если надо поменять - то на форме выбираем
        public string comport = "COM6";
        public int interval = 200;
        public byte slaveAddress = 16;

        public class TrendOne  // класс параметров для первого тренда
        {
            public string value;
            public DateTime DateTrend;
        //    public DateTime DateTrendCurr;

        }

        public class TrendTwo  // класс параметров для второго тренда
        {
            public string value1;
            public string value2;
            public DateTime DateTrend;
          //  public DateTime DateTrendCurr;
        }

        public class installer // класс параметров для чтения из файла
        {
            public string name;
            public string trends;
            public string lengthOfTrnd;
            public string startReg;
        }
        
        Queue<TrendOne> TrendOnelistQ = new Queue<TrendOne>();

        Queue<string> trend1 = new Queue<string>();
        List<TrendOne> TrendOnelist = new List<TrendOne>();
        List<TrendTwo> TrendTwolist = new List<TrendTwo>();
        List<installer> installerList = new List<installer>();
        int[] trend1mas;
        string[] array2;
        string[] array3;


        public FormMain()
        {
            InitializeComponent();
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            comboBox2.Items.Add("Первый тренд");  // открывается форма - прогружаются варианты выбора для раскрывающегося списка нижнего
            comboBox2.Items.Add("Второй тренд");
            comboBox2.Items.Add("Оба тренда");
        }
        
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)   // Если выбираем какой тренд качать - соответствующие параметры тренда прогружаются 
        {
            int ix = comboBox1.SelectedIndex;
            label15.Text = Convert.ToString(installerList[ix].lengthOfTrnd);
            label19.Text = Convert.ToString(installerList[ix].startReg);
            label14.Text = Convert.ToString(installerList[ix].trends);
            LengthTRND = Convert.ToInt32(installerList[ix].lengthOfTrnd); // прочитали количество точек
            downloadTime = LengthTRND * 0.00073;
            label23.Text = Convert.ToString(downloadTime);
            if (installerList[ix].trends == "1")
            {
                comboBox2.Enabled = false;    // определение: один или два тренда на одном графике
                Ntrd = 1;
            }
            else { comboBox2.Enabled = true; Ntrd = 2; }
        }
        
        private void button1_Click(object sender, EventArgs e)   // ввод настроек для модбаса (открываем порт при нажатии)
        {
            speed = Convert.ToInt16(textBox1.Text);
            comport = textBox2.Text;
            slaveAddress = Convert.ToByte(textBox3.Text);
            interval = Convert.ToInt16(textBox4.Text);
            try
            {
                serialPort1 = new SerialPort(comport, speed, Parity.None, 8, StopBits.One); // настройки как на панели
                serialPort1.Open(); // Занимаем порт
                timer1.Interval = 100; // период опроса слейва
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            try  // номер и кол-во трендов "1" - первый тренд, "2" - второй, "3" - оба
            {
                if (comboBox2.Text == "Первый тренд")
                { NtrdStreams = 1; }
                else if (comboBox2.Text == "Второй тренд")
                { NtrdStreams = 2; }
                else if (comboBox2.Text == "Оба тренда")
                { NtrdStreams = 3; }

                timer1.Start();
                
                p = 0; // всяческие индексы, необходимые для итераций
                tm = 1;
                CurPNT = 1;
                cnt = 1;

                if (Ntrd == 1) // если один трнед, то 7 регистров на точку,если два - то 8
                {
                    numberOfPoints = 35; // 5*7  
                    tmn = 7;
                }
                else if (Ntrd == 2)
                {
                    numberOfPoints = 40; // 5*8
                    tmn = 8;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnStop_Click(object sender, EventArgs e)  // просто кнопка стоп, если что-то вышло из под контроля
        {
            try
            {
                timer1.Stop();
                cnt = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }


        //упаковка кадра, взял готовое решение

        /// <summary>
        /// Function 04 (04hex) Read Input Registers
        /// Read the binary contents of input registers in the slave.
        /// </summary>
        /// <param name="slaveAddress">Slave Address</param>
        /// <param name="startAddress">Starting Address</param>
        /// <param name="function">Function</param>
        /// <param name="numberOfPoints">Quantity of inputs</param>
        /// <returns>Byte Array</returns>
        private byte[] ReadInputRegistersMsg(byte slaveAddress, int startAddress, byte function, uint numberOfPoints)  //объявление функции чтения регистра
        {
            byte[] frame = new byte[8];
            frame[0] = slaveAddress;			    // Slave Address
            frame[1] = function;				    // Function             
            frame[2] = (byte)(startAddress >> 8);	// Starting Address High
            frame[3] = (byte)startAddress;		    // Starting Address Low            
            frame[4] = (byte)(numberOfPoints >> 8);	// Quantity of Registers High
            frame[5] = (byte)numberOfPoints;		// Quantity of Registers Low
            byte[] crc = this.CalculateCRC(frame);  // Calculate CRC.
            frame[frame.Length - 2] = crc[0];       // Error Check Low
            frame[frame.Length - 1] = crc[1];       // Error Check High
            return frame;
        }

        // контрольная сумма - опять готовое решение

        /// <summary>
        /// CRC Calculation 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private byte[] CalculateCRC(byte[] data)
        {
            ushort CRCFull = 0xFFFF; // Set the 16-bit register (CRC register) = FFFFH.
            byte CRCHigh = 0xFF, CRCLow = 0xFF;
            char CRCLSB;
            byte[] CRC = new byte[2];
            for (int i = 0; i < (data.Length) - 2; i++)
            {
                CRCFull = (ushort)(CRCFull ^ data[i]); // 

                for (int j = 0; j < 8; j++)
                {
                    CRCLSB = (char)(CRCFull & 0x0001);
                    CRCFull = (ushort)((CRCFull >> 1) & 0x7FFF);

                    if (CRCLSB == 1)
                        CRCFull = (ushort)(CRCFull ^ 0xA001);
                }
            }
            CRC[1] = CRCHigh = (byte)((CRCFull >> 8) & 0xFF);
            CRC[0] = CRCLow = (byte)(CRCFull & 0xFF);
            return CRC;
        }

        /// <summary>
        /// Display Data
        /// </summary>
        /// <param name="data">Data</param>
        /// <returns>Message</returns>
        private string Display(byte[] data)
        {
            string result = string.Empty;
            foreach (var item in data)
            {
                result += string.Format("{0:X2}", item);
            }
            return result;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                if ((serialPort1.IsOpen) && (cnt > 0) && (CurPNT <= LengthTRND) && (CurPNT <= 6500)) // условия для обработки информации и передачи
                {                                                    
                    switch (Ntrd) // количество трендов на графике, помним
                    {
                        case 1:
                        startAddress = startAddrTrd + 4 + 1 + 7 * CurPNT;  // Начало чтения происходит спустя несколько регистров по формуле, выведеной эмпирически
                        break;
                        case 2:
                        startAddress = startAddrTrd + 4 + 2 + 8 * CurPNT;                        
                        break;                        
                        default:
                        { }
                        break;
                    } 

                        byte[] frame = ReadInputRegistersMsg(slaveAddress, startAddress, function, numberOfPoints);
                        serialPort1.Write(frame, 0, frame.Length);  // отправили запрос
                        Thread.Sleep(interval); // Delay 100ms - это время, которое требуется на обработку инфы, ввод и формы: 200мс по умолч. быстрее = ошибки; медленнее = медленнее
                        if (serialPort1.BytesToRead >= 5)   // если пришел не какой то левый шум, то го распаковывать
                        {
                            byte[] bufferReceiver = new byte[this.serialPort1.BytesToRead];
                            serialPort1.Read(bufferReceiver, 0, serialPort1.BytesToRead);
                            serialPort1.DiscardInBuffer();
                            this.Invoke(new EventHandler((o, evt) =>
                            {
                 //               txtReceiMsg.Text = Display(bufferReceiver);
                            }));

                            // Прочитали - далее распаковываем
                            byte[] data = new byte[bufferReceiver.Length - 5];
                            Array.Copy(bufferReceiver, 3, data, 0, data.Length);
                            UInt16[] temp = Word.ByteToUInt16(data);
                            string result = string.Empty;

                            foreach (var item in temp)
                            {
                                if (tm > tmn)  // итерационно читаем по 7 или по 8 регистров в зависимости от количества трендов (ведь известно как они записаны)
                                {
                                     tm = 1;
                                }

                                    try
                                    {
                                        switch (tm) // собсно - по порядку, итерационно 5 точек читаются по  или 8 регистров
                                        {
                                            case 1:
                                               if ((item <= 8250) && (item >= 8208))    // на СП-270 время в формате DEC, и если перевести в HEX, то отображается норм.                                                           
                                               {                                        // Детектор ошибок, если вне диапазона какая-либо часть даты, то мы ее не читаем
                                                   foolYear = false;                    // Если ошибка, то дата просто перезаписывается. т.о. игнорятся ошибки и не крашат процесс
                                                   year = Convert.ToString(item);
                                               }
                                               else { label13.Visible = true; label5.Visible = true; foolYear = true;  }

                                                break;
                                            case 2:

                                                if ((item <= 19) && (item >= 1))
                                                {
                                                    foolYear = false;
                                                    mounth = Convert.ToString(item);
                                                }
                                                else { label13.Visible = true; label5.Visible = true; foolYear = true; }
                                                break;

                                            case 3:
                                                if ((item <= 53) && (item >= 1))
                                                {
                                                    foolYear = false;
                                                    day = Convert.ToString(item);
                                                }
                                                else { label13.Visible = true; label5.Visible = true; foolYear = true; }
                                                break;

                                            case 4:
                                                if ((item <= 37) && (item >= 0))
                                                {
                                                    foolYear = false;
                                                    hour = Convert.ToString(item);
                                                }
                                                else { label13.Visible = true; label5.Visible = true; foolYear = true; }
                                                break;

                                            case 5:
                                                if ((item <= 97) && (item >= 0))
                                                {
                                                    foolYear = false;
                                                    minute = Convert.ToString(item);
                                                }
                                                else { label13.Visible = true; label5.Visible = true; foolYear = true; }
                                                break;

                                            case 6:
                                                if ((item <= 97) && (item >= 0))
                                                {
                                                    foolYear = false;
                                                    second = Convert.ToString(item);
                                                }
                                                else { label13.Visible = true; label5.Visible = true; foolYear = true; }

                                                iyear = Convert.ToInt32(year);  // вот прочитавши дату, преобразуем ее и корвертируем в спец. формат
                                                imounth = Convert.ToInt32(mounth);
                                                iday = Convert.ToInt32(day);
                                                ihour = Convert.ToInt32(hour);
                                                iminute = Convert.ToInt32(minute);
                                                isecond = Convert.ToInt32(second);

                                                date = iyear.ToString("X") + "." + imounth.ToString("X") + "." + iday.ToString("X") + " " + ihour.ToString("X") + ":" + iminute.ToString("X") + ":" + isecond.ToString("X"); //перевели дату из dec в hex
                                                break;

                                            case 7:  // в зависимости от кол-ва трендов присваиваем параметры объекту соотв. класса
                                                    if (Ntrd == 1)
                                                    {
                                                        var p13 = new TrendOne() { value = Convert.ToString(item), DateTrend = Convert.ToDateTime(date) };
                                                        TrendOnelist.Add(p13);
                                                    }
                                                    else if (Ntrd == 2)
                                                    {
                                                        forval1 = Convert.ToString(item);
                                                    }
                                                    label12.Text = Convert.ToString(CurPNT) + " / " + LengthTRND + "   | " + item; // просто отображение на форме, прогресс показывается
                                                break;

                                            case 8:
                                                 if (Ntrd == 2)
                                                    {
                                                        forval2 = Convert.ToString(item);
                                                        var p14 = new TrendTwo() { value1 = forval1, value2 = forval2, DateTrend = Convert.ToDateTime(date) };
                                                        TrendTwolist.Add(p14);
                                                    }
                                            break;

                                            default:
                                                { }
                                                break;
                                        }

                                        tm++;
                                    }
                                    catch (Exception ex)
                                    {
                                        MessageBox.Show(ex.Message);
                                    }

                            cnt++;
                        }
                                CurPNT = CurPNT + 5; 
                                p++;

                        if ((CurPNT >= LengthTRND) || ((CurPNT >= 6500)))
                        {
                            timer1.Stop();
                            cnt = 0;
                            tm = 1;
                            //АКТИВИРУЕТСЯ КНОПКА СКАЧАТЬ СЛЕДУЮЩУЮ ЧАСТЬ, ЕСЛИ ДЛИНА ТРЕНДА ПОЗВОЛЯЕТ
                            label30.Visible = true;
                            if (CurPNT >= 6500) { button5.Enabled = true; label2.Visible = true; label30.Visible = false; }
                        }
                    }
                }
           }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
    }
        }

        private void txtResult_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            
        }

        private void button2_Click(object sender, EventArgs e)               //присвоить имя файлу и определить путь сохранения
        {
            saveFileDialog1.Title = "Save an CSV File";
            saveFileDialog1.Filter = "csv files (*.csv)|";
            saveFileDialog1.ShowDialog();
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                if (Ntrd == 1)
                {
                    TrendOnelist.Sort((x, y) => x.DateTrend.CompareTo(y.DateTrend));  //сортируем класс по времени в порядке возрастания

                    FileStream fs1 = new FileStream(saveFileDialog1.FileName + ".csv", FileMode.Create); // начинаем процесс записи в файл
                    StreamWriter writeInCsv = new StreamWriter(fs1, Encoding.Unicode);

                    foreach (TrendOne dt in TrendOnelist)
                    {
                        int ivalue = Convert.ToInt32(dt.value);
                        double fvalue = ivalue * 0.1;
                        if (itd<1)
                        {
                            writeInCsv.WriteLine("time"+ "\t" + "trend");  // заголовки столбцов написали разоу и хватит
                        } itd++;
                        writeInCsv.WriteLine(dt.DateTrend.Year + "." + dt.DateTrend.Month + "." + dt.DateTrend.Day + " " + dt.DateTrend.Hour + ":" + dt.DateTrend.Minute + ":" + dt.DateTrend.Second + "\t" + fvalue); // первый столбец - дата, второй - данные
                    }
                    writeInCsv.Close(); // закончили записывать
                    fs1.Close();
                }

                else if (Ntrd == 2)  // аналогично и для случая с двумя трендами, но еще с выбором кол-ва трендов для записи
                {
                    TrendTwolist.Sort((x, y) => x.DateTrend.CompareTo(y.DateTrend));  //сортируем время

                    FileStream fs1 = new FileStream(saveFileDialog1.FileName + ".csv", FileMode.Create);
                    StreamWriter writeInCsv = new StreamWriter(fs1, Encoding.Unicode);

                    foreach (TrendTwo dt in TrendTwolist)
                    {
                        int ivalue1 = Convert.ToInt32(dt.value1);
                        int ivalue2 = Convert.ToInt32(dt.value2);
                        double fvalue1 = ivalue1 * 0.1;
                        double fvalue2 = ivalue2 * 0.1;

                        if (NtrdStreams == 1)
                        {
                            if (itd < 1)
                            {
                                writeInCsv.WriteLine("time" + "\t" + "trend1");
                            } 
                            writeInCsv.WriteLine(dt.DateTrend.Year + "." + dt.DateTrend.Month + "." + dt.DateTrend.Day + " " + dt.DateTrend.Hour + ":" + dt.DateTrend.Minute + ":" + dt.DateTrend.Second + "\t" + fvalue1);
                        }
                        else if (NtrdStreams == 2)
                        {
                            if (itd < 1)
                            {
                                writeInCsv.WriteLine("time" + "\t" + "trend2");
                            } 
                            writeInCsv.WriteLine(dt.DateTrend.Year + "." + dt.DateTrend.Month + "." + dt.DateTrend.Day + " " + dt.DateTrend.Hour + ":" + dt.DateTrend.Minute + ":" + dt.DateTrend.Second + "\t" + fvalue2);
                        }
                        else if (NtrdStreams == 3)
                        {
                            if (itd < 1)
                            {
                                writeInCsv.WriteLine("time" + "\t" + "trend1" + "\t" + "trend2");
                            } 
                            writeInCsv.WriteLine(dt.DateTrend.Year + "." + dt.DateTrend.Month + "." + dt.DateTrend.Day + " " + dt.DateTrend.Hour + ":" + dt.DateTrend.Minute + ":" + dt.DateTrend.Second + "\t" + fvalue1 + "\t" + fvalue2);
                        }
                        itd++;
                    }
                    writeInCsv.Close();
                    fs1.Close();
                }
            }
        }

        private void label11_Click(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
            tm = 1;
            CurPNT = 1;
            cnt = 1;

            comboBox1.Text = "";
            comboBox2.Text = "";
            label13.Visible = false;
            label5.Visible = false;
            label30.Visible = false;
            ix = 1;
            label14.Text = "0";
            label15.Text = "0";
            label19.Text = "0";
            label12.Text = "0";
            label25.Text = "0";
            itd = 0;

            //НАДО ОЧИСТКУ ЛИСТОВ СДЕЛАТЬ

        }

        private void button4_Click(object sender, EventArgs e)  // загрузка списка доступных для скачивания трендов, записан в отдельном файле с параметрами
        {
            foreach (string line in File.ReadAllLines(@"install.csv"))
            {
                var values = line.Split(';');
                var p15 = new installer() { name = Convert.ToString(values[0]), trends = Convert.ToString(values[1]), lengthOfTrnd = Convert.ToString(values[2]), startReg = Convert.ToString(values[3]) };
                installerList.Add(p15);
                comboBox1.Items.Add(Convert.ToString(values[0]));  // накидали из файла в раскрываищйися список верхний
            }
        }
       
        private void справкаToolStripMenuItem1_Click(object sender, EventArgs e) // вызов справки
        {
            Support form2 = new Support();
            form2.Show();
        }

        private void button5_Click(object sender, EventArgs e) // скачивание следующей части
        {
            tm = 1;
            CurPNT = 1;
            cnt = 1;
            LengthTRND = LengthTRND - 6500; // длина уменьшается на длину одной части.
            timer1.Start();

        }
    }
}
