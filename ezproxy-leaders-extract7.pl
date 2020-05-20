use strict;
use warnings;
use v5.14.2;
use DateTime;
## дать в консоли: perl -MCPAN -e 'install DateTime::Format::Strptime'
use DateTime::Format::Strptime;

# дата для извлечения из файла лога инфы только за определенную дату
my $strdate_argv = $ARGV[0]; # аргумент -- дата в формате YYYY-mm-dd
# будем юзать либу для валидации дат и преобразования в dd/Mmm/YYYY
my $dt_format = DateTime::Format::Strptime->new(
    pattern  => '%Y-%m-%d',
    on_error => 'croak',
);
# читаем время в специальную переменную
my $dt = $dt_format->parse_datetime($strdate_argv);
# дата для парсинга
   $dt_format = DateTime::Format::Strptime->new(
    pattern  => '%d\/%b\/%Y',
    on_error => 'croak',
);
# дата для парсинга -- в строку
my $strdate_parsing = $dt_format->format_datetime($dt);
# дата для шапки CSV-файла
   $dt_format = DateTime::Format::Strptime->new(
    pattern  => '%d/%b/%Y',
    on_error => 'croak',
);
# дата для шапки -- в строку
my $strdate_head = $dt_format->format_datetime($dt);
# дата для имени входного файла
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
if (my @strm = $row =~ m/$pattern/) { $hosts1{$2}{SIZE} += $4; $hosts1{$2}{IP}{$1}++; }

}

close $fh;

# выводим результат

my $o_filename = $strdate_argv.".csv";

open($fh, '>', $o_filename) or die "Could not open file '$o_filename' $!";
# шапка csv
#   комментарий с датой отчета
## printf $fh "# DATE IS %s\n",$strdate_head;
#   шапка таблицы
printf $fh "READER,SIZE,IP\n";
# сортируем строки хеша по убыванию значений (лидеры по скачиванию будут наверху)
for my $key (sort { $hosts1{$b}{SIZE} <=> $hosts1{$a}{SIZE} } keys %hosts1) {
# выводим
  printf $fh "%s,%s,%s\n",$key,$hosts1{$key}{SIZE}, join (' ', keys %{$hosts1{$key}{IP}});
}

close $fh;
