namespace MovieNight.Web.Services;

/// <summary>
/// Service for showing a confirmation modal and executing an action based on user response.
/// </summary>
public interface IAskAndRunService
{
    /// <summary>
    /// Shows a confirmation modal with the given message and executes the provided function if confirmed.
    /// </summary>
    /// <param name="message">The confirmation message to display</param>
    /// <param name="action">The action to execute if the user confirms</param>
    /// <param name="title">Optional title for the modal (default: "Confirm")</param>
    /// <returns>A task that completes when the user responds to the confirmation</returns>
    Task ConfirmAndRunAsync(string message, Func<Task> action, string title = "Confirm");
    
    /// <summary>
    /// Shows a confirmation modal with the given message and executes the provided function if confirmed.
    /// </summary>
    /// <param name="message">The confirmation message to display</param>
    /// <param name="action">The action to execute if the user confirms</param>
    /// <param name="title">Optional title for the modal (default: "Confirm")</param>
    void ConfirmAndRun(string message, Action action, string title = "Confirm");
}
