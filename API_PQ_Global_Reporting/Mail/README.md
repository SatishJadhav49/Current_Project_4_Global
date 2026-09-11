# Mail Notification System

This folder contains the scalable mail notification system for the MCAR application.

## Structure

```
Mail/
├── MailNotificationService.cs          # Main service with all mail methods
├── MailDTOs.cs                         # Data Transfer Objects for mail system
├── Services/                           # Individual mail service implementations
│   ├── BaseMailService.cs             # Base class with common functionality
│   └── AssignedToMailService.cs        # Service for concern assignment notifications
└── Templates/                          # HTML email templates
    └── assigned-concern-template.html   # Template for concern assignment emails
```

## How to Use

### 1. From your controller or service, inject MailNotificationService:

```csharp
public class YourController : ControllerBase
{
    private readonly MailNotificationService _mailService;
    
    public YourController(MailNotificationService mailService)
    {
        _mailService = mailService;
    }
    
    // Send concern assignment notification
    public async Task<IActionResult> AssignConcern(decimal concernId)
    {
        await _mailService.SendMailForConcernAsync(concernId);
        return Ok();
    }
}
```

### 2. Register the service in Program.cs:

```csharp
builder.Services.AddScoped<MailNotificationService>();
```

## Adding New Mail Notifications

To add a new mail notification type:

1. **Create a new service** in the `Services/` folder (e.g., `ReminderMailService.cs`)
2. **Inherit from BaseMailService** for common functionality
3. **Create an HTML template** in the `Templates/` folder
4. **Add a method** in `MailNotificationService.cs` to call your new service
5. **Update the constructor** in `MailNotificationService.cs` to initialize your new service

### Example: Adding Reminder Mail

1. Create `Services/ReminderMailService.cs`:
```csharp
public class ReminderMailService : BaseMailService
{
    public ReminderMailService(string connectionString, SmtpSettings smtpSettings, SystemSettings systemSettings) 
        : base(connectionString, smtpSettings, systemSettings) { }
    
    public async Task<bool> SendMailAsync(decimal concernId)
    {
        // Implementation here
    }
}
```

2. Add template `Templates/reminder-template.html`

3. Update `MailNotificationService.cs`:
```csharp
private readonly ReminderMailService _reminderMailService;

// In constructor:
_reminderMailService = new ReminderMailService(_connectionString, _smtpSettings, _systemSettings);

// Update method:
public async Task<bool> SendReminderMailAsync(decimal concernId)
{
    return await _reminderMailService.SendMailAsync(concernId);
}
```

## Features

- **Scalable**: Easy to add new notification types
- **Template-based**: HTML templates for easy customization
- **Reusable**: Common functionality in BaseMailService
- **Simple**: No complex interfaces, just inheritance
- **Maintainable**: Clear separation of concerns

## Current Mail Types

- ✅ **Concern Assignment**: Notifies when a concern is assigned to an employee
- 🚧 **Reminder**: TODO - For overdue actions
- 🚧 **Approval**: TODO - For approval notifications  
- 🚧 **Escalation**: TODO - For escalated concerns
- 🚧 **Closure**: TODO - For closed concerns
