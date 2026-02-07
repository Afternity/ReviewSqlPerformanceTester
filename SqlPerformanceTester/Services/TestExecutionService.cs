using System.Collections.Concurrent;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Text.RegularExpressions;
using SqlPerformanceTester.Models;
using SqlPerformanceTester.Services.Interfaces;

namespace SqlPerformanceTester.Services;

public class TestExecutionService : ITestExecutionService
{
    // ════════════════════════════════════════════════════════════════════════════════
    // 🔍 CODE REVIEW: Анализ параллельного выполнения запросов
    // ════════════════════════════════════════════════════════════════════════════════
    //
    // 📝 Ты говорил:
    //    "У меня на одну БД только один connection"
    //    "Я не вызываю Task.WhenAll"
    //    "Я просто через цикл обращаюсь к БД"
    //
    // 🤔 Давай проверим, так ли это на самом деле:
    //
    // ❓ Где в коде создаётся SqlConnection?
    // ❓ Сколько раз он создаётся?
    // ❓ Есть ли в коде Task.WhenAll? (поиск по файлу: Ctrl+F)
    // ❓ Запросы выполняются один за другим или могут идти параллельно?
    //
    // 💭 Пройдись по коду и попробуй найти ответы.
    // 💭 Может быть, твоё понимание кода отличается от того, что реально происходит?
    //
    // ════════════════════════════════════════════════════════════════════════════════

    public async Task<IReadOnlyCollection<TestResult>> RunLoadTestAsync(
        TestConfiguration config,
        Action<string> updateCountdown,
        CancellationToken cancellationToken)
    {
        var results = new ConcurrentBag<TestResult>();
        int maxAccountIdValue = GetMaxPercentMarkerValue(config.Query);

        var connectionString =
            $"Server={config.Server};Database={config.Database};Integrated Security=true;TrustServerCertificate=true;";

        await TestConnectionAsync(connectionString, cancellationToken);

        var testEndTime = DateTime.UtcNow.AddSeconds(config.TestDuration);
        var tasks = new List<Task>();

        // ════════════════════════════════════════════════════════════════
        // 🤔 ВОПРОСЫ ДЛЯ РАЗМЫШЛЕНИЯ:
        // ════════════════════════════════════════════════════════════════
        //
        // ❓ Сколько раз выполнится этот цикл for?
        // ❓ Что делает Task.Run? Когда начинает выполняться задача?
        // ❓ Цикл создаёт задачи последовательно... а как они выполняются?
        //
        // 💭 Попробуй представить: если threadsCount = 10, сколько Task.Run вызовется?
        // 💭 Все эти Task.Run вызовы - они ждут друг друга или нет?
        //
        // ════════════════════════════════════════════════════════════════

        for (int threadIndex = 0; threadIndex < config.ThreadsCount; threadIndex++)
        {
            var threadId = threadIndex + 1;

            // ════════════════════════════════════════════════════════════════
            // 🤔 ВОПРОСЫ ПРО Task.Run:
            // ════════════════════════════════════════════════════════════════
            //
            // ❓ Для чего обычно используется Task.Run?
            // ❓ Task.Run планирует задачу на ThreadPool... а что внутри async лямбды?
            // 💭 Посмотри внутрь: connection.OpenAsync(), ExecuteReaderAsync()...
            // 💭 Это CPU-bound операции (вычисления) или I/O-bound (ожидание)?
            //
            // 🤔 Что происходит:
            //    1. Task.Run планирует задачу на ThreadPool
            //    2. Задача начинается
            //    3. Сразу же await connection.OpenAsync() - уходит в ожидание I/O
            //    4. Поток из ThreadPool освобождается...
            //
            // 💭 Может быть, есть способ запустить async метод напрямую без Task.Run?
            // 💭 Нужен ли здесь Task.Run, если всё внутри - это await (I/O)?
            //
            // ════════════════════════════════════════════════════════════════

            var task = Task.Run(async () =>
            {
                // ❓ Где находится эта строка? Внутри чего?
                // ❓ Сколько раз выполнится создание SqlConnection?
                // 💭 Если у меня 10 потоков... сколько connection будет создано?
                // 💭 Это один общий connection для всех или у каждого свой?
                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync(cancellationToken);

                var accountId = 0;

                // ❓ Этот while блокирует весь метод RunLoadTestAsync?
                // ❓ Или он блокирует только ЭТУ задачу (Task)?
                // 💭 А что происходит с ДРУГИМИ задачами, пока эта в цикле while?
                while (DateTime.UtcNow < testEndTime && !cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        // ❓ Когда выполняется await, что происходит с этой задачей?
                        // ❓ Останавливается ли вся программа? Или только эта Task?
                        // ❓ Могут ли ДРУГИЕ Task работать пока эта ждёт ответа от БД?
                        //
                        // 💭 Подумай: если у меня 10 Task, и в каждой есть await в цикле...
                        // 💭 Сколько запросов могут выполняться ОДНОВРЕМЕННО в БД?
                        // 💭 Это последовательно (один за другим) или параллельно?
                        await ExecuteQueryAsync(connection, config.Query, accountId, threadId, results, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Thread error: {ex.Message}");
                    }

                    if (maxAccountIdValue > 1)
                    {
                        accountId++;
                        if (accountId >= maxAccountIdValue)
                        {
                            accountId = 0;
                        }
                    }
                }
            }, cancellationToken);

            tasks.Add(task);
        }

        // ❓ А это что делает?
        // ❓ Ты говорил, что у тебя нет Task.WhenAll... а что тогда здесь?
        // 💭 Зачем нужен WhenAll, если задачи и так уже запущены выше?
        // 💭 Что будет, если убрать эту строку?
        //
        // 🤔 Если есть Task.WhenAll... значит есть несколько Task...
        // 🤔 А если есть несколько Task... как они работают относительно друг друга?
        await Task.WhenAll(tasks);

        // ════════════════════════════════════════════════════════════════
        // 🎯 ИТОГОВЫЕ ВОПРОСЫ:
        // ════════════════════════════════════════════════════════════════
        //
        // ❓ Сколько запросов выполнялось ОДНОВРЕМЕННО в базе данных?
        // ❓ Если пользователь указал 50 потоков, сколько connections было открыто?
        // ❓ Это нагрузочное тестирование работает корректно?
        //
        // 🤔 Подумай про альтернативы:
        // ❓ Мог бы ты использовать SemaphoreSlim? Зачем? Что это даёт?
        // 💭 SemaphoreSlim ограничивает КОЛИЧЕСТВО параллельных операций
        // 💭 Сейчас у тебя threadsCount задач работают ВСЕ параллельно
        // 💭 Нужно ли ограничение? Или текущее решение правильное?
        //
        // ❓ А TaskScheduler? Зачем он нужен?
        // 💭 TaskScheduler управляет КАК и ГДЕ выполняются задачи
        // 💭 По умолчанию используется ThreadPool
        // 💭 Нужен ли тебе кастомный планировщик здесь?
        //
        // 🤔 А что если убрать Task.Run и вынести логику в отдельный async метод?
        // 💭 Изменится ли параллельность?
        // 💭 Станет ли код быстрее/медленнее?
        //
        // ════════════════════════════════════════════════════════════════

        return results;
    }

    private static async Task TestConnectionAsync(string connectionString, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
    }

    // ════════════════════════════════════════════════════════════════
    // 🤔 ВОПРОСЫ ПРО ExecuteQueryAsync:
    // ════════════════════════════════════════════════════════════════
    //
    // ❓ Этот метод принимает connection как параметр... откуда он приходит?
    // ❓ Сколько раз этот метод может быть вызван одновременно?
    // 💭 Если у меня 10 Task, и каждая вызывает этот метод...
    // 💭 Может ли этот метод выполняться параллельно для разных Task?
    //
    // ❓ А если два разных потока используют ОДИН И ТОТ ЖЕ connection?
    // 💭 SqlConnection - thread-safe или нет?
    // 💭 Может ли один connection выполнять несколько запросов одновременно?
    //
    // 🤔 Подумай про _results.Add(result) внизу:
    // ❓ Если 10 потоков одновременно вызывают Add()... это безопасно?
    // ❓ Какой тип у _results? (посмотри строку 16)
    // 💭 Почему именно ConcurrentBag, а не обычный List?
    //
    // ════════════════════════════════════════════════════════════════

    private static async Task ExecuteQueryAsync(
        SqlConnection connection,
        string queryTemplate,
        int accountId,
        int threadId,
        ConcurrentBag<TestResult> results,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new TestResult
        {
            AccountId = accountId,
            ThreadId = threadId,
            Timestamp = DateTime.UtcNow
        };

        try
        {
            var query = ReplacePercentMarkers(queryTemplate, accountId);

            // ❓ Создаётся SqlCommand с connection из параметра...
            // 💭 Если два потока одновременно создадут команду с одним connection?
            // 💭 Может ли один connection обрабатывать две команды параллельно?
            await using var command = new SqlCommand(query, connection)
            {
                CommandTimeout = 30
            };

            // ❓ Что происходит здесь с потоком выполнения?
            // 💭 Поток блокируется? Или освобождается и ждёт ответа от БД?
            // 💭 Пока этот await ждёт... могут ли ДРУГИЕ Task работать?
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {

            }

            stopwatch.Stop();
            result.ExecutionTimeMs = stopwatch.Elapsed.TotalMilliseconds;
            result.IsError = false;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.ExecutionTimeMs = stopwatch.Elapsed.TotalMilliseconds;
            result.IsError = true;
            result.ErrorMessage = ex.Message;
        }

        // ❓ ConcurrentBag - зачем? Чем отличается от List<T>?
        // 💭 Что будет, если 10 потоков одновременно вызовут Add()?
        results.Add(result);
    }

    private static int GetMaxPercentMarkerValue(string query)
    {
        int maxPercentCount = 0;
        int currentPercentCount = 0;

        foreach (var ch in query)
        {
            if (ch == '%')
            {
                currentPercentCount++;
                maxPercentCount = Math.Max(maxPercentCount, currentPercentCount);
            }
            else
            {
                currentPercentCount = 0;
            }
        }

        return maxPercentCount > 0 ? (int)Math.Pow(10, maxPercentCount) : 1;
    }

    private static string ReplacePercentMarkers(string query, int counter)
    {
        return Regex.Replace(query, "%+", match =>
        {
            var percentCount = match.Value.Length;
            var maxValue = (int)Math.Pow(10, percentCount);
            var value = counter % maxValue;

            return value.ToString($"D{percentCount}");
        });
    }
}
