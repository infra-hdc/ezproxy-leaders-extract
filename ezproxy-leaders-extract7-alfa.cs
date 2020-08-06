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
                public long all_ip_size; // трафик к читателю со всех IP-адресов
                public Dictionary<string, long> ip_addrs_size; // трафик к читателю, распределенный по IP-адресам
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
            DateTime dateValue; // дата-время во внутреннем формате C#
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
            Regex regex = new Regex(pattern); // делаем объект для работы регулярного выражения с нашим шаблоном
            
            //Console.WriteLine("{0} {1}", strdate_parsing, pattern);
            //return(0);
            
            string i_path = $".{Path.DirectorySeparatorChar}ezp"+dateValue.ToString("yyyyMM")+".log"; // файл со входными данными
            try
            {
                using (StreamReader sr = new StreamReader(i_path, System.Text.Encoding.Default))
                {
                    string line; // текущая строка
                    while ((line = sr.ReadLine()) != null) // цикл по всем строкам, пока не EOF
                    {
              //          Console.WriteLine(line);
                        MatchCollection matches = regex.Matches(line); // натравливаем регулярку на нашу текущую строку файла
                        //Console.WriteLine("{0}: {1}", matches.Count, line);
                        if (matches.Count == 1) // если совпадение нашлось
                        //foreach (Match match in matches)
                        {
                        //    Console.WriteLine(match.Key, match.Value);
                            //extracted_stack.Push(matches[0].Groups);
                            GroupCollection groups = matches[0].Groups; // извлекаем группу данных на выходе регулярки
                            //Console.WriteLine(groups["data_size"].Value);
                            if (!for_report.ContainsKey(Convert.ToInt64(groups["reader"].Value))) // если в массиве для результатов извлечения данных еще нет этого читателя
                            {
                                // добавляем объект для читателя
                                for_report.Add(Convert.ToInt64(groups["reader"].Value), new ForReport_helper());
                            }
                            // увеличиваем счетчик со всех IP-адресов для конкретного читателя
                            for_report[Convert.ToInt64(groups["reader"].Value)].all_ip_size += Convert.ToInt64(groups["data_size"].Value);
                            // если не инициализирован объект обсчета по каждому из IP-адресов
                            if (for_report[Convert.ToInt64(groups["reader"].Value)].ip_addrs_size == null)
                            {
                                // то инициализируем этот объект
                                for_report[Convert.ToInt64(groups["reader"].Value)].ip_addrs_size = new Dictionary<string, long>();
                            }
                            // если в словаре нету данного конкретного (текущего) IP-адреса
                            if (!for_report[Convert.ToInt64(groups["reader"].Value)].ip_addrs_size.ContainsKey(groups["ip_addr"].Value))
                            {
                                // то добавляем его
                                for_report[Convert.ToInt64(groups["reader"].Value)].ip_addrs_size.Add(groups["ip_addr"].Value, Convert.ToInt64(groups["data_size"].Value));
                            }
                            else
                            {
                                // иначе, если уже есть, увеличиваем счетчик трафика этого IP-адреса
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

            // генерация отчета

            // печатаем шапку CSV-файла
            Console.WriteLine("READER,SIZE,IP");
            // цикл по каждому из читателей, сортировка -- по убыванию входящего трафика
            foreach (KeyValuePair<long, ForReport_helper> kvp in for_report.OrderByDescending(key => key.Value.all_ip_size))
            //или кратко foreach (var kvp in array)
            {
                // выводим номер читательского билета и весь трафик со всех IP-адресов
                Console.Write("{0},{1},", kvp.Key, kvp.Value.all_ip_size.ToString());
                // переменная i -- для контроля, последний ли IP-адрес в списке
                int i = 1; // https://stackoverflow.com/a/6199560/2645896
                // проходим циклом по всем IP-адресам читателя
                foreach (KeyValuePair<string, long> kvp2 in kvp.Value.ip_addrs_size.OrderByDescending(key => key.Value))
                {
                    // если больше одного IP-адреса, с которого ходил читатель
                    if (kvp.Value.ip_addrs_size.Count > 1)
                    {
                    // если трафик есть, то есть не равен нулю
                    if (kvp.Value.all_ip_size > 0)
                    {
                        // печатаем IP-адрес и в скобках -- сколько процентов трафика на него пришло
                        Console.Write("{0}({1:0}%)", kvp2.Key, (double)kvp2.Value*100/kvp.Value.all_ip_size);
                    }
                    else
                    {
                        // если же трафика нет, но IP-адрес не один, то пишем IP-адрес и 0 без знака процента
                        Console.Write("{0}({1})", kvp2.Key, 0);
                    }
                    }
                    else
                    {
                        // если у читателя всего один IP-адрес, то в скобках не указываем процент трафика, который с него пришел, так как очевидно, при этом всегда будет 100%
                        Console.Write("{0}", kvp2.Key);
                    }
                    //if (!(kvp2 == kvp.Value.ip_addrs_size.Last())) { Console.Write(" "); }
                    // если не последний элемент в списке IP-адресов
                    if (i < kvp.Value.ip_addrs_size.Count) //Use count or length as supported by your collection
                    {
                        // то добавляем разделитель -- пробел
                        //NOT last element
                        Console.Write(" ");
                        i++;
                    }
                    else
                        {  }
                }
                // добавляем конец строки в вывод результата
                Console.WriteLine();
            }
            return(0);
        }
    }
}
