using System.ComponentModel;

namespace Ecommerce.Shipping.Geliver.Types.Network;


public enum ErrorCode
{
    [Description("Gönderi parse edilemedi. İsteğin yanlış olduğunu belirtir.")]
    E1054,

    [Description("Organizasyon bulunamadı")]
    E1053,
    [Description("Alıcı adresi oluşturulamadı")]
    E1056,

    [Description("Sipariş oluşturulamadı")]
    E1057,

    [Description("Gönderi kaydedilemedi")]
    E1058,

    [Description("Alıcı adresi boş")]
    E1129,

    [Description("Alıcı şehri boş")]
    E1130,

    [Description("Alıcı ülkesi boş")]
    E1131,

    [Description("Alıcı ilçesi boş")]
    E1132,

    [Description("Alıcı ismi boş")]
    E1133,

    [Description("Alıcı telefon numarası boş")]
    E1134,

    [Description("Alıcı e-posta adresi boş")]
    E1136,
    [Description("Hatalı istek.")]
    E1085,

    [Description("Yetkiniz yok")]
    E1055,

    [Description("Teklif bulunamadı")]
    E1064,

    [Description("Gönderi bulunamadı")]
    E1051,

    [Description("Bu gönderi için daha önce bir teklif kabul edilmiş")]
    E1065,

    [Description("Organizasyonun yeterli bakiyesi yok")]
    E1066,

    [Description("Ürün bilgisi alınamadı")]
    E1067,

    [Description("Bakiye güncelleme hatası")]
    E1074,

    [Description("Kredi kartı ödemesi alınamadı")]
    E1115,

    [Description("Gönderici adresi alınamadı")]
    E1068,

    [Description("Gönderi barkodu oluşturulamadı")]
    E1071,

    [Description("Kargo etiketi oluşturulamadı")]
    E1072,

    [Description("Gönderi güncellenemedi")]
    E1073,

    [Description("İşlem oluşturulamadı")]
    E1086,
}