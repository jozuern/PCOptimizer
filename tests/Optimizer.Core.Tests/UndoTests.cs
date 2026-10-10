using System.Text.Json;
using Optimizer.Core.Actions;
using Optimizer.Core.Backup;
using Optimizer.Core.Tweaks;

namespace Optimizer.Core.Tests;

/// <summary>Undo in the cases that went wrong before: removed tweaks, plan switches, keys that existed, value types.</summary>
public class UndoTests
{
    private static readonly Facts Facts = new Facts().Set("os.build", 26300).Set("elevated", true).Set("system.laptop", false);
    private static readonly ApplyOptions Options = new() { ExpertMode = true, ContinueWithoutRestorePoint = true };

    private static TweakDefinition Tweak(string id, params TweakAction[] actions) => new()
    {
        Id = id,
        Category = "Test",
        Hidden = true,
        Impact = new ImpactInfo { Gaming = 0, Basis = "situational", Effect = ["none"] },
        Actions = [.. actions],
    };

    private static RegistryAction Reg(string path, string name, object value, string kind = "dword", string? removeKeyOnUndo = null) => new()
    {
        Hive = Hive.Machine, Path = path, Name = name, Kind = kind, Value = JsonSerializer.SerializeToElement(value), RemoveKeyOnUndo = removeKeyOnUndo,
    };

    [Fact]
    public void ChangeOfARemovedTweakCanStillBeUndone()
    {
        using var fx = new EngineFixture();
        const string path = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile";
        RegistryValue.Write(fx.Registry, Hive.Machine, path, "SystemResponsiveness", "dword", "10");
        // Backup as an earlier version wrote it for a catalog tweak that no longer exists (no stored definition).
        var action = Reg(path, "SystemResponsiveness", 10);
        fx.Store.Save(new TweakBackup
        {
            TweakId = "latency.mmcss",
            Entries =
            [
                new BackupEntry
                {
                    TargetKey = action.TargetKey, Description = "SystemResponsiveness", Original = new StoredValue(true, "dword", "20"),
                    Applied = new StoredValue(true, "dword", "10"), Action = JsonSerializer.Serialize<TweakAction>(action, TweakCatalog.JsonOptions),
                },
            ],
        });

        var t = fx.Engine.Resolve("latency.mmcss");
        Assert.NotNull(t);
        Assert.Equal("retired", t!.DocId);
        Assert.Single(fx.Engine.RevertAll());
        Assert.Equal("20", RegistryValue.Read(fx.Registry, Hive.Machine, path, "SystemResponsiveness").Data);
        Assert.Null(fx.Store.Get("latency.mmcss"));
    }

    /// <summary>
    /// The data folder is shared by every Windows account. A backup of one account's own settings is kept for that
    /// account; another account neither sees it nor takes its originals, while backups of PC-wide settings are shared.
    /// </summary>
    [Fact]
    public void BackupsOfAnAccountsOwnSettingsStayWithThatAccount()
    {
        var root = TestFolders.Create("peruser");
        try
        {
            var alice = new BackupStore(root, secure: false, userSid: "S-1-5-21-1-1-1-1001");
            var bob = new BackupStore(root, secure: false, userSid: "S-1-5-21-1-1-1-1002");
            BackupEntry Entry(Hive hive) => new()
            {
                TargetKey = new RegistryAction { Hive = hive, Path = @"Software\Test", Name = "V" }.TargetKey,
                Description = "V", Original = new StoredValue(true, "dword", "1"), Applied = new StoredValue(true, "dword", "0"),
            };
            alice.Save(new TweakBackup { TweakId = "personalize.darkMode", Entries = [Entry(Hive.User)] });
            alice.Save(new TweakBackup { TweakId = "power.hibernateOff", Entries = [Entry(Hive.Machine)] });

            Assert.Equal("S-1-5-21-1-1-1-1001", alice.Get("personalize.darkMode")!.Owner);
            Assert.Null(bob.Get("personalize.darkMode"));
            Assert.Equal(["power.hibernateOff"], bob.All().Select(b => b.TweakId));
            Assert.Equal(2, alice.All().Count);

            // Bob applies the same tweak: his own backup, Alice's stays as it was.
            bob.Save(new TweakBackup { TweakId = "personalize.darkMode", Entries = [Entry(Hive.User)] });
            Assert.Equal("S-1-5-21-1-1-1-1002", bob.Get("personalize.darkMode")!.Owner);
            Assert.Equal("S-1-5-21-1-1-1-1001", alice.Get("personalize.darkMode")!.Owner);
            bob.Archive("personalize.darkMode");
            Assert.Null(bob.Get("personalize.darkMode"));
            Assert.NotNull(alice.Get("personalize.darkMode"));

            Assert.True(BackupStore.IsPerUser(Entry(Hive.User).TargetKey));
            Assert.True(BackupStore.IsPerUser("spi:stickykeys:hotkey"));
            Assert.False(BackupStore.IsPerUser(Entry(Hive.Machine).TargetKey));
        }
        finally
        {
            TestFolders.Delete(root);
        }
    }

    [Fact]
    public async Task PowerSettingKeepsTheOriginalOfEveryPlanItChanged()
    {
        using var fx = new EngineFixture();
        var setting = new PowerSettingAction { Subgroup = "SUB_PROCESSOR", Setting = "PERFBOOSTMODE", Ac = 2 };
        var t = Tweak("test.boost", setting);
        var sub = PowerAliases.Resolve("SUB_PROCESSOR");
        var key = PowerAliases.Resolve("PERFBOOSTMODE");
        fx.Power.Ac[(FakePower.Balanced, sub, key)] = 0;
        fx.Power.Ac[(FakePower.High, sub, key)] = 1;

        await fx.Engine.ApplyAsync(t, Facts, new HashSet<string>(), Options); // on Balanced
        fx.Power.Active = FakePower.High;
        // Another plan is active: the tweak is simply not applied there, not "reset by Windows".
        Assert.Equal(TweakState.NotApplied, fx.Engine.DetectState(t, Facts));
        await fx.Engine.ApplyAsync(t, Facts, new HashSet<string>(), Options); // on High performance

        Assert.Equal(2, fx.Store.Get(t.Id)!.Entries.Count);
        Assert.True(fx.Engine.Revert(t).Success);
        Assert.Equal(0u, fx.Power.Ac[(FakePower.Balanced, sub, key)]);
        Assert.Equal(1u, fx.Power.Ac[(FakePower.High, sub, key)]);
    }

    [Fact]
    public async Task CreatedPlanIsRemovedEvenIfTheUserSwitchedPlans()
    {
        using var fx = new EngineFixture();
        var t = Tweak("test.plan", new PowerSchemeAction { DuplicateFrom = "highPerformance", Name = "PCOptimizer Test" });
        await fx.Engine.ApplyAsync(t, Facts, new HashSet<string>(), Options);
        Assert.Contains("PCOptimizer Test", fx.Power.SchemeNames.Values);

        fx.Power.Active = FakePower.High; // the user picked another plan
        Assert.True(fx.Engine.Revert(t).Success);
        Assert.Equal(FakePower.High, fx.Power.Active); // the user's choice stays
        Assert.DoesNotContain("PCOptimizer Test", fx.Power.SchemeNames.Values);
    }

    [Fact]
    public async Task CreatedPlanIsRemovedWhenThePreviousPlanIsGone()
    {
        using var fx = new EngineFixture();
        var custom = Guid.NewGuid();
        fx.Power.SchemeNames[custom] = "My plan";
        fx.Power.Active = custom;
        var t = Tweak("test.plan", new PowerSchemeAction { DuplicateFrom = "highPerformance", Name = "PCOptimizer Test" });
        await fx.Engine.ApplyAsync(t, Facts, new HashSet<string>(), Options);
        fx.Power.SchemeNames.Remove(custom);

        Assert.True(fx.Engine.Revert(t).Success);
        Assert.Equal(FakePower.Balanced, fx.Power.Active);
        Assert.DoesNotContain("PCOptimizer Test", fx.Power.SchemeNames.Values);
    }

    [Fact]
    public async Task UndoRemovesOnlyKeysThatAreEmptyAfterwards()
    {
        using var fx = new EngineFixture();
        const string root = @"Software\Classes\CLSID\{test}";
        const string path = root + @"\InprocServer32";
        var t = Tweak("test.keys", Reg(path, "", "", "string", root));

        // Keys did not exist: undo removes both.
        await fx.Engine.ApplyAsync(t, Facts, new HashSet<string>(), Options);
        Assert.True(fx.Engine.Revert(t).Success);
        using (var k = fx.Registry.Open(Hive.Machine, root, writable: false)) Assert.Null(k);

        // The outer key existed with a value of its own: it stays, only the created subkey goes.
        RegistryValue.Write(fx.Registry, Hive.Machine, root, "Keep", "string", "x");
        await fx.Engine.ApplyAsync(t, Facts, new HashSet<string>(), Options);
        Assert.True(fx.Engine.Revert(t).Success);
        Assert.Equal("x", RegistryValue.Read(fx.Registry, Hive.Machine, root, "Keep").Data);
        using (var k = fx.Registry.Open(Hive.Machine, path, writable: false)) Assert.Null(k);

        // The inner key existed with another value: nothing is removed.
        RegistryValue.Write(fx.Registry, Hive.Machine, path, "ThreadingModel", "string", "Both");
        await fx.Engine.ApplyAsync(t, Facts, new HashSet<string>(), Options);
        Assert.True(fx.Engine.Revert(t).Success);
        Assert.Equal("Both", RegistryValue.Read(fx.Registry, Hive.Machine, path, "ThreadingModel").Data);
        Assert.False(RegistryValue.Read(fx.Registry, Hive.Machine, path, "").Existed);
    }

    [Fact]
    public async Task NicKeywordStoredAsDwordStaysADword()
    {
        using var fx = new EngineFixture();
        var key = $@"{NicAdapters.ClassPath}\0001";
        RegistryValue.Write(fx.Registry, Hive.Machine, key, "NetCfgInstanceId", "string", "{11111111-1111-1111-1111-111111111111}");
        RegistryValue.Write(fx.Registry, Hive.Machine, key, "DeviceInstanceID", "string", @"PCI\VEN_8086&DEV_15BC\3");
        RegistryValue.Write(fx.Registry, Hive.Machine, key, "Characteristics", "dword", "132");
        RegistryValue.Write(fx.Registry, Hive.Machine, $@"{key}\Ndi\params\*EEE\enum", "0", "string", "Disabled");
        RegistryValue.Write(fx.Registry, Hive.Machine, $@"{key}\Ndi\params\*EEE\enum", "1", "string", "Enabled");
        RegistryValue.Write(fx.Registry, Hive.Machine, key, "*EEE", "dword", "1");
        var t = Tweak("test.eee", new NicPropertyAction { Properties = new(StringComparer.OrdinalIgnoreCase) { ["*EEE"] = "0" } });

        await fx.Engine.ApplyAsync(t, Facts, new HashSet<string>(), Options);
        Assert.Equal(new StoredValue(true, "dword", "0"), RegistryValue.Read(fx.Registry, Hive.Machine, key, "*EEE"));
        Assert.True(fx.Engine.Revert(t).Success);
        Assert.Equal(new StoredValue(true, "dword", "1"), RegistryValue.Read(fx.Registry, Hive.Machine, key, "*EEE"));
    }

    [Fact]
    public async Task VisualEffectsChangeOnlyTheirOwnBitsOfUserPreferencesMask()
    {
        using var fx = new EngineFixture();
        const string path = @"Control Panel\Desktop";
        // Windows default plus "activate a window by hovering" (bit 0) and "underline access keys" (bit 5).
        RegistryValue.Write(fx.Registry, Hive.User, path, "UserPreferencesMask", "binary", "BF1E078012000000");
        var t = TweakCatalog.Current.Get("visual.bestPerformance")!;
        var mask = t.Actions.OfType<RegistryBinaryBitsAction>().Single();
        var only = Tweak("test.mask", mask);

        await fx.Engine.ApplyAsync(only, Facts, new HashSet<string>(), Options);
        Assert.Equal("B112038010000000", RegistryValue.Read(fx.Registry, Hive.User, path, "UserPreferencesMask").Data); // bits 0 and 5 kept

        // The user turns off "underline access keys" meanwhile: not a reset by Windows, and undo keeps the user's choice.
        RegistryValue.Write(fx.Registry, Hive.User, path, "UserPreferencesMask", "binary", "9112038010000000");
        Assert.Equal(TweakState.Applied, fx.Engine.DetectState(only, Facts));
        Assert.True(fx.Engine.Revert(only).Success);
        Assert.Equal("9F1E078012000000", RegistryValue.Read(fx.Registry, Hive.User, path, "UserPreferencesMask").Data);
    }

    [Fact]
    public void EthernetMeansInterfaceType6()
    {
        using var fx = new EngineFixture();
        void Add(string sub, string guid, int ifType)
        {
            var key = $@"{NicAdapters.ClassPath}\{sub}";
            RegistryValue.Write(fx.Registry, Hive.Machine, key, "NetCfgInstanceId", "string", guid);
            RegistryValue.Write(fx.Registry, Hive.Machine, key, "DeviceInstanceID", "string", @"PCI\VEN_1234&DEV_" + sub);
            RegistryValue.Write(fx.Registry, Hive.Machine, key, "Characteristics", "dword", "132");
            RegistryValue.Write(fx.Registry, Hive.Machine, key, "*IfType", "dword", ifType.ToString());
            RegistryValue.Write(fx.Registry, Hive.Machine, $@"{key}\Ndi\params\*EEE\enum", "0", "string", "Disabled");
        }
        Add("0001", "{11111111-1111-1111-1111-111111111111}", 6);   // Ethernet
        Add("0002", "{22222222-2222-2222-2222-222222222222}", 243); // mobile broadband modem
        Add("0003", "{33333333-3333-3333-3333-333333333333}", 71);  // Wi-Fi
        var t = Tweak("test.eth", new NicPropertyAction { Properties = new(StringComparer.OrdinalIgnoreCase) { ["*EEE"] = "0" }, Media = "ethernet" });

        var expanded = fx.Engine.Expand(t).Cast<NicPropertyAction>().ToList();
        Assert.Equal(["{11111111-1111-1111-1111-111111111111}"], expanded.Select(a => a.Adapter!.InterfaceGuid));
    }

    [Fact]
    public async Task UndoOfAServiceThatWasUninstalledSucceeds()
    {
        using var fx = new EngineFixture();
        fx.Services.Start["OldSvc"] = ServiceStart.Automatic;
        var t = Tweak("test.svc", new ServiceAction { Name = "OldSvc", StartType = ServiceStart.Manual });
        await fx.Engine.ApplyAsync(t, Facts, new HashSet<string>(), Options);
        fx.Services.Start.Remove("OldSvc");

        Assert.True(fx.Engine.Revert(t).Success);
        Assert.Null(fx.Store.Get(t.Id));
    }
}

/// <summary>Rules every catalog entry has to follow.</summary>
public class CatalogRuleTests
{
    [Fact]
    public void ExpertAndBootCriticalTweaksArePreviewsAndPreviewsAreNeverBatched()
    {
        foreach (var t in TweakCatalog.Current.Tweaks.Where(t => !t.Hidden))
        {
            if (t.EffectiveRisk == Risk.Expert || t.IsBootCritical) Assert.True(t.Preview, $"{t.Id} is Expert or boot-critical but not marked preview");
            if (t.Preview) Assert.False(t.IsBatchSafe, t.Id);
        }
    }

    [Fact]
    public void NoTweakMixesPlanSwitchesAndPlanSettings()
    {
        // Settings are written to the plan that is active when the tweak is expanded, before a plan switch in the
        // same tweak would run.
        foreach (var t in TweakCatalog.Current.Tweaks)
            Assert.False(t.Actions.Any(a => a is PowerSchemeAction) && t.Actions.Any(a => a is PowerSettingAction), t.Id);
    }

    [Fact]
    public void ConsumerFeaturesPolicyOnlyAppliesWhereWindowsHonorsIt()
    {
        var t = TweakCatalog.Current.Get("privacy.consumerFeaturesOff")!;
        Assert.False(TweakEngine.AppliesTo(t, new Facts().Set("os.build", 26300).Set("os.edition", "Professional")));
        Assert.False(TweakEngine.AppliesTo(t, new Facts().Set("os.build", 26300).Set("os.edition", "Core")));
        Assert.True(TweakEngine.AppliesTo(t, new Facts().Set("os.build", 26300).Set("os.edition", "Enterprise")));
    }

    [Theory]
    [InlineData("updates.driversExcluded", @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "ExcludeWUDriversInQualityUpdate")]
    [InlineData("privacy.deviceMetadataOff", @"SOFTWARE\Policies\Microsoft\Windows\Device Metadata", "PreventDeviceMetadataFromNetwork")]
    [InlineData("system.registryBackup", @"SYSTEM\CurrentControlSet\Control\Session Manager\Configuration Manager", "EnablePeriodicBackup")]
    public async Task NewPolicyTweaksWriteOneValueAndUndoRemovesIt(string id, string path, string name)
    {
        using var fx = new EngineFixture();
        var t = TweakCatalog.Current.Get(id)!;
        var facts = new Facts().Set("os.build", 26300).Set("elevated", true).Set("os.edition", "Professional");
        var options = new ApplyOptions { ExpertMode = true, ContinueWithoutRestorePoint = true };

        Assert.Equal(ApplyOutcome.Applied, (await fx.Engine.ApplyAsync(t, facts, new HashSet<string>(), options)).Outcome);
        Assert.Equal("1", RegistryValue.Read(fx.Registry, Hive.Machine, path, name).Data);
        Assert.True(fx.Engine.Revert(t).Success);
        Assert.False(RegistryValue.Read(fx.Registry, Hive.Machine, path, name).Existed);
    }

    [Theory]
    [InlineData("updates.driversExcluded")]
    [InlineData("privacy.deviceMetadataOff")]
    public void PoliciesMicrosoftListsForProAndUpAreNotOfferedOnHome(string id)
    {
        var t = TweakCatalog.Current.Get(id)!;
        Assert.False(TweakEngine.AppliesTo(t, new Facts().Set("os.build", 26300).Set("os.edition", "Core")));
        Assert.True(TweakEngine.AppliesTo(t, new Facts().Set("os.build", 26300).Set("os.edition", "Professional")));
    }

    [Fact]
    public void VbsTweakShowsNoGainWhenVbsIsAlreadyOff()
    {
        var t = TweakCatalog.Current.Get("security.vbsOff")!;
        Assert.Equal(0, TweakEngine.ImpactFor(t, new Facts().Set("vbs.running", false)).Impact);
        Assert.True(TweakEngine.ImpactFor(t, new Facts().Set("vbs.running", true)).Impact > 0);
    }
}
