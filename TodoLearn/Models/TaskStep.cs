using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TodoLearn.Models;

public sealed class TaskStep : INotifyPropertyChanged
{
    public int Id { get; set; }
    public int TaskItemId { get; set; }
    public int Position { get; set; }
    private string _text = string.Empty;
    private bool _isCompleted;
    public string Text
    {
        get => _text;
        set { if (_text == value) return; _text = value; Notify(); }
    }
    public bool IsCompleted
    {
        get => _isCompleted;
        set { if (_isCompleted == value) return; _isCompleted = value; Notify(); }
    }
    public TaskStep Copy() => new() { Id = Id, TaskItemId = TaskItemId, Position = Position, Text = Text, IsCompleted = IsCompleted };
    public event PropertyChangedEventHandler? PropertyChanged;
    private void Notify([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
