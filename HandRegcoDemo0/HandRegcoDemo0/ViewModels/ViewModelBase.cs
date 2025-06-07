using CommunityToolkit.Mvvm.ComponentModel;

namespace HandRegcoDemo0.ViewModels;

public class ViewModelBase : ObservableObject
{
    public virtual void Dispose()
    {
        // Logic dọn dẹp chung nếu có
    }
}
