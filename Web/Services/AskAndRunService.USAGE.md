## AskAndRunService Usage Examples

The `AskAndRunService` provides a confirmation modal for user actions. It's already registered as a scoped service and can be injected into any Blazor component.

### Basic Usage

Inject the service into your component:

```razor
@inject IAskAndRunService AskAndRunService

<button @onclick="DeleteItem">Delete Item</button>

@code {
    private async Task DeleteItem()
    {
        await AskAndRunService.ConfirmAndRunAsync(
            "Are you sure you want to delete this item?",
            async () =>
            {
                // This code runs only if user clicks OK
                // Perform your async action here
                await SomeApiCall();
                // Update UI, show success message, etc.
            },
            title: "Delete Item"
        );
    }
}
```

### Synchronous Action

```razor
@code {
    private void DeleteLocal()
    {
        AskAndRunService.ConfirmAndRun(
            "Delete this local item?",
            () =>
            {
                // Synchronous action
                _items.Remove(itemToDelete);
            },
            title: "Confirm Delete"
        );
    }
}
```

### With Lambda

```razor
<button @onclick="() => AskAndRunService.ConfirmAndRun(
    $\"Delete '{item.Name}'?\", 
    () => DeleteItem(item.Id),
    \"Delete\")">
    Delete
</button>

@code {
    private void DeleteItem(int id)
    {
        // Handle deletion
    }
}
```

### Advanced: Async with Error Handling

```razor
@code {
    private async Task SaveChanges()
    {
        await AskAndRunService.ConfirmAndRunAsync(
            "Save all pending changes?",
            async () =>
            {
                try
                {
                    await _apiClient.SaveAsync();
                    ShowSuccessMessage("Changes saved successfully!");
                }
                catch (Exception ex)
                {
                    ShowErrorMessage($"Failed to save: {ex.Message}");
                }
            },
            title: "Save Changes"
        );
    }
}
```

## How It Works

1. **AskAndRunService** - Manages state and triggers modal events
2. **AskAndRunHost.razor** - The modal component (placed in MainLayout)
3. When you call `ConfirmAndRunAsync()` or `ConfirmAndRun()`, the modal appears
4. If user clicks OK, the provided action/function executes
5. If user clicks Cancel, nothing happens and the task completes
6. Modal is registered in dependency injection and automatically available

## Features

- ✅ Supports both async and sync actions
- ✅ Customizable title and message
- ✅ Modal shows/hides automatically
- ✅ Awaitable - code continues after user responds
- ✅ Uses Bootstrap CSS classes (works with any Bootstrap theme)
- ✅ Properly handles component lifecycle
