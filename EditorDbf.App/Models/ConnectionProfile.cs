using EditorDbf.App.Infrastructure;

namespace EditorDbf.App.Models;

public sealed class ConnectionProfile : ObservableObject
{
    private string _name = string.Empty;
    private string? _customName;
    private bool _exists = true;

    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string? CustomName
    {
        get => _customName;
        set
        {
            if (SetProperty(ref _customName, value))
            {
                OnPropertyChanged(nameof(EffectiveName));
                OnPropertyChanged(nameof(DisplayName));
            }
        }
    }

    public bool Exists
    {
        get => _exists;
        set => SetProperty(ref _exists, value);
    }

    public string FolderPath { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public string EffectiveName => !string.IsNullOrWhiteSpace(CustomName) ? CustomName : Name;

    public string DisplayName => $"{EffectiveName} ({FolderPath})";
}
