using Optimizer.Core.Actions;

namespace Optimizer.Core.Tests;

/// <summary>A whole registry key: saved with every value and subkey, never half (too large means not offered), summarized for people.</summary>
public class RegistryKeyActionTests
{
    [Fact]
    public void AKeyOverTheLimitIsNotOffered()
    {
        using var fx = new EngineFixture();
        for (var i = 0; i <= RegistryKeyAction.MaxValues; i++)
            RegistryValue.Write(fx.Registry, Hive.Machine, @"SOFTWARE\Fixture\Huge\Part" + (i / 500), "v" + i, "dword", "1");
        var action = new RegistryKeyAction { Hive = Hive.Machine, Path = @"SOFTWARE\Fixture\Huge" };
        Assert.Null(action.Read(fx.Context));
        Assert.Equal(ActionState.Unsupported, action.State(fx.Context));
    }

    [Fact]
    public void AMissingKeyCountsAsDone()
    {
        using var fx = new EngineFixture();
        var action = new RegistryKeyAction { Hive = Hive.Machine, Path = @"SOFTWARE\Fixture\NotThere" };
        Assert.Equal(ActionState.Applied, action.State(fx.Context));
    }

    [Fact]
    public void TheConfirmationShowsTheKeysSizeNotItsData()
    {
        using var fx = new EngineFixture();
        RegistryValue.Write(fx.Registry, Hive.User, @"Software\Fixture\Menu", "A", "string", "x");
        RegistryValue.Write(fx.Registry, Hive.User, @"Software\Fixture\Menu\command", "", "string", "notepad.exe");
        var action = new RegistryKeyAction { Hive = Hive.User, Path = @"Software\Fixture\Menu" };
        var line = action.Change(fx.Context);
        Assert.Equal("@regkey:2:1", line.Before);
        Assert.Equal("key with 2 values and 1 subkeys", Docs.Labels.Current.Display("en", line.Before));
        Assert.Equal("Schlüssel mit 2 Werten und 1 Unterschlüsseln", Docs.Labels.Current.Display("de", line.Before));
    }
}
