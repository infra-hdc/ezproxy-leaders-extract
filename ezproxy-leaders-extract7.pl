use strict;
use warnings;
use v5.14.2;
use DateTime;
## ���� � �������: perl -MCPAN -e 'install DateTime::Format::Strptime'
use DateTime::Format::Strptime;

# ���� ��� ���������� �� ����� ���� ���� ������ �� ������������ ����
my $strdate_argv = $ARGV[0]; # �������� -- ���� � ������� YYYY-mm-dd
# ����� ����� ���� ��� ��������� ��� � �������������� � dd/Mmm/YYYY
my $dt_format = DateTime::Format::Strptime->new(
    pattern  => '%Y-%m-%d',
    on_error => 'croak',
);
# ������ ����� � ����������� ����������
my $dt = $dt_format->parse_datetime($strdate_argv);
# ���� ��� ��������
   $dt_format = DateTime::Format::Strptime->new(
    pattern  => '%d\/%b\/%Y',
    on_error => 'croak',
);
# ���� ��� �������� -- � ������
my $strdate_parsing = $dt_format->format_datetime($dt);
# ���� ��� ����� CSV-�����
   $dt_format = DateTime::Format::Strptime->new(
    pattern  => '%d/%b/%Y',
    on_error => 'croak',
);
# ���� ��� ����� -- � ������
my $strdate_head = $dt_format->format_datetime($dt);
# ���� ��� ����� �������� �����
   $dt_format = DateTime::Format::Strptime->new(
    pattern  => '%Y%m',
    on_error => 'croak',
);
# ���� ��� ����� �������� ����� -- � ������
my $strdate_ifname = $dt_format->format_datetime($dt);

my $i_filename = 'ezp'.$strdate_ifname.'.log'; # �������� ���� ����, ���������
open(my $fh, '<:encoding(UTF-8)', $i_filename)
  or die "Could not open file '$i_filename' $!";

# ������ ��� �������� ����� ����
# ���:
#   $1 -- IP-�����
#   $2 -- ������ ���� ID ������, ������ "��� ������������"
#   $3 -- ����� (��� ����)
#   $4 -- ������ �������� ������� ������, ����
my $pattern  = q|^(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})|;                                # IP-�����
   $pattern .= q|\s+-|;                                                                 # ��� identd
#   $pattern .= q|\s+([a-zA-Z0-9]{15})|;                                                 # ID ������
   $pattern .= q|\s+libfl_(\d+)|;                                                       # ����� ������������� ������
   $pattern .= q|\s+\[|.$strdate_parsing.q|\:(\d{2}\:\d{2}\:\d{2})\s{1}\+\d{4}\]|;      # ����-����� (������������ ������ �����, ��� ����)
   $pattern .= q|\s+["][^"]+["]|;                                                       # URL
   $pattern .= q|\s+\d{3}|;                                                             # ��� ������ HTTP
   $pattern .= q|\s+(\d+)$|;                                                            # ������ �������� ������� ������, ����

# ������������� ������ (���) ��� �������� ������, ���� -- ����� ������������� ������, �������� -- ��������� ������ �� ����������� �����
my %hosts1 = ();

# ������ ������� ���� ���������
while (my $row = <$fh>) {
  chomp $row;

# ���� ������ ������������� ����������� ���������, ��������� ������ � ���������� �� � ������������� ������ (���)
if (my @strm = $row =~ m/$pattern/) { $hosts1{$2}{SIZE} += $4; $hosts1{$2}{IP}{$1}++; }

}

close $fh;

# ������� ���������

my $o_filename = $strdate_argv.".csv";

open($fh, '>', $o_filename) or die "Could not open file '$o_filename' $!";
# ����� csv
#   ����������� � ����� ������
## printf $fh "# DATE IS %s\n",$strdate_head;
#   ����� �������
printf $fh "READER,SIZE,IP\n";
# ��������� ������ ���� �� �������� �������� (������ �� ���������� ����� �������)
for my $key (sort { $hosts1{$b}{SIZE} <=> $hosts1{$a}{SIZE} } keys %hosts1) {
# �������
  printf $fh "%s,%s,%s\n",$key,$hosts1{$key}{SIZE}, join (' ', keys %{$hosts1{$key}{IP}});
}

close $fh;
