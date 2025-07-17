namespace CertificateTelegramBot_Enums
{
    public enum UserRole //перечисление ролей
    {
        Student,
        Admin
    }

    //перечисление состояний пользователя
    public enum UserState
    {
        AwaitingRegistrationFullName,
        AwaitingRegistrationGroup,
        AwaitingRegistrationPhoneNumber,
        AwaitingAdminStudentStatus,
        AwaitingCertificateDestination,
        AwaitingCertificateSearchSurname
    }
    // перечисление для статуса справки
    public enum CertificateStatus 
    {
        Pending, // в ожидании
        Completed,// завершена
        InProgress,
        Rejected,
        Ready
    }

}
