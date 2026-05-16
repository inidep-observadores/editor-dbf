using System.ComponentModel;
using EditorDbf.App.Infrastructure;
using FluentAssertions;
using Xunit;

namespace EditorDbf.Tests.Infrastructure;

public class ObservableObjectTests
{
    private class TestObservableObject : ObservableObject
    {
        public string _name = "";
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public int _age;
        public int Age
        {
            get => _age;
            set => SetProperty(ref _age, value);
        }

        public bool SetPropertyPublic<T>(ref T field, T value, string propertyName)
        {
            return SetProperty(ref field, value, propertyName);
        }

        public void OnPropertyChangedPublic(string propertyName)
        {
            OnPropertyChanged(propertyName);
        }
    }

    [Fact]
    public void SetProperty_SameValue_ReturnsFalseNoEvent()
    {
        var obj = new TestObservableObject { Name = "Test" };
        var eventRaised = false;

        obj.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(TestObservableObject.Name))
                eventRaised = true;
        };

        var result = obj.SetPropertyPublic(ref obj._name, "Test", nameof(TestObservableObject.Name));

        result.Should().BeFalse();
        eventRaised.Should().BeFalse();
    }

    [Fact]
    public void SetProperty_DifferentValue_ReturnsTrueAndRaisesEvent()
    {
        var obj = new TestObservableObject { Name = "Old" };
        var eventRaised = false;
        string? eventPropertyName = null;

        obj.PropertyChanged += (s, e) =>
        {
            eventRaised = true;
            eventPropertyName = e.PropertyName;
        };

        obj.Name = "New";

        obj.Name.Should().Be("New");
        eventRaised.Should().BeTrue();
        eventPropertyName.Should().Be(nameof(TestObservableObject.Name));
    }

    [Fact]
    public void OnPropertyChanged_RaisesEventWithCorrectName()
    {
        var obj = new TestObservableObject();
        string? eventPropertyName = null;

        obj.PropertyChanged += (s, e) => eventPropertyName = e.PropertyName;

        obj.OnPropertyChangedPublic(nameof(TestObservableObject.Name));

        eventPropertyName.Should().Be(nameof(TestObservableObject.Name));
    }

    [Fact]
    public void SetProperty_MultipleProperties_OnlyTargetPropertyRaisesEvent()
    {
        var obj = new TestObservableObject();
        var nameEventCount = 0;
        var ageEventCount = 0;

        obj.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(TestObservableObject.Name)) nameEventCount++;
            if (e.PropertyName == nameof(TestObservableObject.Age)) ageEventCount++;
        };

        obj.Name = "Test";
        obj.Age = 25;

        nameEventCount.Should().Be(1);
        ageEventCount.Should().Be(1);
    }

    [Fact]
    public void SetProperty_ValueTypes_WorksCorrectly()
    {
        var obj = new TestObservableObject();
        var eventRaised = false;

        obj.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(TestObservableObject.Age))
                eventRaised = true;
        };

        obj.Age = 30;

        obj.Age.Should().Be(30);
        eventRaised.Should().BeTrue();
    }
}
