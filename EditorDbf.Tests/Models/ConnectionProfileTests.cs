using System;
using System.Collections.Generic;
using EditorDbf.App.Models;
using FluentAssertions;
using Xunit;

namespace EditorDbf.Tests.Models;

public class ConnectionProfileTests
{
    [Fact]
    public void EffectiveName_CustomNameNull_ReturnsName()
    {
        var profile = new ConnectionProfile { Name = "Folder", CustomName = null };
        profile.EffectiveName.Should().Be("Folder");
    }

    [Fact]
    public void EffectiveName_CustomNameWhitespace_ReturnsName()
    {
        var profile = new ConnectionProfile { Name = "Folder", CustomName = "   " };
        profile.EffectiveName.Should().Be("Folder");
    }

    [Fact]
    public void EffectiveName_CustomNameValue_ReturnsCustomName()
    {
        var profile = new ConnectionProfile { Name = "Folder", CustomName = "MyName" };
        profile.EffectiveName.Should().Be("MyName");
    }

    [Fact]
    public void DisplayName_ConcatenatesEffectiveNameAndPath()
    {
        var profile = new ConnectionProfile { Name = "Folder", FolderPath = "/path/to/folder" };
        profile.DisplayName.Should().Be("Folder (/path/to/folder)");
    }

    [Fact]
    public void DisplayName_UsesCustomName_WhenSet()
    {
        var profile = new ConnectionProfile
        {
            Name = "Folder",
            CustomName = "Custom",
            FolderPath = "/path/to/folder"
        };
        profile.DisplayName.Should().Be("Custom (/path/to/folder)");
    }

    [Fact]
    public void CustomName_ChangeRaisesThreePropertyChangedEvents()
    {
        var profile = new ConnectionProfile { Name = "Folder" };
        var events = new List<string>();

        profile.PropertyChanged += (s, e) => events.Add(e.PropertyName ?? "");

        profile.CustomName = "NewName";

        events.Should().Contain(nameof(ConnectionProfile.CustomName));
        events.Should().Contain(nameof(ConnectionProfile.EffectiveName));
        events.Should().Contain(nameof(ConnectionProfile.DisplayName));
    }

    [Fact]
    public void Name_ChangeRaisesPropertyChanged()
    {
        var profile = new ConnectionProfile();
        var eventRaised = false;

        profile.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ConnectionProfile.Name))
                eventRaised = true;
        };

        profile.Name = "NewName";

        eventRaised.Should().BeTrue();
    }

    [Fact]
    public void Id_GeneratedAsNewGuid()
    {
        var profile1 = new ConnectionProfile();
        var profile2 = new ConnectionProfile();

        profile1.Id.Should().NotBeEmpty();
        profile2.Id.Should().NotBeEmpty();
        profile1.Id.Should().NotBe(profile2.Id);
    }

    [Fact]
    public void Exists_CanBeSet()
    {
        var profile = new ConnectionProfile { Exists = true };
        profile.Exists.Should().BeTrue();

        profile.Exists = false;
        profile.Exists.Should().BeFalse();
    }
}
