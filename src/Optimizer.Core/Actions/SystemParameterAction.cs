using System.Globalization;

namespace Optimizer.Core.Actions;

/// <summary>
/// A per-user timing of the documented SystemParametersInfo function (SPI_SETMENUSHOWDELAY, SPI_SETMOUSEHOVERTIME), set
/// with SPIF_UPDATEINIFILE and SPIF_SENDCHANGE: it takes effect at once and Windows saves it to the user profile itself,
/// instead of a registry value that is read only at the next sign-in and whose place Microsoft does not document.
/// </summary>
public sealed class SystemParameterAction : TweakAction
{
    public SystemParameter Parameter { get; init; }

    /// <summary>Milliseconds.</summary>
    public uint Value { get; init; }

    public override string TargetKey => $"spi:{Parameter}".ToLowerInvariant();

    public override TargetText TargetLabel(ActionContext c) => TargetText.Of(Parameter switch
    {
        SystemParameter.MenuShowDelay => "target.menuShowDelay",
        _ => "target.mouseHoverTime",
    });

    private static StoredValue Milliseconds(uint value) => new(true, "ms", value.ToString(CultureInfo.InvariantCulture));

    public override StoredValue Desired(ActionContext c) => Milliseconds(Value);

    public override StoredValue? Read(ActionContext c) => c.SystemParameters?.Get(Parameter) is { } value ? Milliseconds(value) : null;

    public override void Apply(ActionContext c) => Write(c, Value);

    public override void Restore(ActionContext c, StoredValue original) =>
        Write(c, uint.TryParse(original.Data, NumberStyles.None, CultureInfo.InvariantCulture, out var value) ? value
            : throw new InvalidOperationException($"no stored value for {Parameter}"));

    private void Write(ActionContext c, uint value)
    {
        if (c.SystemParameters is not { } parameters || parameters.Get(Parameter) is null)
            throw new InvalidOperationException($"{Parameter} is not available for the signed-in user");
        parameters.Set(Parameter, value);
    }

    protected override string Show(ActionContext c, StoredValue value) => value.Existed ? $"{value.Data} ms" : value.Display;
}
