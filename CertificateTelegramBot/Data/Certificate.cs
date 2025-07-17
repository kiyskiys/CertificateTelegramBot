using CertificateTelegramBot_Enums;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;


namespace CertificateTelegramBot_Main.Data
{
    [Table("Certificates")] //таблица для справок
    public class Certificate
    {
        [Key]
        public int CertificateId { get; set; } //айди для справок (автоинкремент)

        [ForeignKey(nameof(User))] //используется для связи между праймари кей из таблицы класса/таблицы student
        public long UserId { get; set; } //соответственно это и есть айди студента 
        public User? User { get; set; } //навигацонное свойство

        public string? CertificateType { get; set; } // вид справки
        public string? Destination { get; set; } // куда нужна справка (напр. в какую-то организацию)
        public DateTime CreatedAt { get; set; } //когда создана справка
        public CertificateStatus Status { get; set; } = CertificateStatus.Pending; //статус справки (по умолчанию "висит")
    } 
}
