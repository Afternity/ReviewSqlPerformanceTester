namespace SqlPerformanceTester.Common.Constants;

public static class AppMessages
{
    public const string ValidationError = "Ошибка валидации";
    public const string TestStopped = "Тестирование остановлено пользователем";
    public const string TestStoppedTitle = "Остановка";
    public const string ErrorTitle = "Ошибка";
    public const string TestCompleteTitle = "Результаты тестирования";
    public const string SaveError = "Не удалось сохранить результаты: {0}";

    public static string TestCompleteMessage(int total, int success, int errors, string filePath) =>
        $"Тестирование завершено!\n\n" +
        $"Всего запросов: {total}\n" +
        $"Успешных: {success}\n" +
        $"Ошибок: {errors}\n" +
        $"Результаты сохранены в:\n{filePath}";
}
