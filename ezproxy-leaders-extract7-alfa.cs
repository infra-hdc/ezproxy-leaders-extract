using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions; // https://metanit.com/sharp/tutorial/7.4.php
using System.Collections.Generic; // https://docs.microsoft.com/ru-ru/dotnet/api/system.collections.generic.list-1?view=netcore-3.0 -- только не список, а стек
using System.Linq; // https://stackoverflow.com/a/61875551/2645896

namespace helloconsole0001
{
    class MainClass
    {
           class ForReport_helper // для обсчета
            {
                // long reader;
                public long all_ip_size;
                public Dictionary<string, long> ip_addrs_size;
            }
        public static int Main(string[] args)
        {
            // список, куда вставлять результаты извлечения:
            //Stack<GroupCollection> extracted_stack = new Stack<GroupCollection>(); // https://docs.microsoft.com/ru-ru/dotnet/api/system.collections.generic.list-1?view=netcore-3.0 -- только не список, а стек
         
            Dictionary<long, ForReport_helper> for_report = new Dictionary<long, ForReport_helper>(); // в словаре ключ -- номер читательского билета
            //Dictionary<string, long> extract_array = new Dictionary<string, long>();
            string strdate_argv; // дата для извлечения из файла лога инфы за месяц
            try
            {
                strdate_argv = args[0]; // читаем входную дату, из первого аргумента командной строки
            }
            catch (IndexOutOfRangeException)
            {
                Console.WriteLine("Пропущен аргумент командной строки");
                return(1);
            }
            // https://docs.microsoft.com/ru-ru/dotnet/api/system.datetime.tryparseexact?view=netframework-4.8
            // Allow a leading space in the date string.
            DateTime dateValue;
            CultureInfo enUS = new CultureInfo("en-US"); // даты в логах и на входе -- по-английски
            if (DateTime.TryParseExact(strdate_argv, "M-yyyy", enUS, // если дата на входе ок
                DateTimeStyles.None, out dateValue))
            {
                // то сообщаем об этом
                //Console.WriteLine("Выбрана дата: {0:MM-yyyy}", dateValue);
            }
            else
            {
                // иначе выходим по ошибке
                Console.WriteLine("Указана неправильная дата в аргументе командной строки: дата должна быть в формате MM-yyyy");
                return(1);
            }
            
            
            // готовимся к парсингу лога
            string strdate_parsing = dateValue.ToString(@"MMM\\\/yyyy", enUS);                                         // дата для парсинга лога -- в строку
            string  pattern  = @"^(?<ip_addr>\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})";                             // IP-адрес
                    pattern += @"\s+-";                                                                        // имя identd
                    pattern += @"\s+libfl_(?<reader>\d+)";                                                     // номер читательского билета
                    pattern += @"\s+\[\d{2}\/"+strdate_parsing+@"\:(?<time>\d{2}\:\d{2}\:\d{2})\s{1}\+\d{4}\]"; // дата-время (возвращается только время, без даты)
                    pattern += @"\s+[""][^""]+[""]";                                                           // URL
                    pattern += @"\s+\d{3}";                                                                    // Код ответа HTTP
                    pattern += @"\s+(?<data_size>\d+)$";                                                       // размер отданных клиенту данных, байт
            Regex regex = new Regex(pattern);
            
            //Console.WriteLine("{0} {1}", strdate_parsing, pattern);
            //return(0);
            
            string i_path = @".\ezp"+dateValue.ToString("yyyyMM")+".log"; // файл со входными данными
            try
            {
                using (StreamReader sr = new StreamReader(i_path, System.Text.Encoding.Default))
                {
                    string line; // текущая строка
                    while ((line = sr.ReadLine()) != null) // цикл по всем строкам, пока не EOF
                    {
              //          Console.WriteLine(line);
                        MatchCollection matches = regex.Matches(line);
                        //Console.WriteLine("{0}: {1}", matches.Count, line);
                        if (matches.Count == 1) 
                        //foreach (Match match in matches)
                        {
                        //    Console.WriteLine(match.Key, match.Value);
                            //extracted_stack.Push(matches[0].Groups);
                            GroupCollection groups = matches[0].Groups;
                            //Console.WriteLine(groups["data_size"].Value);
                            if (!for_report.ContainsKey(Convert.ToInt64(groups["reader"].Value)))
                            {
                                for_report.Add(Convert.ToInt64(groups["reader"].Value), new ForReport_helper());
                            }
                            for_report[Convert.ToInt64(groups["reader"].Value)].all_ip_size += Convert.ToInt64(groups["data_size"].Value);
                            // еще надо добавить по IP-адресам
                            // тут будет обсчет
                            if (for_report[Convert.ToInt64(groups["reader"].Value)].ip_addrs_size == null)
                            {
                                for_report[Convert.ToInt64(groups["reader"].Value)].ip_addrs_size = new Dictionary<string, long>();
                            }
                            if (!for_report[Convert.ToInt64(groups["reader"].Value)].ip_addrs_size.ContainsKey(groups["ip_addr"].Value))
                            {
                                for_report[Convert.ToInt64(groups["reader"].Value)].ip_addrs_size.Add(groups["ip_addr"].Value, Convert.ToInt64(groups["data_size"].Value));
                            }
                            else
                            {
                                for_report[Convert.ToInt64(groups["reader"].Value)].ip_addrs_size[groups["ip_addr"].Value] += Convert.ToInt64(groups["data_size"].Value);
                            }
                        }
                    }
                    sr.Close();
                }
            }
            catch (Exception e) // если ошибка при извлечении данных -- выводим
            {
                Console.WriteLine(e.Message);
                return(1);
            }
            
            // Console.ReadLine(); // чтобы посмотреть, сколько памяти скушал (в панели задач)

            // далее -- отчет по результатам извлечения данных

            // список: READER,SIZE,IP(список IP-адресов с распределением процента по трафику)
            
            //while (extracted_stack.Count > 0)
            //{
            //    GroupCollection i = extracted_stack.Pop();
            //    
            //    Console.WriteLine(i["data_size"].Value);
            //}

            //foreach (GroupCollection i in extracted_list)
            //{
                // тут вставить код отчета
                //Console.WriteLine(i["data_size"].Value);
            //}
            // тут должна быть остальная прикладная часть

            Console.WriteLine("READER,SIZE,IP");
            foreach (KeyValuePair<long, ForReport_helper> kvp in for_report.OrderByDescending(key => key.Value.all_ip_size))
            //или кратко foreach (var kvp in array)
            {
                Console.Write("{0},{1},", kvp.Key, kvp.Value.all_ip_size.ToString());
                int i = 1; // https://stackoverflow.com/a/6199560/2645896
                foreach (KeyValuePair<string, long> kvp2 in kvp.Value.ip_addrs_size.OrderByDescending(key => key.Value))
                {
                    if (kvp.Value.ip_addrs_size.Count > 1)
                    {
                    if (kvp.Value.all_ip_size > 0)
                    {
                        Console.Write("{0}({1:0}%)", kvp2.Key, (double)kvp2.Value*100/kvp.Value.all_ip_size);
                    }
                    else
                    {
                        Console.Write("{0}({1})", kvp2.Key, 0);
                    }
                    }
                    else
                    {
                        Console.Write("{0}", kvp2.Key);
                    }
                    //if (!(kvp2 == kvp.Value.ip_addrs_size.Last())) { Console.Write(" "); }

                    if (i < kvp.Value.ip_addrs_size.Count) //Use count or length as supported by your collection
                    {
                        //NOT last element
                        Console.Write(" ");
                        i++;
                    }
                    else
                        {  }
                }
                Console.WriteLine();
            }
            return(0);
        }
    }
}
