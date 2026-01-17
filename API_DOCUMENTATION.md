# Kantar API Dokümantasyonu

Base URL: `https://your-domain.com/api`

## İçindekiler
- [Kimlik Doğrulama (Authentication)](#kimlik-doğrulama)
- [Kullanıcı İşlemleri](#kullanıcı-işlemleri)
- [Ürün İşlemleri](#ürün-işlemleri)
- [Birim Fiyat İşlemleri](#birim-fiyat-işlemleri)
- [LLM (Yapay Zeka)](#llm-yapay-zeka)
- [SignalR Hub](#signalr-hub)

---

## Kimlik Doğrulama

API, JWT (JSON Web Token) tabanlı kimlik doğrulama kullanır. Token'ı `Authorization` header'ında gönderin:

```
Authorization: Bearer <your-access-token>
```

### Roller
| Rol | Açıklama |
|-----|----------|
| `SuperAdmin` | Tüm yetkilere sahip |
| `Admin` | Ürün ve birim fiyat yönetimi |
| `User` | Sadece görüntüleme |

---

## Kullanıcı İşlemleri

### Giriş Yap
Sisteme giriş yaparak access ve refresh token alın.

```
POST /api/user/login
```

**Request Body:**
```json
{
  "username": "kullanici_adi",
  "password": "sifre123"
}
```

**Response (200):**
```json
{
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIs...",
    "refreshToken": "a1b2c3d4-e5f6-7890..."
  },
  "statusCode": 200,
  "isSuccess": true
}
```

---

### Kayıt Ol
Yeni kullanıcı oluşturun.

```
POST /api/user/register
```

**Request Body:**
```json
{
  "username": "yeni_kullanici",
  "password": "sifre123",
  "email": "kullanici@email.com",
  "role": "User"
}
```

**Response (201):**
```json
{
  "statusCode": 201,
  "isSuccess": true
}
```

> **Not:** Geçerli roller: `SuperAdmin`, `Admin`, `User`

---

### Token Yenile
Süresi dolan access token'ı yenileyin.

```
POST /api/user/refreshtoken
```

**Request Body:**
```json
{
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "refreshToken": "a1b2c3d4-e5f6-7890..."
}
```

**Response (200):**
```json
{
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIs...",
    "refreshToken": "x9y8z7w6-v5u4-3210..."
  },
  "statusCode": 200,
  "isSuccess": true
}
```

---

### Çıkış Yap
Oturumu sonlandırın ve token'ları geçersiz kılın.

```
POST /api/user/logout
```
**Yetki:** `Authorize` (Giriş yapmış kullanıcı)

**Request Body:**
```json
{
  "refreshToken": "a1b2c3d4-e5f6-7890...",
  "userId": "550e8400-e29b-41d4-a716-446655440000"
}
```

> **Not:** `userId` opsiyoneldir, boş bırakılırsa JWT'den alınır.

---

### Şifremi Unuttum
Şifre sıfırlama e-postası gönderin.

```
POST /api/user/forgot-password
```

**Request Body:**
```json
{
  "email": "kullanici@email.com"
}
```

**Response (200):**
```json
{
  "statusCode": 200,
  "isSuccess": true
}
```

---

### Şifre Sıfırla
E-posta ile gelen token kullanarak yeni şifre belirleyin.

```
POST /api/user/reset-password
```

**Request Body:**
```json
{
  "email": "kullanici@email.com",
  "token": "sifre-sifirlama-tokeni",
  "newPassword": "yeni_sifre123"
}
```

---

### Tüm Kullanıcıları Listele
Sistemdeki tüm kullanıcıları sayfalı olarak listeleyin.

```
GET /api/user/all/{pagenumber}/{pagesize}?search=arama
```
**Yetki:** `SuperAdmin`

**Path Parameters:**
| Parametre | Tip | Zorunlu | Açıklama |
|-----------|-----|---------|----------|
| pagenumber | int | Evet | Sayfa numarası |
| pagesize | int | Evet | Sayfa başına kayıt |

**Query Parameters:**
| Parametre | Tip | Zorunlu | Açıklama |
|-----------|-----|---------|----------|
| search | string | Hayır | Kullanıcı adı veya e-posta araması |

---

### Kullanıcı Detayı
ID ile kullanıcı bilgilerini getirin.

```
GET /api/user/{id}
```
**Yetki:** `SuperAdmin`

---

### Kullanıcı Sil
Kullanıcıyı sistemden silin (soft delete).

```
DELETE /api/user/{id}
```
**Yetki:** `SuperAdmin`

---

## Ürün İşlemleri

### Ürün Ekle
Yeni ürün kaydı oluşturun.

```
POST /api/products/add
```
**Yetki:** `SuperAdmin`, `Admin`

**Request Body:**
```json
{
  "weight": 150.5,
  "unitId": "550e8400-e29b-41d4-a716-446655440000",
  "price": 25.50
}
```

| Alan | Tip | Zorunlu | Açıklama |
|------|-----|---------|----------|
| weight | double | Evet | Ürün ağırlığı (kg) |
| unitId | Guid | Evet | Birim fiyat ID'si |
| price | double | Hayır | Manuel fiyat (boş ise birim fiyattan hesaplanır) |

**Response (201):**
```json
{
  "data": {
    "id": "...",
    "weight": 150.5,
    "price": 25.50,
    "totalPrice": 3832.75
  },
  "statusCode": 201,
  "isSuccess": true
}
```

---

### Ürün Sil
Ürünü sistemden silin (soft delete).

```
DELETE /api/products/delete
```
**Yetki:** `SuperAdmin`, `Admin`

**Request Body:**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000"
}
```

---

### Ürün Sevk Et
Ürünü sevkiyat durumuna alın.

```
POST /api/products/ship
```
**Yetki:** `SuperAdmin`, `Admin`

**Request Body:**
```json
{
  "weight": 150.5,
  "unitId": "550e8400-e29b-41d4-a716-446655440000",
  "price": 25.50
}
```

---

### Ürün Listesi
Tüm ürünleri sayfalı ve filtrelenmiş olarak listeleyin.

```
GET /api/products/all/{pagesize}/{pagenumber}
```
**Yetki:** `SuperAdmin`, `Admin`, `User`

**Path Parameters:**
| Parametre | Tip | Zorunlu | Açıklama |
|-----------|-----|---------|----------|
| pagesize | int | Evet | Sayfa başına kayıt |
| pagenumber | int | Evet | Sayfa numarası |

**Query Parameters:**
| Parametre | Tip | Zorunlu | Açıklama |
|-----------|-----|---------|----------|
| startdate | DateTime | Hayır | Başlangıç tarihi |
| enddate | DateTime | Hayır | Bitiş tarihi |
| search | string | Hayır | Ürün adı araması |

**Response (200):**
```json
{
  "data": [
    {
      "id": "...",
      "name": "Elma",
      "status": 1,
      "kilogram": "150.5 kilogram",
      "price": 25.50,
      "totalPrice": 3832.75,
      "createdDate": "2026-01-17"
    }
  ],
  "pagination": {
    "pageNumber": 1,
    "pageSize": 10,
    "totalRecords": 150,
    "totalPages": 15
  },
  "statusCode": 200,
  "isSuccess": true
}
```

---

### Ürün Özeti (Birim Bazlı)
Ürünleri birim bazında gruplandırılmış özet olarak listeleyin.

```
GET /api/products/summary/{pagesize}/{pagenumber}
```
**Yetki:** `SuperAdmin`, `Admin`, `User`

**Query Parameters:**
| Parametre | Tip | Zorunlu | Açıklama |
|-----------|-----|---------|----------|
| startdate | DateTime | Hayır | Başlangıç tarihi |
| enddate | DateTime | Hayır | Bitiş tarihi |
| id | Guid | Hayır | Belirli bir birim ID'si |

**Response (200):**
```json
{
  "data": [
    {
      "name": "Elma",
      "totalWeight": "1500.5 kg",
      "totalPrice": "38327.50 TL"
    }
  ],
  "statusCode": 200,
  "isSuccess": true
}
```

---

### Excel Export İste
Ürün verilerini Excel olarak dışa aktarma isteği gönderin.

```
POST /api/products/export
```

**Request Body:**
```json
{
  "exportType": 1,
  "startDate": "2026-01-01",
  "endDate": "2026-01-31",
  "searchTerm": "elma"
}
```

| Alan | Tip | Değerler | Açıklama |
|------|-----|----------|----------|
| exportType | int | `1` = ProductList, `2` = ProductByUnit | Export tipi |
| startDate | DateTime | - | Başlangıç tarihi (opsiyonel) |
| endDate | DateTime | - | Bitiş tarihi (opsiyonel) |
| searchTerm | string | - | Arama terimi (opsiyonel) |

**Response (202):**
```json
{
  "data": {
    "correlationId": "a1b2c3d4-e5f6-7890...",
    "message": "Excel export request has been queued. You will be notified when it's ready."
  },
  "statusCode": 202,
  "isSuccess": true
}
```

> **Not:** Export işlemi asenkron çalışır. İşlem tamamlandığında SignalR ile bildirim alırsınız.

---

### Excel İndir
Hazırlanan Excel dosyasını indirin.

```
GET /api/products/download/{correlationId}
```
**Yetki:** `SuperAdmin`, `Admin`, `User`

**Path Parameters:**
| Parametre | Tip | Açıklama |
|-----------|-----|----------|
| correlationId | Guid | Export isteğinden dönen correlation ID |

**Response:** Excel dosyası (application/vnd.openxmlformats-officedocument.spreadsheetml.sheet)

> **Not:** Dosyalar S3'te 30 dakika saklanır, sonra otomatik silinir.

---

## Birim Fiyat İşlemleri

### Birim Fiyat Ekle
Yeni birim fiyat tanımı oluşturun.

```
POST /api/unitprice/add
```
**Yetki:** `SuperAdmin`, `Admin`

**Request Body:**
```json
{
  "name": "Elma",
  "price": 25.50
}
```

**Response (201):**
```json
{
  "data": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "name": "Elma",
    "price": 25.50
  },
  "statusCode": 201,
  "isSuccess": true
}
```

---

### Birim Fiyat Listesi
Tüm birim fiyatları listeleyin.

```
GET /api/unitprice/all/{pagesize}/{pagenumber}?search=elma
```
**Yetki:** `SuperAdmin`, `Admin`, `User`

**Query Parameters:**
| Parametre | Tip | Zorunlu | Açıklama |
|-----------|-----|---------|----------|
| search | string | Hayır | Birim adı araması |

---

### Birim Fiyat Güncelle
Mevcut birim fiyatını güncelleyin.

```
PUT /api/unitprice/update
```
**Yetki:** `SuperAdmin`, `Admin`

**Request Body:**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "price": 30.00
}
```

---

### Birim Fiyat Sil
Birim fiyatı silin.

```
DELETE /api/unitprice/delete
```
**Yetki:** `SuperAdmin`, `Admin`

**Request Body:**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000"
}
```

---

## LLM (Yapay Zeka)

### Soru Sor
Yapay zekaya soru sorun.

```
GET /api/llm/ask?prompt=soru
```
**Yetki:** `SuperAdmin`, `Admin`, `User`

**Query Parameters:**
| Parametre | Tip | Zorunlu | Açıklama |
|-----------|-----|---------|----------|
| prompt | string | Evet | Sorulacak soru |

**Örnek:**
```
GET /api/llm/ask?prompt=Bugün hava nasıl?
```

**Response (200):**
```json
{
  "data": {
    "response": "Yapay zeka yanıtı burada..."
  },
  "statusCode": 200,
  "isSuccess": true
}
```

---

## SignalR Hub

### Excel Export Bildirimleri

Excel export işlemi tamamlandığında real-time bildirim almak için SignalR hub'ına bağlanın.

**Hub URL:** `/hubs/excel-export`

**Bağlantı (JavaScript):**
```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/excel-export", {
        accessTokenFactory: () => "your-jwt-token"
    })
    .build();

connection.on("ExcelExportCompleted", (notification) => {
    console.log(notification);
    // {
    //   correlationId: "...",
    //   fileName: "UrunListesi_01_01_2026_to_31_01_2026.xlsx",
    //   downloadUrl: "https://s3-presigned-url...",
    //   s3Key: "excel/userId/correlationId_filename.xlsx",
    //   isSuccess: true,
    //   errorMessage: null,
    //   completedAt: "2026-01-17T10:30:00Z",
    //   expiresAt: "2026-01-17T11:00:00Z"
    // }
});

connection.start();
```

> **Not:** `downloadUrl` 30 dakika geçerlidir. `expiresAt` alanında son kullanma tarihi belirtilir.

---

## Hata Yanıtları

Tüm endpoint'ler aynı hata formatını kullanır:

```json
{
  "data": null,
  "statusCode": 400,
  "isSuccess": false,
  "errors": ["Hata mesajı burada"]
}
```

### HTTP Durum Kodları

| Kod | Açıklama |
|-----|----------|
| 200 | Başarılı |
| 201 | Oluşturuldu |
| 202 | Kabul edildi (asenkron işlem) |
| 400 | Geçersiz istek |
| 401 | Yetkisiz (token geçersiz/eksik) |
| 403 | Erişim reddedildi (yetki yok) |
| 404 | Bulunamadı |
| 500 | Sunucu hatası |

---

## Health Check

Sistem durumunu kontrol edin.

```
GET /health
```

**Response (200):**
```json
{
  "status": "healthy",
  "timestamp": "2026-01-17T10:30:00Z"
}
```
