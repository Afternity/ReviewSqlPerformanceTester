namespace SqlPerformanceTester.Common.Constants;

public static class ValidationMessages
{
    public const string ServerRequired = "Укажите адрес сервера";
    public const string DatabaseRequired = "Укажите имя базы данных";
    public const string QueryRequired = "Укажите SQL-запрос";
    public const string ThreadsRangeError = "Количество потоков должно быть от 1 до 100";
    public const string DurationMinError = "Длительность теста должна быть больше 0 секунд";
    public const string OutputFileRequired = "Укажите путь к файлу результатов";
}
