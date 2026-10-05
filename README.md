# LuxeCafe — Management & Order Engine (Backend API)

LuxeCafe sisteminin sipariş yaşam döngüsünü, masa durumlarını, adisyon hesaplamalarını ve veri güvenliğini yöneten yüksek performanslı RESTful API servisidir.

## Öne Çıkan Özellikler
- Katmanlı Mimari (Clean / N-Tier Architecture): İş mantığı, veri erişim katmanı ve API uçlarının birbirinden bağımsız, ölçeklenebilir tasarımı.
- Sipariş ve Masa Durum Yönetimi: Masaların anlık sipariş durumlarının veri tabanı üzerinde tutarlı biçimde yönetilmesi.
- Veri Tabanı ve Migrasyonlar: PostgreSQL üzerinde Entity Framework Core Code-First yaklaşımı ile optimize edilmiş veri tabanı şeması.
- Güvenlik ve Doğrulama: JWT tabanlı kimlik doğrulama, rol bazlı yetkilendirme ve FluentValidation ile güvenli girdi denetimi.

## Teknolojiler
- .NET / ASP.NET Core Web API
- C#
- Entity Framework Core
- PostgreSQL
- Docker
- Swagger / OpenAPI
