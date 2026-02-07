using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentValidation;
using SqlPerformanceTester.Common.Constants;
using SqlPerformanceTester.Models;
using SqlPerformanceTester.Services.Interfaces;

namespace SqlPerformanceTester.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ITestExecutionService _testExecutionService;
    private readonly IResultSaveService _resultSaveService;
    private readonly IDialogService _dialogService;
    private readonly IValidator<TestConfiguration> _validator;

    private CancellationTokenSource? _cancellationTokenSource;
    private DispatcherTimer? _countdownTimer;
    private DateTime _testEndTime;
    private IReadOnlyCollection<TestResult>? _testResults;

    [ObservableProperty]
    private string _server = "COMF-AXDEV";

    [ObservableProperty]
    private string _database = "HCS_Ax2009_Dev";

    [ObservableProperty]
    private string _query = "select * from HCSMETERTABLE where METERID = 'П00489%%6' and DATAAREAID = '0c10'";

    [ObservableProperty]
    private string _threadsCount = "50";

    [ObservableProperty]
    private string _testDuration = "10";

    [ObservableProperty]
    private string _outputFile = FileConstants.DefaultOutputFile;

    [ObservableProperty]
    private string _countdown = "--";

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private bool _canStart = true;

    [ObservableProperty]
    private bool _canStop;

    [ObservableProperty]
    private bool _canExit = true;

    public MainViewModel(
        ITestExecutionService testExecutionService,
        IResultSaveService resultSaveService,
        IDialogService dialogService,
        IValidator<TestConfiguration> validator)
    {
        _testExecutionService = testExecutionService;
        _resultSaveService = resultSaveService;
        _dialogService = dialogService;
        _validator = validator;
    }

    [RelayCommand]
    private async Task StartAsync()
    {
        if (IsRunning)
            return;

        var config = CreateConfiguration();
        var validationResult = await _validator.ValidateAsync(config);

        if (!validationResult.IsValid)
        {
            var errors = string.Join("\n", validationResult.Errors.Select(e => e.ErrorMessage));
            _dialogService.ShowWarning(errors, AppMessages.ValidationError);
            return;
        }

        IsRunning = true;
        CanStart = false;
        CanStop = true;
        CanExit = false;

        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            SetupCountdownTimer(config.TestDuration);

            _testResults = await _testExecutionService.RunLoadTestAsync(
                config,
                UpdateCountdown,
                _cancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            _dialogService.ShowInfo(
                AppMessages.TestStopped,
                AppMessages.TestStoppedTitle);
        }
        catch (Exception ex)
        {
            _dialogService.ShowError(ex.Message, AppMessages.ErrorTitle);
        }
        finally
        {
            StopCountdownTimer();

            IsRunning = false;
            CanStart = true;
            CanStop = false;
            CanExit = true;

            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            if (_testResults != null && _testResults.Count > 0)
            {
                _resultSaveService.SaveResults(_testResults, config.Query, config.OutputFile);
            }
        }
    }

    [RelayCommand]
    private void Stop()
    {
        _cancellationTokenSource?.Cancel();
        StopCountdownTimer();
        Countdown = "--";
    }

    [RelayCommand]
    private void Exit()
    {
        Application.Current.Shutdown();
    }

    [RelayCommand]
    private void Browse()
    {
        var result = _dialogService.ShowSaveFileDialog(
            FileConstants.CsvFilter,
            FileConstants.CsvExtension,
            OutputFile);

        if (result != null)
            OutputFile = result;
    }

    private TestConfiguration CreateConfiguration()
    {
        return new TestConfiguration
        {
            Server = Server.Trim(),
            Database = Database.Trim(),
            Query = Query.Trim(),
            ThreadsCount = int.TryParse(ThreadsCount, out var threads) ? threads : 0,
            TestDuration = int.TryParse(TestDuration, out var duration) ? duration : 0,
            OutputFile = OutputFile
        };
    }

    private void SetupCountdownTimer(int durationSeconds)
    {
        _testEndTime = DateTime.UtcNow.AddSeconds(durationSeconds);

        _countdownTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _countdownTimer.Tick += OnCountdownTick;
        _countdownTimer.Start();

        Countdown = durationSeconds.ToString();
    }

    private void StopCountdownTimer()
    {
        _countdownTimer?.Stop();
        Countdown = "0";
    }

    private void OnCountdownTick(object? sender, EventArgs e)
    {
        var remainingTime = _testEndTime - DateTime.UtcNow;

        if (remainingTime.TotalSeconds <= 0)
        {
            Countdown = "0";
            _countdownTimer?.Stop();
        }
        else
        {
            var seconds = (int)Math.Ceiling(remainingTime.TotalSeconds);
            Countdown = seconds.ToString();
        }
    }

    private void UpdateCountdown(string value)
    {
        Countdown = value;
    }
}
