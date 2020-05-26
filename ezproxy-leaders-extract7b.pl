use strict;
use warnings;
use v5.14.2;
#use DateTime;
## дать в консоли: perl -MCPAN -e 'install DateTime::Format::Strptime'
use DateTime::Format::Strptime;

# 1/2 Извлечение данных (pErl)

# дата для извлечения из файла лога инфы только за определенную дату
my $strdate_argv = $ARGV[0]  # читаем входную дату, из первого аргумента командной строки
   or die "Usage: script.pl YYYY-mm-dd";
# входная дата, в формате YYYY-mm-dd
my $dt_format = DateTime::Format::Strptime->new(
    pattern  => '%Y-%m-%d',
    on_error => 'croak',
);
# читаем время в специальную переменную
my $dt = $dt_format->parse_datetime($strdate_argv);
# перезаписываем более корректно входную дату, если, например, в месяце нет ведущих нулей
   $strdate_argv = $dt_format->format_datetime($dt);

# дата для парсинга лога, в формате dd/Mmm/YYYY
   $dt_format = DateTime::Format::Strptime->new(
    pattern  => '%d\/%b\/%Y',
    on_error => 'croak',
);
# дата для парсинга лога -- в строку
my $strdate_parsing = $dt_format->format_datetime($dt);

#комментарий в шапке не выводим, поэтому этот кусок не нужен - BEGIN
=comment
# дата для шапки CSV-файла
   $dt_format = DateTime::Format::Strptime->new(
    pattern  => '%d/%b/%Y',
    on_error => 'croak',
);
# дата для шапки CSV-файла -- в строку
my $strdate_head = $dt_format->format_datetime($dt);
=cut
#комментарий в шапке не выводим, поэтому этот кусок не нужен - END

# дата для имени входного файла, в формате YYYYmm
   $dt_format = DateTime::Format::Strptime->new(
    pattern  => '%Y%m',
    on_error => 'croak',
);
# дата для имени входного файла -- в строку
my $strdate_ifname = $dt_format->format_datetime($dt);

my $i_filename = 'ezp'.$strdate_ifname.'.log'; # входящий файл лога, открываем
open(my $fh, '<:encoding(UTF-8)', $i_filename)
  or die "Could not open file '$i_filename' $!";

# шаблон для парсинга строк лога
# где:
#   $1 -- IP-адрес
#   $2 -- раньше было ID сессии, сейчас "имя пользователя"
#   $3 -- время (без даты)
#   $4 -- размер отданных клиенту данных, байт
my $pattern  = q|^(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})|;                                # IP-адрес
   $pattern .= q|\s+-|;                                                                 # имя identd
#   $pattern .= q|\s+([a-zA-Z0-9]{15})|;                                                 # ID сессии
   $pattern .= q|\s+libfl_(\d+)|;                                                       # номер читательского билета
   $pattern .= q|\s+\[|.$strdate_parsing.q|\:(\d{2}\:\d{2}\:\d{2})\s{1}\+\d{4}\]|;      # дата-время (возвращается только время, без даты)
   $pattern .= q|\s+["][^"]+["]|;                                                       # URL
   $pattern .= q|\s+\d{3}|;                                                             # Код ответа HTTP
   $pattern .= q|\s+(\d+)$|;                                                            # размер отданных клиенту данных, байт

# ассоциативный массив (хеш) для хранения отчета, ключ -- номер читательского билета, значение -- суммарный трафик за календарные сутки
my %hosts1 = ();

# читаем входной файл построчно
while (my $row = <$fh>) {
  chomp $row;

  # если строка удовлетворяет регулярному выражению, извлекаем данные и записываем их в ассоциативный массив (хеш)
  if ($row =~ m/$pattern/) {
    # по каждому пользователю, накапливаем:
    $hosts1{$2}{SIZE} += $4; # размер полученных им данных
# в версии 7b это не нужно -- BEGIN
=comment
    $hosts1{$2}{COUNT}++;    # счетчик обращений
    $hosts1{$2}{IP}{$1}++;   # и счетчик обращений с каждого из его IP-адресов 
=cut
# в версии 7b это не нужно -- END
    $hosts1{$2}{IP_SIZE}{$1} += $4; # размер полученных им данных с каждого из его IP-адресов 
  }

}

close $fh;

# 2/2 Формирование отчета (peRl)

# файл для вывода отчета, открываем
my $o_filename = $strdate_argv.".csv";
open($fh, '>', $o_filename) or die "Could not open file '$o_filename' $!";

# шапка csv
#комментарий в шапке не выводим, поэтому этот кусок не нужен - BEGIN2
=comment
#   комментарий с датой отчета
printf $fh "# DATE IS %s\n",$strdate_head;
=cut
#комментарий в шапке не выводим, поэтому этот кусок не нужен - END2
#   шапка таблицы
printf $fh "READER,SIZE,IP\n";
# сортируем строки хеша по убыванию значений (лидеры по скачиванию будут наверху)
for my $key (sort { $hosts1{$b}{SIZE} <=> $hosts1{$a}{SIZE} } keys %hosts1) {
# выводим
  # айпишники пользователя в порядке убывания размера входящего в пользователя трафика
  my @ips_desc = sort { $hosts1{$key}{IP_SIZE}{$b} <=> $hosts1{$key}{IP_SIZE}{$a} } keys %{$hosts1{$key}{IP_SIZE}};
  my $ip_out;
  # если один айпишник -- то все просто
  if (@ips_desc == 1) {
    $ip_out = $ips_desc[0];
  } else {
    # иначе получаем значения в процентах -- в версии 7b процент считается не по количествам обращений,
    # а по трафику
    my $all_zero = ($hosts1{$key}{SIZE} == 0); # суммарный трафик -- нуль?
    if (!$all_zero) { # не нуль -- высчитываем процентный вклад в каждый IP-адрес
    for my $ips_desc_i (@ips_desc) {
      $hosts1{$key}{IP_SIZE_P}{$ips_desc_i} =
        $hosts1{$key}{IP_SIZE}{$ips_desc_i} * 100 / $hosts1{$key}{SIZE};
    }}
    # добавляем вывод в массив для вывода
    for my $ips_desc_i (@ips_desc) {
      push @{$hosts1{$key}{IP_OUT}},
        (!$all_zero ? # суммарный трафик -- нуль?
          sprintf("%s\(%.0f%%\)",$ips_desc_i,$hosts1{$key}{IP_SIZE_P}{$ips_desc_i}) # тогда с добавлением %
          : sprintf("%s\(0\)",$ips_desc_i)                                          # иначе с нулями  
        );
    }
    # выводим из массива наши IP-адреса
    $ip_out = join ' ', @{$hosts1{$key}{IP_OUT}};
  }
  printf $fh "%s,%s,%s\n",                  # выводим в каждой строке:
    $key,                                   #   Номер читательского билета
    $hosts1{$key}{SIZE},                    #   Количество трафика к пользователю
    $ip_out;                                #   IP-адреса; если их больше одного,
                                            #     то указывается процент трафика на каждый из них
}

close $fh;
