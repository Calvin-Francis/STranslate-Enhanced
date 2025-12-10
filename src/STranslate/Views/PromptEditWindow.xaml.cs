using STranslate.Plugin;
using STranslate.ViewModels;
using System.Collections.ObjectModel;

namespace STranslate.Views;

public partial class PromptEditWindow
{
    public PromptEditWindow(ObservableCollection<Prompt> prompts, List<string>? roles = default)
    {
        InitializeComponent();

        DataContext = new PromptEditViewModel(prompts, roles);
    }
}
