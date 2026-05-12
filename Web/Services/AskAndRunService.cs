namespace MovieNight.Web.Services;

/// <summary>
/// Service for showing a confirmation modal and executing an action based on user response.
/// </summary>
public class AskAndRunService : IAskAndRunService
{
    private TaskCompletionSource<bool>? _tcs;
    private Func<Task>? _pendingAction;

    public event Action<string, string>? OnShowModal;
    public event Action? OnHideModal;

    /// <summary>
    /// Shows a confirmation modal with the given message and executes the provided async function if confirmed.
    /// </summary>
    public async Task ConfirmAndRunAsync(string message, Func<Task> action, string title = "Confirm")
    {
        _tcs = new TaskCompletionSource<bool>();
        _pendingAction = action;

        OnShowModal?.Invoke(title, message);

        bool confirmed = await _tcs.Task;
        OnHideModal?.Invoke();

        if (confirmed && _pendingAction != null)
        {
            await _pendingAction();
        }

        _pendingAction = null;
    }

    /// <summary>
    /// Shows a confirmation modal with the given message and executes the provided synchronous function if confirmed.
    /// </summary>
    public void ConfirmAndRun(string message, Action action, string title = "Confirm")
    {
        ConfirmAndRunAsync(message, () =>
        {
            action();
            return Task.CompletedTask;
        }, title).ConfigureAwait(false);
    }

    /// <summary>
    /// Called by the modal component when the user confirms.
    /// </summary>
    internal void Confirm()
    {
        _tcs?.SetResult(true);
    }

    /// <summary>
    /// Called by the modal component when the user cancels.
    /// </summary>
    internal void Cancel()
    {
        _tcs?.SetResult(false);
    }
}
