using CertificateTelegramBot_Enums;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace CertificateTelegramBot_Main.Data
{
    [Table("Users")] //таблица для самих студентов
    public class User
    {
        [Key]
        public long TelegramId { get; set; } //тгайди
        public string? Name { get; set; } //имя студента
        public string? Surname { get; set; } //фамилия студента
        public string? Patronymic { get; set; } //отчество студента
        public string? GroupName { get; set; } //название группы
        public string? PhoneNumber { get; set; } //номер телефона студента
        public bool IsAuthorised { get; set; } = false; //проверка авторизован или нет
        public UserRole Role { get; set; } = UserRole.Student; //роль пользователя (студент или админ)
    }
}
