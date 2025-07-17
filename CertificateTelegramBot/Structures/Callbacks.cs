namespace CertificateTelegramBot_Callbacks
{
    public static class Callbacks
    {
        //чисто для визуального различия между коллбеками
        private const string MenuPrefix = "menu_";
        private const string AdminPrefix = "admin_";
        private const string OrderPrefix = "order_";
        private const string HelpPrefix = "help_";

        //для главного меню
        public const string OrderCertificate = MenuPrefix + "order_cert";
        public const string MyCertificates = MenuPrefix + "my_certs";
        public const string Help = MenuPrefix + "help";
        public const string BackToMainMenu = AdminPrefix + "back_to_main";

        //для меню заказа справок "1 уровень"
        public const string OrderSimple = OrderPrefix + "simple"; // обычная (по месту требования)
        public const string OrderSpecial = OrderPrefix + "special_menu"; // переход в меню специальных справок

        //для меню заказа справок "2 уровень" (спец. справки, подвиды)
        public const string OrderPfr = OrderPrefix + "pfr"; //справка Пенсионный фонд России
        public const string OrderSfr = OrderPrefix + "sfr"; //справка Социальный фонд России
        public const string OrderEfs = OrderPrefix + "efs"; //справка ЕФС-1 (Единая форма сведений)
        public const string OrderVoenkomat = OrderPrefix + "voenkomat"; //справка в военкомат
        public const string OrderCall = OrderPrefix + "call"; // справка-вызов

        //общее для справок
        public const string ViewPendingCertificates = AdminPrefix + "view_certs"; // Посмотреть заявки
        public const string ViewCertificateDetails = AdminPrefix + "cert_details_"; // Посмотреть детали (с параметром)
        public const string ChangeCertificateStatus = AdminPrefix + "cert_status_"; // Изменить статус (с параметром)
        public const string PageCertificates = AdminPrefix + "cert_page_"; // Пагинация (с параметром)
        public const string BackToOrderMenu = OrderPrefix + "back_to_order_menu"; // назад к выбору типа (Обычная/специальная)
        public const string BackToMainMenuFromOrder = OrderPrefix + "back_to_main"; // назад в главное меню
        public const string MyCertificatesPage = MenuPrefix + "my_certs_page_";

        //для меню помощи
        public const string HelpCertificateTypes = HelpPrefix + "types";
        public const string HelpDeadlines = HelpPrefix + "deadlines";
        public const string BackToMainMenuFromHelp = HelpPrefix + "back_to_main"; // Кнопка "назад" из меню помощи

        //для админов
        public const string ToggleAdminMode = AdminPrefix + "toggle_mode";
        public const string AdminIsStudent = AdminPrefix + "is_student_yes";
        public const string AdminIsNotStudent = AdminPrefix + "is_student_no";
        public const string ListUsersToApprove = AdminPrefix + "list_to_approve";
        public const string BackToAdminMenu = AdminPrefix + "back_to_admin_menu";
        public const string BackToMainMenuFromAdmin = AdminPrefix + "back_to_main";
        public const string SearchCertificates = AdminPrefix + "search_certificates";
        public const string SearchCertificatesPage = AdminPrefix + "search_page";
    }
}
