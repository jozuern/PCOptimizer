using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using Wpf.Ui.Controls;

namespace Optimizer.App.Views;

/// <summary>
/// A card that does something when clicked (opens the details, opens a group). WPF-UI's card reports itself to UI
/// Automation as a plain pane without the Invoke pattern: Narrator said "pane", and Voice Access or another assistive
/// tool could not click it by name. This one is a button with Invoke, which runs the card's command.
/// </summary>
public class ClickCard : CardControl
{
    static ClickCard() => DefaultStyleKeyProperty.OverrideMetadata(typeof(ClickCard), new FrameworkPropertyMetadata(typeof(CardControl)));

    protected override AutomationPeer OnCreateAutomationPeer() => new ClickCardPeer(this);

    private sealed class ClickCardPeer(ClickCard owner) : FrameworkElementAutomationPeer(owner), IInvokeProvider
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Button;

        protected override string GetClassNameCore() => nameof(ClickCard);

        public override object? GetPattern(PatternInterface pattern) => pattern == PatternInterface.Invoke ? this : base.GetPattern(pattern);

        public void Invoke()
        {
            if (!IsEnabled()) throw new System.Windows.Automation.ElementNotEnabledException();
            if (owner.Command is { } command && command.CanExecute(owner.CommandParameter)) command.Execute(owner.CommandParameter);
        }
    }
}
