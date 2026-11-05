using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;
using Xunit;

namespace Trackwise.Application.Tests.IdentityAccess;

/// <summary>A tenant created for PH or EU starts on that region's currency, units and clock, not the US defaults.</summary>
public class TenantSettingsTests
{
    [Fact]
    public void ApplyRegionDefaults_Ph_PesoMetricAndManilaTime()
    {
        var settings = new TenantSettings();

        settings.ApplyRegionDefaults(Region.PH);

        Assert.Equal(Region.PH, settings.Region);
        Assert.Equal(CurrencyCode.PHP, settings.Currency);
        Assert.Equal(DistanceUnit.Kilometers, settings.DistanceUnit);
        Assert.Equal(WeightUnit.Kilograms, settings.WeightUnit);
        Assert.Equal(VolumeUnit.Liters, settings.VolumeUnit);
        Assert.Equal(TemperatureUnit.Celsius, settings.TemperatureUnit);
        Assert.Equal("Asia/Manila", settings.Timezone);
    }

    [Fact]
    public void ApplyRegionDefaults_Eu_EuroAndEuropeanDates()
    {
        var settings = new TenantSettings();

        settings.ApplyRegionDefaults(Region.EU);

        Assert.Equal(CurrencyCode.EUR, settings.Currency);
        Assert.Equal(DateFormatType.European, settings.DateFormat);
        Assert.Equal(DistanceUnit.Kilometers, settings.DistanceUnit);
    }

    [Fact]
    public void ApplyRegionDefaults_BackToUs_RestoresImperialUnits()
    {
        var settings = new TenantSettings();
        settings.ApplyRegionDefaults(Region.PH);

        settings.ApplyRegionDefaults(Region.US);

        Assert.Equal(CurrencyCode.USD, settings.Currency);
        Assert.Equal(DistanceUnit.Miles, settings.DistanceUnit);
        Assert.Equal("America/New_York", settings.Timezone);
    }

    [Fact]
    public void ApplyRegionDefaults_OtherSettings_LeftAlone()
    {
        var settings = new TenantSettings { OperatingMode = OperatingMode.SoloOperator, MinBrokerCreditScore = 70 };

        settings.ApplyRegionDefaults(Region.PH);

        Assert.Equal(OperatingMode.SoloOperator, settings.OperatingMode);
        Assert.Equal(70, settings.MinBrokerCreditScore);
    }
}
