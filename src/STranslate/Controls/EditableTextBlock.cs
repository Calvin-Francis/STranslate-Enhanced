using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace STranslate.Controls;

public class EditableTextBlock : Control
{
    static EditableTextBlock()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(EditableTextBlock),
            new FrameworkPropertyMetadata(typeof(EditableTextBlock)));
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(EditableTextBlock),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public bool IsEditing
    {
        get => (bool)GetValue(IsEditingProperty);
        set => SetValue(IsEditingProperty, value);
    }

    public static readonly DependencyProperty IsEditingProperty =
        DependencyProperty.Register(nameof(IsEditing), typeof(bool), typeof(EditableTextBlock),
            new FrameworkPropertyMetadata(false));

    public ICommand? UpdateTextCommand
    {
        get => (ICommand?)GetValue(UpdateTextCommandProperty);
        set => SetValue(UpdateTextCommandProperty, value);
    }

    public static readonly DependencyProperty UpdateTextCommandProperty =
        DependencyProperty.Register(
            nameof(UpdateTextCommand),
            typeof(ICommand),
            typeof(EditableTextBlock));

    public bool DisallowSpecialCharacters
    {
        get => (bool)GetValue(DisallowSpecialCharactersProperty);
        set => SetValue(DisallowSpecialCharactersProperty, value);
    }

    public static readonly DependencyProperty DisallowSpecialCharactersProperty =
        DependencyProperty.Register(
            nameof(DisallowSpecialCharacters),
            typeof(bool),
            typeof(EditableTextBlock),
            new PropertyMetadata(false));

    private string _oldText = string.Empty;

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (GetTemplateChild("PART_TextBlock") is TextBlock tb)
        {
            tb.MouseDown += (s, e) =>
            {
                if (e.ClickCount == 2)
                {
                    IsEditing = true;
                    _oldText = tb.Text;

                    // 延迟到UI渲染后再Focus+SelectAll
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (GetTemplateChild("PART_TextBox") is TextBox box)
                        {
                            box.Focus();
                            box.SelectAll();
                        }
                    }), System.Windows.Threading.DispatcherPriority.Input);
                }
            };
        }

        if (GetTemplateChild("PART_TextBox") is TextBox box)
        {
            void CommitEdit()
            {
                if (!IsEditing) return;
                UpdateTextCommand?.Execute((_oldText, box.Text));
                IsEditing = false;
            }

            box.LostFocus += (s, e) => CommitEdit();
            box.KeyDown += (s, e) =>
            {
                switch (e.Key)
                {
                    case Key.Enter:
                        CommitEdit();
                        e.Handled = true;
                        break;
                    case Key.Escape:
                        // 取消编辑时回退原值
                        box.GetBindingExpression(TextBox.TextProperty)?.UpdateTarget();
                        IsEditing = false;
                        e.Handled = true;
                        break;
                }
            };
        }
    }
}