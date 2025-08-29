using System.ComponentModel;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Ecommerce.Shipping.Geliver.Types;

public enum ProviderService
{
    [EnumMember(Value="SURAT_STANDART")]
    [Description("Sürat Kargo standart gönderi.")]
    SuratStandart,

    [EnumMember(Value="YURTICI_STANDART")]
    [Description("Yurtiçi Kargo standart gönderi.")]
    YurticiStandart,

    [EnumMember(Value="PTT_STANDART")]
    [Description("PTT Kargo standart gönderi.")]
    PttStandart,

    [EnumMember(Value="PTT_KAPIDA_ODEME")]
    [Description("PTT Kargo kapıda ödeme gönderisi.")]
    PttKapidaOdeme,

    [EnumMember(Value="SENDEO_STANDART")]
    [Description("Sendeo standart gönderi.")]
    SendeoStandart,

    [EnumMember(Value="MNG_STANDART")]
    [Description("MNG Kargo standart gönderi.")]
    MngStandart,

    [EnumMember(Value="HEPSIJET_STANDART")]
    [Description("hepsiJET Kargo standart gönderi.")]
    HepsiJetStandart,

    [EnumMember(Value="KOLAYGELSIN_STANDART")]
    [Description("Kolay Gelsin Kargo standart gönderi.")]
    KolayGelsinStandart,

    [EnumMember(Value="PAKETTAXI_STANDART")]
    [Description("Paket Taxi Kurye standart gönderi.")]
    PaketTaxiStandart,

    [EnumMember(Value="ARAS_STANDART")]
    [Description("Aras Kargo standart gönderi.")]
    ArasStandart,

    [EnumMember(Value = "GELIVER_STANDART")]
    [Description("Test gönderisi oluştururken kullanılır.")]
    GeliverStandart
}